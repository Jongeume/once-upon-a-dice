using System.Collections;
using System.Collections.Generic;
using OUD.Unity.Adapter;
using OUD.Unity.Battle.View;
using UnityEngine;

namespace OUD.Unity.Tutorial
{
    /// <summary>
    /// 튜토리얼 진행 관리자.
    /// BattleUIAdapter 이벤트를 구독하여 TutorialStep[] 순차 실행.
    /// 기존 전투 코드를 수정하지 않고 관찰자로 동작한다.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        private BattleUIAdapter     _adapter;
        private TutorialOverlayView _overlayView;

        private TutorialStep[] _steps;
        private int            _currentStepIndex;
        private bool           _isActive;
        private bool           _waitingForTrigger;
        private Coroutine      _autoAdvanceCoroutine;

        // Step 10 조건부 — 적이 실제로 쉴드를 획득한 경우에만 표시
        private bool _enemyShieldedThisBattle;

        // ── 13단계 Step 정의 (설계 문서 기준) ────────────────────────────────

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
                    TutorialTrigger.ButtonClicked, // DiceRolled 이벤트로 진행
                    new[] { GlowTarget.RollDiceButton }),

                // Step 3: Reroll 안내
                new TutorialStep(
                    "Reroll 버튼으로 주사위를 다시 굴릴 수 있습니다",
                    TutorialTrigger.ButtonClicked, // DiceRolled (reroll) 이벤트로 진행
                    new[] { GlowTarget.RerollButton }),

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
                    new[] { GlowTarget.SkillCards }),

                // Step 7: Use Skill 버튼 안내
                new TutorialStep(
                    "기술사용 버튼을 누르세요",
                    TutorialTrigger.ButtonClicked, // UseSkillClicked 이벤트로 진행
                    new[] { GlowTarget.UseSkillButton }),

                // Step 8: 타겟 지정 안내
                new TutorialStep(
                    "공격할 적을 선택하세요!",
                    TutorialTrigger.TargetSelected,
                    new[] { GlowTarget.EnemyCards }),

                // Step 9: Execute 안내
                new TutorialStep(
                    "Execute 버튼으로 기술을 발동하세요!",
                    TutorialTrigger.ButtonClicked, // ExecuteClicked 이벤트로 진행
                    new[] { GlowTarget.ExecuteButton }),

                // Step 10: 적 쉴드 안내 (조건부)
                new TutorialStep(
                    "적이 방어를 올렸습니다! 쉴드를 먼저 깎아야 합니다",
                    TutorialTrigger.Auto,
                    delayBefore: 2.0f,
                    isConditional: true),

                // Step 11: 자유 플레이 (가이드 없음, 승리 대기)
                new TutorialStep(
                    "",
                    TutorialTrigger.BattleWon),

                // Step 12: 승리 축하
                new TutorialStep(
                    "축하합니다! 튜토리얼을 완료했습니다!",
                    TutorialTrigger.Auto,
                    delayBefore: 2.0f),
            };
        }

        // ── 초기화 & 시작 ──────────────────────────────────────────────────

        /// <summary>BattleBootstrapper에서 호출. 튜토리얼 시퀀스를 시작한다.</summary>
        public void Begin(BattleUIAdapter adapter, TutorialOverlayView overlayView)
        {
            _adapter    = adapter;
            _overlayView = overlayView;
            _steps = BuildSteps();
            _currentStepIndex = 0;
            _isActive = true;
            _waitingForTrigger = false;
            _enemyShieldedThisBattle = false;

            // View 참조 주입
            _overlayView.InjectViews(
                _adapter.DiceViewRef,
                _adapter.SlotAssignmentViewRef,
                _adapter.TargetSelectionViewRef,
                _adapter.DiceEntries,
                _adapter.GetEnemyEntryViews());

            // 이벤트 구독
            _adapter.OnTutorialEvent += HandleTutorialEvent;

            Debug.Log("[TutorialManager] 튜토리얼 시작 — 13단계 가이드 시퀀스");
        }

        private void OnDestroy()
        {
            if (_adapter != null)
                _adapter.OnTutorialEvent -= HandleTutorialEvent;
        }

        // ── Step 실행 ──────────────────────────────────────────────────────

        /// <summary>현재 Step 가이드 표시 + 글로우 시작 + 트리거 대기.</summary>
        private void ExecuteCurrentStep()
        {
            if (!_isActive) return;
            if (_currentStepIndex >= _steps.Length)
            {
                FinishTutorial();
                return;
            }

            TutorialStep step = _steps[_currentStepIndex];

            // 조건부 Step 10: 적이 쉴드를 획득하지 않았으면 스킵
            if (step.IsConditional && !_enemyShieldedThisBattle)
            {
                Debug.Log($"[TutorialManager] Step {_currentStepIndex} 스킵 — 조건 미충족 (적 쉴드 없음)");
                _currentStepIndex++;
                ExecuteCurrentStep();
                return;
            }

            // 가이드 텍스트 표시
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
            // 글로우 적용
            foreach (var target in step.GlowTargets)
                _overlayView.SetGlow(target, true);

            // Auto 트리거: 딜레이 후 자동 진행
            if (step.Trigger == TutorialTrigger.Auto)
            {
                float autoDelay = string.IsNullOrEmpty(step.GuideText) ? 1.0f : 2.0f;
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvance(autoDelay));
            }
            else
            {
                _waitingForTrigger = true;
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

        /// <summary>다음 Step으로 진행.</summary>
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

            // BattleStart 이벤트 → 첫 Step 실행
            if (eventName == "BattleStart" && _currentStepIndex == 0)
            {
                ExecuteCurrentStep();
                return;
            }

            // EnemyShielded 플래그 추적
            if (eventName == "EnemyShielded")
                _enemyShieldedThisBattle = true;

            // 트리거 대기 중이 아니면 무시
            if (!_waitingForTrigger) return;
            if (_currentStepIndex >= _steps.Length) return;

            TutorialStep step = _steps[_currentStepIndex];
            bool matched = false;

            switch (step.Trigger)
            {
                case TutorialTrigger.ButtonClicked:
                    // Step 2: RollDice → DiceRolled
                    // Step 3: Reroll → DiceRolled
                    // Step 7: UseSkill → UseSkillClicked
                    // Step 9: Execute → ExecuteClicked
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
            }

            if (matched)
            {
                _overlayView.HideGuide();
                _overlayView.ClearAllGlows();
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
