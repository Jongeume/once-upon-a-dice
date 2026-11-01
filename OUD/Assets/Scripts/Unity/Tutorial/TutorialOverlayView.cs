using System.Collections;
using System.Collections.Generic;
using OUD.Unity.Battle.View;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Tutorial
{
    public class TutorialOverlayView : ViewBase
    {
        [Header("가이드 패널")]
        [SerializeField] private GameObject _guidePanel;
        [SerializeField] private TMP_Text   _guideText;
        [SerializeField] private CanvasGroup _guidePanelCanvasGroup;

        public const float FADE_DURATION = 0.3f;

        // 금색 버튼 프레임과 대비되도록 하늘색 계열 사용 (금색-on-금색 문제 해결)
        private static readonly Color   GLOW_COLOR            = new Color(0.40f, 0.85f, 1.0f, 1f);
        private static readonly Vector2 GLOW_OUTLINE_DISTANCE = new Vector2(6f, -6f);
        private const float GLOW_PULSE_PERIOD    = 1.0f;
        private const float GLOW_PULSE_MIN_ALPHA = 0.3f;
        // 스케일 펄스 — Outline보다 훨씬 잘 보이는 주 효과 (1.0 ↔ 1.08 호흡)
        private const float GLOW_SCALE_MAX       = 1.08f;

        private DiceView            _diceView;
        private SlotAssignmentView  _slotAssignmentView;
        private TargetSelectionView _targetSelectionView;
        private List<DiceEntryView> _diceEntries;
        private List<EnemyEntryView> _enemyEntryViews;
        private GameObject           _rollDiceButtonRef;

        private readonly List<Coroutine> _activeGlowCoroutines = new();
        private readonly List<Outline>   _activeGlowOutlines   = new();
        // 스케일 펄스 복원용 — (대상 RectTransform, 원래 스케일)
        private readonly List<(RectTransform rt, Vector3 originalScale)> _activeGlowScales = new();

        private Coroutine _fadeCoroutine;

        public void InjectViews(
            DiceView diceView,
            SlotAssignmentView slotAssignmentView,
            TargetSelectionView targetSelectionView,
            List<DiceEntryView> diceEntries,
            List<EnemyEntryView> enemyEntryViews,
            GameObject rollDiceButton = null)
        {
            _diceView            = diceView;
            _slotAssignmentView  = slotAssignmentView;
            _targetSelectionView = targetSelectionView;
            _diceEntries         = diceEntries;
            _enemyEntryViews     = enemyEntryViews;
            _rollDiceButtonRef   = rollDiceButton;
        }

        // ── 가이드 텍스트 ──────────────────────────────────────────────────

        public void ShowGuide(string text)
        {
            if (_guideText != null) _guideText.text = text;
            if (_guidePanel != null) _guidePanel.SetActive(true);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_guidePanelCanvasGroup, 1f));
        }

        public void HideGuide()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_guidePanelCanvasGroup, 0f, () =>
            {
                if (_guidePanel != null) _guidePanel.SetActive(false);
            }));
        }

        public void HideGuideImmediate()
        {
            if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
            if (_guidePanelCanvasGroup != null) _guidePanelCanvasGroup.alpha = 0f;
            if (_guidePanel != null) _guidePanel.SetActive(false);
        }

        // ── 글로우 제어 ──────────────────────────────────────────────────

        public void SetGlow(GlowTarget target, bool active)
        {
            if (!active)
            {
                ClearAllGlows();
                return;
            }

            switch (target)
            {
                case GlowTarget.RollDiceButton:
                    AddGlow(GetRollDiceButton());
                    break;
                case GlowTarget.RerollButton:
                    AddGlow(GetRerollButton());
                    break;
                case GlowTarget.UseSkillButton:
                    AddGlow(GetUseSkillButton());
                    break;
                case GlowTarget.ExecuteButton:
                    AddGlow(GetExecuteButton());
                    break;
                case GlowTarget.DiceEntries:
                    if (_diceEntries != null)
                        foreach (var entry in _diceEntries)
                            if (entry != null) AddGlow(entry.gameObject);
                    break;
                case GlowTarget.SkillList:
                    AddGlowToSkillList();
                    break;
                case GlowTarget.EnemyCards:
                    if (_enemyEntryViews != null)
                        foreach (var entry in _enemyEntryViews)
                            if (entry != null && entry.gameObject.activeSelf)
                                AddGlow(entry.gameObject);
                    break;
            }
        }

        public void ClearAllGlows()
        {
            foreach (var co in _activeGlowCoroutines)
                if (co != null) StopCoroutine(co);
            _activeGlowCoroutines.Clear();

            foreach (var outline in _activeGlowOutlines)
            {
                if (outline != null)
                    outline.enabled = false;
            }
            _activeGlowOutlines.Clear();

            // 스케일 펄스 원복 (코루틴은 위에서 이미 정지됨)
            foreach (var (rt, originalScale) in _activeGlowScales)
            {
                if (rt != null)
                    rt.localScale = originalScale;
            }
            _activeGlowScales.Clear();
        }

        // ── 글로우 헬퍼 ──────────────────────────────────────────────────

        private void AddGlow(GameObject go)
        {
            if (go == null) return;

            // 1) 색 대비 아웃라인 — 대상에 Graphic이 있을 때만 그려짐 (없으면 무해한 no-op)
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor    = GLOW_COLOR;
            outline.effectDistance = GLOW_OUTLINE_DISTANCE;
            outline.enabled = true;
            _activeGlowOutlines.Add(outline);

            if (isActiveAndEnabled)
            {
                _activeGlowCoroutines.Add(StartCoroutine(PulseOutline(outline)));

                // 2) 스케일 펄스 — Graphic 유무와 무관하게 항상 보이는 주 효과.
                //    localScale은 레이아웃 계산 이후 적용되므로 LayoutGroup 형제 재배치 없음.
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector3 baseScale = rt.localScale;
                    _activeGlowScales.Add((rt, baseScale));
                    _activeGlowCoroutines.Add(StartCoroutine(PulseScale(rt, baseScale)));
                }
            }
        }

        private void AddGlowToSkillList()
        {
            if (_slotAssignmentView == null) return;
            var attackCol  = _slotAssignmentView.SkillListAttackColumn;
            var defenseCol = _slotAssignmentView.SkillListDefenseColumn;
            if (attackCol != null)  AddGlow(attackCol.gameObject);
            if (defenseCol != null) AddGlow(defenseCol.gameObject);
        }

        private IEnumerator PulseOutline(Outline outline)
        {
            if (outline == null) yield break;
            float t = 0f;
            while (outline != null && outline.enabled)
            {
                t += Time.deltaTime / GLOW_PULSE_PERIOD;
                float wave  = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
                float alpha = Mathf.Lerp(GLOW_PULSE_MIN_ALPHA, 1f, wave);
                outline.effectColor = new Color(GLOW_COLOR.r, GLOW_COLOR.g, GLOW_COLOR.b, alpha);
                yield return null;
            }
        }

        private IEnumerator PulseScale(RectTransform rt, Vector3 baseScale)
        {
            if (rt == null) yield break;
            float t = 0f;
            // ClearAllGlows / OnDisable에서 StopCoroutine으로 종료됨
            while (rt != null)
            {
                t += Time.deltaTime / GLOW_PULSE_PERIOD;
                float wave  = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
                float scale = Mathf.Lerp(1f, GLOW_SCALE_MAX, wave);
                rt.localScale = baseScale * scale;
                yield return null;
            }
        }

        // ── View 내부 버튼 접근 ──────────────────────────────────────────

        private GameObject GetRollDiceButton()
        {
            if (_rollDiceButtonRef != null) return _rollDiceButtonRef;
            if (_diceView == null) return null;
            Transform root = _diceView.transform.root;
            var t = FindDescendantByName(root, "ActionButtonA");
            if (t == null) t = FindDescendantByName(root, "RollDiceButton");
            return t != null ? t.gameObject : null;
        }

        private GameObject GetRerollButton()
        {
            if (_diceView == null) return null;
            var t = FindDescendantByName(_diceView.transform, "RerollButton");
            if (t != null) return t.gameObject;
            if (_slotAssignmentView == null) return null;
            t = FindDescendantByName(_slotAssignmentView.transform, "RerollButton");
            return t != null ? t.gameObject : null;
        }

        private GameObject GetUseSkillButton()
        {
            if (_diceView == null) return null;
            var t = FindDescendantByName(_diceView.transform, "UseSkillButton");
            if (t != null) return t.gameObject;
            if (_slotAssignmentView == null) return null;
            t = FindDescendantByName(_slotAssignmentView.transform, "UseSkillButton");
            return t != null ? t.gameObject : null;
        }

        private GameObject GetExecuteButton()
        {
            if (_targetSelectionView == null) return null;
            var t = FindDescendantByName(_targetSelectionView.transform, "ExecuteButton");
            return t != null ? t.gameObject : null;
        }

        // ── 유틸리티 ────────────────────────────────────────────────────

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, System.Action onComplete = null)
        {
            if (cg == null) { onComplete?.Invoke(); yield break; }

            float startAlpha = cg.alpha;
            float elapsed = 0f;

            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / FADE_DURATION);
                yield return null;
            }

            cg.alpha = targetAlpha;
            _fadeCoroutine = null;
            onComplete?.Invoke();
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == targetName) return child;
                Transform inner = FindDescendantByName(child, targetName);
                if (inner != null) return inner;
            }
            return null;
        }

        private void OnDisable()
        {
            ClearAllGlows();
            if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
        }
    }
}
