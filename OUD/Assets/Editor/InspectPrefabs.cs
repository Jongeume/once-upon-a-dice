// InspectPrefabs.cs — 프리팹 내부 구조 출력 (1회용 진단 도구)
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class InspectPrefabs
{
    [MenuItem("OUD/Inspect Prefabs")]
    public static void Execute()
    {
        string[] paths = {
            "Assets/Prefabs/SkillCardButton.prefab",
            "Assets/Prefabs/EnemyEntry.prefab"
        };

        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.Log($"NOT FOUND: {path}"); continue; }

            Debug.Log($"===== {path} =====");
            Inspect(prefab.transform, 0);
        }
    }

    static void Inspect(Transform t, int depth)
    {
        var indent = new string(' ', depth * 2);
        var rt = t.GetComponent<RectTransform>();
        var sz = rt != null ? $"size={rt.sizeDelta} anchor=[{rt.anchorMin},{rt.anchorMax}]" : "(no RT)";

        var comps = new System.Text.StringBuilder();
        foreach (var c in t.GetComponents<Component>())
        {
            var name = c.GetType().Name;
            if (name == "RectTransform" || name == "Transform") continue;
            comps.Append($" [{name}");
            if (c is TMP_Text tmp) comps.Append($" fs={tmp.fontSize}");
            if (c is LayoutElement le) comps.Append($" prefW={le.preferredWidth} flexW={le.flexibleWidth}");
            if (c is HorizontalOrVerticalLayoutGroup lg) comps.Append($" expandW={lg.childForceExpandWidth}");
            if (c is ContentSizeFitter csf) comps.Append($" horFit={csf.horizontalFit}");
            comps.Append("]");
        }

        Debug.Log($"{indent}{t.name}  {sz}{comps}");

        foreach (Transform child in t)
            Inspect(child, depth + 1);
    }
}
