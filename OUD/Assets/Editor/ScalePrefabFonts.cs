// ScalePrefabFonts.cs — Assets/Prefabs 안 TMP 폰트 크기 스케일
// 씬 오브젝트가 아닌 프리팹에 직접 저장되므로 런타임 생성 시에도 반영됨.
// 주의: 멱등(idempotent) 아님 — 1회만 실행할 것.
using UnityEngine;
using UnityEditor;
using TMPro;

public static class ScalePrefabFonts
{
    [MenuItem("OUD/Scale Prefab Fonts")]
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
                float sz    = tmp.fontSize;
                float newSz = sz <= 14f ? Mathf.Round(sz * 1.6f) :
                              sz <= 18f ? Mathf.Round(sz * 1.5f) :
                              sz <= 28f ? Mathf.Round(sz * 1.4f) :
                                          Mathf.Round(sz * 1.2f);
                if (Mathf.Abs(newSz - sz) > 0.1f)
                {
                    tmp.fontSize = newSz;
                    EditorUtility.SetDirty(tmp);
                    modified = true;
                    total++;
                    Debug.Log($"[ScalePrefabFonts] {path} / {tmp.name}: {sz} → {newSz}");
                }
            }

            if (modified)
                PrefabUtility.SavePrefabAsset(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ScalePrefabFonts] 완료 — {total}개 TMP 폰트 크기 조정");
    }
}
