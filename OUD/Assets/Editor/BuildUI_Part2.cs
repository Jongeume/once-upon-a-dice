// BuildUI_Part2.cs — DiceTablePanel + OverlayPanel
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildUI_Part2
{
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

    static readonly Color cBg      = Hex("#0c0804") is var bg ? bg : Color.black;
    static readonly Color cPanel   = new Color(0.09f, 0.06f, 0.04f, 0.97f);
    static readonly Color cGold    = Hex("#d4a017") is var gd ? gd : Color.yellow;
    static readonly Color cParch   = Hex("#f0e0b0") is var pc ? pc : Color.white;
    static readonly Color cAtk     = Hex("#c04030") is var ak ? ak : Color.red;
    static readonly Color cDef     = Hex("#2860c0") is var df ? df : Color.blue;
    static readonly Color cDisabled = Hex("#2a2018") is var di ? di : Color.gray;

    [UnityEditor.MenuItem("OUD/Build UI/Part2 - DiceTablePanel + Overlay")]
    public static void Execute()
    {
        // BattleCanvas 찾기
        var canvasGO = GameObject.Find("BattleCanvas");
        if (canvasGO == null) { Debug.LogError("BattleCanvas not found! Run Part1 first."); return; }

        // ── DiceTablePanel (화면 B, 비활성) ──────────────────────────────
        var dp = GO("DiceTablePanel", canvasGO.transform, false);
        Anchor(RT(dp), 0, 0, 1, 0.87f);
        Img(dp, new Color(0.07f, 0.04f, 0.02f, 0.97f));

        // SkillListPanel 좌측 25%
        var slp = GO("SkillListPanel", dp.transform);
        Anchor(RT(slp), 0, 0, 0.25f, 1);
        Img(slp, new Color(0.09f, 0.06f, 0.04f, 0.95f));

        var sltGO = GO("Title", slp.transform);
        Anchor(RT(sltGO), 0, 0.88f, 1, 1, 6, 0, -6, -4);
        Txt(sltGO, "기술 목록", 18, cGold);

        // 공격/수비 헤더
        var hdrGO = GO("ColumnHeaders", slp.transform);
        Anchor(RT(hdrGO), 0, 0.79f, 1, 0.88f);
        var hdrHL = hdrGO.AddComponent<HorizontalLayoutGroup>();
        hdrHL.childForceExpandWidth = true;
        hdrHL.padding = new RectOffset(4, 4, 2, 2);

        var ahGO = GO("AtkHeader", hdrGO.transform);
        ahGO.AddComponent<RectTransform>();
        Txt(ahGO, "공격", 15, cAtk);

        var dhGO = GO("DefHeader", hdrGO.transform);
        dhGO.AddComponent<RectTransform>();
        Txt(dhGO, "수비", 15, cDef);

        // 기술 컬럼
        var colsGO = GO("SkillColumns", slp.transform);
        Anchor(RT(colsGO), 0, 0, 1, 0.79f);
        var colsHL = colsGO.AddComponent<HorizontalLayoutGroup>();
        colsHL.childForceExpandWidth = true;
        colsHL.childForceExpandHeight = true;

        var atkCol = GO("AttackColumn", colsGO.transform);
        atkCol.AddComponent<RectTransform>();
        var atkVL = atkCol.AddComponent<VerticalLayoutGroup>();
        atkVL.spacing = 4; atkVL.padding = new RectOffset(4, 2, 4, 4);
        atkVL.childForceExpandHeight = false;
        atkVL.childForceExpandWidth = true;

        var defCol = GO("DefenseColumn", colsGO.transform);
        defCol.AddComponent<RectTransform>();
        var defVL = defCol.AddComponent<VerticalLayoutGroup>();
        defVL.spacing = 4; defVL.padding = new RectOffset(2, 4, 4, 4);
        defVL.childForceExpandHeight = false;
        defVL.childForceExpandWidth = true;

        // 샘플 기술 카드
        foreach (var (col, skills) in new[] {
            (atkCol, new[] { ("화염탄", "OnePair"), ("번개", "SmStraight") }),
            (defCol, new[] { ("방패벽", "OnePair"), ("회복", "Triple") })
        })
        {
            foreach (var (sName, sHand) in skills)
            {
                var card = GO($"Card_{sName}", col.transform);
                var cRT = card.AddComponent<RectTransform>(); cRT.sizeDelta = new Vector2(0, 52);
                Img(card, new Color(0.12f, 0.09f, 0.05f, 0.9f));
                Btn(card);
                var cardLayout = card.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(6, 6, 4, 4);
                cardLayout.childForceExpandWidth = true;
                cardLayout.childForceExpandHeight = false;

                var cnGO = GO("Name", card.transform);
                cnGO.AddComponent<RectTransform>();
                Txt(cnGO, sName, 14, cParch, TextAlignmentOptions.Left);

                var chGO = GO("Hand", card.transform);
                chGO.AddComponent<RectTransform>();
                Txt(chGO, $"({sHand})", 11, new Color(0.7f, 0.55f, 0.3f), TextAlignmentOptions.Left);
            }
        }

        // Keep Slots Area T1
        var ka = GO("KeepSlotsArea", dp.transform);
        Anchor(RT(ka), 0.25f, 0.73f, 1, 0.97f);
        Img(ka, new Color(0.11f, 0.08f, 0.05f, 0.9f));
        var kaHL = ka.AddComponent<HorizontalLayoutGroup>();
        kaHL.spacing = 12; kaHL.childAlignment = TextAnchor.MiddleCenter;
        kaHL.padding = new RectOffset(12, 12, 8, 8);
        kaHL.childForceExpandWidth = false; kaHL.childForceExpandHeight = true;

        var klGO = GO("Label", ka.transform);
        var klRT = klGO.AddComponent<RectTransform>(); klRT.sizeDelta = new Vector2(55, 0);
        Txt(klGO, "KEEP", 13, cGold);

        for (int i = 0; i < 5; i++)
        {
            var ks = GO($"KeepSlot{i + 1}", ka.transform);
            var ksRT = ks.AddComponent<RectTransform>(); ksRT.sizeDelta = new Vector2(72, 72);
            Img(ks, new Color(0.14f, 0.10f, 0.06f, 0.9f));
            Btn(ks);
            var ksTxt = GO("Value", ks.transform);
            Fill(RT(ksTxt));
            Txt(ksTxt, "-", 28, new Color(0.33f, 0.25f, 0.19f));
        }

        // DiceArea T2 중앙
        var da = GO("DiceArea", dp.transform);
        Anchor(RT(da), 0.25f, 0.38f, 1, 0.73f);
        var daHL = da.AddComponent<HorizontalLayoutGroup>();
        daHL.spacing = 18; daHL.childAlignment = TextAnchor.MiddleCenter;
        daHL.padding = new RectOffset(18, 18, 10, 10);
        daHL.childForceExpandWidth = false; daHL.childForceExpandHeight = true;

        for (int i = 0; i < 5; i++)
        {
            var dice = GO($"Dice{i + 1}", da.transform);
            var dRT = dice.AddComponent<RectTransform>(); dRT.sizeDelta = new Vector2(92, 92);
            Img(dice, new Color(0.09f, 0.06f, 0.04f, 1));
            Btn(dice);
            var dTxt = GO("Value", dice.transform);
            Fill(RT(dTxt));
            Txt(dTxt, (i + 1).ToString(), 34, cGold);
        }

        // SlotPanel_B B1 (화면A 동일 위치)
        var spb = GO("SlotPanel_B", dp.transform);
        Anchor(RT(spb), 0.25f, 0.02f, 0.82f, 0.38f);
        var spbHL = spb.AddComponent<HorizontalLayoutGroup>();
        spbHL.spacing = 14; spbHL.childAlignment = TextAnchor.MiddleCenter;
        spbHL.padding = new RectOffset(8, 8, 6, 6);
        spbHL.childForceExpandWidth = false; spbHL.childForceExpandHeight = true;

        for (int i = 0; i < 3; i++)
        {
            var sl = GO($"Slot{i + 1}_B", spb.transform);
            var sRT = sl.AddComponent<RectTransform>(); sRT.sizeDelta = new Vector2(185, 95);
            Img(sl, cDisabled);
            Btn(sl);
            var sNm = GO("NameText", sl.transform);
            Anchor(RT(sNm), 0, 0.55f, 1, 1, 6, 2, -6, -2);
            Txt(sNm, "빈 슬롯", 15, new Color(0.33f, 0.25f, 0.19f));
            var sHd = GO("HandText", sl.transform);
            Anchor(RT(sHd), 0, 0, 1, 0.55f, 6, 2, -6, -2);
            Txt(sHd, "", 12, new Color(0.5f, 0.4f, 0.2f));
        }

        // ActionButtons (버튼A + 버튼B) 하단 좌측
        var ab = GO("ActionButtons", dp.transform);
        Anchor(RT(ab), 0.25f, 0.02f, 0.45f, 0.38f);
        var abVL = ab.AddComponent<VerticalLayoutGroup>();
        abVL.spacing = 10; abVL.childAlignment = TextAnchor.MiddleCenter;
        abVL.padding = new RectOffset(8, 8, 8, 8);
        abVL.childForceExpandWidth = true; abVL.childForceExpandHeight = false;

        // 뒤로가기 (버튼A)
        var bbGO = GO("BackButton", ab.transform);
        var bbRT = bbGO.AddComponent<RectTransform>(); bbRT.sizeDelta = new Vector2(0, 52);
        Img(bbGO, new Color(0.35f, 0.25f, 0.08f));
        Btn(bbGO);
        var bbTxt = GO("Text", bbGO.transform); Fill(RT(bbTxt));
        Txt(bbTxt, "뒤로가기", 16, cParch);

        // 리롤 (버튼B)
        var rbGO = GO("RerollButton", ab.transform);
        var rbRT = rbGO.AddComponent<RectTransform>(); rbRT.sizeDelta = new Vector2(0, 52);
        Img(rbGO, cGold);
        Btn(rbGO);
        var rbTxt = GO("Text", rbGO.transform); Fill(RT(rbTxt));
        Txt(rbTxt, "리롤 2/2", 16, Hex("#0c0804"));

        // 기술사용 (버튼B 전환, 비활성)
        var ubGO = GO("UseSkillButton", ab.transform, false);
        var ubRT = ubGO.AddComponent<RectTransform>(); ubRT.sizeDelta = new Vector2(0, 52);
        Img(ubGO, cDef);
        Btn(ubGO);
        var ubTxt = GO("Text", ubGO.transform); Fill(RT(ubTxt));
        Txt(ubTxt, "기술 사용", 16, Color.white);

        // ── OverlayPanel ─────────────────────────────────────────────────
        var op = GO("OverlayPanel", canvasGO.transform);
        Fill(RT(op));
        op.AddComponent<Canvas>().sortingOrder = 10;

        // DamagePopupPool
        var ppGO = GO("DamagePopupPool", op.transform);
        Fill(RT(ppGO));

        // WinScreen (비활성)
        var ws = GO("WinScreen", op.transform, false);
        Fill(RT(ws));
        Img(ws, new Color(0, 0, 0, 0.82f));
        var wsTxt = GO("WinText", ws.transform);
        Anchor(RT(wsTxt), 0.15f, 0.35f, 0.85f, 0.65f);
        Txt(wsTxt, "전투 승리", 60, cGold);

        // LoseScreen (비활성)
        var ls = GO("LoseScreen", op.transform, false);
        Fill(RT(ls));
        Img(ls, new Color(0, 0, 0, 0.85f));
        var lsTxt = GO("LoseText", ls.transform);
        Anchor(RT(lsTxt), 0.15f, 0.35f, 0.85f, 0.65f);
        Txt(lsTxt, "전투 패배", 60, cAtk);

        // ── UIManager GO ─────────────────────────────────────────────────
        var mgrGO = new GameObject("BattleManager");
        mgrGO.AddComponent<OUD.Unity.Common.UIManager>();

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[BuildUI_Part2] DiceTablePanel + OverlayPanel + BattleManager 생성 완료");
    }
}
