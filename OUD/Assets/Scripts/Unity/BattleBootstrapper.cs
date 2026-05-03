// BattleBootstrapper.cs
// BattleEngine 전체 흐름을 BattleUIAdapter에 연결하는 진입점 MonoBehaviour.
// 씬에 하나만 배치. Inspector에서 BattleUIAdapter와 Roll Dice 버튼 바인딩 필수.
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
    /// BattleEngine 초기화 진입점.
    /// TurnManager ↔ BattleUIAdapter를 연결하고 StartBattle()을 호출한다.
    /// </summary>
    public class BattleBootstrapper : MonoBehaviour
    {
        [Header("BattleUIAdapter (씬 내 BattleCanvas 오브젝트)")]
        [SerializeField] private BattleUIAdapter _adapter;

        [Header("Roll Dice 버튼 (BattlePanel/ActionButtonA)")]
        [SerializeField] private Button _rollDiceButton;

        private TurnManager _turnManager;
        private BattleState _state;

        private void Start()
        {
            InitBattle();
        }

        // ── 초기화 ────────────────────────────────────────────────────────────

        private void InitBattle()
        {
            if (_adapter == null)
            {
                Debug.LogError("[BattleBootstrapper] BattleUIAdapter가 바인딩되지 않았습니다! Inspector를 확인하세요.");
                return;
            }

            // BattleEngine 객체 조립
            IRandom      random   = new UnityRandom();
            PlayerState  player   = new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));
            List<MonsterInstance> enemies = MonsterDatabase.CreateSprint0Encounter();
            DiceHand     diceHand = new DiceHand(random);

            _state       = new BattleState(player, enemies, diceHand);
            _turnManager = new TurnManager(_state, _adapter);

            // 보상 시스템 (F-11 Phase A) — Adapter가 OnBattleWon 시점에 호출
            RewardSystem rewardSystem = new RewardSystem(random);

            // BattleUIAdapter에 TurnManager + RewardSystem 주입 (리롤 콜백 + 보상 처리 연결)
            _adapter.Initialize(_turnManager, rewardSystem);

            // Roll Dice 버튼 onClick 연결 (Screen A → Screen B 전환)
            if (_rollDiceButton != null)
                _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
            else
                Debug.LogWarning("[BattleBootstrapper] Roll Dice 버튼이 바인딩되지 않았습니다.");

            // 전투 시작: OnBattleStart(Screen A) 후 자동으로 StartPlayerTurn → OnDiceRolled(Screen B)
            _turnManager.StartBattle();

            Debug.Log("[BattleBootstrapper] 초기화 완료 — BattleUIAdapter 연결됨");
            Debug.Log("  인카운터: Slime(HP:20)×2 + Skeleton(HP:25)");
            Debug.Log("  플레이어: HP:60 ATK:6 DEF:5");
        }

        // ── 버튼 핸들러 ───────────────────────────────────────────────────────

        /// <summary>
        /// Screen A의 Roll Dice 버튼 클릭 시 호출.
        /// 현재 턴의 플레이어 페이즈를 시작해 DiceTablePanel(Screen B)을 표시한다.
        /// </summary>
        private void OnRollDiceClicked()
        {
            _turnManager.StartPlayerTurn();
        }
    }
}
