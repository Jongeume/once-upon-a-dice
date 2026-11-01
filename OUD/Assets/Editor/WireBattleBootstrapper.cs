// WireBattleBootstrapper.cs — BattleBootstrapper Inspector 바인딩 자동화
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OUD.Unity;
using OUD.Unity.Adapter;

public static class WireBattleBootstrapper
{
    [MenuItem("OUD/Wire BattleBootstrapper")]
    public static void Execute()
    {
        // BattleBootstrapper GO
        var bbGO = GameObject.Find("BattleBootstrapper");
        if (bbGO == null) { Debug.LogError("BattleBootstrapper GO not found"); return; }

        var bb = bbGO.GetComponent<BattleBootstrapper>();
        if (bb == null) { Debug.LogError("BattleBootstrapper component not found"); return; }

        // BattleUIAdapter (BattleManager에 붙어 있음 - Part4 기준)
        var mgrGO = GameObject.Find("BattleManager");
        if (mgrGO == null) { Debug.LogError("BattleManager not found"); return; }

        var adapter = mgrGO.GetComponent<BattleUIAdapter>();
        if (adapter == null) { Debug.LogError("BattleUIAdapter not found on BattleManager"); return; }

        // ActionButtonA (Roll Dice 버튼)
        var canvasGO = GameObject.Find("BattleCanvas");
        if (canvasGO == null) { Debug.LogError("BattleCanvas not found"); return; }
        var btnT = canvasGO.transform.Find("BattlePanel/ActionButtonA");
        if (btnT == null) { Debug.LogError("BattlePanel/ActionButtonA not found"); return; }
        var btn = btnT.GetComponent<Button>();

        // SerializedObject로 바인딩
        var so = new SerializedObject(bb);
        so.FindProperty("_adapter").objectReferenceValue = adapter;
        so.FindProperty("_rollDiceButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[WireBattleBootstrapper] 바인딩 완료: _adapter=" + adapter.name + ", _rollDiceButton=" + btn.name);
    }
}
