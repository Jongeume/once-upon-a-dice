using System.Collections;
using System.Collections.Generic;
using OUD.Unity.Battle.View;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Tutorial
{
    /// <summary>
    /// 튜토리얼 가이드 패널 + 글로우 이펙트 제어.
    /// BattleScene Canvas 최상위에 배치하여 모든 UI 위에 표시.
    /// </summary>
    public class TutorialOverlayView : ViewBase
    {
        [Header("가이드 패널")]
        [SerializeField] private GameObject _guidePanel;
        [SerializeField] private TMP_Text   _guideText;
        [SerializeField] private CanvasGroup _guidePanelCanvasGroup;

        private const float FADE_DURATION = 0.3f;

        // 글로우 펄스 상수 — EnemyEntryView 패턴 재사용
        private static readonly Color GLOW_COLOR = new Color(0.95f, 0.78f, 0.18f, 1f);
        private const float GLOW_PULSE_PERIOD    = 1.0f;
        private const float GLOW_PULSE_MIN_ALPHA = 0.3f;

        // 글로우 대상 참조 — TutorialManager가 주입
        private DiceView            _diceView;
        private SlotAssignmentView  _slotAssignmentView;
        private TargetSelectionView _targetSelectionView;
        private List<DiceEntryView> _diceEntries;
        private List<EnemyEntryView> _enemyEntryViews;

        // 활성 글로우 코루틴 추적
        private readonly List<Coroutine> _activeGlowCoroutines = new();
        private readonly List<Outline>   _activeGlowOutlines   = new();

        private Coroutine _fadeCoroutine;

        // ── 초기화 ─────────────────────────────────────────────────────────

        /// <summary>글로우 대상 View 참조를 주입한다. TutorialManager.Begin()에서 호출.</summary>
        public void InjectViews(
            DiceView diceView,
            SlotAssignmentView slotAssignmentView,
            TargetSelectionView targetSelectionView,
            List<DiceEntryView> diceEntries,
            List<EnemyEntryView> enemyEntryViews)
        {
            _diceView            = diceView;
            _slotAssignmentView  = slotAssignmentView;
            _targetSelectionView = targetSelectionView;
            _diceEntries         = diceEntries;
            _enemyEntryViews     = enemyEntryViews;
        }

        // ── 가이드 텍스트 ──────────────────────────────────────────────────

        /// <summary>가이드 텍스트를 페이드인으로 표시.</summary>
        public void ShowGuide(string text)
        {
            if (_guideText != null) _guideText.text = text;
            if (_guidePanel != null) _guidePanel.SetActive(true);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_guidePanelCanvasGroup, 1f));
        }

        /// <summary>가이드 텍스트를 페이드아웃으로 숨김.</summary>
        public void HideGuide()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_guidePanelCanvasGroup, 0f, () =>
            {
                if (_guidePanel != null) _guidePanel.SetActive(false);
            }));
        }

        /// <summary>즉시 숨김 (전투 종료 시 등).</summary>
        public void HideGuideImmediate()
        {
            if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
            if (_guidePanelCanvasGroup != null) _guidePanelCanvasGroup.alpha = 0f;
            if (_guidePanel != null) _guidePanel.SetActive(false);
        }

        // ── 글로우 제어 ──────────────────────────────────────────────────

        /// <summary>지정 대상에 골드색 Outline 펄스를 시작.</summary>
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
                    AddOutlineGlow(GetRollDiceButton());
                    break;
                case GlowTarget.RerollButton:
                    AddOutlineGlow(GetRerollButton());
                    break;
                case GlowTarget.UseSkillButton:
                    AddOutlineGlow(GetUseSkillButton());
                    break;
                case GlowTarget.ExecuteButton:
                    AddOutlineGlow(GetExecuteButton());
                    break;
                case GlowTarget.DiceEntries:
                    if (_diceEntries != null)
                        foreach (var entry in _diceEntries)
                            if (entry != null) AddOutlineGlow(entry.gameObject);
                    break;
                case GlowTarget.SkillCards:
                    AddGlowToSkillCards();
                    break;
                case GlowTarget.EnemyCards:
                    if (_enemyEntryViews != null)
                        foreach (var entry in _enemyEntryViews)
                            if (entry != null && entry.gameObject.activeSelf)
                                AddOutlineGlow(entry.gameObject);
                    break;
            }
        }

        /// <summary>모든 글로우 이펙트 정지 + Outline 정리.</summary>
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
        }

        // ── 글로우 헬퍼 ──────────────────────────────────────────────────

        private void AddOutlineGlow(GameObject go)
        {
            if (go == null) return;

            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = true;
            _activeGlowOutlines.Add(outline);

            if (isActiveAndEnabled)
            {
                var co = StartCoroutine(PulseOutline(outline));
                _activeGlowCoroutines.Add(co);
            }
        }

        private void AddGlowToSkillCards()
        {
            if (_slotAssignmentView == null) return;
            // 기술 카드는 동적 생성이므로 공격/수비 컬럼 하위 자식을 탐색
            var columns = new Transform[]
            {
                FindDescendantByName(_slotAssignmentView.transform, "AttackColumn"),
                FindDescendantByName(_slotAssignmentView.transform, "DefenseColumn"),
            };
            foreach (var col in columns)
            {
                if (col == null) continue;
                for (int i = 0; i < col.childCount; i++)
                {
                    var child = col.GetChild(i);
                    if (child != null && child.gameObject.activeSelf)
                        AddOutlineGlow(child.gameObject);
                }
            }
        }

        /// <summary>Outline effectColor alpha를 코사인 파동으로 깜빡임.</summary>
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

        // ── View 내부 버튼 접근 ──────────────────────────────────────────

        private GameObject GetRollDiceButton()
        {
            if (_diceView == null) return null;
            // BattleBootstrapper의 _rollDiceButton은 BattlePanel 하위에 있음
            // DiceView 상위 Canvas에서 "ActionButtonA" 또는 "RollDiceButton" 탐색
            Transform root = _diceView.transform.root;
            var t = FindDescendantByName(root, "ActionButtonA");
            return t != null ? t.gameObject : null;
        }

        private GameObject GetRerollButton()
        {
            if (_diceView == null) return null;
            var t = FindDescendantByName(_diceView.transform, "RerollButton");
            if (t != null) return t.gameObject;
            // SlotAssignmentView에도 리롤 버튼이 있음
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
