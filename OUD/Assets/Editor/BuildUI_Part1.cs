// BuildUI_Part1.cs — BattleCanvas + TopBar + TraitChipBar + BattlePanel
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildUI_Part1
{
    // ── 헬퍼 ────────────────────────────────────────────────────────────────
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    static GameObject GO(string name, Transform parent, bool active = true)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.SetActive(active);
        return g;
    }

    static RectTransform RT(GameObject g) => g.AddComponent<RectTransform>();

    static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rt, float ax, float ay, float bx, float by,
                       float ox0 = 0, float oy0 = 0, float ox1 = 0, float oy1 = 0)
    {
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(ox0, oy0); rt.offsetMax = new Vector2(ox1, oy1);
    }

    static Image Img(GameObject g, Color c) { var i = g.AddComponent<Image>(); i.color = c; return i; }

    static TextMeshProUGUI Txt(GameObject g, string t, int sz, Color c,
                               TextAlignmentOptions a = TextAlignmentOptions.Center)
    {
        var tmp = g.AddComponent<TextMeshProUGUI>();
        tmp.text = t; tmp.fontSize = sz; tmp.color = c; tmp.alignment = a;
        return tmp;
    }

    static Button Btn(GameObject g) => g.AddComponent<Button>();

    // ── 색상 ────────────────────────────────────────────────────────────────
    static readonly Color cBg       = Hex("#0c0804") is var bg       ? bg       : Color.black;
    static readonly Color cPanel    = new Color(0.09f, 0.06f, 0.04f, 0.97f);
    static readonly Color cGold     = Hex("#d4a017") is var gd       ? gd       : Color.yellow;
    static readonly Color cGoldBrt  = Hex("#f0c040") is var gb       ? gb       : Color.yellow;
    static readonly Color cParch    = Hex("#f0e0b0") is var pc       ? pc       : Color.white;
    static readonly Color cAtk      = Hex("#c04030") is var ak       ? ak       : Color.red;
    static readonly Color cDef      = Hex("#2860c0") is var df       ? df       : Color.blue;
    static readonly Color cDisabled = Hex("#2a2018") is var di       ? di       : Color.gray;

    // ── 엔트리 포인트 ───────────────────────────────────────────────────────
    [UnityEditor.MenuItem("OUD/Build UI/Part1 - Canvas + BattlePanel")]
    public static void Execute()
    {
        // ── BattleCanvas ──────────────────────────────────────────────────
        var cGO = new GameObject("BattleCanvas");
        var cv  = cGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        Img(cGO, cBg);

        // ── TopBar (H1) 상단 8% ───────────────────────────────────────────
        var topBar = GO("TopBar", cGO.transform);
        Anchor(RT(topBar), 0, 0.92f, 1, 1);
        Img(topBar, cPanel);

        var piGO = GO("PlayerInfo", topBar.transform);
        Anchor(RT(piGO), 0, 0, 0.5f, 1, 10, 4, -10, -4);
        Txt(piGO, "Alice   HP: 60/60   💰0   ⚡0", 20, cParch, TextAlignmentOptions.Left);

        var flGO = GO("FloorInfo", topBar.transform);
        Anchor(RT(flGO), 0.5f, 0, 1, 1, 10, 4, -10, -4);
        Txt(flGO, "50F   [지도] [일지] [⚙]", 20, cGold, TextAlignmentOptions.Right);

        // ── TraitChipBar (H2) 87~92% ─────────────────────────────────────
        var traitBar = GO("TraitChipBar", cGO.transform);
        Anchor(RT(traitBar), 0, 0.87f, 1, 0.92f);
        Img(traitBar, new Color(0.12f, 0.08f, 0.04f, 0.9f));
        var thl = traitBar.AddComponent<HorizontalLayoutGroup>();
        thl.padding = new RectOffset(10, 10, 4, 4); thl.spacing = 8;
        thl.childAlignment = TextAnchor.MiddleLeft;
        thl.childControlWidth = false; thl.childControlHeight = false;

        foreach (var ct in new[] { "🔥 화염체질", "🎯 명사수", "🛡 단단한등껍질" })
        {
            var chip = GO($"Chip", traitBar.transform);
            var crt = chip.AddComponent<RectTransform>(); crt.sizeDelta = new Vector2(130, 28);
            Img(chip, new Color(0.2f, 0.14f, 0.06f, 0.95f));
            Txt(chip, ct, 13, cParch);
        }

        // ── BattlePanel (화면 A/C) 0~87% ─────────────────────────────────
        var bp = GO("BattlePanel", cGO.transform);
        Anchor(RT(bp), 0, 0, 1, 0.87f);

        // EnemyPanel 상단 55%
        var ep = GO("EnemyPanel", bp.transform);
        Anchor(RT(ep), 0.25f, 0.42f, 1, 1);
        var ehl = ep.AddComponent<HorizontalLayoutGroup>();
        ehl.spacing = 40; ehl.childAlignment = TextAnchor.MiddleCenter;
        ehl.childForceExpandWidth = false; ehl.childForceExpandHeight = false;
        ehl.padding = new RectOffset(20, 20, 20, 10);

        foreach (var en in new[] { "슬라임", "슬라임", "스켈레톤" })
        {
            var eGO = GO($"EnemyEntry_{en}", ep.transform);
            var eRT = eGO.AddComponent<RectTransform>(); eRT.sizeDelta = new Vector2(170, 220);
            Img(eGO, new Color(0.14f, 0.09f, 0.05f, 0.92f));

            // 인텐트
            var intGO = GO("IntentText", eGO.transform);
            Anchor(RT(intGO), 0, 0.82f, 1, 1, 4, 0, -4, -4);
            Txt(intGO, "ATK 6", 17, cAtk);

            // 이름
            var nmGO = GO("NameText", eGO.transform);
            Anchor(RT(nmGO), 0, 0.67f, 1, 0.82f, 4, 0, -4, -2);
            Txt(nmGO, en, 15, cParch);

            // HP 배경
            var hbGO = GO("HPBg", eGO.transform);
            Anchor(RT(hbGO), 0.05f, 0.55f, 0.95f, 0.67f);
            Img(hbGO, new Color(0.3f, 0.08f, 0.08f));

            var hfGO = GO("HPFill", hbGO.transform);
            Fill(RT(hfGO));
            var hfi = Img(hfGO, Hex("#4a9e3a"));
            hfi.type = Image.Type.Filled; hfi.fillMethod = Image.FillMethod.Horizontal;

            var htGO = GO("HPText", eGO.transform);
            Anchor(RT(htGO), 0, 0.42f, 1, 0.55f, 4, 0, -4, -2);
            Txt(htGO, "HP: 20/20", 13, cParch);

            // 실드
            var shGO = GO("ShieldGroup", eGO.transform);
            Anchor(RT(shGO), 0, 0.28f, 0.6f, 0.42f, 4, 0, -4, -2);
            Txt(shGO, "🛡 0", 14, cGold);
            shGO.SetActive(false);

            // 타겟 버튼
            var tbGO = GO("TargetButton", eGO.transform);
            Anchor(RT(tbGO), 0, 0, 1, 0.28f);
            Img(tbGO, new Color(0, 0, 0, 0));
            Btn(tbGO);
            var thGO = GO("TargetHighlight", tbGO.transform);
            Fill(RT(thGO));
            var thi = Img(thGO, new Color(0.83f, 0.63f, 0.09f, 0.25f));
            thGO.SetActive(false);
        }

        // PlayerPanel 하단 좌측
        var pp = GO("PlayerPanel", bp.transform);
        Anchor(RT(pp), 0.01f, 0.02f, 0.24f, 0.72f);
        Img(pp, new Color(0.09f, 0.06f, 0.04f, 0.8f));

        var pnGO = GO("PlayerName", pp.transform);
        Anchor(RT(pnGO), 0, 0.78f, 1, 1, 8, 0, -8, -4);
        Txt(pnGO, "Alice", 22, cGoldBrt, TextAlignmentOptions.Left);

        var phbGO = GO("PlayerHPBg", pp.transform);
        Anchor(RT(phbGO), 0.05f, 0.6f, 0.95f, 0.75f);
        Img(phbGO, new Color(0.3f, 0.08f, 0.08f));

        var phfGO = GO("PlayerHPFill", phbGO.transform);
        Fill(RT(phfGO));
        var phfi = Img(phfGO, Hex("#4a9e3a"));
        phfi.type = Image.Type.Filled; phfi.fillMethod = Image.FillMethod.Horizontal;

        var phtGO = GO("PlayerHPText", pp.transform);
        Anchor(RT(phtGO), 0, 0.46f, 1, 0.6f, 8, 0, -8, -2);
        Txt(phtGO, "HP: 60 / 60", 16, cParch);

        var pshGO = GO("ShieldGroup", pp.transform);
        Anchor(RT(pshGO), 0, 0.3f, 0.6f, 0.46f, 8, 0, -4, -2);
        Txt(pshGO, "🛡 0", 18, cGold, TextAlignmentOptions.Left);
        pshGO.SetActive(false);

        var patGO = GO("AtkDefText", pp.transform);
        Anchor(RT(patGO), 0, 0.14f, 1, 0.3f, 8, 0, -8, -2);
        Txt(patGO, "ATK 6   DEF 5", 14, new Color(0.7f, 0.6f, 0.4f), TextAlignmentOptions.Left);

        // SlotPanel B1 하단 중앙
        var slotPnl = GO("SlotPanel", bp.transform);
        Anchor(RT(slotPnl), 0.24f, 0.02f, 0.82f, 0.38f);
        var shl = slotPnl.AddComponent<HorizontalLayoutGroup>();
        shl.spacing = 14; shl.childAlignment = TextAnchor.MiddleCenter;
        shl.padding = new RectOffset(8, 8, 6, 6);
        shl.childForceExpandWidth = false; shl.childForceExpandHeight = true;

        for (int i = 0; i < 3; i++)
        {
            var sl = GO($"Slot{i + 1}", slotPnl.transform);
            var srt = sl.AddComponent<RectTransform>(); srt.sizeDelta = new Vector2(185, 95);
            Img(sl, cDisabled);
            var slBtn = Btn(sl);
            var sColorBlock = slBtn.colors;
            sColorBlock.highlightedColor = new Color(0.25f, 0.18f, 0.1f);
            slBtn.colors = sColorBlock;

            var sLbl = GO("NameText", sl.transform);
            Anchor(RT(sLbl), 0, 0.55f, 1, 1, 6, 2, -6, -2);
            Txt(sLbl, "빈 슬롯", 15, new Color(0.33f, 0.25f, 0.19f));

            var sHnd = GO("HandText", sl.transform);
            Anchor(RT(sHnd), 0, 0, 1, 0.55f, 6, 2, -6, -2);
            Txt(sHnd, "", 12, new Color(0.5f, 0.4f, 0.2f));
        }

        // ActionButtonA 버튼A 고정 자리
        var btnA = GO("ActionButtonA", bp.transform);
        Anchor(RT(btnA), 0.82f, 0.03f, 0.99f, 0.19f);
        Img(btnA, cGold);
        Btn(btnA);
        var baTxt = GO("Text", btnA.transform);
        Fill(RT(baTxt));
        Txt(baTxt, "🎲 주사위 굴리기", 17, Hex("#0c0804"));

        // TargetSelectionPanel (화면 C, 비활성)
        var tsp = GO("TargetSelectionPanel", bp.transform, false);
        Fill(RT(tsp));

        var etBtn = GO("EndTurnButton", tsp.transform);
        Anchor(RT(etBtn), 0.82f, 0.03f, 0.99f, 0.19f);
        Img(etBtn, cAtk);
        Btn(etBtn);
        var etTxt = GO("Text", etBtn.transform); Fill(RT(etTxt));
        Txt(etTxt, "턴 종료", 17, Color.white);

        EditorUtility.SetDirty(cGO);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[BuildUI_Part1] BattleCanvas + TopBar + TraitChipBar + BattlePanel 생성 완료");
    }
}
