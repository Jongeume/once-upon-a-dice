// CleanEnemyPanel.cs — EnemyPanel 정적 EnemyEntry 제거 (런타임에 스폰되므로 씬에 불필요)
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CleanEnemyPanel
{
    [MenuItem("OUD/Clean Enemy Panel")]
    public static void Execute()
    {
        var ep = GameObject.Find("EnemyPanel");
        if (ep == null) { Debug.LogError("EnemyPanel not found"); return; }

        int count = 0;
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in ep.transform)
            if (child.name == "EnemyEntry") toDelete.Add(child.gameObject);

        foreach (var go in toDelete)
        {
            Object.DestroyImmediate(go);
            count++;
        }

        EditorUtility.SetDirty(ep);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[CleanEnemyPanel] 정적 EnemyEntry " + count + "개 삭제 완료");
    }
}
