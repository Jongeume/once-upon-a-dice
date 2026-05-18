using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using OUD.Unity.Battle.View;

/// <summary>
/// X-크로스 연결선 4개 삭제 및 NodeMapView._connections 14개로 재바인딩.
/// </summary>
public class RemoveXCrossLines
{
    // X-크로스 제거 후 남은 14개 연결 [fromId, toId]
    static readonly int[,] CONN = new int[,]
    {
        { 0,  1}, { 0,  2},    // Col0 → Col1
        { 1,  3}, { 2,  4},    // Col1 → Col2 (직선, 행 유지)
        { 3,  5}, { 4,  5},    // Col2 → Shop
        { 5,  6}, { 5,  7},    // Shop → Col4
        { 6,  8}, { 7,  9},    // Col4 → Col5 (직선, 행 유지)
        { 8, 10}, { 9, 10},    // Col5 → Elite
        {10, 11}, {11, 12},    // Elite → Shop → Boss
    };

    static readonly string[] XCROSS_NAMES = { "Line_1_4", "Line_2_3", "Line_6_9", "Line_7_8" };

    [MenuItem("Tools/Remove XCross Lines")]
    public static void Execute()
    {
        var views = Object.FindObjectsByType<NodeMapView>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (views == null || views.Length == 0)
        { Debug.LogError("[RemoveXCrossLines] NodeMapView not found."); return; }

        var view  = views[0];
        Transform panel = view.transform;

        // ── 1. X-크로스 선 4개 삭제 ─────────────────────────────────────────
        foreach (string name in XCROSS_NAMES)
        {
            var child = panel.Find(name);
            if (child != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                Debug.Log($"[RemoveXCrossLines] 삭제: {name}");
            }
            else
            {
                Debug.LogWarning($"[RemoveXCrossLines] 없음(이미 삭제됨?): {name}");
            }
        }

        // ── 2. _connections 14개로 재바인딩 ──────────────────────────────────
        var so       = new SerializedObject(view);
        var connProp = so.FindProperty("_connections");
        if (connProp == null)
        { Debug.LogError("[RemoveXCrossLines] _connections 프로퍼티 없음."); return; }

        connProp.ClearArray();
        int count = CONN.GetLength(0);
        for (int i = 0; i < count; i++)
        {
            int fromId = CONN[i, 0];
            int toId   = CONN[i, 1];
            string lineName = $"Line_{fromId}_{toId}";

            var lineGO = panel.Find(lineName);
            if (lineGO == null)
            {
                Debug.LogError($"[RemoveXCrossLines] {lineName} not found in panel!");
                continue;
            }

            connProp.InsertArrayElementAtIndex(i);
            var el = connProp.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("FromNodeId").intValue           = fromId;
            el.FindPropertyRelative("ToNodeId").intValue             = toId;
            el.FindPropertyRelative("LineImage").objectReferenceValue = lineGO.GetComponent<Image>();
        }
        so.ApplyModifiedProperties();
        Debug.Log($"[RemoveXCrossLines] _connections 재바인딩 완료 ({count}개)");

        // ── 3. 씬 저장 ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[RemoveXCrossLines] 완료 — X-크로스 선 제거, 씬 저장됨.");
    }
}
