// BattleBootstrapper.cs
// BattleEngine 전체 흐름을 BattleUIAdapter에 연결하는 진입점 MonoBehaviour.
// Phase D-1: RunManager 통합 — 3전투 자동 진행. 보상 [계속] → 다음 전투 또는 런 클리어.
// 노드맵 UI / 분기 선택은 후속 작업 (Phase D-2).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Dice;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;
using OUD.Unity.Adapter;

namespace OUD.Unity
{
    /// <summary>
    /// 런 + 전투 진입점.
    /// 책임: RunManager + EncounterTable + RewardSystem 인스턴스 보유,
    ///       매 전투마다 BattleState/TurnManager 재생성,
    ///       보상 [계속] 클릭 시 다음 전투 또는 런 클리어 분기.
    /// </summary>
    public class BattleBootstrapper : MonoBehaviour
    {
        [Header("BattleUIAdapter (씬 내 BattleCanvas 오브젝트)")]
        [SerializeField] private BattleUIAdapter _adapter;

        [Header("Roll Dice 버튼 (BattlePanel/ActionButtonA)")]
        [SerializeField] private Button _rollDiceButton;

        // ── BattleEngine 코어 인스턴스 ────────────────────────────────────────
        private IRandom        _random;
        private RewardSystem   _rewardSystem;
        private EncounterTable _encounterTable;
        private RunManager     _runManager;

        // ── 매 전투마다 재생성 ────────────────────────────────────────────────
        private TurnManager _turnManager;
        private BattleState _state;

        private void Start()
        {
            InitRun();
        }

        // ── 런 초기화 ─────────────────────────────────────────────────────────

        private void InitRun()
        {
            if (_adapter == null)
            {
                Debug.LogError("[BattleBootstrapper] BattleUIAdapter가 바인딩되지 않았습니다! Inspector를 확인하세요.");
                return;
            }

            // 코어 인스턴스 1회 생성
            _random         = new UnityRandom();
            _rewardSystem   = new RewardSystem(_random);
            _encounterTable = new EncounterTable(_random);
            _runManager     = new RunManager(_encounterTable);

            // 새 PlayerState로 런 시작 (Phase D-1: 패배 재시작 미구현)
            PlayerState player = new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));
            _runManager.StartRun(player);

            // Roll Dice 버튼 onClick 등록 (1회)
            if (_rollDiceButton != null)
                _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
            else
                Debug.LogWarning("[BattleBootstrapper] Roll Dice 버튼이 바인딩되지 않았습니다.");

            Debug.Log("[BattleBootstrapper] 런 초기화 완료 — 3전투 진행 (sprint MVP 데모)");
            Debug.Log($"  플레이어: HP:{player.MaxHp} ATK:{player.Atk} DEF:{player.Def}");

            StartNextBattle();
        }

        // ── 전투 진입 (RunManager가 결정한 적 구성으로 새 BattleState/TurnManager 생성) ─

        private void StartNextBattle()
        {
            if (_runManager.IsRunComplete())
            {
                Debug.LogWarning("[BattleBootstrapper] StartNextBattle 호출됐지만 런이 이미 완료됨 — 무시.");
                return;
            }

            // RunManager에서 적 구성 받아 MonsterInstance로 변환
            List<MonsterData>     enemyData     = _runManager.GetNextBattle();
            List<MonsterInstance> enemyInstances = new List<MonsterInstance>(enemyData.Count);
            foreach (MonsterData d in enemyData)
                enemyInstances.Add(new MonsterInstance(d));

            // 매 전투마다 새 BattleState + TurnManager (DiceHand는 IRandom 공유)
            DiceHand diceHand = new DiceHand(_random);
            _state       = new BattleState(_runManager.State.Player, enemyInstances, diceHand);
            _turnManager = new TurnManager(_state, _adapter);

            // Adapter 갱신 (TurnManager 새로 주입, RewardSystem/RunManager/콜백은 동일)
            _adapter.Initialize(_turnManager, _rewardSystem, _runManager, OnContinueAfterReward);

            int nodeIndex = _runManager.State.CurrentNodeIndex;
            string enemyNames = string.Join(", ", enemyInstances.ConvertAll(e => e.Data.Name));
            Debug.Log($"[BattleBootstrapper] 노드 {nodeIndex + 1}/{RunState.TOTAL_NODES} 시작 — 적: {enemyNames}");

            _turnManager.StartBattle();
        }

        // ── 보상 [계속] 클릭 시 호출 — 다음 전투 또는 런 클리어 ────────────────

        private void OnContinueAfterReward()
        {
            if (_runManager.IsRunComplete())
            {
                Debug.Log("[BattleBootstrapper] === 런 클리어! 모든 전투 종료 ===");
                // Phase D-1 한정: 클리어 화면 미구현 — 콘솔 로그만.
                // 후속 작업(F-14)에서 클리어 UI 추가 예정.
                return;
            }

            StartNextBattle();
        }

        // ── 버튼 핸들러 ───────────────────────────────────────────────────────

        /// <summary>Screen A의 Roll Dice 버튼 클릭 — 현재 턴의 플레이어 페이즈 시작.</summary>
        private void OnRollDiceClicked()
        {
            if (_turnManager == null) return;
            _turnManager.StartPlayerTurn();
        }
    }
}
