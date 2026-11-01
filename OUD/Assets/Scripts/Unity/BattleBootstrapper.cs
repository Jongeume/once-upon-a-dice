// BattleBootstrapper.cs
// Play 버튼 하나로 BattleEngine 전체 흐름을 자동 검증하는 개발용 MonoBehaviour.
// 씬에 하나만 배치. DebugBattleUI가 Console에 전투 로그를 출력한다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Dice;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;
using OUD.Unity.Adapter;

namespace OUD.Unity
{
    /// <summary>
    /// Sprint-0 검증용 부트스트래퍼.
    /// Slime×2 + Skeleton 인카운터를 자동으로 진행하며
    /// Unity Console에서 전투 이벤트를 실시간으로 확인할 수 있다.
    /// </summary>
    public class BattleBootstrapper : MonoBehaviour
    {
        [Header("자동 진행 딜레이 (초)")]
        [SerializeField] private float _stepDelay = 0.5f;

        private TurnManager   _turnManager;
        private BattleState   _state;
        private DebugBattleUI _debugUI;

        private void Start()
        {
            InitBattle();
            StartCoroutine(AutoPlayLoop());
        }

        // ── 초기화 ────────────────────────────────────────────────────────────

        private void InitBattle()
        {
            // BattleEngine 객체 조립 (IRandom → DiceHand → BattleState → TurnManager)
            IRandom      random   = new UnityRandom();
            PlayerState  player   = new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));
            List<MonsterInstance> enemies = MonsterDatabase.CreateSprint0Encounter();
            DiceHand     diceHand = new DiceHand(random);

            _state       = new BattleState(player, enemies, diceHand);
            _debugUI     = new DebugBattleUI();
            _turnManager = new TurnManager(_state, _debugUI);

            Debug.Log("[BattleBootstrapper] 초기화 완료 — Play 버튼으로 전투가 자동 진행됩니다.");
            Debug.Log("  인카운터: Slime(HP:20)×2 + Skeleton(HP:25)");
            Debug.Log("  플레이어: HP:60 ATK:6 DEF:5  |  기본 해금 족보: OnePair/TwoPair/Triple/FullHouse");
        }

        // ── 자동 진행 코루틴 ──────────────────────────────────────────────────

        /// <summary>
        /// 전투를 단계별로 자동 진행한다.
        /// DebugBattleUI의 플래그를 폴링하여 다음 행동을 결정한다.
        /// </summary>
        private IEnumerator AutoPlayLoop()
        {
            yield return new WaitForSeconds(_stepDelay);
            _turnManager.StartBattle();

            const int MAX_ITERATIONS = 300; // 안전 카운터 (무한 루프 방지)
            int iteration = 0;

            while (!_debugUI.BattleOver && iteration < MAX_ITERATIONS)
            {
                iteration++;

                if (_debugUI.DiceRolled)
                {
                    // 주사위 결과를 확인 후 즉시 확정 (리롤 없이)
                    _debugUI.ClearDiceRolled();
                    yield return new WaitForSeconds(_stepDelay);
                    _turnManager.ConfirmDice();
                    // ConfirmDice 이후: 잡패면 DiceRolled=true, 족보면 SlotAssignmentPending=true
                }
                else if (_debugUI.SlotAssignmentPending)
                {
                    // 가용 스킬을 슬롯에 자동 배분 후 실행
                    _debugUI.ClearSlotAssignmentPending();
                    yield return new WaitForSeconds(_stepDelay);
                    List<SlotAssignment> slots = AutoAssignSlots(
                        _debugUI.PendingSkills,
                        _debugUI.PendingEnemies);
                    _debugUI.PendingCallback(slots);
                    // 콜백 이후: DiceRolled=true 또는 BattleOver=true
                }
                else
                {
                    yield return null; // 다음 프레임에서 재확인
                }
            }

            if (iteration >= MAX_ITERATIONS)
                Debug.LogError("[BattleBootstrapper] 안전 카운터 초과 — 무한 루프 방지로 중단됨");
            else
                Debug.Log($"[BattleBootstrapper] 전투 종료. 반복 횟수: {iteration}");
        }

        // ── 슬롯 자동 배분 ────────────────────────────────────────────────────

        /// <summary>
        /// 가용 스킬을 슬롯에 자동 배분한다 (최대 3슬롯).
        /// 공격(Single) → 첫 번째 생존 적 지정
        /// 공격(AllEnemies) / 수비(Self) → targetIndex = -1
        /// </summary>
        private List<SlotAssignment> AutoAssignSlots(
            List<SkillData>      skills,
            List<MonsterInstance> aliveEnemies)
        {
            var slots = new List<SlotAssignment>();
            int count = Mathf.Min(skills.Count, SlotManager.MAX_SLOTS);

            for (int i = 0; i < count; i++)
            {
                SkillData skill       = skills[i];
                int       targetIndex = -1;

                if (skill.Category == SkillCategory.Attack &&
                    skill.Target   == TargetType.Single    &&
                    aliveEnemies.Count > 0)
                {
                    // aliveEnemies의 원소는 _state.Enemies와 동일 참조 → IndexOf 사용 가능
                    targetIndex = _state.Enemies.IndexOf(aliveEnemies[0]);
                }

                slots.Add(new SlotAssignment { Skill = skill, TargetIndex = targetIndex });
            }

            var log = new System.Text.StringBuilder("[슬롯 자동 배분]");
            foreach (var s in slots)
                log.Append($" | {s.Skill.Name}→[{s.TargetIndex}]");
            Debug.Log(log.ToString());

            return slots;
        }
    }
}
