// FixKoreanFont.cs — 씬/프리팹 내 모든 TMP 텍스트를 NotoSansKR로 교체
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

public static class FixKoreanFont
{
    private const string FONT_PATH = "Assets/Fonts/NotoSansKR-Regular SDF.asset";

    [MenuItem("OUD/Fix Korean Font")]
    public static void Execute()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null)
        {
            Debug.LogError("[FixKoreanFont] 폰트 에셋을 찾을 수 없음: " + FONT_PATH);
            return;
        }

        int count = 0;

        // 씬 내 모든 TextMeshProUGUI (비활성 포함)
        var allTMPs = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var tmp in allTMPs)
        {
            // 에셋(프리팹 원본)은 건드리지 않고 씬 인스턴스만 처리
            if (EditorUtility.IsPersistent(tmp)) continue;
            tmp.font = font;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FixKoreanFont] " + count + "개 TMP 컴포넌트 폰트 변경 완료 → NotoSansKR-Regular SDF");
    }
}
