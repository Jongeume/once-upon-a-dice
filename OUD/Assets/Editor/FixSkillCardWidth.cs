// FixSkillCardWidth.cs
// SkillCardButton 프리팹 너비 수정 + 씬의 AttackColumn/DefenseColumn childForceExpandWidth 확인
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class FixSkillCardWidth
{
    [MenuItem("OUD/Fix SkillCard Width")]
    public static void Execute()
    {
        // ── 1. SkillCardButton 프리팹 수정 ─────────────────────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SkillCardButton.prefab");
        if (prefab == null) { Debug.LogError("[FixSkillCardWidth] SkillCardButton.prefab not found!"); return; }

        var root = prefab.GetComponent<RectTransform>();
        if (root != null)
        {
            Debug.Log($"[FixSkillCardWidth] 이전 sizeDelta={root.sizeDelta}");
            // LayoutElement 추가 또는 기존 것 수정: preferredWidth=120, Height 유지
            var le = prefab.GetComponent<LayoutElement>();
            if (le == null) le = prefab.AddComponent<LayoutElement>();
            le.preferredWidth  = 120f;
            le.preferredHeight = 52f;
            le.flexibleWidth   = 1f;   // 부모 VerticalLayoutGroup의 childForceExpandWidth=true와 호환
            EditorUtility.SetDirty(prefab);
            Debug.Log("[FixSkillCardWidth] SkillCardButton prefab: LayoutElement 설정 완료 (preferredWidth=120, flexibleWidth=1)");
        }

        // 씬 내 TMP 폰트도 함께 확인
        foreach (var tmp in prefab.GetComponentsInChildren<TMP_Text>(true))
        {
            Debug.Log($"[FixSkillCardWidth]  TMP [{tmp.name}] fontSize={tmp.fontSize}");
        }

        PrefabUtility.SavePrefabAsset(prefab);
        AssetDatabase.SaveAssets();

        // ── 2. 씬 내 AttackColumn / DefenseColumn childForceExpandWidth 확인 ──
        var scene = SceneManager.GetActiveScene();
        var roots  = scene.GetRootGameObjects();
        int colFixed = 0;
        foreach (var root2 in roots)
        {
            foreach (var vl in root2.GetComponentsInChildren<VerticalLayoutGroup>(true))
            {
                if (vl.name == "AttackColumn" || vl.name == "DefenseColumn")
                {
                    if (!vl.childForceExpandWidth)
                    {
                        vl.childForceExpandWidth = true;
                        EditorUtility.SetDirty(vl);
                        colFixed++;
                        Debug.Log($"[FixSkillCardWidth] {vl.name}: childForceExpandWidth → true");
                    }
                    else
                        Debug.Log($"[FixSkillCardWidth] {vl.name}: childForceExpandWidth 이미 true");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[FixSkillCardWidth] 완료 — 프리팹 수정, 컬럼 {colFixed}개 수정");
    }
}
