using System;
using System.Collections.Generic;
using UnityEngine;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
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

        // ── 초기화 ───────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildPresenters();
            WireViewEvents();
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
            _slotAssignmentView.OnUseSkillClicked  += _slotAssignmentPresenter.OnUseSkillClicked;

            // 적 클릭 (화면 C)
            _enemyPresenter.OnEnemyClicked += _targetSelectionPresenter.OnEnemyClicked;

            // 턴 종료 버튼
            _targetSelectionView.OnExecuteClicked += HandleExecuteClicked;
        }

        // ── 화면 전환 핸들러 ──────────────────────────────────────────────────

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
                    // 모든 타겟 확정 → 턴 종료 버튼 활성화는 TargetSelectionPresenter가 처리
                });
        }

        private void HandleExecuteClicked()
        {
            _enemyPresenter.SetTargetSelectable(false);
            // TargetSelectionPresenter에서 이미 Confirm 했으므로 콜백 호출
            _targetSelectionPresenter.OnEnemyClicked(-1); // 더미 - 실제론 Confirm 직접 호출
        }

        // ── IBattleUI 구현 ────────────────────────────────────────────────────

        public void OnBattleStart(PlayerState player, List<MonsterInstance> enemies)
        {
            _playerPresenter.Init(player);
            _enemyPresenter.Init(enemies);
            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
        }

        public void OnDiceRolled(int[] values, int rerollsLeft)
        {
            _dicePresenter.UpdateDice(values, rerollsLeft);
            _slotAssignmentPresenter.OnRerollCountChanged(rerollsLeft);
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
            _pendingSlotCallback = onComplete;
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

        public void OnBattleWon()  => _battleLogPresenter.ShowBattleWon();
        public void OnBattleLost() => _battleLogPresenter.ShowBattleLost();
    }
}
