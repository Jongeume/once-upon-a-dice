// RemoveDuplicatePanels.cs — BattleCanvas 아래 중복 패널 제거
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RemoveDuplicatePanels
{
    [MenuItem("OUD/Remove Duplicate Panels")]
    public static void Execute()
    {
        var canvas = GameObject.Find("BattleCanvas");
        if (canvas == null) { Debug.LogError("BattleCanvas not found"); return; }

        RemoveDuplicates(canvas.transform, "DiceTablePanel");
        RemoveDuplicates(canvas.transform, "OverlayPanel");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[RemoveDuplicatePanels] 완료");
    }

    static void RemoveDuplicates(Transform parent, string childName)
    {
        var found = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName) found.Add(child);
        }

        if (found.Count <= 1)
        {
            Debug.Log("[RemoveDuplicatePanels] " + childName + " 중복 없음");
            return;
        }

        // sibling index가 낮은 것(먼저 생성된 것)을 유지, 나머지 삭제
        for (int i = 1; i < found.Count; i++)
        {
            Debug.Log("[RemoveDuplicatePanels] 삭제: " + childName + " (sibling " + found[i].GetSiblingIndex() + ")");
            Object.DestroyImmediate(found[i].gameObject);
        }
    }
}
