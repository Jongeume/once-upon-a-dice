// ReducePrefabFonts.cs — Assets/Prefabs 내 TMP 폰트를 현재 크기의 2/3로 축소
// ScalePrefabFonts가 여러 번 실행되어 누적 증가된 크기를 정상화.
using UnityEngine;
using UnityEditor;
using TMPro;

public static class ReducePrefabFonts
{
    [MenuItem("OUD/Reduce Prefab Fonts (x2/3)")]
    public static void Execute()
    {
        int total = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

        foreach (var guid in guids)
        {
            string path   = AssetDatabase.GUIDToAssetPath(guid);
            var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modified = false;
            foreach (var tmp in prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                float current = tmp.fontSize;
                // 2/3 축소, 최솟값 10 보장
                float newSz = Mathf.Max(10f, Mathf.Round(current * 2f / 3f));
                if (Mathf.Abs(newSz - current) > 0.1f)
                {
                    Debug.Log($"[ReducePrefabFonts] {path}/{tmp.name}: {current} → {newSz}");
                    tmp.fontSize = newSz;
                    EditorUtility.SetDirty(tmp);
                    modified = true;
                    total++;
                }
            }

            if (modified)
                PrefabUtility.SavePrefabAsset(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ReducePrefabFonts] 완료 — {total}개 TMP 폰트 크기 2/3 축소");
    }
}
