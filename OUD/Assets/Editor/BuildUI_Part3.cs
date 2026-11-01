// BuildUI_Part3.cs — BattlePanel (기존 BattleCanvas에 추가)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildUI_Part3
{
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    static GameObject GO(string n, Transform p, bool a = true)
    { var g = new GameObject(n); g.transform.SetParent(p, false); g.SetActive(a); return g; }
    static RectTransform RT(GameObject g) => g.AddComponent<RectTransform>();
    static void Fill(RectTransform r)
    { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }
    static void Anch(RectTransform r, float ax, float ay, float bx, float by,
        float ox0 = 0, float oy0 = 0, float ox1 = 0, float oy1 = 0)
    { r.anchorMin = new Vector2(ax, ay); r.anchorMax = new Vector2(bx, by); r.offsetMin = new Vector2(ox0, oy0); r.offsetMax = new Vector2(ox1, oy1); }
    static Image Img(GameObject g, Color c) { var i = g.AddComponent<Image>(); i.color = c; return i; }
    static TextMeshProUGUI Txt(GameObject g, string t, int sz, Color c, TextAlignmentOptions a = TextAlignmentOptions.Center)
    { var tmp = g.AddComponent<TextMeshProUGUI>(); tmp.text = t; tmp.fontSize = sz; tmp.color = c; tmp.alignment = a; return tmp; }
    static Button Btn(GameObject g) => g.AddComponent<Button>();

    [MenuItem("OUD/Build UI/Part3 - BattlePanel")]
    public static void Execute()
    {
        var cGO = GameObject.Find("BattleCanvas");
        if (cGO == null) { Debug.LogError("BattleCanvas not found!"); return; }

        // 기존 BattlePanel 있으면 삭제
        var old = cGO.transform.Find("BattlePanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var cGold    = Hex("#d4a017");
        var cGoldBrt = Hex("#f0c040");
        var cParch   = Hex("#f0e0b0");
        var cAtk     = Hex("#c04030");
        var cDis     = Hex("#2a2018");
        var cHpGreen = Hex("#4a9e3a");

        // BattlePanel (화면 A/C)
        var bp = GO("BattlePanel", cGO.transform);
        Anch(RT(bp), 0, 0, 1, 0.87f);

        // ── EnemyPanel ────────────────────────────────────────────────────
        var ep = GO("EnemyPanel", bp.transform);
        Anch(RT(ep), 0.25f, 0.40f, 1f, 1f);
        var ehl = ep.AddComponent<HorizontalLayoutGroup>();
        ehl.spacing = 40; ehl.childAlignment = TextAnchor.MiddleCenter;
        ehl.childForceExpandWidth = false; ehl.childForceExpandHeight = false;
        ehl.padding = new RectOffset(20, 20, 20, 10);

        string[] enemyNames = new string[] { "Slime", "Slime", "Skeleton" };
        foreach (var en in enemyNames)
        {
            var eGO = GO("EnemyEntry", ep.transform);
            var eRT = eGO.AddComponent<RectTransform>(); eRT.sizeDelta = new Vector2(170, 220);
            Img(eGO, new Color(0.14f, 0.09f, 0.05f, 0.92f));

            var intGO = GO("IntentText", eGO.transform);
            Anch(RT(intGO), 0, 0.82f, 1, 1, 4, 0, -4, -4);
            Txt(intGO, "ATK 6", 17, cAtk);

            var nmGO = GO("NameText", eGO.transform);
            Anch(RT(nmGO), 0, 0.67f, 1, 0.82f, 4, 0, -4, -2);
            Txt(nmGO, en, 15, cParch);

            var hbGO = GO("HPBg", eGO.transform);
            Anch(RT(hbGO), 0.05f, 0.55f, 0.95f, 0.67f);
            Img(hbGO, new Color(0.3f, 0.08f, 0.08f));

            var hfGO = GO("HPFill", hbGO.transform);
            Fill(RT(hfGO));
            var hfi = Img(hfGO, cHpGreen);
            hfi.type = Image.Type.Filled; hfi.fillMethod = Image.FillMethod.Horizontal;

            var htGO = GO("HPText", eGO.transform);
            Anch(RT(htGO), 0, 0.42f, 1, 0.55f, 4, 0, -4, -2);
            Txt(htGO, "HP: 20/20", 13, cParch);

            var shGO = GO("ShieldGroup", eGO.transform);
            Anch(RT(shGO), 0, 0.28f, 0.6f, 0.42f, 4, 0, -4, -2);
            Txt(shGO, "DEF 0", 14, cGold);
            shGO.SetActive(false);

            var tbGO = GO("TargetButton", eGO.transform);
            Anch(RT(tbGO), 0, 0, 1, 0.28f);
            Img(tbGO, new Color(0, 0, 0, 0));
            Btn(tbGO);
            var thGO = GO("TargetHighlight", tbGO.transform);
            Fill(RT(thGO));
            Img(thGO, new Color(0.83f, 0.63f, 0.09f, 0.25f));
            thGO.SetActive(false);
        }

        // ── PlayerPanel ───────────────────────────────────────────────────
        var pp = GO("PlayerPanel", bp.transform);
        Anch(RT(pp), 0.01f, 0.02f, 0.24f, 0.72f);
        Img(pp, new Color(0.09f, 0.06f, 0.04f, 0.8f));

        var pnGO = GO("PlayerName", pp.transform);
        Anch(RT(pnGO), 0, 0.78f, 1, 1, 8, 0, -8, -4);
        Txt(pnGO, "Alice", 22, cGoldBrt, TextAlignmentOptions.Left);

        var phbGO = GO("PlayerHPBg", pp.transform);
        Anch(RT(phbGO), 0.05f, 0.60f, 0.95f, 0.75f);
        Img(phbGO, new Color(0.3f, 0.08f, 0.08f));

        var phfGO = GO("PlayerHPFill", phbGO.transform);
        Fill(RT(phfGO));
        var phfi = Img(phfGO, cHpGreen);
        phfi.type = Image.Type.Filled; phfi.fillMethod = Image.FillMethod.Horizontal;

        var phtGO = GO("PlayerHPText", pp.transform);
        Anch(RT(phtGO), 0, 0.46f, 1, 0.60f, 8, 0, -8, -2);
        Txt(phtGO, "HP: 60 / 60", 16, cParch);

        var pshGO = GO("ShieldGroup", pp.transform);
        Anch(RT(pshGO), 0, 0.30f, 0.6f, 0.46f, 8, 0, -4, -2);
        Txt(pshGO, "DEF 0", 18, cGold, TextAlignmentOptions.Left);
        pshGO.SetActive(false);

        var patGO = GO("AtkDefText", pp.transform);
        Anch(RT(patGO), 0, 0.14f, 1, 0.30f, 8, 0, -8, -2);
        Txt(patGO, "ATK 6   DEF 5", 14, new Color(0.7f, 0.6f, 0.4f), TextAlignmentOptions.Left);

        // ── SlotPanel (B1) ────────────────────────────────────────────────
        var slPnl = GO("SlotPanel", bp.transform);
        Anch(RT(slPnl), 0.24f, 0.02f, 0.82f, 0.38f);
        var shl = slPnl.AddComponent<HorizontalLayoutGroup>();
        shl.spacing = 14; shl.childAlignment = TextAnchor.MiddleCenter;
        shl.padding = new RectOffset(8, 8, 6, 6);
        shl.childForceExpandWidth = false; shl.childForceExpandHeight = true;

        for (int i = 0; i < 3; i++)
        {
            var sl = GO("Slot" + (i + 1), slPnl.transform);
            var srt = sl.AddComponent<RectTransform>(); srt.sizeDelta = new Vector2(185, 95);
            Img(sl, cDis);
            Btn(sl);
            var sLbl = GO("NameText", sl.transform);
            Anch(RT(sLbl), 0, 0.55f, 1, 1, 6, 2, -6, -2);
            Txt(sLbl, "Empty Slot", 15, new Color(0.33f, 0.25f, 0.19f));
            var sHnd = GO("HandText", sl.transform);
            Anch(RT(sHnd), 0, 0, 1, 0.55f, 6, 2, -6, -2);
            Txt(sHnd, "", 12, new Color(0.5f, 0.4f, 0.2f));
        }

        // ── ActionButtonA (버튼A 고정 위치) ─────────────────────────────
        var btnA = GO("ActionButtonA", bp.transform);
        Anch(RT(btnA), 0.82f, 0.03f, 0.99f, 0.19f);
        Img(btnA, cGold);
        Btn(btnA);
        var baTxt = GO("Text", btnA.transform); Fill(RT(baTxt));
        Txt(baTxt, "Roll Dice", 17, Hex("#0c0804"));

        // ── TargetSelectionPanel (화면 C, 비활성) ────────────────────────
        var tsp = GO("TargetSelectionPanel", bp.transform, false);
        Fill(RT(tsp));

        var etBtn = GO("EndTurnButton", tsp.transform);
        Anch(RT(etBtn), 0.82f, 0.03f, 0.99f, 0.19f);
        Img(etBtn, cAtk);
        Btn(etBtn);
        var etTxt = GO("Text", etBtn.transform); Fill(RT(etTxt));
        Txt(etTxt, "End Turn", 17, Color.white);

        // BattlePanel을 DiceTablePanel 앞으로 이동 (z-order)
        bp.transform.SetSiblingIndex(2);

        EditorUtility.SetDirty(cGO);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[BuildUI_Part3] BattlePanel 생성 완료 (EnemyPanel + PlayerPanel + SlotPanel + Buttons)");
    }
}
