using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public class BattleUIAdapter : MonoBehaviour, IBattleUI
    {
        [Header("Views")]
        [SerializeField] private PlayerView           _playerView;
        [SerializeField] private EnemyView            _enemyView;
        [SerializeField] private DiceView             _diceView;
        [SerializeField] private SlotAssignmentView   _slotAssignmentView;
        [SerializeField] private TargetSelectionView  _targetSelectionView;
        [SerializeField] private BattleLogView        _battleLogView;
        [SerializeField] private RewardView           _rewardView;
        [SerializeField] private NodeMapView          _nodeMapView;

        [Header("Growth Views")]
        [SerializeField] private LevelUpStatView      _levelUpStatView;
        [SerializeField] private LevelUpSkillView     _levelUpSkillView;
        [SerializeField] private ShopView             _shopView;

        [Header("End-of-Run Views")]
        [SerializeField] private ClearView  _clearView;
        [SerializeField] private DefeatView _defeatView;

        [Header("턴 전환 배너")]
        [SerializeField] private TurnBannerView _turnBannerView;

        [Header("화면 관리")]
        [SerializeField] private UIManager            _uiManager;

        [Header("MonsterSpriteMap")]
        [SerializeField] private MonsterSpriteMap     _spriteMap;

        [Header("Hit Effect — 적 공격 피격 연출")]
        [SerializeField] private HitEffectLibrary     _hitEffectLibrary;
        [SerializeField] private HitEffectPlayer      _hitEffectPrefab;
        [SerializeField] private Transform            _hitEffectOverlay;  // 비우면 플레이어 카드에 부모

        [Header("주사위 Entry Views (5개)")]
        [SerializeField] private List<DiceEntryView>  _diceEntries;

        [Header("상단 정보 바 (선택)")]
        [SerializeField] private string   _playerName = "Alice";
        [SerializeField] private TMP_Text _topBarText;
        [SerializeField] private UnityEngine.UI.Button _mapButton;     // 지도 버튼 — 노드맵 토글

        [Header("전투 배경")]
        [SerializeField] private Sprite               _combatBackgroundSprite;
        [SerializeField] private Sprite               _combatBackground2Sprite;
        [SerializeField] private Sprite               _eliteBackgroundSprite;
        [SerializeField] private Sprite               _bossBackgroundSprite;

        private PlayerPresenter           _playerPresenter;
        private EnemyPresenter            _enemyPresenter;
        private DicePresenter             _dicePresenter;
        private SlotAssignmentPresenter   _slotAssignmentPresenter;
        private TargetSelectionPresenter  _targetSelectionPresenter;
        private BattleLogPresenter        _battleLogPresenter;

        private Action<bool[]>               _onRerollRequested;
        private Action<List<SlotAssignment>> _pendingSlotCallback;

        private TurnManager _turnManager;
        private RewardSystem _rewardSystem;
        private RunManager _runManager;
        private LevelUpSystem    _levelUpSystem;
        private SkillPointSystem _skillPointSystem;
        private ShopSystem       _shopSystem;

        private Action _onContinueRequested;

        private bool _rewardContinueWired;
        private bool _nodeMapClickWired;
        private bool _mapButtonWired;
        private bool _mapPeekMode;
        private bool _growthViewsWired;
        private bool _endRunViewsWired;
        private bool _shopViewWired;

        private bool _hasUsableSkills = false;
        private UnityEngine.UI.Button _backButton;

        // 튜토리얼 전투 여부. true면 승리 시 보상/레벨업을 건너뛰고 곧바로 노드맵을 표시한다.
        private bool _isTutorialBattle;

        private PostBattleFlow _pendingFlow;

        // ── Tutorial 이벤트 — TutorialManager가 구독 ────────────────────────

        /// <summary>튜토리얼 트리거 이벤트. 키: "BattleStart", "DiceRolled", "DiceKept",
        /// "SkillSelected", "UseSkillClicked", "TargetSelected", "ExecuteClicked",
        /// "EnemyShielded", "BattleWon", "BattleLost", "PlayerTurnStarted"</summary>
        public event System.Action<string> OnTutorialEvent;

        // ── Tutorial View 접근자 ────────────────────────────────────────────

        public DiceView            DiceViewRef            => _diceView;
        public SlotAssignmentView  SlotAssignmentViewRef  => _slotAssignmentView;
        public TargetSelectionView TargetSelectionViewRef => _targetSelectionView;
        public List<DiceEntryView> DiceEntries            => _diceEntries;
        public EnemyPresenter      EnemyPresenterRef      => _enemyPresenter;

        /// <summary>현재 살아있는 적 중 쉴드를 보유한 적이 있는지. 튜토리얼 쉴드 안내 조건 판정용.</summary>
        public bool AnyAliveEnemyHasShield()
        {
            if (_battleEnemies == null) return false;
            foreach (var e in _battleEnemies)
                if (e != null && !e.IsDead && e.Shield > 0) return true;
            return false;
        }

        /// <summary>현재 전투의 적 EnemyEntryView 목록. EnemyPresenter의 내부 뷰를 반환.</summary>
        public List<EnemyEntryView> GetEnemyEntryViews()
        {
            var result = new List<EnemyEntryView>();
            if (_enemyPresenter == null) return result;
            var transforms = _enemyPresenter.GetEntryTransforms();
            foreach (var t in transforms)
            {
                if (t != null)
                {
                    var view = t.GetComponent<EnemyEntryView>();
                    if (view != null) result.Add(view);
                }
            }
            return result;
        }

        // ── Action Queue — 전투 행동 순차 연출 ───────────────────────────────

        private const float ACTION_DELAY = 0.6f;
        private const float INTENT_DELAY = 0.15f;

        // 피격 이펙트 크기 = 플레이어 카드의 1.5배 (경계 살짝 넘침, 클리핑 없음).
        private const float HIT_EFFECT_SCALE = 1.5f;

        // "Enemy Turn" 배너가 페이드인할 짧은 리드 타임 — 첫 적 행동 직전 1회.
        private const float ENEMY_BANNER_LEAD = 0.35f;
        // 큐 재생당 "Enemy Turn" 배너 1회 제한 플래그.
        private bool _enemyTurnBannerShown;

        private enum BattleActionType
        {
            SlotExecuted,
            EnemyAction,
            ShieldsReset,
            IntentUpdated,
            EnemySummoned,
            BattleWon,
            BattleLost
        }

        private struct EnemySnapshot
        {
            public int Hp, MaxHp, Shield;
            public bool IsDead;
        }

        private struct DisplaySnapshot
        {
            public int PlayerHp, PlayerMaxHp, PlayerShield;
            public EnemySnapshot[] Enemies;
        }

        private struct QueuedBattleAction
        {
            public BattleActionType Type;
            public int Index;
            public SkillResult SkillResult;
            public IntentType Intent;
            public int Value;
            public MonsterInstance SummonedMonster;
            public DisplaySnapshot Snapshot;
        }

        private bool _isQueueMode;
        private readonly Queue<QueuedBattleAction> _actionQueue = new Queue<QueuedBattleAction>();
        private Coroutine _playbackCoroutine;
        private List<MonsterInstance> _battleEnemies;

        public bool IsPlayingQueue { get; private set; }

        private void Awake()
        {
            BuildPresenters();
            WireViewEvents();
            WireMapButton();
        }

        private void WireMapButton()
        {
            if (_mapButton == null || _mapButtonWired) return;
            _mapButton.onClick.AddListener(ToggleMapPeek);
            _mapButtonWired = true;
        }

        /// <summary>지도(peek) 버튼 활성/비활성 토글. 노드 선택 모드 동안에는 비활성화하여
        /// 선택용 노드맵을 peek 토글로 닫아버리는 충돌을 막는다.</summary>
        private void SetMapButtonEnabled(bool enabled)
        {
            if (_mapButton != null) _mapButton.interactable = enabled;
        }

        /// <summary>지도 버튼 클릭 — 노드맵 peek 모드 토글. 노드 진행 없이 보기만.
        /// peek 동안 노드 Button 컴포넌트는 비활성화 → 클릭 모션(하이라이트/눌림)까지 차단.
        /// 닫을 땐 다시 지도 버튼 클릭.</summary>
        public void ToggleMapPeek()
        {
            if (_runManager == null || _nodeMapView == null) return;

            if (_nodeMapView.gameObject.activeSelf)
            {
                _nodeMapView.SetAllButtonsEnabled(true);  // 다음 정상 사용을 위해 복원
                _nodeMapView.Hide();
                _mapPeekMode = false;
                return;
            }

            int currentNodeId = _runManager.State.CurrentNodeId;
            IReadOnlyList<MapNode> nextCandidates = _runManager.GetAvailableNextNodes();
            int[] nextIds = new int[nextCandidates.Count];
            for (int i = 0; i < nextCandidates.Count; i++) nextIds[i] = nextCandidates[i].Id;

            // Show() 먼저 (Awake 선행) → Bind. ShowInitialNodeMap 주석 참조.
            _nodeMapView.Show();
            _nodeMapView.Bind(_runManager.Map, currentNodeId, nextIds, _runManager.State.VisitedNodeIds);
            _nodeMapView.SetAllButtonsEnabled(false);  // peek: 클릭 모션 차단
            _mapPeekMode = true;
        }

        public void Initialize(
            TurnManager      turnManager,
            RewardSystem     rewardSystem,
            RunManager       runManager,
            LevelUpSystem    levelUpSystem,
            SkillPointSystem skillPointSystem,
            ShopSystem       shopSystem,
            Action           onContinueRequested)
        {
            _turnManager         = turnManager;
            _rewardSystem        = rewardSystem;
            _runManager          = runManager;
            _levelUpSystem       = levelUpSystem;
            _skillPointSystem    = skillPointSystem;
            _shopSystem          = shopSystem;
            _onContinueRequested = onContinueRequested;
            _onRerollRequested   = keepMask => turnManager?.RequestReroll(keepMask);

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
            WireShopView();

            if (!_endRunViewsWired)
            {
                if (_clearView != null)
                    _clearView.OnRestartClicked += HandleRestartClicked;
                if (_defeatView != null)
                    _defeatView.OnRestartClicked += HandleRestartClicked;
                _endRunViewsWired = true;
            }
        }

        /// <summary>현재 전투가 튜토리얼 전투인지 설정. 튜토리얼이면 승리 시
        /// 보상/레벨업을 건너뛰고 곧바로 노드맵(시작 노드)을 표시한다.</summary>
        public void SetTutorialBattle(bool isTutorial)
        {
            _isTutorialBattle = isTutorial;
        }

        // ── Shop node direct entry (no battle) ─────────────────────────────

        public void ShowShop()
        {
            PlayerState player = _runManager.State.Player;
            if (_shopView != null)
            {
                // 상점 진입 시 BattlePanel/DiceTablePanel/Targeting 모두 비활성 — 뒤에 적/플레이어가 비치지 않도록.
                if (_uiManager != null) _uiManager.ShowScreen(UIManager.BattleScreen.D_Shop);
                // 직전 전투의 "전투 승리/패배" 결과 화면도 정리.
                if (_battleLogView != null) _battleLogView.HideResultScreens();
                _shopView.Bind(player.Hp, player.MaxHp, player.Gold, player.Xp);
                _shopView.Show();
            }
            else
            {
                FinishShop();
            }
        }

        private void HandleHpRecoveryConfirmed(int investGold)
        {
            PlayerState player = _runManager.State.Player;
            _shopSystem.ApplyHpRecovery(player, investGold);
            _playerPresenter.SyncView();
            RefreshTopBar();

            SoundManager.Instance?.PlayCoin();
            _shopView.RefreshGold(player.Gold, player.Hp, player.Xp);
        }

        private void HandleXpPurchased(int count)
        {
            if (count <= 0) return;
            PlayerState player = _runManager.State.Player;
            for (int i = 0; i < count; i++)
                _shopSystem.BuyXp(player);
            _playerPresenter.SyncView();
            RefreshTopBar();
            SoundManager.Instance?.PlayCoin();

            if (_levelUpSystem != null && _levelUpSystem.CanLevelUp(player.Xp, player.Level))
            {
                if (_shopView != null) _shopView.Hide();
                ShowLevelUpFromShop();
                return;
            }

            _shopView.RefreshGold(player.Gold, player.Hp, player.Xp);
        }

        private void HandleShopSkipped()
        {
            if (_shopView != null) _shopView.Hide();
            FinishShop();
        }

        private void ShowLevelUpFromShop()
        {
            _statChoicePending = false;   // 이전 가드 해제
            PlayerState player = _runManager.State.Player;
            if (_levelUpStatView != null)
            {
                SoundManager.Instance?.PlayLevelUp();
                _levelUpStatView.SetStats(player.Atk, player.Def, player.MaxHp, player.Hp);
                _levelUpStatView.Show();
                _pendingShopLevelUp = true;
            }
            else
            {
                FinishShop();
            }
        }

        private bool _pendingShopLevelUp;

        private void ReturnToShop()
        {
            _pendingShopLevelUp = false;
            PlayerState player = _runManager.State.Player;
            if (_shopView != null)
            {
                _shopView.RefreshGold(player.Gold, player.Hp, player.Xp);
                _shopView.Show();
            }
            else
            {
                FinishShop();
            }
        }

        private void FinishShop()
        {
            ShowNodeMap();
        }

        public void CheckLevelUpAfterShop()
        {
            FinishShop();
        }

        // ── Post-battle flow (Reward → LevelUp → SP → NodeMap) ─────────

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
            _statChoicePending = false;   // 이전 턴 가드 해제
            PlayerState player = _playerPresenter.Player;

            if (_levelUpStatView != null)
            {
                SoundManager.Instance?.PlayLevelUp();
                _levelUpStatView.SetStats(player.Atk, player.Def, player.MaxHp, player.Hp);
                _levelUpStatView.Show();
                _pendingFlow = flow;
                _pendingShopLevelUp = false;
            }
            else
            {
                ContinueAfterLevelUp(flow);
            }
        }

        private bool _statChoicePending;
        private bool _skillChoicePending;

        private void HandleStatChosen(StatChoice choice)
        {
            // 더블클릭 방지 — 첫 클릭만 처리
            if (_statChoicePending) return;
            _statChoicePending = true;

            PlayerState player = _playerPresenter.Player;
            _levelUpSystem.ApplyLevelUp(player, choice);
            _playerPresenter.SyncView();
            RefreshTopBar();

            if (_levelUpStatView != null) _levelUpStatView.Hide();

            if (_pendingShopLevelUp)
            {
                ShowLevelUpSkillFromShop();
                return;
            }

            ShowLevelUpSkill(_pendingFlow);
        }

        private void ShowLevelUpSkill(PostBattleFlow flow)
        {
            _skillChoicePending = false;  // 이전 가드 해제
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

        private void ShowLevelUpSkillFromShop()
        {
            _skillChoicePending = false;  // 이전 가드 해제
            PlayerState player = _runManager.State.Player;
            if (_levelUpSkillView != null && player.Sp > 0)
            {
                _levelUpSkillView.Bind(player.Sp, player.UnlockedHands);
                _levelUpSkillView.Show();
                _pendingShopLevelUp = true;
            }
            else
            {
                ReturnToShop();
            }
        }

        private void HandleUnlockChosen(HandType hand)
        {
            // 더블클릭 방지
            if (_skillChoicePending) return;
            _skillChoicePending = true;

            PlayerState player = _playerPresenter.Player;
            _skillPointSystem.Unlock(player, hand);
            if (_levelUpSkillView != null) _levelUpSkillView.Hide();

            if (_pendingShopLevelUp)
            {
                ReturnToShop();
                return;
            }

            ContinueAfterLevelUp(_pendingFlow);
        }

        private void HandleSkillSkipClicked()
        {
            // 더블클릭 방지
            if (_skillChoicePending) return;
            _skillChoicePending = true;

            if (_levelUpSkillView != null) _levelUpSkillView.Hide();

            if (_pendingShopLevelUp)
            {
                ReturnToShop();
                return;
            }

            ContinueAfterLevelUp(_pendingFlow);
        }

        private void ContinueAfterLevelUp(PostBattleFlow flow)
        {
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
        }

        private void WireShopView()
        {
            if (_shopViewWired) return;
            _shopViewWired = true;

            if (_shopView != null)
            {
                _shopView.OnHpRecoveryConfirmed += HandleHpRecoveryConfirmed;
                _shopView.OnXpPurchased += HandleXpPurchased;
                _shopView.OnShopSkipped += HandleShopSkipped;
            }
        }

        /// <summary>
        /// 게임 시작 시 최초 노드맵 표시. 플레이어를 시작 노드(현재 위치)에 두고,
        /// 시작 노드의 다음 노드(node 0 = 첫 전투)를 클릭 가능(Available)으로 강조한다.
        /// </summary>
        public void ShowInitialNodeMap()
        {
            if (_runManager == null) return;
            if (_nodeMapView == null)
            {
                _onContinueRequested?.Invoke();
                return;
            }

            int currentNodeId = _runManager.State.CurrentNodeId;  // 시작 노드(START_NODE_ID)
            IReadOnlyList<MapNode> nextCandidates = _runManager.GetAvailableNextNodes();
            int[] nextIds = new int[nextCandidates.Count];
            for (int i = 0; i < nextCandidates.Count; i++) nextIds[i] = nextCandidates[i].Id;

            // Show() 먼저 — 패널이 비활성 상태로 시작하므로, Bind 전에 활성화해
            // NodeView.Awake(노드를 투명/숨김으로 리셋)가 먼저 돌게 한다.
            // 순서가 바뀌면 Awake가 Bind 직후에 실행되어 노드 비주얼을 지워버린다.
            _nodeMapView.Show();
            _nodeMapView.Bind(_runManager.Map, currentNodeId, nextIds, _runManager.State.VisitedNodeIds);
            SetMapButtonEnabled(false);  // 노드 선택 모드 — peek 버튼 비활성
        }

        private void ShowNodeMap()
        {
            if (_runManager == null) return;

            if (_nodeMapView == null)
            {
                _runManager.AdvanceNode();
                _onContinueRequested?.Invoke();
                return;
            }

            int currentNodeId = _runManager.State.CurrentNodeId;
            IReadOnlyList<MapNode> nextCandidates = _runManager.GetAvailableNextNodes();
            int[] nextIds = new int[nextCandidates.Count];
            for (int i = 0; i < nextCandidates.Count; i++) nextIds[i] = nextCandidates[i].Id;

            // Show() 먼저 (Awake 선행) → Bind. ShowInitialNodeMap 주석 참조.
            _nodeMapView.Show();
            _nodeMapView.Bind(_runManager.Map, currentNodeId, nextIds, _runManager.State.VisitedNodeIds, animateIcon: true);
            SetMapButtonEnabled(false);  // 노드 선택 모드 — peek 버튼 비활성
        }

        private void HandleNodeMapClicked(int nodeId)
        {
            if (_runManager == null) return;

            // Peek 모드(지도 버튼으로 열림): 노드 진행 없이 단순 닫기.
            if (_mapPeekMode)
            {
                if (_nodeMapView != null) _nodeMapView.Hide();
                _mapPeekMode = false;
                return;
            }

            SoundManager.Instance?.PlayMapClick();

            int currentNodeId = _runManager.State.CurrentNodeId;

            // 현재 서 있는 노드를 클릭한 경우.
            if (nodeId == currentNodeId)
            {
                // 시작 노드(전투 없음)는 자기 자신 클릭 시 아무 동작 안 함 — node 0을 눌러 진행한다.
                if (_runManager.GetCurrentNode().Type == NodeType.Start)
                    return;

                // (레거시) 그 외 현재 노드 클릭 → 해당 노드 전투 진입.
                if (_nodeMapView != null) _nodeMapView.Hide();
                SetMapButtonEnabled(true);  // 선택 종료 — peek 버튼 복원
                _onContinueRequested?.Invoke();
                return;
            }

            try
            {
                _runManager.SelectNextNode(nodeId);
            }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning($"[BattleUIAdapter] 노드맵 클릭 무시: {e.Message}");
                return;
            }

            _nodeMapView.SetAllButtonsEnabled(false);

            int newNodeId = _runManager.State.CurrentNodeId;
            IReadOnlyList<MapNode> nextCandidates = _runManager.GetAvailableNextNodes();
            int[] nextIds = new int[nextCandidates.Count];
            for (int i = 0; i < nextCandidates.Count; i++) nextIds[i] = nextCandidates[i].Id;

            _nodeMapView.Bind(_runManager.Map, newNodeId, nextIds,
                _runManager.State.VisitedNodeIds, animateIcon: true,
                onIconMoveComplete: () =>
                {
                    if (_nodeMapView != null)
                    {
                        _nodeMapView.SetAllButtonsEnabled(true);
                        _nodeMapView.Hide();
                    }
                    SetMapButtonEnabled(true);
                    _onContinueRequested?.Invoke();
                });
        }

        private void BuildPresenters()
        {
            _playerPresenter = new PlayerPresenter(_playerView);

            _enemyPresenter = new EnemyPresenter(
                tier => _enemyView.SpawnEntry(tier),
                _spriteMap);

            var diceEntryInterfaces = new List<IDiceEntryView>();
            foreach (var e in _diceEntries) diceEntryInterfaces.Add(e);

            _dicePresenter = new DicePresenter(
                _diceView,
                diceEntryInterfaces,
                keepMask => _onRerollRequested?.Invoke(keepMask));

            _dicePresenter.OnRollingFinished += () => SetBackButtonEnabled(true);

            _slotAssignmentPresenter = new SlotAssignmentPresenter(
                _slotAssignmentView,
                _dicePresenter,
                OnUseSkillButtonClicked);

            _targetSelectionPresenter = new TargetSelectionPresenter(_targetSelectionView);
            _targetSelectionPresenter.SetEnemyPresenter(_enemyPresenter);
            _targetSelectionPresenter.SetPlayerView(_playerView);

            _battleLogPresenter = new BattleLogPresenter(
                _battleLogView,
                _playerView.transform,
                BuildEnemyTransforms());
        }

        private Transform[] BuildEnemyTransforms()
        {
            return _enemyPresenter?.GetEntryTransforms() ?? Array.Empty<Transform>();
        }

        private void WireViewEvents()
        {
            for (int i = 0; i < _diceEntries.Count; i++)
            {
                int idx = i;
                _diceEntries[i].OnToggled += () =>
                {
                    _dicePresenter.OnDieToggleKeep(idx);
                    OnTutorialEvent?.Invoke("DiceKept");
                };
            }

            _slotAssignmentView.OnSkillCardClicked += id =>
            {
                _slotAssignmentPresenter.OnSkillClicked(id);
                OnTutorialEvent?.Invoke("SkillSelected");
                var slots = _slotAssignmentPresenter.GetSlots();
                int filled = 0;
                foreach (var s in slots) if (s != null) filled++;
                if (filled >= SlotManager.MAX_SLOTS)
                    OnTutorialEvent?.Invoke("AllSlotsFilled");
            };
            _slotAssignmentView.OnRerollClicked    += _dicePresenter.RequestReroll;
            _slotAssignmentView.OnSlotClicked      += _slotAssignmentPresenter.OnSlotClicked;

            _diceView.OnUseSkillClicked += OnUseSkillButtonClicked;

            _enemyPresenter.OnEnemyClicked += idx =>
            {
                _targetSelectionPresenter.OnEnemyClicked(idx);
                OnTutorialEvent?.Invoke("TargetSelected");
            };
            _targetSelectionView.OnSlotClicked += _targetSelectionPresenter.OnSlotClicked;
            _targetSelectionView.OnExecuteClicked += HandleExecuteClicked;

            WireBackButton();
        }

        /// <summary>
        /// Screen B(DiceTablePanel)의 뒤로가기 버튼을 Screen A(진행 중 배틀 화면)로 전환하도록 연결.
        /// 상태 리셋 없이 패널 가시성만 토글한다 — Roll Dice 버튼이 재진입 시 상태 유지 분기 처리.
        /// 씬에 정적으로 배치된 BackButton을 DiceTablePanel(=_diceView) 하위에서 이름으로 탐색.
        /// SafeArea 등 래퍼 GameObject가 중간에 삽입돼도 동작하도록 재귀 탐색을 사용한다.
        /// (회귀 이력: SafeArea wrapper 도입으로 정적 경로 "ActionButtons/BackButton"이 깨지며 클릭 무응답)
        /// </summary>
        private void WireBackButton()
        {
            if (_diceView == null || _uiManager == null) return;
            Transform backBtnT = FindDescendantByName(_diceView.transform, "BackButton");
            if (backBtnT == null)
            {
                Debug.LogWarning("[BattleUIAdapter] WireBackButton: 'BackButton' GameObject not found under DiceView. 뒤로가기 동작이 비활성화됩니다.");
                return;
            }
            var btn = backBtnT.GetComponent<UnityEngine.UI.Button>();
            if (btn == null)
            {
                Debug.LogWarning("[BattleUIAdapter] WireBackButton: 'BackButton'에 Button 컴포넌트가 없습니다.");
                return;
            }
            _backButton = btn;
            btn.onClick.AddListener(HandleBackClicked);
        }

        /// <summary>지정한 root 하위에서 이름이 일치하는 첫 Transform을 DFS로 탐색.</summary>
        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == targetName) return child;
                Transform inner = FindDescendantByName(child, targetName);
                if (inner != null) return inner;
            }
            return null;
        }

        private void SetBackButtonEnabled(bool enabled)
        {
            if (_backButton != null) _backButton.interactable = enabled;
        }

        private void HandleBackClicked()
        {
            if (_dicePresenter != null && _dicePresenter.IsRolling) return;

            MirrorAssignedSlotsToScreenA();
            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
        }

        /// <summary>SlotAssignmentPresenter가 보유한 현재 슬롯을 Screen A 슬롯 뷰(slotViewA)에 복사한다.</summary>
        private void MirrorAssignedSlotsToScreenA()
        {
            if (_slotAssignmentPresenter == null || _targetSelectionView == null) return;
            SkillData[] slots = _slotAssignmentPresenter.GetSlots();
            if (slots == null) return;

            var cards = new SkillCardData[slots.Length];
            PlayerState player = _playerPresenter?.Player;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                string valueText = null;
                if (player != null)
                {
                    int enhLv = player.GetEnhanceLevel(slots[i].Hand);
                    valueText = OUD.Unity.Battle.SkillValueHelper.BuildValueText(
                        slots[i], player.Atk, player.Def, enhLv);
                }
                cards[i] = new SkillCardData
                {
                    SkillId      = slots[i].Id,
                    DisplayName  = slots[i].Name,
                    RequiredHand = slots[i].Hand,
                    Category     = slots[i].Category,
                    ValueText    = valueText,
                    IsEnabled    = true
                };
            }
            _targetSelectionView.ShowSlots(cards);
        }

        /// <summary>진행 중인 턴 상태를 그대로 유지하면서 주사위 패널(Screen B)을 다시 표시.</summary>
        public void ShowDicePanel()
        {
            if (_uiManager != null)
                _uiManager.ShowScreen(UIManager.BattleScreen.B_DiceTable);
        }

        /// <summary>현재 노드 타입에 맞춰 전투 배경 스프라이트를 UIManager에 전달.
        /// Combat / Shop → 숲 배경 (Combat sprite).
        /// Boss → 보스 배경 sprite 있으면 사용, 없으면 숲 배경 fallback.
        /// Elite → 엘리트 sprite 있으면 사용, 없으면 숲 배경 fallback.</summary>
        /// <summary>노드 타입 + 레이어에 따라 전투 배경 스프라이트 선택.
        /// Combat 노드는 layer 4 이상이면 2스테이지 배경 사용.</summary>
        public void SetBattleBackground(NodeType nodeType, int layer = 0)
        {
            Sprite chosen;
            if (nodeType == NodeType.Boss)
                chosen = _bossBackgroundSprite != null ? _bossBackgroundSprite : _combatBackgroundSprite;
            else if (nodeType == NodeType.Elite)
                chosen = _eliteBackgroundSprite != null ? _eliteBackgroundSprite : _combatBackgroundSprite;
            else if (layer >= 4 && _combatBackground2Sprite != null)
                chosen = _combatBackground2Sprite;
            else
                chosen = _combatBackgroundSprite;

            _uiManager.SetBattleBackground(chosen);
        }

        private void OnUseSkillButtonClicked()
        {
            // 주사위 롤링 중에는 확정 불가
            if (_dicePresenter != null && _dicePresenter.IsRolling) return;

            if (_turnManager == null)
            {
                Debug.LogWarning("[BattleUIAdapter] _turnManager null — Initialize가 호출되지 않은 인스턴스입니다. 무시합니다.");
                return;
            }

            OnTutorialEvent?.Invoke("UseSkillClicked");

            _isQueueMode = true;
            _turnManager.ConfirmDice();

            if (_hasUsableSkills)
            {
                _isQueueMode = false;
                HandleUseSkillClicked();
            }
            else
            {
                _isQueueMode = false;
                _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
                StartQueuePlayback();
            }
        }

        private void HandleUseSkillClicked()
        {
            _uiManager.ShowScreen(UIManager.BattleScreen.C_Targeting);
            _enemyPresenter.RefreshAll();
            _enemyPresenter.SetTargetSelectable(true);

            // _battleEnemies(전체 목록)를 전달해야 인덱스가 EnemyPresenter와 일치.
            // BattleState.AliveEnemies는 필터된 리스트라 인덱스 불일치 → 프리뷰 미표시 버그 발생.
            var slots = _slotAssignmentPresenter.GetSlots();

            // DEBUG: 슬롯 비어있는 버그 추적
            int filled = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) filled++;
            Debug.Log($"[HandleUseSkillClicked] slots={slots.Length}, filled={filled}");
            if (filled == 0)
                Debug.LogWarning("[HandleUseSkillClicked] 슬롯이 모두 비어있습니다! 기술 선택이 유실된 것 같습니다.");

            _targetSelectionPresenter.Begin(
                slots,
                _battleEnemies,
                targetIndices => { },
                _playerPresenter.Player);
        }

        private void HandleExecuteClicked()
        {
            OnTutorialEvent?.Invoke("ExecuteClicked");
            _enemyPresenter.SetTargetSelectable(false);
            _playerView.ClearDefenseBadges();
            var targetIndices = _targetSelectionPresenter.GetTargetIndices();

            _isQueueMode = true;
            if (targetIndices != null)
                _slotAssignmentPresenter.Confirm(targetIndices);
            _isQueueMode = false;

            _slotAssignmentPresenter.ResetForNewTurn();
            _targetSelectionPresenter?.ResetForNewTurn();
            if (_targetSelectionView != null) _targetSelectionView.ResetForNewTurn();

            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
            StartQueuePlayback();
        }

        // ── IBattleUI ────────────────────────────────────────────────────

        public void OnBattleStart(PlayerState player, List<MonsterInstance> enemies)
        {
            if (_battleLogView != null) _battleLogView.HideResultScreens();

            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);

            _battleEnemies = enemies;
            _playerPresenter.Init(player);
            _enemyPresenter.Init(enemies);
            _battleLogPresenter = new BattleLogPresenter(
                _battleLogView,
                _playerView.transform,
                BuildEnemyTransforms());

            // 이전 전투의 슬롯 / 주사위 keep / 기술 카드가 새 스테이지에 남지 않도록 초기화.
            _slotAssignmentPresenter.ResetForNewTurn();
            _dicePresenter.ResetKeep();
            _targetSelectionPresenter?.ResetForNewTurn();
            if (_targetSelectionView != null) _targetSelectionView.ResetForNewTurn();
            _hasUsableSkills = false;

            RefreshTopBar();
            ShowPlayerTurnReady(); // 전투 시작 직후 첫 턴 — 플레이어가 Roll Dice 입력을 기다리는 시점.
            OnTutorialEvent?.Invoke("BattleStart");
        }

        /// <summary>플레이어가 조작권을 얻어 Roll Dice 입력을 기다리는 시점에 "Your Turn" 배너를 띄우고
        /// Roll Dice 버튼에 글로우를 건다. 실제 StartPlayerTurn()은 Roll Dice 클릭이 호출하므로,
        /// 체감상 "내 턴 시작"은 전투 시작 직후 / 적 턴 큐 재생 종료 후 이 시점이다.
        /// 글로우는 튜토리얼 전투에선 생략한다(TutorialOverlayView가 같은 버튼을 직접 제어 → 충돌 방지).</summary>
        private void ShowPlayerTurnReady()
        {
            _turnBannerView?.ShowYourTurn();
            if (!_isTutorialBattle) _turnBannerView?.SetRollDiceGlow(true);
            // "Your Turn" 배너가 실제로 뜨는 시점 — 튜토리얼 쉴드 안내가 이 신호 이후에 나오도록.
            OnTutorialEvent?.Invoke("PlayerTurnReady");
        }

        public void OnPlayerTurnStarted()
        {
            _slotAssignmentPresenter.ResetForNewTurn();
            _dicePresenter.ResetKeep();
            _targetSelectionPresenter?.ResetForNewTurn();
            if (_targetSelectionView != null) _targetSelectionView.ResetForNewTurn();
            _hasUsableSkills = false;

            var allLearned = SkillDatabase.GetSkillsByUnlockedHands(_playerPresenter.Player.UnlockedHands);
            _slotAssignmentPresenter.ShowAllSkillsDisabled(allLearned, _playerPresenter.Player);
            OnTutorialEvent?.Invoke("PlayerTurnStarted");
        }

        public void OnDiceRolled(int[] values, int rerollsLeft)
        {
            _hasUsableSkills = false;
            _turnBannerView?.SetRollDiceGlow(false); // 주사위를 굴리면 "지금 네 턴" 글로우 해제.
            _uiManager.ShowScreen(UIManager.BattleScreen.B_DiceTable);
            _dicePresenter.UpdateDice(values, rerollsLeft);
            _slotAssignmentPresenter.OnRerollCountChanged(rerollsLeft);
            SoundManager.Instance?.PlayDiceRoll();

            SetBackButtonEnabled(!_dicePresenter.IsRolling);
            OnTutorialEvent?.Invoke("DiceRolled");
            if (rerollsLeft == 0)
                OnTutorialEvent?.Invoke("RerollsExhausted");
        }

        public void OnHandsEvaluated(List<HandType> hands, List<SkillData> usableSkills) { }

        public void RequestSlotAssignment(
            List<SkillData>            usableSkills,
            List<SkillData>            allLearnedSkills,
            List<MonsterInstance>      aliveEnemies,
            Action<List<SlotAssignment>> onComplete)
        {
            _hasUsableSkills     = true;
            _pendingSlotCallback = onComplete;
            _slotAssignmentPresenter.Begin(usableSkills, allLearnedSkills, aliveEnemies, onComplete, _playerPresenter.Player);
            OnTutorialEvent?.Invoke("SkillsReady");
        }

        public void OnSlotExecuted(int slotIndex, SkillResult result)
        {
            if (_isQueueMode)
            {
                _actionQueue.Enqueue(new QueuedBattleAction
                {
                    Type = BattleActionType.SlotExecuted,
                    Index = slotIndex,
                    SkillResult = result,
                    Snapshot = CaptureSnapshot()
                });
                return;
            }
            _battleLogPresenter.ShowSlotResult(slotIndex, result);
            _playerPresenter.SyncView();
            _enemyPresenter.RefreshAll();
            RefreshTopBar();

            if (result.Skill.Category == SkillCategory.Attack)
                SoundManager.Instance?.PlayPlayerAttack();
            else
                SoundManager.Instance?.PlayShield();
        }

        public void OnEnemyAction(int enemyIndex, IntentType intent, int value)
        {
            if (intent == IntentType.Shield)
                OnTutorialEvent?.Invoke("EnemyShielded");

            if (_isQueueMode)
            {
                _actionQueue.Enqueue(new QueuedBattleAction
                {
                    Type = BattleActionType.EnemyAction,
                    Index = enemyIndex,
                    Intent = intent,
                    Value = value,
                    Snapshot = CaptureSnapshot()
                });
                return;
            }
            _enemyPresenter.ShowAction(enemyIndex, intent, value);
            _battleLogPresenter.ShowEnemyAction(enemyIndex, intent, value);
            SpawnHitEffectOnPlayer(enemyIndex, intent);
            _playerPresenter.SyncView();
            RefreshTopBar();

            if (intent == IntentType.Attack || intent == IntentType.StrongAttack)
                SoundManager.Instance?.PlayMonsterAttack();
        }

        public void OnIntentUpdated(int enemyIndex, IntentType intent, int value)
        {
            if (_isQueueMode)
            {
                _actionQueue.Enqueue(new QueuedBattleAction
                {
                    Type = BattleActionType.IntentUpdated,
                    Index = enemyIndex,
                    Intent = intent,
                    Value = value,
                    Snapshot = CaptureSnapshot()
                });
                return;
            }
            _enemyPresenter.ShowAction(enemyIndex, intent, value);
        }

        public void OnEnemySummoned(int enemyIndex, MonsterInstance clone)
        {
            if (_isQueueMode)
            {
                _actionQueue.Enqueue(new QueuedBattleAction
                {
                    Type = BattleActionType.EnemySummoned,
                    Index = enemyIndex,
                    SummonedMonster = clone,
                    Snapshot = CaptureSnapshot()
                });
                return;
            }
            _enemyPresenter.AddEntry(clone, enemyIndex);
        }

        public void OnShieldsReset()
        {
            if (_isQueueMode)
            {
                _actionQueue.Enqueue(new QueuedBattleAction
                {
                    Type = BattleActionType.ShieldsReset,
                    Snapshot = CaptureSnapshot()
                });
                return;
            }
            _playerPresenter.SyncShield();
            _enemyPresenter.RefreshAllShields();
            RefreshTopBar();
        }

        public void OnBattleWon()
        {
            if (_isQueueMode)
            {
                _actionQueue.Enqueue(new QueuedBattleAction
                {
                    Type = BattleActionType.BattleWon,
                    Snapshot = CaptureSnapshot()
                });
                return;
            }

            if (IsPlayingQueue)
                StopPlayback();

            _playerPresenter.SyncView();
            _enemyPresenter.RefreshAll();
            RefreshTopBar();
            ExecuteBattleWon();
        }

        private void ExecuteBattleWon()
        {
            OnTutorialEvent?.Invoke("BattleWon");
            SoundManager.Instance?.PlayVictory();
            _battleLogPresenter.ShowBattleWon();
            _winRewardTransitioned = false;
            if (_battleLogView != null)
            {
                _battleLogView.OnWinScreenClicked -= HandleWinScreenClicked;
                _battleLogView.OnWinScreenClicked += HandleWinScreenClicked;
            }
            StartCoroutine(ShowRewardAfterWinScreen());
        }

        private const float WIN_SCREEN_DURATION = 3.0f;
        private bool _winRewardTransitioned;

        private void HandleWinScreenClicked()
        {
            TransitionToReward();
        }

        private System.Collections.IEnumerator ShowRewardAfterWinScreen()
        {
            // WinScreen 표시 중에는 화면 어디든 클릭/탭하면 즉시 보상 화면으로 넘어간다.
            // UI Button 한 개에만 의존하면 다른 패널이 raycast를 가로챌 때 동작하지 않으므로
            // 글로벌 Pointer 입력(마우스 + 터치 통합)을 폴링하여 처리한다.
            float elapsed = 0f;
            // VICTORY 화면 진입 직후 직전 클릭이 잔류해 즉시 닫히는 것 방지용 1프레임 대기.
            yield return null;
            while (elapsed < WIN_SCREEN_DURATION)
            {
                if (_winRewardTransitioned) yield break;
                var pointer = Pointer.current;  // Mouse / TouchScreen / Pen 통합 추상화
                if (pointer != null && pointer.press.wasPressedThisFrame)
                {
                    TransitionToReward();
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            TransitionToReward();
        }

        private void TransitionToReward()
        {
            if (_winRewardTransitioned) return;
            _winRewardTransitioned = true;
            if (_battleLogView != null)
            {
                _battleLogView.OnWinScreenClicked -= HandleWinScreenClicked;
                _battleLogView.HideResultScreens();
            }

            // 튜토리얼 전투: 보상/레벨업을 건너뛰고 곧바로 노드맵 표시.
            // CurrentNodeId는 시작 노드 그대로이므로 ShowNodeMap이 시작 노드(현재) +
            // node 0(다음 진입 가능)을 바인딩한다.
            if (_isTutorialBattle)
            {
                _isTutorialBattle = false;
                ShowNodeMap();
                return;
            }

            GrantReward();
        }

        public void OnBattleLost()
        {
            if (_isQueueMode)
            {
                _actionQueue.Enqueue(new QueuedBattleAction
                {
                    Type = BattleActionType.BattleLost,
                    Snapshot = CaptureSnapshot()
                });
                return;
            }

            if (IsPlayingQueue)
                StopPlayback();

            _playerPresenter.SyncView();
            _enemyPresenter.RefreshAll();
            RefreshTopBar();
            ExecuteBattleLost();
        }

        private void ExecuteBattleLost()
        {
            SoundManager.Instance?.PlayDefeat();
            _battleLogPresenter.ShowBattleLost();
            _loseDefeatTransitioned = false;
            if (_battleLogView != null)
            {
                _battleLogView.OnLoseScreenClicked -= HandleLoseScreenClicked;
                _battleLogView.OnLoseScreenClicked += HandleLoseScreenClicked;
            }
            StartCoroutine(ShowDefeatAfterLoseScreen());
        }

        private const float LOSE_SCREEN_DURATION = 3.0f;
        private bool _loseDefeatTransitioned;

        private void HandleLoseScreenClicked()
        {
            TransitionToDefeat();
        }

        private System.Collections.IEnumerator ShowDefeatAfterLoseScreen()
        {
            float elapsed = 0f;
            // 패배 화면 진입 직후 직전 클릭이 잔류해 즉시 닫히는 것 방지용 1프레임 대기.
            yield return null;
            while (elapsed < LOSE_SCREEN_DURATION)
            {
                if (_loseDefeatTransitioned) yield break;
                var pointer = Pointer.current;
                if (pointer != null && pointer.press.wasPressedThisFrame)
                {
                    TransitionToDefeat();
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            TransitionToDefeat();
        }

        private void TransitionToDefeat()
        {
            if (_loseDefeatTransitioned) return;
            _loseDefeatTransitioned = true;
            if (_battleLogView != null)
            {
                _battleLogView.OnLoseScreenClicked -= HandleLoseScreenClicked;
                _battleLogView.HideResultScreens();
            }
            ShowDefeatScreen();
        }

        private void GrantReward()
        {
            if (_rewardSystem == null) return;
            if (_playerPresenter == null) return;
            PlayerState player = _playerPresenter.Player;
            if (player == null) return;

            MapNode currentNode = _runManager.GetCurrentNode();
            RewardResult reward = _rewardSystem.CalculateReward(currentNode.Type);
            player.AddXp(reward.Xp);
            player.AddGold(reward.Gold);

            SoundManager.Instance?.PlayCoin();

            if (_rewardView != null)
            {
                _rewardView.SetReward(reward.Xp, reward.Gold, player.Xp, player.Gold);
                _rewardView.Show();
            }

            _playerPresenter.SyncView();
            RefreshTopBar();
        }

        // ── Queue Playback Engine ────────────────────────────────────────

        private DisplaySnapshot CaptureSnapshot()
        {
            var player = _playerPresenter.Player;
            var snap = new DisplaySnapshot
            {
                PlayerHp = player.Hp,
                PlayerMaxHp = player.MaxHp,
                PlayerShield = player.Shield,
                Enemies = new EnemySnapshot[_battleEnemies.Count]
            };
            for (int i = 0; i < _battleEnemies.Count; i++)
            {
                var m = _battleEnemies[i];
                snap.Enemies[i] = new EnemySnapshot
                {
                    Hp = m.Hp,
                    MaxHp = m.Data.MaxHp,
                    Shield = m.Shield,
                    IsDead = m.IsDead
                };
            }
            return snap;
        }

        private void ApplySnapshot(DisplaySnapshot snapshot)
        {
            _playerPresenter.DisplayValues(snapshot.PlayerHp, snapshot.PlayerMaxHp, snapshot.PlayerShield);
            for (int i = 0; i < snapshot.Enemies.Length; i++)
            {
                var e = snapshot.Enemies[i];
                _enemyPresenter.DisplayValues(i, e.Hp, e.MaxHp, e.Shield, e.IsDead);
            }
            RefreshTopBarFromSnapshot(snapshot.PlayerHp, snapshot.PlayerMaxHp);
        }

        private void RefreshTopBarFromSnapshot(int hp, int maxHp)
        {
            if (_topBarText == null || _playerPresenter == null) return;
            PlayerState player = _playerPresenter.Player;
            if (player == null) return;

            string nameText = string.IsNullOrEmpty(_playerName) ? "Player" : _playerName;
            string xpText;
            if (LevelUpSystem.IsMaxLevel(player.Level))
                xpText = "MAX";
            else
                xpText = $"{player.Xp}/{LevelUpSystem.GetXpThreshold(player.Level)}";
            _topBarText.text = $"{nameText}  HP {hp}/{maxHp}  <sprite name=\"xp\"> {xpText}  <sprite name=\"gold\"> {player.Gold}";
        }

        private void StartQueuePlayback()
        {
            if (_actionQueue.Count == 0)
            {
                IsPlayingQueue = false;
                ShowPlayerTurnReady(); // 큐가 비어 바로 플레이어 입력 대기로 돌아가는 경우(드묾).
                return;
            }
            _enemyTurnBannerShown = false; // 이번 큐 재생에서 "Enemy Turn" 배너 1회 허용.
            IsPlayingQueue = true;
            _playbackCoroutine = StartCoroutine(PlayActionQueue());
        }

        private System.Collections.IEnumerator PlayActionQueue()
        {
            bool battleEnded = false;
            while (_actionQueue.Count > 0)
            {
                var action = _actionQueue.Dequeue();

                // 플레이어 스킬 해소가 모두 끝나고 첫 적 행동이 나오기 직전에 "Enemy Turn" 배너 1회.
                // (적 행동이 없는 큐 — 적 전멸 등 — 에선 자동으로 표시되지 않는다.)
                if (action.Type == BattleActionType.EnemyAction && !_enemyTurnBannerShown)
                {
                    _enemyTurnBannerShown = true;
                    _turnBannerView?.ShowEnemyTurn();
                    yield return new WaitForSeconds(ENEMY_BANNER_LEAD);
                }

                ExecuteQueuedAction(action);

                if (action.Type == BattleActionType.BattleWon || action.Type == BattleActionType.BattleLost)
                {
                    battleEnded = true;
                    _actionQueue.Clear();
                    break;
                }

                float delay = ACTION_DELAY;
                if (action.Type == BattleActionType.IntentUpdated)
                    delay = INTENT_DELAY;
                if (delay > 0f && _actionQueue.Count > 0
                    && _actionQueue.Peek().Type == BattleActionType.IntentUpdated
                    && action.Type == BattleActionType.IntentUpdated)
                    delay = 0f;

                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }

            _playerPresenter.SyncView();
            _enemyPresenter.RefreshAll();
            RefreshTopBar();

            IsPlayingQueue = false;
            _playbackCoroutine = null;

            // 적 턴 큐 재생이 끝나고 전투가 계속되면 → 플레이어가 다시 Roll Dice를 기다리는 시점.
            // 승리/패배로 끝난 경우엔 결과 화면과 겹치지 않도록 "Your Turn"을 띄우지 않는다.
            if (!battleEnded) ShowPlayerTurnReady();
        }

        private void ExecuteQueuedAction(QueuedBattleAction action)
        {
            if (action.Type == BattleActionType.EnemySummoned)
            {
                _enemyPresenter.AddEntry(action.SummonedMonster, action.Index);
                _battleLogPresenter = new BattleLogPresenter(
                    _battleLogView,
                    _playerView.transform,
                    BuildEnemyTransforms());
                ApplySnapshot(action.Snapshot);
                return;
            }

            ApplySnapshot(action.Snapshot);

            switch (action.Type)
            {
                case BattleActionType.SlotExecuted:
                    _battleLogPresenter.ShowSlotResult(action.Index, action.SkillResult);
                    if (action.SkillResult.Skill.Category == SkillCategory.Attack)
                        SoundManager.Instance?.PlayPlayerAttack();
                    else
                        SoundManager.Instance?.PlayShield();
                    break;

                case BattleActionType.EnemyAction:
                    _enemyPresenter.ShowAction(action.Index, action.Intent, action.Value);
                    _battleLogPresenter.ShowEnemyAction(action.Index, action.Intent, action.Value);
                    SpawnHitEffectOnPlayer(action.Index, action.Intent);
                    if (action.Intent == IntentType.Attack || action.Intent == IntentType.StrongAttack)
                        SoundManager.Instance?.PlayMonsterAttack();
                    break;

                case BattleActionType.ShieldsReset:
                    break;

                case BattleActionType.IntentUpdated:
                    _enemyPresenter.ShowAction(action.Index, action.Intent, action.Value);
                    break;

                case BattleActionType.BattleWon:
                    ExecuteBattleWon();
                    break;

                case BattleActionType.BattleLost:
                    ExecuteBattleLost();
                    break;
            }
        }

        private void StopPlayback()
        {
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
            _actionQueue.Clear();
            IsPlayingQueue = false;
        }

        // ── Hit Effect — 적 공격 피격 연출 ────────────────────────────────
        //
        // 적 공격(Attack/StrongAttack) 적중 시 플레이어 카드 중심에 피격 이펙트를 1회 스폰한다.
        // 공격자 몬스터 id로 HitEffectLibrary에서 프레임/틴트를 조회한다.
        // 비큐(OnEnemyAction) / 큐 재생(ExecuteQueuedAction) 양쪽에서 ShowEnemyAction 직후 호출되어
        // 큐 타이밍과 자동 동기화된다.
        private void SpawnHitEffectOnPlayer(int enemyIndex, IntentType intent)
        {
            // 공격 의도일 때만 — 수비/소환 등은 피격 연출 없음.
            if (intent != IntentType.Attack && intent != IntentType.StrongAttack) return;
            if (_hitEffectLibrary == null || _hitEffectPrefab == null) return;
            if (_battleEnemies == null || enemyIndex < 0 || enemyIndex >= _battleEnemies.Count) return;

            MonsterInstance attacker = _battleEnemies[enemyIndex];
            if (attacker == null) return;

            if (!_hitEffectLibrary.TryGet(attacker.Data.Id, out Sprite[] frames, out Color tint))
                return;

            RectTransform playerCard = _playerView != null ? _playerView.transform as RectTransform : null;
            if (playerCard == null) return;

            Transform parent = _hitEffectOverlay != null ? _hitEffectOverlay : playerCard;
            HitEffectPlayer effect = Instantiate(_hitEffectPrefab, parent);

            RectTransform rt = effect.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);

                // 위치: 플레이어 카드 중심 (피벗 무관).
                rt.position = playerCard.TransformPoint(playerCard.rect.center);

                // 크기: 카드 월드 사이즈의 HIT_EFFECT_SCALE 배 — 부모 캔버스 스케일을 보정해
                // 실제 렌더 크기가 카드 1.5배가 되도록 sizeDelta를 역산한다.
                Vector2 cardWorld = new Vector2(
                    playerCard.rect.width  * playerCard.lossyScale.x,
                    playerCard.rect.height * playerCard.lossyScale.y);
                Vector2 targetWorld = cardWorld * HIT_EFFECT_SCALE;
                Vector3 ls = rt.lossyScale;
                rt.sizeDelta = new Vector2(
                    Mathf.Approximately(ls.x, 0f) ? targetWorld.x : targetWorld.x / ls.x,
                    Mathf.Approximately(ls.y, 0f) ? targetWorld.y : targetWorld.y / ls.y);

                rt.SetAsLastSibling();  // 카드보다 위에 그림 (클리핑 없는 오버레이 부모 전제)
            }

            effect.Play(frames, tint);
        }

        private void RefreshTopBar()
        {
            if (_topBarText == null) return;
            if (_playerPresenter == null) return;
            PlayerState player = _playerPresenter.Player;
            if (player == null) return;

            string nameText = string.IsNullOrEmpty(_playerName) ? "Player" : _playerName;

            string xpText;
            if (LevelUpSystem.IsMaxLevel(player.Level))
            {
                xpText = "MAX";
            }
            else
            {
                int threshold = LevelUpSystem.GetXpThreshold(player.Level);
                xpText = $"{player.Xp}/{threshold}";
            }

            _topBarText.text = $"{nameText}  HP {player.Hp}/{player.MaxHp}  <sprite name=\"xp\"> {xpText}  <sprite name=\"gold\"> {player.Gold}";
        }
    }
}
