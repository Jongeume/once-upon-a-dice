using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] private NodeMapView          _nodeMapView;

        [Header("Growth Views (F-10/F-08/F-09)")]
        [SerializeField] private LevelUpStatView      _levelUpStatView;
        [SerializeField] private LevelUpSkillView     _levelUpSkillView;
        [SerializeField] private RestView             _restView;

        [Header("End-of-Run Views (F-14)")]
        [SerializeField] private ClearView  _clearView;
        [SerializeField] private DefeatView _defeatView;

        [Header("화면 관리")]
        [SerializeField] private UIManager            _uiManager;

        [Header("MonsterSpriteMap")]
        [SerializeField] private MonsterSpriteMap     _spriteMap;

        [Header("주사위 Entry Views (5개)")]
        [SerializeField] private List<DiceEntryView>  _diceEntries;

        [Header("상단 정보 바 (선택)")]
        [SerializeField] private string   _playerName = "Alice";
        [SerializeField] private TMP_Text _topBarText;

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

        // ── 런 매니저 (Phase D-1) — OnBattleWon 시 노드 진행 ──────────────────
        private RunManager _runManager;

        // ── 성장 시스템 (F-08/F-09/F-10) ─────────────────────────────────────
        private LevelUpSystem    _levelUpSystem;
        private SkillPointSystem _skillPointSystem;
        private RestSystem       _restSystem;

        // ── 보상 [계속] 클릭 시 외부에 통보할 콜백 (Bootstrapper에서 등록) ────
        private Action _onContinueRequested;

        // ── RewardView 이벤트 등록 1회 가드 (Initialize 매번 호출 대비) ───────
        private bool _rewardContinueWired;

        // ── NodeMapView 이벤트 등록 1회 가드 ───────────────────────────────────
        private bool _nodeMapClickWired;

        // ── Growth View 이벤트 등록 1회 가드 ─────────────────────────────────
        private bool _growthViewsWired;

        private bool _endRunViewsWired;

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
        /// BattleBootstrapper가 TurnManager + RewardSystem + RunManager 생성 후 호출.
        /// 매 전투 시작 시 새 TurnManager로 호출되어 리롤 콜백을 갱신한다.
        /// onContinueRequested: 보상 화면 [계속] 클릭 시 호출 — 다음 전투 또는 클리어 처리는 외부 책임.
        /// RewardView 이벤트 등록은 1회만 (중복 누적 방지).
        /// </summary>
        public void Initialize(
            TurnManager      turnManager,
            RewardSystem     rewardSystem,
            RunManager       runManager,
            LevelUpSystem    levelUpSystem,
            SkillPointSystem skillPointSystem,
            RestSystem       restSystem,
            Action           onContinueRequested)
        {
            _turnManager         = turnManager;
            _rewardSystem        = rewardSystem;
            _runManager          = runManager;
            _levelUpSystem       = levelUpSystem;
            _skillPointSystem    = skillPointSystem;
            _restSystem          = restSystem;
            _onContinueRequested = onContinueRequested;
            _onRerollRequested   = keepMask => turnManager.RequestReroll(keepMask);

            if (_rewardView != null && !_rewardContinueWired)
            {
                _rewardView.OnContinueClicked += HandleRewardContinueClicked;
                _rewardContinueWired = true;
            }

            if (_nodeMapView != null && !_nodeMapClickWired)
            {
                _nodeMapView.OnNodeClicked += HandleNodeMapClicked;
                _nodeMapClickWired = true;
            }

            WireGrowthViews();

            if (!_endRunViewsWired)
            {
                if (_clearView != null)
                    _clearView.OnRestartClicked += HandleRestartClicked;
                if (_defeatView != null)
                    _defeatView.OnRestartClicked += HandleRestartClicked;
                _endRunViewsWired = true;
            }
        }

        /// <summary>
        /// 보상 [계속] 클릭 흐름.
        /// 전투 후 흐름: 보상 → (레벨업) → (SP 해금) → (휴식) → 노드맵/Boss클리어
        /// </summary>
        private void HandleRewardContinueClicked()
        {
            if (_rewardView != null) _rewardView.Hide();

            if (_runManager == null)
            {
                _onContinueRequested?.Invoke();
                return;
            }

            PostBattleFlow flow = _runManager.GetPostBattleFlow();
            StartPostBattleFlow(flow);
        }

        private void StartPostBattleFlow(PostBattleFlow flow)
        {
            if (flow.ShowLevelUp && _levelUpSystem != null)
            {
                PlayerState player = _playerPresenter.Player;
                if (_levelUpSystem.CanLevelUp(player.Xp, player.Level))
                {
                    ShowLevelUpStat(flow);
                    return;
                }
            }

            ContinueAfterLevelUp(flow);
        }

        private void ShowLevelUpStat(PostBattleFlow flow)
        {
            PlayerState player = _playerPresenter.Player;

            if (_levelUpStatView != null)
            {
                _levelUpStatView.SetStats(player.Atk, player.Def, player.MaxHp, player.Hp);
                _levelUpStatView.Show();
                _pendingFlow = flow;
            }
            else
            {
                ContinueAfterLevelUp(flow);
            }
        }

        private void HandleStatChosen(StatChoice choice)
        {
            PlayerState player = _playerPresenter.Player;
            _levelUpSystem.ApplyLevelUp(player, choice);
            _playerPresenter.SyncView();
            RefreshTopBar();

            if (_levelUpStatView != null) _levelUpStatView.Hide();

            ShowLevelUpSkill(_pendingFlow);
        }

        private void ShowLevelUpSkill(PostBattleFlow flow)
        {
            PlayerState player = _playerPresenter.Player;

            if (_levelUpSkillView != null && player.Sp > 0)
            {
                _levelUpSkillView.Bind(player.Sp, player.UnlockedHands);
                _levelUpSkillView.Show();
                _pendingFlow = flow;
            }
            else
            {
                ContinueAfterLevelUp(flow);
            }
        }

        private void HandleUnlockChosen(HandType hand)
        {
            PlayerState player = _playerPresenter.Player;
            _skillPointSystem.Unlock(player, hand);
            if (_levelUpSkillView != null) _levelUpSkillView.Hide();
            ContinueAfterLevelUp(_pendingFlow);
        }

        private void HandleSkillSkipClicked()
        {
            if (_levelUpSkillView != null) _levelUpSkillView.Hide();
            ContinueAfterLevelUp(_pendingFlow);
        }

        private void ContinueAfterLevelUp(PostBattleFlow flow)
        {
            if (flow.ShowRest && _restSystem != null)
            {
                ShowRest(flow);
                return;
            }

            FinishPostBattle();
        }

        private void ShowRest(PostBattleFlow flow)
        {
            PlayerState player = _playerPresenter.Player;

            if (_restView != null && player.Hp < player.MaxHp && player.Gold >= RestSystem.GOLD_PER_UNIT)
            {
                _restView.Bind(player.Hp, player.MaxHp, player.Gold);
                _restView.Show();
                _pendingFlow = flow;
            }
            else
            {
                FinishPostBattle();
            }
        }

        private void HandleRestConfirmed(int investGold)
        {
            PlayerState player = _playerPresenter.Player;
            _restSystem.ApplyRest(player, investGold);
            _playerPresenter.SyncView();
            RefreshTopBar();

            if (_restView != null) _restView.Hide();
            FinishPostBattle();
        }

        private void HandleRestSkipped()
        {
            if (_restView != null) _restView.Hide();
            FinishPostBattle();
        }

        private void HandleRestartClicked()
        {
            SceneManager.LoadScene("TitleScene");
        }

        private void ShowClearScreen()
        {
            if (_clearView == null) return;
            PlayerState player = _playerPresenter.Player;
            _clearView.SetSummary(player.Level + 1, player.Gold);
            _clearView.Show();
        }

        private void ShowDefeatScreen()
        {
            if (_defeatView == null) return;
            int reached = _runManager?.State?.CurrentNodeIndex ?? 0;
            int total = RunState.TOTAL_NODES;
            _defeatView.SetDefeatInfo(reached + 1, total);
            _defeatView.Show();
        }

        private void FinishPostBattle()
        {
            MapNode currentNode = _runManager.GetCurrentNode();
            bool isBossNode = currentNode.Type == NodeType.Boss;

            if (isBossNode)
            {
                _runManager.AdvanceNode();
                ShowClearScreen();
                return;
            }

            ShowNodeMap();
        }

        private PostBattleFlow _pendingFlow;

        private void WireGrowthViews()
        {
            if (_growthViewsWired) return;
            _growthViewsWired = true;

            if (_levelUpStatView != null)
                _levelUpStatView.OnStatChosen += HandleStatChosen;
            if (_levelUpSkillView != null)
            {
                _levelUpSkillView.OnUnlockChosen += HandleUnlockChosen;
                _levelUpSkillView.OnSkipClicked += HandleSkillSkipClicked;
            }
            if (_restView != null)
            {
                _restView.OnRestConfirmed += HandleRestConfirmed;
                _restView.OnRestSkipped += HandleRestSkipped;
            }
        }

        /// <summary>
        /// 노드맵 표시. 현재 노드(=방금 클리어) + 진행 가능 후보를 RunMap 데이터와 함께 NodeMapView에 주입.
        /// _nodeMapView 미바인딩 시 ─ 폴백으로 AdvanceNode + Continue 즉시 호출 (UI 없는 환경 호환).
        /// </summary>
        private void ShowNodeMap()
        {
            if (_runManager == null) return;

            if (_nodeMapView == null)
            {
                // 폴백: 노드맵 UI 미바인딩 → 첫 후보 자동 선택 + 다음 전투
                _runManager.AdvanceNode();
                _onContinueRequested?.Invoke();
                return;
            }

            int currentNodeId = _runManager.State.CurrentNodeId;
            IReadOnlyList<MapNode> nextCandidates = _runManager.GetAvailableNextNodes();
            int[] nextIds = new int[nextCandidates.Count];
            for (int i = 0; i < nextCandidates.Count; i++) nextIds[i] = nextCandidates[i].Id;

            _nodeMapView.Bind(_runManager.Map, currentNodeId, nextIds);
            _nodeMapView.Show();
        }

        /// <summary>
        /// 노드맵에서 분기 노드 클릭 시 호출.
        /// SelectNextNode로 RunState 갱신 → 노드맵 닫고 → Bootstrapper에 다음 전투 통보.
        /// </summary>
        private void HandleNodeMapClicked(int nodeId)
        {
            if (_runManager == null) return;

            try
            {
                _runManager.SelectNextNode(nodeId);
            }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning($"[BattleUIAdapter] 노드맵 클릭 무시: {e.Message}");
                return;
            }

            if (_nodeMapView != null) _nodeMapView.Hide();
            _onContinueRequested?.Invoke();
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
            // EnemyPresenter._entryViews 기반으로 transform 추출.
            // Container.children 직접 사용 시 Destroy 마킹된 이전 entry가 포함되어
            // 다음 프레임에 destroyed reference가 됨 (다음 노드 시작 시점 버그).
            return _enemyPresenter?.GetEntryTransforms() ?? Array.Empty<Transform>();
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
            // 이전 라운드 결과 화면 잔존 방지 (Phase D-1: 다음 전투 자동 진행)
            if (_battleLogView != null) _battleLogView.HideResultScreens();

            _playerPresenter.Init(player);
            _enemyPresenter.Init(enemies);
            // 적 Entry 생성 후 BattleLogPresenter에 올바른 Transform 배열 전달
            _battleLogPresenter = new BattleLogPresenter(
                _battleLogView,
                _playerView.transform,
                BuildEnemyTransforms());
            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
            RefreshTopBar();
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
            RefreshTopBar();
        }

        public void OnEnemyAction(int enemyIndex, IntentType intent, int value)
        {
            _enemyPresenter.ShowAction(enemyIndex, intent, value);
            _battleLogPresenter.ShowEnemyAction(enemyIndex, intent, value);
            _playerPresenter.SyncView();
            RefreshTopBar();
        }

        public void OnShieldsReset()
        {
            _playerPresenter.SyncShield();
            _enemyPresenter.RefreshAllShields();
            RefreshTopBar();
        }

        public void OnBattleWon()
        {
            _battleLogPresenter.ShowBattleWon();
            GrantReward();
            // Phase D-2 변경: 노드 진행을 OnBattleWon에서 호출하지 않는다.
            // 이유 — 노드맵 표시 시 "현재 노드"가 *방금 클리어한 노드*여야 시각적으로 자연스럽다.
            // AdvanceNode / SelectNextNode는 보상 [계속] → HandleRewardContinueClicked에서 분기 처리.
        }

        public void OnBattleLost()
        {
            _battleLogPresenter.ShowBattleLost();
            ShowDefeatScreen();
        }

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
            RefreshTopBar();
        }

        // ── 상단 정보 바 갱신 ─────────────────────────────────────────────────
        // 씬에 정적 텍스트로 박혀있던 PlayerInfo 라벨을 PlayerState 변동에 맞춰 갱신.
        // _topBarText 미바인딩 시 호출 무시 (선택 표시).

        private void RefreshTopBar()
        {
            if (_topBarText == null) return;
            if (_playerPresenter == null) return;
            PlayerState player = _playerPresenter.Player;
            if (player == null) return;

            _topBarText.text =
                $"{_playerName}   HP: {player.Hp}/{player.MaxHp}   돈 {player.Gold}   XP {player.Xp}";
        }
    }
}
