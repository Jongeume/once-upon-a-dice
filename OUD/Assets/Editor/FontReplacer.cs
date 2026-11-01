using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FontReplacer
{
    [MenuItem("OUD/Replace LiberationSans with NotoSansKR")]
    public static void Replace()
    {
        var noto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansKR-Regular SDF.asset");
        if (noto == null) { Debug.LogError("NotoSansKR-Regular SDF.asset not found"); return; }

        int sceneCount = ReplaceInScene(noto);
        int prefabCount = ReplaceInPrefabs(noto);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"[FontReplacer] Done. Scene={sceneCount} Prefabs={prefabCount}");
    }

    static int ReplaceInScene(TMP_FontAsset noto)
    {
        var all = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var t in all)
        {
            if (t.font != null && t.font.name.Contains("LiberationSans"))
            {
                t.font = noto;
                EditorUtility.SetDirty(t);
                count++;
            }
        }
        return count;
    }

    static int ReplaceInPrefabs(TMP_FontAsset noto)
    {
        int count = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var tmps = prefab.GetComponentsInChildren<TMP_Text>(true);
            bool changed = false;
            foreach (var t in tmps)
            {
                if (t.font != null && t.font.name.Contains("LiberationSans"))
                {
                    t.font = noto;
                    EditorUtility.SetDirty(t);
                    changed = true;
                    count++;
                    Debug.Log($"Prefab font replaced: {path} / {t.gameObject.name}");
                }
            }
            if (changed) PrefabUtility.SavePrefabAsset(prefab);
        }
        return count;
    }
}
