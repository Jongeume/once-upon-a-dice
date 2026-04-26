// FixDuplicateComponents.cs — BattleManager 중복 컴포넌트 정리
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FixDuplicateComponents
{
    [MenuItem("OUD/Fix Duplicate Components")]
    public static void Execute()
    {
        int total = 0;
        total += CleanDuplicates<OUD.Unity.Adapter.BattleUIAdapter>("BattleManager");
        total += CleanDuplicates<OUD.Unity.Battle.View.PlayerView>("PlayerPanel");
        total += CleanDuplicates<OUD.Unity.Battle.View.EnemyView>("EnemyPanel");
        total += CleanDuplicates<OUD.Unity.Battle.View.DiceView>("DiceTablePanel");
        total += CleanDuplicates<OUD.Unity.Battle.View.SlotAssignmentView>("DiceTablePanel");
        total += CleanDuplicates<OUD.Unity.Battle.View.BattleLogView>("OverlayPanel");
        total += CleanDuplicates<OUD.Unity.Battle.View.SkillSlotView>("SlotPanel");
        total += CleanDuplicates<OUD.Unity.Battle.View.TargetSelectionView>("TargetSelectionPanel");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FixDuplicateComponents] 총 " + total + "개 중복 컴포넌트 제거 완료");
    }

    static int CleanDuplicates<T>(string goName) where T : Component
    {
        var go = GameObject.Find(goName);
        if (go == null) return 0;
        var comps = go.GetComponents<T>();
        if (comps.Length <= 1) return 0;

        // 첫 번째(index 0)를 유지, 나머지 삭제
        int removed = 0;
        for (int i = comps.Length - 1; i >= 1; i--)
        {
            Object.DestroyImmediate(comps[i]);
            removed++;
        }
        Debug.Log("[FixDuplicateComponents] " + goName + " / " + typeof(T).Name + " 중복 " + removed + "개 제거");
        return removed;
    }
}
