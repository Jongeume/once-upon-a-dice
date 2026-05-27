using System.Collections;
using System.Collections.Generic;
using OUD.Unity.Adapter;
using OUD.Unity.Battle.View;
using UnityEngine;

namespace OUD.Unity.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        private BattleUIAdapter     _adapter;
        private TutorialOverlayView _overlayView;

        private TutorialStep[] _steps;
        private int            _currentStepIndex;
        private bool           _isActive;
        private bool           _waitingForTrigger;
        private Coroutine      _autoAdvanceCoroutine;

        private bool _enemyShieldedThisBattle;

        private readonly HashSet<string> _receivedEvents = new();

        // ── 14단계 Step 정의 (설계 문서 v2 기준) ──────────────────────────

        private static TutorialStep[] BuildSteps()
        {
            return new[]
            {
                // Step 0: 전투 시작 — 적 의도 확인
                new TutorialStep(
                    "적의 의도를 확인하세요! 검 아이콘은 공격, 방패 아이콘은 수비입니다",
                    TutorialTrigger.Auto,
                    new[] { GlowTarget.EnemyCards },
                    delayBefore: 0.5f),

                // Step 1: 적 공격/방어 안내
                new TutorialStep(
                    "적들의 공격을 방어하거나, 먼저 물리치세요!",
                    TutorialTrigger.Auto,
                    delayBefore: 2.0f),

                // Step 2: Roll Dice 안내
                new TutorialStep(
                    "Roll Dice 버튼을 눌러 주사위를 굴리세요",
                    TutorialTrigger.ButtonClicked,
                    new[] { GlowTarget.RollDiceButton },
                    expectedEvent: "DiceRolled"),

                // Step 3: Reroll 안내
                new TutorialStep(
                    "리롤 버튼을 눌러 주사위를 다시 굴리세요",
                    TutorialTrigger.ButtonClicked,
                    new[] { GlowTarget.RerollButton },
                    expectedEvent: "DiceRolled"),

                // Step 4: Keep 안내
                new TutorialStep(
                    "원하는 주사위를 터치해서 Keep하세요",
                    TutorialTrigger.DiceKept,
                    new[] { GlowTarget.DiceEntries }),

                // Step 5: 족보 안내
                new TutorialStep(
                    "같은 숫자가 모이면 스킬이 활성화됩니다!",
                    TutorialTrigger.Auto,
                    delayBefore: 1.5f),

                // Step 6: 스킬 선택 안내
                new TutorialStep(
                    "활성화된 기술을 선택하세요!",
                    TutorialTrigger.SkillSelected,
                    new[] { GlowTarget.SkillList }),

                // Step 7: 리롤/슬롯 유도
                new TutorialStep(
                    "남은 리롤을 모두 사용하거나 기술 슬롯을 채우세요!",
                    TutorialTrigger.RerollExhaustedOrSlotsFull,
                    new[] { GlowTarget.RerollButton }),

                // Step 8: Use Skill 버튼 안내
                new TutorialStep(
                    "기술 사용 버튼을 눌러 적들을 물리치러 가세요!",
                    TutorialTrigger.ButtonClicked,
                    new[] { GlowTarget.UseSkillButton },
                    expectedEvent: "UseSkillClicked"),

                // Step 9: 타겟 지정 안내
                new TutorialStep(
                    "공격할 적을 선택하세요!",
                    TutorialTrigger.TargetSelected,
                    new[] { GlowTarget.EnemyCards }),

                // Step 10: Execute 안내
                new TutorialStep(
                    "Execute 버튼으로 기술을 발동하세요!",
                    TutorialTrigger.ButtonClicked,
                    new[] { GlowTarget.ExecuteButton },
                    expectedEvent: "ExecuteClicked"),

                // Step 11: 적 쉴드 안내 (조건부 — End Turn 후 적 쉴드 보유 시)
                new TutorialStep(
                    "적이 방어를 올렸습니다! 쉴드를 먼저 깎아야 합니다",
                    TutorialTrigger.TurnStartedIfShielded),

                // Step 12: 자유 플레이 (가이드 없음, 승리 대기)
                new TutorialStep(
                    "",
                    TutorialTrigger.BattleWon),

                // Step 13: 승리 축하
                new TutorialStep(
                    "축하합니다! 튜토리얼을 완료했습니다!",
                    TutorialTrigger.Auto,
                    delayBefore: 2.0f),
            };
        }

        // ── 초기화 & 시작 ──────────────────────────────────────────────────

        public void Begin(BattleUIAdapter adapter, TutorialOverlayView overlayView,
            GameObject rollDiceButton = null)
        {
            _adapter    = adapter;
            _overlayView = overlayView;
            _steps = BuildSteps();
            _currentStepIndex = 0;
            _isActive = true;
            _waitingForTrigger = false;
            _enemyShieldedThisBattle = false;

            _overlayView.InjectViews(
                _adapter.DiceViewRef,
                _adapter.SlotAssignmentViewRef,
                _adapter.TargetSelectionViewRef,
                _adapter.DiceEntries,
                _adapter.GetEnemyEntryViews(),
                rollDiceButton);

            _adapter.OnTutorialEvent += HandleTutorialEvent;

            Debug.Log("[TutorialManager] 튜토리얼 시작 — 14단계 가이드 시퀀스");

            ExecuteCurrentStep();
        }

        private void OnDestroy()
        {
            if (_adapter != null)
                _adapter.OnTutorialEvent -= HandleTutorialEvent;
        }

        // ── Step 실행 ──────────────────────────────────────────────────────

        private void ExecuteCurrentStep()
        {
            if (!_isActive) return;
            if (_currentStepIndex >= _steps.Length)
            {
                FinishTutorial();
                return;
            }

            TutorialStep step = _steps[_currentStepIndex];

            // TurnStartedIfShielded: 가이드를 바로 표시하지 않고 이벤트 대기
            if (step.Trigger == TutorialTrigger.TurnStartedIfShielded)
            {
                _overlayView.ClearAllGlows();
                if (CheckBufferedEvents(step))
                {
                    HandleShieldStep(step);
                    return;
                }
                _waitingForTrigger = true;
                return;
            }

            _overlayView.ClearAllGlows();

            if (!string.IsNullOrEmpty(step.GuideText))
            {
                if (step.DelayBefore > 0f)
                {
                    _autoAdvanceCoroutine = StartCoroutine(DelayedShowGuide(step));
                    return;
                }
                _overlayView.ShowGuide(step.GuideText);
            }

            ApplyGlowAndWait(step);
        }

        private IEnumerator DelayedShowGuide(TutorialStep step)
        {
            yield return new WaitForSeconds(step.DelayBefore);
            if (!_isActive) yield break;

            if (!string.IsNullOrEmpty(step.GuideText))
                _overlayView.ShowGuide(step.GuideText);

            ApplyGlowAndWait(step);
        }

        private void ApplyGlowAndWait(TutorialStep step)
        {
            foreach (var target in step.GlowTargets)
                _overlayView.SetGlow(target, true);

            if (step.Trigger == TutorialTrigger.Auto)
            {
                float autoDelay = string.IsNullOrEmpty(step.GuideText) ? 1.0f : 2.0f;
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvance(autoDelay));
            }
            else
            {
                if (CheckBufferedEvents(step))
                {
                    StartCoroutine(FadeOutThenAdvance());
                    return;
                }
                _waitingForTrigger = true;
            }
        }

        private bool CheckBufferedEvents(TutorialStep step)
        {
            switch (step.Trigger)
            {
                case TutorialTrigger.ButtonClicked:
                    if (step.ExpectedEvent != null)
                        return _receivedEvents.Remove(step.ExpectedEvent);
                    if (_receivedEvents.Remove("DiceRolled")) return true;
                    if (_receivedEvents.Remove("UseSkillClicked")) return true;
                    if (_receivedEvents.Remove("ExecuteClicked")) return true;
                    return false;
                case TutorialTrigger.DiceKept:
                    return _receivedEvents.Remove("DiceKept");
                case TutorialTrigger.SkillSelected:
                    return _receivedEvents.Remove("SkillSelected");
                case TutorialTrigger.TargetSelected:
                    return _receivedEvents.Remove("TargetSelected");
                case TutorialTrigger.BattleWon:
                    return _receivedEvents.Remove("BattleWon");
                case TutorialTrigger.RerollExhaustedOrSlotsFull:
                    if (_receivedEvents.Remove("RerollsExhausted")) return true;
                    if (_receivedEvents.Remove("AllSlotsFilled")) return true;
                    return false;
                case TutorialTrigger.TurnStartedIfShielded:
                    return _receivedEvents.Remove("PlayerTurnStarted");
                default:
                    return false;
            }
        }

        private IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!_isActive) yield break;

            _overlayView.HideGuide();
            _overlayView.ClearAllGlows();
            _autoAdvanceCoroutine = null;
            Advance();
        }

        private IEnumerator FadeOutThenAdvance()
        {
            _waitingForTrigger = false;
            _overlayView.HideGuide();
            _overlayView.ClearAllGlows();
            yield return new WaitForSeconds(TutorialOverlayView.FADE_DURATION);
            if (!_isActive) yield break;
            Advance();
        }

        private void Advance()
        {
            _waitingForTrigger = false;
            _currentStepIndex++;

            if (_currentStepIndex >= _steps.Length)
            {
                FinishTutorial();
                return;
            }

            ExecuteCurrentStep();
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────────────────

        private void HandleTutorialEvent(string eventName)
        {
            if (!_isActive) return;

            _receivedEvents.Add(eventName);

            if (eventName == "EnemyShielded")
                _enemyShieldedThisBattle = true;

            if (!_waitingForTrigger) return;
            if (_currentStepIndex >= _steps.Length) return;

            TutorialStep step = _steps[_currentStepIndex];
            bool matched = false;

            switch (step.Trigger)
            {
                case TutorialTrigger.ButtonClicked:
                    if (step.ExpectedEvent != null)
                        matched = eventName == step.ExpectedEvent;
                    else
                        matched = eventName == "DiceRolled"
                               || eventName == "UseSkillClicked"
                               || eventName == "ExecuteClicked";
                    break;

                case TutorialTrigger.DiceKept:
                    matched = eventName == "DiceKept";
                    break;

                case TutorialTrigger.SkillSelected:
                    matched = eventName == "SkillSelected";
                    break;

                case TutorialTrigger.TargetSelected:
                    matched = eventName == "TargetSelected";
                    break;

                case TutorialTrigger.BattleWon:
                    matched = eventName == "BattleWon";
                    break;

                case TutorialTrigger.RerollExhaustedOrSlotsFull:
                    matched = eventName == "RerollsExhausted"
                           || eventName == "AllSlotsFilled";
                    break;

                case TutorialTrigger.TurnStartedIfShielded:
                    if (eventName == "PlayerTurnStarted")
                    {
                        _receivedEvents.Remove(eventName);
                        HandleShieldStep(step);
                        return;
                    }
                    break;
            }

            if (matched)
            {
                _receivedEvents.Remove(eventName);
                StartCoroutine(FadeOutThenAdvance());
            }
        }

        private void HandleShieldStep(TutorialStep step)
        {
            _waitingForTrigger = false;
            if (_enemyShieldedThisBattle)
            {
                _overlayView.ShowGuide(step.GuideText);
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvance(2.0f));
            }
            else
            {
                Debug.Log($"[TutorialManager] Step {_currentStepIndex} 스킵 — 적 쉴드 없음");
                Advance();
            }
        }

        // ── 완료 ──────────────────────────────────────────────────────────

        private void FinishTutorial()
        {
            _isActive = false;
            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }
            _overlayView.HideGuideImmediate();
            _overlayView.ClearAllGlows();

            TutorialState.SetCompleted();
            Debug.Log("[TutorialManager] 튜토리얼 완료!");

            if (_adapter != null)
                _adapter.OnTutorialEvent -= HandleTutorialEvent;
        }
    }
}
