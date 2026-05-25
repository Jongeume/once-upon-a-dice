using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Dice;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;
using OUD.Unity.Adapter;

namespace OUD.Unity
{
    public class BattleBootstrapper : MonoBehaviour
    {
        [Header("BattleUIAdapter (씬 내 BattleCanvas 오브젝트)")]
        [SerializeField] private BattleUIAdapter _adapter;

        [Header("Roll Dice 버튼 (BattlePanel/ActionButtonA)")]
        [SerializeField] private Button _rollDiceButton;

        private IRandom          _random;
        private RewardSystem     _rewardSystem;
        private EncounterTable   _encounterTable;
        private RunManager       _runManager;
        private LevelUpSystem    _levelUpSystem;
        private SkillPointSystem _skillPointSystem;
        private ShopSystem       _shopSystem;

        private TurnManager _turnManager;
        private BattleState _state;
        private bool _rollDiceWired;

        private void Start()
        {
            InitRun();
        }

        /// <summary>[DEBUG] 개발 중 빠른 스테이지 검증용 단축키.
        /// C — 현재 스테이지 즉시 승리 (모든 적 사망 + OnBattleWon).</summary>
        private void Update()
        {
            if (_turnManager == null) return;
            if (Keyboard.current == null) return;
            if (_adapter != null && _adapter.IsPlayingQueue) return;

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                Debug.Log("[Debug] C 키 입력 — 현재 스테이지 강제 승리 처리");
                _turnManager.ForceWin();
            }
        }

        private void InitRun()
        {
            if (_adapter == null)
            {
                Debug.LogError("[BattleBootstrapper] BattleUIAdapter가 바인딩되지 않았습니다!");
                return;
            }

            _random           = new UnityRandom();
            _rewardSystem     = new RewardSystem(_random);
            _encounterTable   = new EncounterTable(_random);
            _runManager       = new RunManager(_encounterTable, _random);
            _levelUpSystem    = new LevelUpSystem();
            _skillPointSystem = new SkillPointSystem();
            _shopSystem       = new ShopSystem();

            PlayerState player = new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));
            _runManager.StartRun(player);

            if (_rollDiceButton != null && !_rollDiceWired)
            {
                _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
                _rollDiceWired = true;
            }

            Debug.Log("[BattleBootstrapper] 런 초기화 완료 — 13노드 가로형 분기 구조");
            Debug.Log($"  플레이어: HP:{player.MaxHp} ATK:{player.Atk} DEF:{player.Def}");

            // 게임 시작 시 노드맵을 먼저 표시. 노드 0 클릭 시 첫 전투 진입.
            _adapter.Initialize(
                null, _rewardSystem, _runManager,
                _levelUpSystem, _skillPointSystem, _shopSystem,
                OnContinueAfterNode);
            _adapter.ShowInitialNodeMap();
        }

        private void StartNextNode()
        {
            if (_runManager.IsRunComplete())
            {
                Debug.LogWarning("[BattleBootstrapper] StartNextNode 호출됐지만 런이 이미 완료됨 — 무시.");
                return;
            }

            MapNode node = _runManager.GetCurrentNode();

            // 노드 타입에 맞춰 전투 배경 전환 (Elite 제외 → 숲 배경, Elite → 별도/단색)
            _adapter.SetBattleBackground(node.Type, node.Layer);

            if (node.Type == NodeType.Shop)
            {
                _adapter.Initialize(
                    _turnManager, _rewardSystem, _runManager,
                    _levelUpSystem, _skillPointSystem, _shopSystem,
                    OnContinueAfterNode);

                _adapter.ShowShop();
                return;
            }

            List<MonsterData>     enemyData     = _runManager.GetNextBattle();
            List<MonsterInstance> enemyInstances = new List<MonsterInstance>(enemyData.Count);
            foreach (MonsterData d in enemyData)
                enemyInstances.Add(new MonsterInstance(d));

            DiceHand diceHand = new DiceHand(_random);
            _state       = new BattleState(_runManager.State.Player, enemyInstances, diceHand);
            _turnManager = new TurnManager(_state, _adapter);

            _adapter.Initialize(
                _turnManager, _rewardSystem, _runManager,
                _levelUpSystem, _skillPointSystem, _shopSystem,
                OnContinueAfterNode);

            int nodeIndex = _runManager.State.CurrentNodeIndex;
            string enemyNames = string.Join(", ", enemyInstances.ConvertAll(e => e.Data.Name));
            Debug.Log($"[BattleBootstrapper] 노드 {nodeIndex + 1}/{RunState.TOTAL_NODES} 시작 — 적: {enemyNames}");

            _turnManager.StartBattle();
        }

        private void OnContinueAfterNode()
        {
            if (_runManager.IsRunComplete())
            {
                Debug.Log("[BattleBootstrapper] === 런 클리어! 모든 전투 종료 ===");
                return;
            }

            StartNextNode();
        }

        private void OnRollDiceClicked()
        {
            if (_turnManager == null) return;
            if (_adapter != null && _adapter.IsPlayingQueue) return;

            // 이미 플레이어 턴이 진행 중(뒤로가기로 Screen A로 빠져나온 상태)이면
            // 상태를 리셋하지 않고 주사위 패널만 다시 표시한다 — 패널 토글 동작.
            BattlePhase phase = _turnManager.CurrentPhase;
            if (phase == BattlePhase.DiceRoll || phase == BattlePhase.SlotAssignment)
            {
                _adapter.ShowDicePanel();
                return;
            }

            _turnManager.StartPlayerTurn();
        }
    }
}
