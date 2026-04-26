// ScaleFontSizes.cs — 씬 내 모든 TMP 폰트 크기를 비율로 일괄 조정
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

public static class ScaleFontSizes
{
    // 크기 구간별 배율 (작을수록 더 크게 키움)
    // 기준: 해상도 1920×1080 기준 최소 가독 크기 = 20pt
    [MenuItem("OUD/Scale Font Sizes x1.5")]
    public static void Execute()
    {
        int count = 0;
        var texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var t in texts)
        {
            if (EditorUtility.IsPersistent(t)) continue;

            float cur = t.fontSize;
            float next;

            if (cur <= 14)       next = Mathf.Round(cur * 1.6f); // 11→17, 12→19, 13→20, 14→22
            else if (cur <= 18)  next = Mathf.Round(cur * 1.5f); // 15→22, 16→24, 17→25, 18→27
            else if (cur <= 28)  next = Mathf.Round(cur * 1.4f); // 22→30, 24→33, 28→39
            else                 next = Mathf.Round(cur * 1.2f); // 34→40, 60→72

            t.fontSize = next;
            EditorUtility.SetDirty(t);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[ScaleFontSizes] " + count + "개 TMP 폰트 크기 조정 완료");
    }
}
