// TurnManager.cs
// 전투 루프 오케스트레이터. 페이즈 순환 및 이벤트 발행을 담당.
// 데이터 흐름: Unity → TurnManager(BattleEngine) → IBattleUI → (Unity)Presenter → View
// feature-spec F-05, game-design-v2.2 §2.1
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Dice;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Combat
{
    /// <summary>
    /// 전투 1회의 턴 순환을 관리한다.
    /// BattleState를 읽고 쓰며, IBattleUI를 통해 Unity에 상태 변화를 알린다.
    ///
    /// Unity 호출 순서 (정상 턴):
    ///   StartBattle()
    ///   → StartPlayerTurn()            ← 자동 호출
    ///   → [Unity] RequestReroll() × n  ← 플레이어가 리롤할 때마다
    ///   → [Unity] ConfirmDice()        ← 플레이어가 주사위 확정 시
    ///   → RequestSlotAssignment 콜백 대기
    ///   → [Unity] ExecuteSlots()       ← 콜백으로 자동 호출
    ///   → ExecuteEnemyTurn()           ← 자동 호출
    ///   → EndTurn()                    ← 자동 호출 (Phase=TurnEnd로만 전환, 다음 턴은 Roll Dice 버튼이 호출)
    ///   → [Unity] StartPlayerTurn()    ← Roll Dice 버튼 클릭 시 호출
    /// </summary>
    public class TurnManager
    {
        private readonly BattleState   _state;
        private readonly IBattleUI     _ui;
        private readonly SkillExecutor _executor;
        private readonly SlotManager   _slotManager;

        public TurnManager(BattleState state, IBattleUI ui)
        {
            _state       = state;
            _ui          = ui;
            _executor    = new SkillExecutor();
            _slotManager = new SlotManager();
        }

        /// <summary>현재 전투 페이즈. 뒤로가기/Roll Dice 토글 분기 등 외부에서 상태 판단용.</summary>
        public BattlePhase CurrentPhase => _state.Phase;

        /// <summary>
        /// [DEBUG] 디버그 단축키용 — 모든 적을 즉시 사망 처리 후 OnBattleWon 트리거.
        /// 정상 전투 종료 흐름(EnemyTurn/EndTurn)을 건너뛴다. Boss 노드면 클리어, 일반 노드면 보상 흐름 진입.
        /// </summary>
        public void ForceWin()
        {
            if (_state.Phase == BattlePhase.BattleWon || _state.Phase == BattlePhase.BattleLost) return;

            foreach (var enemy in _state.Enemies)
            {
                if (!enemy.IsDead) enemy.TakeDamage(int.MaxValue);
            }
            _state.Phase = BattlePhase.BattleWon;
            _ui.OnBattleWon();
        }

        // ── 전투 시작 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 전투를 시작한다. 초기 상태를 UI에 전달하고 Screen A를 표시한다.
        /// StartPlayerTurn()은 Unity Layer(Roll Dice 버튼)가 명시적으로 호출한다.
        /// </summary>
        public void StartBattle()
        {
            _state.Phase = BattlePhase.BattleStart;
            _ui.OnBattleStart(_state.Player, _state.Enemies);
            // StartPlayerTurn() 제거 — Roll Dice 버튼 클릭 시 BattleBootstrapper가 호출
        }

        // ── 플레이어 턴 ───────────────────────────────────────────────────────

        /// <summary>
        /// 플레이어 턴을 시작한다. 턴 상태 초기화 후 주사위를 굴리고
        /// 족보를 즉시 평가해 기술 목록을 UI에 전달.
        /// Unity는 이후 RequestReroll() 또는 기술 사용 버튼을 눌러 ConfirmDice()를 호출한다.
        /// </summary>
        public void StartPlayerTurn()
        {
            _state.ResetForNewTurn();
            _state.Phase = BattlePhase.DiceRoll;
            _ui.OnPlayerTurnStarted();
            _state.DiceHand.RollAll();
            EvaluateAndBroadcast();
        }

        /// <summary>
        /// 리롤 요청. keepMask[i]=true이면 i번 주사위를 잠금 유지.
        /// 리롤 가능하면 true 반환 후 OnDiceRolled + 재평가 호출.
        /// 리롤 횟수 소진이면 false 반환.
        /// </summary>
        public bool RequestReroll(bool[] keepMask)
        {
            if (_state.Phase != BattlePhase.DiceRoll) return false;

            // keepMask 적용
            var dices = _state.DiceHand.Dices;
            for (int i = 0; i < keepMask.Length && i < dices.Length; i++)
                dices[i].SetKept(keepMask[i]);

            bool success = _state.DiceHand.Reroll();
            if (success)
                EvaluateAndBroadcast(); // 리롤 후 재평가 + 기술 목록 갱신
            return success;
        }

        /// <summary>
        /// 주사위 평가 + UI 통보 공통 메서드.
        /// StartPlayerTurn / RequestReroll 모두 사용.
        /// 기술이 있으면 RequestSlotAssignment 즉시 호출(phase 유지로 리롤 가능).
        /// 잡패이면 phase=DiceRoll 유지 → UI가 UseSkill 버튼 즉시 표시.
        /// </summary>
        private void EvaluateAndBroadcast()
        {
            int[] values = _state.DiceHand.GetValues();
            _state.AchievedHands = HandEvaluator.Evaluate(values);

            var usableSkills = SkillDatabase.GetUsableSkills(
                _state.AchievedHands,
                _state.Player.UnlockedHands,
                _state.UsedHandsThisTurn);

            _ui.OnDiceRolled(values, _state.DiceHand.RerollsLeft);
            _ui.OnHandsEvaluated(_state.AchievedHands, usableSkills);

            if (usableSkills.Count > 0)
            {
                // phase는 DiceRoll 유지 → 리롤 여전히 가능
                // 배운 모든 스킬은 항상 카드로 표시(비활성 포함), usable은 현재 활성화 대상.
                var allLearned = SkillDatabase.GetSkillsByUnlockedHands(_state.Player.UnlockedHands);
                _ui.RequestSlotAssignment(
                    usableSkills,
                    allLearned,
                    _state.AliveEnemies,
                    ExecuteSlots);
            }
            // 잡패: RequestSlotAssignment 호출하지 않음
            // → BattleUIAdapter가 _hasUsableSkills=false 감지, UseSkill 버튼 즉시 표시
        }

        /// <summary>
        /// 기술 사용 버튼 클릭 시 Unity에서 호출.
        /// 잡패(usableSkills=0): 적 실드 초기화 → 적 턴으로 진행.
        /// 정상: phase를 SlotAssignment로 전환해 추가 리롤을 차단.
        ///        SlotAssignment는 이미 EvaluateAndBroadcast에서 표시됨.
        /// feature-spec F-05 §3, §4
        /// </summary>
        public void ConfirmDice()
        {
            if (_state.Phase != BattlePhase.DiceRoll) return;

            // 잡패 판정
            var usableSkills = SkillDatabase.GetUsableSkills(
                _state.AchievedHands,
                _state.Player.UnlockedHands,
                _state.UsedHandsThisTurn);

            if (usableSkills.Count == 0)
            {
                // 잡패: 슬롯 배분 건너뜀 → 적 실드 초기화 → 적 턴
                _state.Phase = BattlePhase.HandEvaluation;
                ResetEnemyShieldsAndNotify();
                ExecuteEnemyTurn();
                return;
            }

            // 정상: 리롤 차단을 위해 페이즈 전환
            // RequestSlotAssignment + Begin()은 EvaluateAndBroadcast에서 이미 호출됨
            // ExecuteSlots 콜백도 이미 등록됨 → 여기선 재호출 불필요
            _state.Phase = BattlePhase.SlotAssignment;
        }

        // ── 슬롯 실행 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 슬롯 배분 결과를 받아 1→2→3 순서로 실행한다.
        /// RequestSlotAssignment의 onComplete 콜백으로 호출된다.
        /// feature-spec F-05 §5, §5a, §6
        /// </summary>
        public void ExecuteSlots(List<SlotAssignment> slots)
        {
            _state.Phase = BattlePhase.SlotExecution;
            if (slots == null) slots = new List<SlotAssignment>();

            for (int i = 0; i < slots.Count; i++)
            {
                SlotAssignment slot = slots[i];
                if (slot?.Skill == null) continue; // 빈 슬롯: 아무 행동 안 함

                SkillData    skill  = slot.Skill;
                SkillResult  result;

                if (skill.Category == SkillCategory.Attack)
                {
                    // 대상 사망 여부는 SkillExecutor 내부에서 처리 (허공 소멸)
                    MonsterInstance target = ResolveTarget(slot.TargetIndex);
                    result = _executor.ExecuteAttack(
                        skill,
                        _state.Player.GetEnhanceLevel(skill.Hand),
                        _state.Player.Atk,
                        target,
                        _state.Enemies);
                }
                else
                {
                    result = _executor.ExecuteDefense(
                        skill,
                        _state.Player.GetEnhanceLevel(skill.Hand),
                        _state.Player.Def,
                        _state.Player);
                }

                // 족보 사용 표시 (동일 족보 1회 제한)
                _state.UsedHandsThisTurn.Add(skill.Hand);
                _ui.OnSlotExecuted(i, result);
            }

            // 5a: 플레이어 턴 종료 → 적 실드 초기화
            ResetEnemyShieldsAndNotify();

            // 6: 적 전원 사망 체크 → 승리
            if (_state.AreAllEnemiesDead)
            {
                _state.Phase = BattlePhase.BattleWon;
                _ui.OnBattleWon();
                return;
            }

            ExecuteEnemyTurn();
        }

        // ── 적 턴 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 적 턴을 실행한다.
        /// 순서: 분노 체크 → 행동 실행 → 플레이어 실드 초기화 → 사망 체크.
        /// feature-spec F-05 §7~§9, F-06, F-07
        /// </summary>
        public void ExecuteEnemyTurn()
        {
            _state.Phase = BattlePhase.EnemyTurn;

            for (int i = 0; i < _state.Enemies.Count; i++)
            {
                MonsterInstance enemy = _state.Enemies[i];
                if (enemy.IsDead) continue;

                // 7a: 분노 체크 (보스 전용, 일반 몬스터는 HasRage=false이므로 무시)
                enemy.CheckRage();

                // Intent 캡처 (행동 전에 저장해야 표시와 실행이 일치)
                IntentType intent = enemy.GetCurrentIntent();
                int        value  = enemy.GetIntentValue();

                // 행동 실행
                ExecuteEnemyAction(enemy, intent, value);

                _ui.OnEnemyAction(i, intent, value);

                // 패턴 전진 (다음 Intent 결정)
                enemy.AdvancePattern();
            }

            // 적 턴 행동 완료 후 — 다음 턴 의도 UI 갱신
            for (int i = 0; i < _state.Enemies.Count; i++)
            {
                MonsterInstance enemy = _state.Enemies[i];
                if (enemy.IsDead) continue;
                _ui.OnIntentUpdated(i, enemy.GetCurrentIntent(), enemy.GetIntentValue());
            }

            // 8: 플레이어 실드 초기화 (적 �� 종료)
            _state.Player.ResetShield();
            _ui.OnShieldsReset();

            // 9: 플레이어 사망 체크
            if (_state.IsPlayerDead)
            {
                _state.Phase = BattlePhase.BattleLost;
                _ui.OnBattleLost();
                return;
            }

            EndTurn();
        }

        // ── 턴 종료 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 턴을 종료하고 다음 플레이어 턴을 시작한다.
        /// 연출 지연이 필요한 경우 Unity가 이 메서드를 직접 호출해 타이밍을 조절할 수 있다.
        /// </summary>
        public void EndTurn()
        {
            _state.Phase = BattlePhase.TurnEnd;
            // 다음 턴은 Unity Layer(Roll Dice 버튼)가 StartPlayerTurn()을 명시 호출 → Screen A에 머무름
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        /// <summary>
        /// TargetIndex로 MonsterInstance를 조회한다.
        /// 범위 이탈 또는 사망한 적이면 null 반환 → SkillExecutor가 허공 소멸 처리.
        /// </summary>
        private MonsterInstance ResolveTarget(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _state.Enemies.Count) return null;
            MonsterInstance target = _state.Enemies[targetIndex];
            return target.IsDead ? null : target;
        }

        /// <summary>
        /// 적 1체의 Intent에 따라 행동을 실행한다.
        /// Attack/StrongAttack → 플레이어 TakeDamage
        /// Shield → 적 자신 GainShield
        /// RageWarning → 경고 표시용. 실제 행동은 Attack으로 처리.
        /// </summary>
        private void ExecuteEnemyAction(MonsterInstance enemy, IntentType intent, int value)
        {
            switch (intent)
            {
                case IntentType.Attack:
                case IntentType.StrongAttack:
                case IntentType.RageWarning:
                    // RageWarning은 경고 아이콘만 다를 뿐 행동은 공격과 동일
                    _state.Player.TakeDamage(value);
                    break;

                case IntentType.Shield:
                    enemy.GainShield(value);
                    break;
            }
        }

        /// <summary>
        /// 살아있는 적의 실드를 전부 초기화하고 UI에 알린다.
        /// 호출 시점: 플레이어 턴 종료 직후 (슬롯 실행 완료 또는 잡패).
        /// </summary>
        private void ResetEnemyShieldsAndNotify()
        {
            foreach (MonsterInstance enemy in _state.Enemies)
                enemy.ResetShield();
            _ui.OnShieldsReset();
        }
    }
}
