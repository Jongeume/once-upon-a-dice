using System;
using System.Collections.Generic;
using UnityEngine;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;
using OUD.Unity.Battle;
using OUD.Unity.Battle.Presenter;
using OUD.Unity.Battle.View;
using OUD.Unity.Common;

namespace OUD.Unity.Adapter
{
    /// <summary>
    /// IBattleUI 구현체. BattleEngine ↔ Presenter 라우팅.
    /// Inspector에서 View들을 바인딩하고 Awake에서 Presenter 생성.
    /// </summary>
    public class BattleUIAdapter : MonoBehaviour, IBattleUI
    {
        // ── Inspector 바인딩 ─────────────────────────────────────────────────

        [Header("Views")]
        [SerializeField] private PlayerView           _playerView;
        [SerializeField] private EnemyView            _enemyView;
        [SerializeField] private DiceView             _diceView;
        [SerializeField] private SlotAssignmentView   _slotAssignmentView;
        [SerializeField] private TargetSelectionView  _targetSelectionView;
        [SerializeField] private BattleLogView        _battleLogView;
        [SerializeField] private RewardView           _rewardView;

        [Header("화면 관리")]
        [SerializeField] private UIManager            _uiManager;

        [Header("MonsterSpriteMap")]
        [SerializeField] private MonsterSpriteMap     _spriteMap;

        [Header("주사위 Entry Views (5개)")]
        [SerializeField] private List<DiceEntryView>  _diceEntries;

        // ── Presenter 인스턴스 ───────────────────────────────────────────────

        private PlayerPresenter           _playerPresenter;
        private EnemyPresenter            _enemyPresenter;
        private DicePresenter             _dicePresenter;
        private SlotAssignmentPresenter   _slotAssignmentPresenter;
        private TargetSelectionPresenter  _targetSelectionPresenter;
        private BattleLogPresenter        _battleLogPresenter;

        // ── Engine ↔ UI 콜백 ─────────────────────────────────────────────────

        private Action<bool[]>               _onRerollRequested;
        private Action<List<SlotAssignment>> _pendingSlotCallback;

        // ── TurnManager 역참조 (Initialize 후 유효) ───────────────────────────
        private TurnManager _turnManager;

        // ── 보상 시스템 (Initialize 후 유효) ─────────────────────────────────
        private RewardSystem _rewardSystem;

        // ── 사용 가능한 기술 존재 여부 ────────────────────────────────────────
        // true = RequestSlotAssignment 호출됨(기술 있음) / false = 잡패(기술 없음)
        private bool _hasUsableSkills = false;

        // ── 초기화 ───────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildPresenters();
            WireViewEvents();
        }

        /// <summary>
        /// BattleBootstrapper가 TurnManager + RewardSystem 생성 후 호출.
        /// 리롤 콜백 / 보상 시스템 / RewardView 이벤트를 연결한다.
        /// </summary>
        public void Initialize(TurnManager turnManager, RewardSystem rewardSystem)
        {
            _turnManager       = turnManager;
            _rewardSystem      = rewardSystem;
            _onRerollRequested = keepMask => turnManager.RequestReroll(keepMask);

            // 보상 화면 [계속] 버튼 → 화면 닫기 (다음 전투/씬 전환은 Phase D RunManager 통합 시)
            if (_rewardView != null)
                _rewardView.OnContinueClicked += HandleRewardContinueClicked;
        }

        private void HandleRewardContinueClicked()
        {
            if (_rewardView != null) _rewardView.Hide();
        }

        private void BuildPresenters()
        {
            _playerPresenter = new PlayerPresenter(_playerView);

            _enemyPresenter = new EnemyPresenter(
                () => _enemyView.SpawnEntry(),
                _spriteMap);

            var diceEntryInterfaces = new List<IDiceEntryView>();
            foreach (var e in _diceEntries) diceEntryInterfaces.Add(e);

            _dicePresenter = new DicePresenter(
                _diceView,
                diceEntryInterfaces,
                keepMask => _onRerollRequested?.Invoke(keepMask));

            _slotAssignmentPresenter = new SlotAssignmentPresenter(
                _slotAssignmentView,
                _dicePresenter,
                HandleUseSkillClicked);

            _targetSelectionPresenter = new TargetSelectionPresenter(_targetSelectionView);

            _battleLogPresenter = new BattleLogPresenter(
                _battleLogView,
                _playerView.transform,
                BuildEnemyTransforms());
        }

        private Transform[] BuildEnemyTransforms()
        {
            if (_enemyView == null) return Array.Empty<Transform>();
            var list = new List<Transform>();
            foreach (Transform child in _enemyView.Container)
                list.Add(child);
            return list.ToArray();
        }

        private void WireViewEvents()
        {
            // 주사위 Keep 토글
            for (int i = 0; i < _diceEntries.Count; i++)
            {
                int idx = i;
                _diceEntries[i].OnToggled += () => _dicePresenter.OnDieToggleKeep(idx);
            }

            // 기술 카드 클릭 (화면 B)
            _slotAssignmentView.OnSkillCardClicked += _slotAssignmentPresenter.OnSkillClicked;
            _slotAssignmentView.OnRerollClicked    += _dicePresenter.RequestReroll;

            // UseSkill 버튼: DiceView 이벤트로 단일 처리
            // 리롤 소진 or 슬롯 만석 → 버튼 표시 → 1회 클릭으로 ConfirmDice + Screen C 전환
            // SlotAssignmentView.OnUseSkillClicked는 구독하지 않음 (동일 버튼 이중 발화 방지)
            _diceView.OnUseSkillClicked += OnUseSkillButtonClicked;

            // 적 클릭 (화면 C)
            _enemyPresenter.OnEnemyClicked += _targetSelectionPresenter.OnEnemyClicked;

            // 슬롯 클릭 (화면 C) — 슬롯 선택 후 적 클릭으로 타겟 변경
            _targetSelectionView.OnSlotClicked += _targetSelectionPresenter.OnSlotClicked;

            // 턴 종료 버튼
            _targetSelectionView.OnExecuteClicked += HandleExecuteClicked;
        }

        // ── 화면 전환 핸들러 ──────────────────────────────────────────────────

        /// <summary>
        /// UseSkill 버튼 클릭 진입점.
        /// 기술이 있으면 ConfirmDice(리롤 차단) → 화면 C 전환.
        /// 잡패(_hasUsableSkills=false)이면 ConfirmDice가 적 턴으로 직접 진행.
        /// </summary>
        private void OnUseSkillButtonClicked()
        {
            if (_turnManager == null)
            {
                Debug.LogWarning("[BattleUIAdapter] _turnManager null — Initialize가 호출되지 않은 인스턴스입니다. 무시합니다.");
                return;
            }

            // ConfirmDice:
            //   잡패 → 적 턴 즉시 실행 (새 턴 시작됨)
            //   정상 → phase만 SlotAssignment로 전환, 슬롯 배분은 이미 표시 중
            _turnManager.ConfirmDice();

            if (_hasUsableSkills)
                HandleUseSkillClicked(); // 화면 C 전환
            // else: 잡패, ConfirmDice가 이미 처리함
        }

        private void HandleUseSkillClicked()
        {
            // 화면 B → C
            _uiManager.ShowScreen(UIManager.BattleScreen.C_Targeting);
            _enemyPresenter.SetTargetSelectable(true);

            _targetSelectionPresenter.Begin(
                _slotAssignmentPresenter.GetSlots(),
                _slotAssignmentPresenter.GetAliveEnemies(),
                targetIndices =>
                {
                    // 모든 타겟 확정 → Execute 버튼 활성화는 TargetSelectionPresenter가 처리
                });
        }

        private void HandleExecuteClicked()
        {
            _enemyPresenter.SetTargetSelectable(false);
            // TargetSelectionPresenter에서 확정된 타겟 인덱스 → SlotAssignmentPresenter.Confirm
            var targetIndices = _targetSelectionPresenter.GetTargetIndices();
            if (targetIndices != null)
                _slotAssignmentPresenter.Confirm(targetIndices);
            // 슬롯 실행 + 적 턴 처리 후 Screen A로 복귀 (다음 턴은 Roll Dice 버튼으로 시작)
            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
        }

        // ── IBattleUI 구현 ────────────────────────────────────────────────────

        public void OnBattleStart(PlayerState player, List<MonsterInstance> enemies)
        {
            _playerPresenter.Init(player);
            _enemyPresenter.Init(enemies);
            // 적 Entry 생성 후 BattleLogPresenter에 올바른 Transform 배열 전달
            _battleLogPresenter = new BattleLogPresenter(
                _battleLogView,
                _playerView.transform,
                BuildEnemyTransforms());
            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
        }

        public void OnPlayerTurnStarted()
        {
            _slotAssignmentPresenter.ResetForNewTurn();
        }

        public void OnDiceRolled(int[] values, int rerollsLeft)
        {
            _hasUsableSkills = false;                        // RequestSlotAssignment 호출 전까지 false
            _dicePresenter.UpdateDice(values, rerollsLeft);
            _slotAssignmentPresenter.OnRerollCountChanged(rerollsLeft);
            // UseSkill 버튼 표시는 SlotAssignmentPresenter가 관리 (리롤 소진 시 또는 슬롯 만석 시)
            _uiManager.ShowScreen(UIManager.BattleScreen.B_DiceTable);
        }

        public void OnHandsEvaluated(List<HandType> hands, List<SkillData> usableSkills)
        {
            // 잡패 처리 — 족보 없으면 기술 없음, 적 턴으로 자동 진행
        }

        public void RequestSlotAssignment(
            List<SkillData>            usableSkills,
            List<MonsterInstance>      aliveEnemies,
            Action<List<SlotAssignment>> onComplete)
        {
            _hasUsableSkills     = true;
            _pendingSlotCallback = onComplete;
            // Begin()이 DicePresenter.RerollsLeft 기반으로 리롤/UseSkill 버튼 상태를 결정
            _slotAssignmentPresenter.Begin(usableSkills, aliveEnemies, onComplete);
        }

        public void OnSlotExecuted(int slotIndex, SkillResult result)
        {
            _battleLogPresenter.ShowSlotResult(slotIndex, result);
            _playerPresenter.SyncView();
            _enemyPresenter.RefreshAll();
        }

        public void OnEnemyAction(int enemyIndex, IntentType intent, int value)
        {
            _enemyPresenter.ShowAction(enemyIndex, intent, value);
            _battleLogPresenter.ShowEnemyAction(enemyIndex, intent, value);
            _playerPresenter.SyncView();
        }

        public void OnShieldsReset()
        {
            _playerPresenter.SyncShield();
            _enemyPresenter.RefreshAllShields();
        }

        public void OnBattleWon()
        {
            _battleLogPresenter.ShowBattleWon();
            GrantReward();
        }

        public void OnBattleLost() => _battleLogPresenter.ShowBattleLost();

        // ── 보상 처리 (F-13 Phase B) ──────────────────────────────────────────
        // IBattleUI에 보상 콜백을 추가하지 않고 BattleUIAdapter 내부에서 처리.
        // 근거: Phase A에서 "보상 계산은 외부 책임" 결정 — BattleEngine은 OnBattleWon만 알린다.

        private void GrantReward()
        {
            if (_rewardSystem == null) return;            // Initialize 미호출 방어
            if (_playerPresenter == null) return;
            PlayerState player = _playerPresenter.Player;
            if (player == null) return;

            RewardResult reward = _rewardSystem.CalculateReward();
            player.AddXp(reward.Xp);
            player.AddGold(reward.Gold);

            if (_rewardView != null)
            {
                _rewardView.SetReward(reward.Xp, reward.Gold, player.Xp, player.Gold);
                _rewardView.Show();
            }

            _playerPresenter.SyncView();
        }
    }
}
