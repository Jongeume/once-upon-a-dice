// SetupNodePositions.cs
// 에디터 유틸리티: NodeMapPanel 1920x1080 기준 노드 + 연결선 위치 자동 배치.
// 메뉴: OUD > Setup Node Positions
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SetupNodePositions
{
    // ── 노드 배치 (13개, 9레이어 좌→우, 보스 우측) ──────────────────────────
    // 패널 1920x1080, pivot=center. 내용 영역 ±780(x) ±180(y).
    private static readonly Vector2[] NODE_POS = new Vector2[]
    {
        new Vector2(-780,    0), // Node_0  Start
        new Vector2(-585,  180), // Node_1  upper branch
        new Vector2(-585, -180), // Node_2  lower branch
        new Vector2(-390,  180), // Node_3
        new Vector2(-390, -180), // Node_4
        new Vector2(-195,    0), // Node_5  merge
        new Vector2(   0,  180), // Node_6
        new Vector2(   0, -180), // Node_7
        new Vector2( 195,  180), // Node_8
        new Vector2( 195, -180), // Node_9
        new Vector2( 390,    0), // Node_10 merge
        new Vector2( 585,    0), // Node_11
        new Vector2( 780,    0), // Node_12 Boss
    };

    // ── 연결선 정의 ──────────────────────────────────────────────────────────
    private static readonly (int From, int To)[] CONNECTIONS = new[]
    {
        (0,1),(0,2),(1,3),(2,4),(3,5),(4,5),
        (5,6),(5,7),(6,8),(7,9),(8,10),(9,10),
        (10,11),(11,12)
    };

    [MenuItem("OUD/Setup Node Positions")]
    public static void Execute()
    {
        // 1. 노드 위치 설정
        for (int i = 0; i < NODE_POS.Length; i++)
        {
            GameObject go = GameObject.Find("Node_" + i);
            if (go == null) { Debug.LogWarning("[SetupNodePositions] Node_" + i + " 없음"); continue; }
            RectTransform rt = go.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Setup Node Position");
            rt.anchoredPosition = NODE_POS[i];
            EditorUtility.SetDirty(rt);
        }

        // 2. 연결선 위치 / 회전 / 길이 설정
        foreach (var (from, to) in CONNECTIONS)
        {
            string name = "Line_" + from + "_" + to;
            GameObject lineGo = GameObject.Find(name);
            if (lineGo == null) { Debug.LogWarning("[SetupNodePositions] " + name + " 없음"); continue; }

            RectTransform rt = lineGo.GetComponent<RectTransform>();
            Vector2 posFrom = NODE_POS[from];
            Vector2 posTo   = NODE_POS[to];
            Vector2 dir     = posTo - posFrom;
            float dist      = dir.magnitude;
            float angle     = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float thickness = rt.sizeDelta.y; // 기존 두께 유지

            Undo.RecordObject(rt, "Setup Line Position");
            rt.anchoredPosition = (posFrom + posTo) * 0.5f;
            rt.sizeDelta        = new Vector2(dist, thickness);
            rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
            EditorUtility.SetDirty(rt);
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SetupNodePositions] 완료! 노드 13개 + 연결선 " + CONNECTIONS.Length + "개 위치 설정.");
    }
}
#endif
