// BuildUI_Part4.cs  - Component binding + Prefab creation
// OUD/Build UI/Part4 - Bind Components 메뉴 실행
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using OUD.Unity.Battle.View;
using OUD.Unity.Adapter;
using OUD.Unity.Common;

public static class BuildUI_Part4
{
    // ── 공통 헬퍼 ─────────────────────────────────────────────────────────────

    static void Set(Component comp, System.Action<SerializedObject> fn)
    {
        var so = new SerializedObject(comp);
        fn(so);
        so.ApplyModifiedProperties();
    }

    static T Find<T>(Transform root, string path) where T : Component
    {
        var t = root.Find(path);
        return t != null ? t.GetComponent<T>() : null;
    }

    static GameObject FindGO(Transform root, string path)
    {
        var t = root.Find(path);
        return t != null ? t.gameObject : null;
    }

    // ── 메인 ─────────────────────────────────────────────────────────────────

    [MenuItem("OUD/Build UI/Part4 - Bind Components")]
    public static void Execute()
    {
        // 루트 찾기
        var canvas = GameObject.Find("BattleCanvas");
        if (canvas == null) { Debug.LogError("[Part4] BattleCanvas not found. Run Part1 first."); return; }
        var mgr = GameObject.Find("BattleManager");
        if (mgr == null) { Debug.LogError("[Part4] BattleManager not found. Run Part2 first."); return; }

        var ct = canvas.transform;
        var bp = ct.Find("BattlePanel").gameObject;
        var dp = ct.Find("DiceTablePanel").gameObject;
        var op = ct.Find("OverlayPanel").gameObject;

        var bpt = bp.transform;
        var dpt = dp.transform;
        var opt = op.transform;

        var enemyPanel  = FindGO(bpt, "EnemyPanel");
        var playerPanel = FindGO(bpt, "PlayerPanel");
        var slotPanel   = FindGO(bpt, "SlotPanel");
        var targetPanel = FindGO(bpt, "TargetSelectionPanel");

        var ept = enemyPanel.transform;
        var ppt = playerPanel.transform;

        var actionBtns   = FindGO(dpt, "ActionButtons");
        var abt          = actionBtns.transform;
        var rerollBtnGO  = FindGO(abt, "RerollButton");
        var rerollTxtGO  = FindGO(rerollBtnGO.transform, "Text");
        var useSkillBtnGO = FindGO(abt, "UseSkillButton");

        var skillColumns = dp.transform.Find("SkillListPanel/SkillColumns");
        var attackCol    = skillColumns.Find("AttackColumn");
        var defenseCol   = skillColumns.Find("DefenseColumn");

        var slotPanelB   = FindGO(dpt, "SlotPanel_B");
        var winScreen    = FindGO(opt, "WinScreen");
        var loseScreen   = FindGO(opt, "LoseScreen");
        var popupPool    = FindGO(opt, "DamagePopupPool");
        var endTurnBtn   = FindGO(targetPanel.transform, "EndTurnButton");

        // Prefabs 폴더 생성
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // ── 1. PlayerView ─────────────────────────────────────────────────

        var playerView = playerPanel.AddComponent<PlayerView>();
        Set(playerView, so => {
            so.FindProperty("_hpFill").objectReferenceValue      = Find<Image>(ppt, "PlayerHPBg/PlayerHPFill");
            so.FindProperty("_hpText").objectReferenceValue      = Find<TMP_Text>(ppt, "PlayerHPText");
            so.FindProperty("_shieldGroup").objectReferenceValue = FindGO(ppt, "ShieldGroup");
            so.FindProperty("_shieldText").objectReferenceValue  = Find<TMP_Text>(ppt, "ShieldGroup");
            so.FindProperty("_atkText").objectReferenceValue     = Find<TMP_Text>(ppt, "AtkDefText");
        });
        Debug.Log("[Part4] PlayerView added");

        // ── 2. EnemyView ──────────────────────────────────────────────────

        var enemyView = enemyPanel.AddComponent<EnemyView>();
        Set(enemyView, so => {
            so.FindProperty("_container").objectReferenceValue = enemyPanel.transform;
            // _entryPrefab: 이후 EnemyEntry 프리팹 생성 후 연결
        });
        Debug.Log("[Part4] EnemyView added");

        // ── 3. EnemyEntryView on each EnemyEntry ─────────────────────────

        foreach (Transform child in ept)
        {
            if (child.name != "EnemyEntry") continue;
            var go = child.gameObject;
            var got = go.transform;
            var ev = go.AddComponent<EnemyEntryView>();
            Set(ev, so => {
                so.FindProperty("_intentText").objectReferenceValue     = Find<TMP_Text>(got, "IntentText");
                so.FindProperty("_nameText").objectReferenceValue       = Find<TMP_Text>(got, "NameText");
                so.FindProperty("_hpFill").objectReferenceValue         = Find<Image>(got, "HPBg/HPFill");
                so.FindProperty("_hpText").objectReferenceValue         = Find<TMP_Text>(got, "HPText");
                so.FindProperty("_shieldGroup").objectReferenceValue    = FindGO(got, "ShieldGroup");
                so.FindProperty("_targetButton").objectReferenceValue   = Find<Button>(got, "TargetButton");
                so.FindProperty("_targetHighlight").objectReferenceValue = Find<Image>(got, "TargetButton/TargetHighlight");
            });
        }
        Debug.Log("[Part4] EnemyEntryView x3 added");

        // ── 4. DiceEntryView on each Dice ────────────────────────────────

        var diceArea = FindGO(dpt, "DiceArea");
        var diceEntryViews = new List<DiceEntryView>();
        for (int i = 1; i <= 5; i++)
        {
            var diceGO = FindGO(diceArea.transform, "Dice" + i);
            if (diceGO == null) { Debug.LogWarning("[Part4] Dice" + i + " not found"); continue; }
            var dev = diceGO.AddComponent<DiceEntryView>();
            var dt = diceGO.transform;
            Set(dev, so => {
                so.FindProperty("_valueText").objectReferenceValue  = Find<TMP_Text>(dt, "Value");
                so.FindProperty("_background").objectReferenceValue = diceGO.GetComponent<Image>();
                so.FindProperty("_button").objectReferenceValue     = diceGO.GetComponent<Button>();
            });
            diceEntryViews.Add(dev);
        }
        Debug.Log("[Part4] DiceEntryView x5 added");

        // ── 5. DiceView on DiceTablePanel ────────────────────────────────

        var diceView = dp.AddComponent<DiceView>();
        Set(diceView, so => {
            so.FindProperty("_rerollButton").objectReferenceValue    = rerollBtnGO.GetComponent<Button>();
            so.FindProperty("_rerollCountText").objectReferenceValue = rerollTxtGO.GetComponent<TMP_Text>();
            so.FindProperty("_useSkillButton").objectReferenceValue  = useSkillBtnGO.GetComponent<Button>();
        });
        Debug.Log("[Part4] DiceView added");

        // ── 6. SkillSlotView on SlotPanel (Screen A) ─────────────────────

        var slotViewA = slotPanel.AddComponent<SkillSlotView>();
        WireSlotView(slotViewA, slotPanel.transform, "Slot", 3, "");
        Debug.Log("[Part4] SkillSlotView (A) added");

        // ── 7. SkillSlotView on SlotPanel_B (Screen B) ───────────────────

        var slotViewB = slotPanelB.AddComponent<SkillSlotView>();
        WireSlotView(slotViewB, slotPanelB.transform, "Slot", 3, "_B");
        Debug.Log("[Part4] SkillSlotView (B) added");

        // ── 8. SlotAssignmentView on DiceTablePanel ───────────────────────

        var slotAssignView = dp.AddComponent<SlotAssignmentView>();
        Set(slotAssignView, so => {
            so.FindProperty("_attackColumn").objectReferenceValue    = attackCol;
            so.FindProperty("_defenseColumn").objectReferenceValue   = defenseCol;
            so.FindProperty("_slotView").objectReferenceValue        = slotViewB;
            so.FindProperty("_rerollButton").objectReferenceValue    = rerollBtnGO.GetComponent<Button>();
            so.FindProperty("_rerollCountText").objectReferenceValue = rerollTxtGO.GetComponent<TMP_Text>();
            so.FindProperty("_useSkillButton").objectReferenceValue  = useSkillBtnGO.GetComponent<Button>();
            // _skillCardPrefab: 이후 프리팹 생성 후 연결
        });
        Debug.Log("[Part4] SlotAssignmentView added");

        // ── 9. TargetSelectionView on TargetSelectionPanel ───────────────

        var targetView = targetPanel.AddComponent<TargetSelectionView>();
        Set(targetView, so => {
            so.FindProperty("_executeButton").objectReferenceValue = endTurnBtn.GetComponent<Button>();
            so.FindProperty("_slotView").objectReferenceValue      = slotViewA;
        });
        Debug.Log("[Part4] TargetSelectionView added");

        // ── 10. BattleLogView on OverlayPanel ────────────────────────────

        var logView = op.AddComponent<BattleLogView>();
        Set(logView, so => {
            so.FindProperty("_popupParent").objectReferenceValue = popupPool.transform;
            so.FindProperty("_winScreen").objectReferenceValue   = winScreen;
            so.FindProperty("_loseScreen").objectReferenceValue  = loseScreen;
            // _popupPrefab: 이후 프리팹 생성 후 연결
        });
        Debug.Log("[Part4] BattleLogView added");

        // ── 11. UIManager 패널 레퍼런스 ─────────────────────────────────

        var uiMgr = mgr.GetComponent<UIManager>();
        if (uiMgr != null)
        {
            Set(uiMgr, so => {
                so.FindProperty("_battlePanel").objectReferenceValue    = bp;
                so.FindProperty("_diceTablePanel").objectReferenceValue = dp;
                so.FindProperty("_targetingOverlay").objectReferenceValue = targetPanel;
            });
            Debug.Log("[Part4] UIManager panels wired");
        }

        // ── 12. 프리팹 생성 ────────────────────────────────────────────────

        CreateSkillCardButtonPrefab(slotAssignView);
        CreateDamagePopupPrefab(logView);
        CreateEnemyEntryPrefab(enemyView);

        // ── 13. BattleUIAdapter on BattleManager ─────────────────────────

        var adapter = mgr.AddComponent<BattleUIAdapter>();
        Set(adapter, so => {
            so.FindProperty("_playerView").objectReferenceValue          = playerView;
            so.FindProperty("_enemyView").objectReferenceValue           = enemyView;
            so.FindProperty("_diceView").objectReferenceValue            = diceView;
            so.FindProperty("_slotAssignmentView").objectReferenceValue  = slotAssignView;
            so.FindProperty("_targetSelectionView").objectReferenceValue = targetView;
            so.FindProperty("_battleLogView").objectReferenceValue       = logView;
            so.FindProperty("_uiManager").objectReferenceValue           = uiMgr;
            var listProp = so.FindProperty("_diceEntries");
            listProp.arraySize = diceEntryViews.Count;
            for (int i = 0; i < diceEntryViews.Count; i++)
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = diceEntryViews[i];
        });
        Debug.Log("[Part4] BattleUIAdapter added");

        // ── 저장 ──────────────────────────────────────────────────────────

        EditorUtility.SetDirty(canvas);
        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Part4] ===== All components bound. Scene saved. =====");
    }

    // ── SkillSlotView 배선 헬퍼 ────────────────────────────────────────────

    static void WireSlotView(SkillSlotView view, Transform parent, string prefix, int count, string suffix)
    {
        var so = new SerializedObject(view);
        var slots = so.FindProperty("_slots");
        slots.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            string childName = prefix + (i + 1) + suffix;
            var slotT = parent.Find(childName);
            if (slotT == null) { Debug.LogWarning("[Part4] Slot not found: " + childName); continue; }
            var slotGO = slotT.gameObject;
            var el = slots.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("_nameText").objectReferenceValue   = slotT.Find("NameText")?.GetComponent<TMP_Text>();
            el.FindPropertyRelative("_handText").objectReferenceValue   = slotT.Find("HandText")?.GetComponent<TMP_Text>();
            el.FindPropertyRelative("_background").objectReferenceValue = slotGO.GetComponent<Image>();
            el.FindPropertyRelative("_button").objectReferenceValue     = slotGO.GetComponent<Button>();
        }
        so.ApplyModifiedProperties();
    }

    // ── SkillCardButton 프리팹 생성 ───────────────────────────────────────

    static void CreateSkillCardButtonPrefab(SlotAssignmentView target)
    {
        const string path = "Assets/Prefabs/SkillCardButton.prefab";

        var root = new GameObject("SkillCardButton");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 52);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.06f, 0.04f, 0.9f);
        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;
        var scb = root.AddComponent<SkillCardButton>();

        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(root.transform, false);
        var nrt = nameGO.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = Vector2.one;
        nrt.offsetMin = new Vector2(6, 2); nrt.offsetMax = new Vector2(-6, -2);
        var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "Skill"; nameTmp.fontSize = 14;
        ColorUtility.TryParseHtmlString("#f0e0b0", out var parch);
        nameTmp.color = parch;
        nameTmp.alignment = TextAlignmentOptions.Left;

        var handGO = new GameObject("Hand");
        handGO.transform.SetParent(root.transform, false);
        var hrt = handGO.AddComponent<RectTransform>();
        hrt.anchorMin = Vector2.zero; hrt.anchorMax = new Vector2(1, 0.5f);
        hrt.offsetMin = new Vector2(6, 2); hrt.offsetMax = new Vector2(-6, -2);
        var handTmp = handGO.AddComponent<TextMeshProUGUI>();
        handTmp.text = "(OnePair)"; handTmp.fontSize = 11;
        handTmp.color = new Color(0.7f, 0.55f, 0.3f);
        handTmp.alignment = TextAlignmentOptions.Left;

        var scbSO = new SerializedObject(scb);
        scbSO.FindProperty("_nameText").objectReferenceValue   = nameTmp;
        scbSO.FindProperty("_handText").objectReferenceValue   = handTmp;
        scbSO.FindProperty("_button").objectReferenceValue     = btn;
        scbSO.FindProperty("_background").objectReferenceValue = bg;
        scbSO.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        var so = new SerializedObject(target);
        so.FindProperty("_skillCardPrefab").objectReferenceValue =
            prefab.GetComponent<SkillCardButton>();
        so.ApplyModifiedProperties();
        Debug.Log("[Part4] SkillCardButton prefab created: " + path);
    }

    // ── DamagePopup 프리팹 생성 ───────────────────────────────────────────

    static void CreateDamagePopupPrefab(BattleLogView target)
    {
        const string path = "Assets/Prefabs/DamagePopup.prefab";

        var root = new GameObject("DamagePopup");
        root.AddComponent<RectTransform>();
        var dp = root.AddComponent<DamagePopup>();

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(root.transform, false);
        var trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "0"; tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.red;

        var dpSO = new SerializedObject(dp);
        dpSO.FindProperty("_text").objectReferenceValue = tmp;
        dpSO.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        var so = new SerializedObject(target);
        so.FindProperty("_popupPrefab").objectReferenceValue =
            prefab.GetComponent<DamagePopup>();
        so.ApplyModifiedProperties();
        Debug.Log("[Part4] DamagePopup prefab created: " + path);
    }

    // ── EnemyEntry 프리팹 생성 (EnemyView._entryPrefab 용) ───────────────

    static void CreateEnemyEntryPrefab(EnemyView target)
    {
        const string path = "Assets/Prefabs/EnemyEntry.prefab";

        // Root
        var root = new GameObject("EnemyEntry");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(170, 220);
        root.AddComponent<Image>().color = new Color(0.14f, 0.09f, 0.05f, 0.92f);
        var ev = root.AddComponent<EnemyEntryView>();
        var rootT = root.transform;

        // IntentText
        var eiGO = MakeChild("IntentText", rootT, 0, 0.82f, 1, 1, 4, 0, -4, -4);
        ColorUtility.TryParseHtmlString("#c04030", out var cAtk);
        var eiTmp = AddTmp(eiGO, "ATK 6", 17, cAtk);

        // NameText
        ColorUtility.TryParseHtmlString("#f0e0b0", out var cParch);
        var enGO = MakeChild("NameText", rootT, 0, 0.67f, 1, 0.82f, 4, 0, -4, -2);
        var enTmp = AddTmp(enGO, "Enemy", 15, cParch);

        // HPBg + HPFill
        var hbGO = MakeChild("HPBg", rootT, 0.05f, 0.55f, 0.95f, 0.67f);
        hbGO.AddComponent<Image>().color = new Color(0.3f, 0.08f, 0.08f);
        var hfGO = MakeChild("HPFill", hbGO.transform, 0, 0, 1, 1);
        var hfImg = hfGO.AddComponent<Image>();
        hfImg.color = new Color(0.29f, 0.62f, 0.23f);
        hfImg.type = Image.Type.Filled;
        hfImg.fillMethod = Image.FillMethod.Horizontal;

        // HPText
        var htGO = MakeChild("HPText", rootT, 0, 0.42f, 1, 0.55f, 4, 0, -4, -2);
        var htTmp = AddTmp(htGO, "HP: 20/20", 13, cParch);

        // ShieldGroup
        ColorUtility.TryParseHtmlString("#d4a017", out var cGold);
        var sgGO = MakeChild("ShieldGroup", rootT, 0, 0.28f, 0.6f, 0.42f, 4, 0, -4, -2);
        AddTmp(sgGO, "DEF 0", 14, cGold);
        sgGO.SetActive(false);

        // TargetButton
        var tbGO = MakeChild("TargetButton", rootT, 0, 0, 1, 0.28f);
        var tbImg = tbGO.AddComponent<Image>(); tbImg.color = new Color(0, 0, 0, 0);
        var tbBtn = tbGO.AddComponent<Button>(); tbBtn.targetGraphic = tbImg;

        var thGO = MakeChild("TargetHighlight", tbGO.transform, 0, 0, 1, 1);
        thGO.AddComponent<Image>().color = new Color(0.83f, 0.63f, 0.09f, 0.25f);
        thGO.SetActive(false);

        // Wire EnemyEntryView
        var eeSO = new SerializedObject(ev);
        eeSO.FindProperty("_intentText").objectReferenceValue      = eiTmp;
        eeSO.FindProperty("_nameText").objectReferenceValue        = enTmp;
        eeSO.FindProperty("_hpFill").objectReferenceValue          = hfImg;
        eeSO.FindProperty("_hpText").objectReferenceValue          = htTmp;
        eeSO.FindProperty("_shieldGroup").objectReferenceValue     = sgGO;
        eeSO.FindProperty("_targetButton").objectReferenceValue    = tbBtn;
        eeSO.FindProperty("_targetHighlight").objectReferenceValue = thGO.GetComponent<Image>();
        eeSO.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        var so = new SerializedObject(target);
        so.FindProperty("_entryPrefab").objectReferenceValue =
            prefab.GetComponent<EnemyEntryView>();
        so.ApplyModifiedProperties();
        Debug.Log("[Part4] EnemyEntry prefab created: " + path);
    }

    // ── 씬 오브젝트 생성 헬퍼 ────────────────────────────────────────────

    static GameObject MakeChild(string name, Transform parent,
        float ax, float ay, float bx, float by,
        float ox0 = 0, float oy0 = 0, float ox1 = 0, float oy1 = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(ox0, oy0); rt.offsetMax = new Vector2(ox1, oy1);
        return go;
    }

    static TextMeshProUGUI AddTmp(GameObject go, string text, int size, Color color)
    {
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        return tmp;
    }
}
