using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using OUD.Unity.Battle.View;

/// <summary>
/// 1회성 에디터 스크립트 — BattleScene NodeMapPanel을 13노드/18연결선 구조로 재구성한다.
/// Tools/Setup NodeMap 메뉴 또는 execute_script로 실행.
/// </summary>
public class SetupNodeMap
{
    // 노드 anchoredPosition (NodeMapPanel 700×520 기준, 열 간격 77, 행 ±130)
    static readonly Vector2[] NODE_POS = new Vector2[]
    {
        new Vector2(-315f,    0f),  // 0  Col0  Combat
        new Vector2(-238f,  130f),  // 1  Col1  Combat (상)
        new Vector2(-238f, -130f),  // 2  Col1  Combat (하)
        new Vector2(-161f,  130f),  // 3  Col2  Combat (상)
        new Vector2(-161f, -130f),  // 4  Col2  Combat (하)
        new Vector2( -84f,    0f),  // 5  Col3  Shop
        new Vector2(  -7f,  130f),  // 6  Col4  Combat (상)
        new Vector2(  -7f, -130f),  // 7  Col4  Combat (하)
        new Vector2(  70f,  130f),  // 8  Col5  Combat (상)
        new Vector2(  70f, -130f),  // 9  Col5  Combat (하)
        new Vector2( 147f,    0f),  // 10 Col6  Elite
        new Vector2( 224f,    0f),  // 11 Col7  Shop
        new Vector2( 301f,    0f),  // 12 Col8  Boss
    };

    // 18개 연결 [fromId, toId]
    static readonly int[,] CONN = new int[,]
    {
        { 0,  1}, { 0,  2},          // Col0 → Col1
        { 1,  3}, { 1,  4},          // X-cross upper
        { 2,  3}, { 2,  4},          // X-cross lower
        { 3,  5}, { 4,  5},          // Col2 → Shop
        { 5,  6}, { 5,  7},          // Shop → Col4
        { 6,  8}, { 6,  9},          // X-cross upper
        { 7,  8}, { 7,  9},          // X-cross lower
        { 8, 10}, { 9, 10},          // Col5 → Elite
        {10, 11}, {11, 12},          // Elite → Shop → Boss
    };

    [MenuItem("Tools/Setup NodeMap")]
    public static void Execute()
    {
        // FindObjectsByType로 비활성 오브젝트도 탐색
        var views = Object.FindObjectsByType<NodeMapView>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (views == null || views.Length == 0)
        { Debug.LogError("[SetupNodeMap] NodeMapView not found in scene."); return; }
        var view   = views[0];
        var panelGO = view.gameObject;

        Transform panel = panelGO.transform;

        // ── 1. 기존 Line_ 오브젝트 삭제 ─────────────────────────────────────
        var toDelete = new List<GameObject>();
        for (int i = 0; i < panel.childCount; i++)
        {
            string n = panel.GetChild(i).name;
            if (n.StartsWith("Line_")) toDelete.Add(panel.GetChild(i).gameObject);
        }
        foreach (var go in toDelete)
        {
            Debug.Log($"[SetupNodeMap] 삭제: {go.name}");
            Undo.DestroyObjectImmediate(go);
        }

        // ── 2. Node_8..Node_12가 이미 있으면 삭제 (재실행 안전성) ───────────
        for (int i = 8; i <= 12; i++)
        {
            var existing = panel.Find($"Node_{i}");
            if (existing != null)
            {
                Debug.Log($"[SetupNodeMap] 기존 {existing.name} 삭제");
                Undo.DestroyObjectImmediate(existing.gameObject);
            }
        }

        // ── 3. Node_0..Node_7 위치 갱신 ──────────────────────────────────────
        var nodeGOs = new GameObject[13];
        for (int i = 0; i <= 7; i++)
        {
            var child = panel.Find($"Node_{i}");
            if (child == null) { Debug.LogError($"[SetupNodeMap] Node_{i} not found!"); return; }
            nodeGOs[i] = child.gameObject;
            var rt = child.GetComponent<RectTransform>();
            Undo.RecordObject(rt, $"Move Node_{i}");
            rt.anchoredPosition = NODE_POS[i];
        }

        // ── 4. Node_8..Node_12 생성 (Node_0 복제) ────────────────────────────
        for (int i = 8; i <= 12; i++)
        {
            var newNode = Object.Instantiate(nodeGOs[0], panel);
            newNode.name = $"Node_{i}";
            Undo.RegisterCreatedObjectUndo(newNode, $"Create Node_{i}");
            var rt = newNode.GetComponent<RectTransform>();
            rt.anchoredPosition = NODE_POS[i];
            nodeGOs[i] = newNode;
        }

        // ── 5. 연결선 18개 생성 ───────────────────────────────────────────────
        var lineImages = new Image[CONN.GetLength(0)];
        for (int i = 0; i < CONN.GetLength(0); i++)
        {
            int fromId = CONN[i, 0];
            int toId   = CONN[i, 1];

            Vector2 from  = NODE_POS[fromId];
            Vector2 to    = NODE_POS[toId];
            Vector2 diff  = to - from;
            float   dist  = diff.magnitude;
            float   angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            Vector2 mid   = (from + to) * 0.5f;

            var lineGO = new GameObject($"Line_{fromId}_{toId}");
            Undo.RegisterCreatedObjectUndo(lineGO, $"Create Line_{fromId}_{toId}");
            lineGO.transform.SetParent(panel, false);
            lineGO.transform.SetSiblingIndex(0); // 노드 뒤(배경)에 위치

            var img = lineGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // 기본: 비활성색

            var rt = lineGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(dist, 4f);
            rt.anchoredPosition = mid;
            rt.localRotation    = Quaternion.Euler(0f, 0f, angle);

            lineImages[i] = img;
        }

        // ── 6. NodeMapView SerializedObject 바인딩 ────────────────────────────
        var so = new SerializedObject(view);

        // _nodes
        var nodesProp = so.FindProperty("_nodes");
        if (nodesProp != null)
        {
            nodesProp.ClearArray();
            for (int i = 0; i < 13; i++)
            {
                nodesProp.InsertArrayElementAtIndex(i);
                var el = nodesProp.GetArrayElementAtIndex(i);
                el.objectReferenceValue = nodeGOs[i].GetComponent<NodeView>();
            }
            Debug.Log("[SetupNodeMap] _nodes 바인딩 완료 (13개)");
        }
        else
        {
            Debug.LogWarning("[SetupNodeMap] _nodes 프로퍼티를 찾을 수 없음.");
        }

        // _connections
        var connProp = so.FindProperty("_connections");
        if (connProp != null)
        {
            connProp.ClearArray();
            int count = CONN.GetLength(0);
            for (int i = 0; i < count; i++)
            {
                connProp.InsertArrayElementAtIndex(i);
                var el = connProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("FromNodeId").intValue        = CONN[i, 0];
                el.FindPropertyRelative("ToNodeId").intValue          = CONN[i, 1];
                el.FindPropertyRelative("LineImage").objectReferenceValue = lineImages[i];
            }
            Debug.Log($"[SetupNodeMap] _connections 바인딩 완료 ({count}개)");
        }
        else
        {
            Debug.LogWarning("[SetupNodeMap] _connections 프로퍼티를 찾을 수 없음.");
        }

        so.ApplyModifiedProperties();

        // ── 7. 씬 저장 ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupNodeMap] 완료 — 13노드 + 18연결선 배치/바인딩, 씬 저장됨.");
    }
}
