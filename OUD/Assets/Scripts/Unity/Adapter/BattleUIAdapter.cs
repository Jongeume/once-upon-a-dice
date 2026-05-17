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

        [Header("화면 관리")]
        [SerializeField] private UIManager            _uiManager;

        [Header("MonsterSpriteMap")]
        [SerializeField] private MonsterSpriteMap     _spriteMap;

        [Header("주사위 Entry Views (5개)")]
        [SerializeField] private List<DiceEntryView>  _diceEntries;

        [Header("상단 정보 바 (선택)")]
        [SerializeField] private string   _playerName = "Alice";
        [SerializeField] private TMP_Text _topBarText;

        [Header("전투 배경")]
        [SerializeField] private UnityEngine.UI.Image _battleBackgroundImage;
        [SerializeField] private Sprite               _combatBackgroundSprite;
        [SerializeField] private Sprite               _eliteBackgroundSprite;
        [SerializeField] private Color                _eliteFallbackColor = new Color(0.05f, 0.03f, 0.02f, 1f);

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
        private bool _growthViewsWired;
        private bool _endRunViewsWired;
        private bool _shopViewWired;

        private bool _hasUsableSkills = false;

        private PostBattleFlow _pendingFlow;

        private void Awake()
        {
            BuildPresenters();
            WireViewEvents();
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
            PlayerState player = _runManager.State.Player;
            if (_levelUpStatView != null)
            {
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
            PlayerState player = _playerPresenter.Player;

            if (_levelUpStatView != null)
            {
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

        private void HandleStatChosen(StatChoice choice)
        {
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
            PlayerState player = _runManager.State.Player;
            if (_levelUpSkillView != null && player.Sp > 0)
            {
                _levelUpSkillView.Bind(player.Sp, player.UnlockedHands);
                _levelUpSkillView.Show();
                _pendingShopLevelUp = true;
            }
            else
            {
                FinishShop();
            }
        }

        private void HandleUnlockChosen(HandType hand)
        {
            PlayerState player = _playerPresenter.Player;
            _skillPointSystem.Unlock(player, hand);
            if (_levelUpSkillView != null) _levelUpSkillView.Hide();

            if (_pendingShopLevelUp)
            {
                _pendingShopLevelUp = false;
                FinishShop();
                return;
            }

            ContinueAfterLevelUp(_pendingFlow);
        }

        private void HandleSkillSkipClicked()
        {
            if (_levelUpSkillView != null) _levelUpSkillView.Hide();

            if (_pendingShopLevelUp)
            {
                _pendingShopLevelUp = false;
                FinishShop();
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

            _nodeMapView.Bind(_runManager.Map, currentNodeId, nextIds);
            _nodeMapView.Show();
        }

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
            return _enemyPresenter?.GetEntryTransforms() ?? Array.Empty<Transform>();
        }

        private void WireViewEvents()
        {
            for (int i = 0; i < _diceEntries.Count; i++)
            {
                int idx = i;
                _diceEntries[i].OnToggled += () => _dicePresenter.OnDieToggleKeep(idx);
            }

            _slotAssignmentView.OnSkillCardClicked += _slotAssignmentPresenter.OnSkillClicked;
            _slotAssignmentView.OnRerollClicked    += _dicePresenter.RequestReroll;

            _diceView.OnUseSkillClicked += OnUseSkillButtonClicked;

            _enemyPresenter.OnEnemyClicked += _targetSelectionPresenter.OnEnemyClicked;
            _targetSelectionView.OnSlotClicked += _targetSelectionPresenter.OnSlotClicked;
            _targetSelectionView.OnExecuteClicked += HandleExecuteClicked;

            WireBackButton();
        }

        /// <summary>
        /// Screen B(DiceTablePanel)의 뒤로가기 버튼을 Screen A(진행 중 배틀 화면)로 전환하도록 연결.
        /// 상태 리셋 없이 패널 가시성만 토글한다 — Roll Dice 버튼이 재진입 시 상태 유지 분기 처리.
        /// BuildUI_Part2에서 생성된 "ActionButtons/BackButton" 경로를 따라 런타임에 찾는다.
        /// </summary>
        private void WireBackButton()
        {
            if (_diceView == null || _uiManager == null) return;
            Transform backBtnT = _diceView.transform.Find("ActionButtons/BackButton");
            if (backBtnT == null) return;
            var btn = backBtnT.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) return;
            btn.onClick.AddListener(HandleBackClicked);
        }

        private void HandleBackClicked()
        {
            // Screen B에서 배정한 슬롯을 Screen A의 슬롯 패널에 미러링 후 전환.
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
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                cards[i] = new SkillCardData
                {
                    SkillId      = slots[i].Id,
                    DisplayName  = slots[i].Name,
                    RequiredHand = slots[i].Hand,
                    Category     = slots[i].Category,
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

        /// <summary>현재 노드 타입에 맞춰 전투 배경 스프라이트/색을 적용.
        /// Combat / Boss / Shop → 숲 배경 (Combat sprite).
        /// Elite → 별도 sprite 있으면 사용, 없으면 어두운 단색.</summary>
        public void SetBattleBackground(NodeType nodeType)
        {
            if (_battleBackgroundImage == null) return;

            if (nodeType == NodeType.Elite)
            {
                if (_eliteBackgroundSprite != null)
                {
                    _battleBackgroundImage.sprite = _eliteBackgroundSprite;
                    _battleBackgroundImage.color  = Color.white;
                }
                else
                {
                    _battleBackgroundImage.sprite = null;
                    _battleBackgroundImage.color  = _eliteFallbackColor;
                }
            }
            else
            {
                _battleBackgroundImage.sprite = _combatBackgroundSprite;
                _battleBackgroundImage.color  = _combatBackgroundSprite != null ? Color.white : _eliteFallbackColor;
            }
        }

        private void OnUseSkillButtonClicked()
        {
            if (_turnManager == null)
            {
                Debug.LogWarning("[BattleUIAdapter] _turnManager null — Initialize가 호출되지 않은 인스턴스입니다. 무시합니다.");
                return;
            }

            _turnManager.ConfirmDice();

            if (_hasUsableSkills)
                HandleUseSkillClicked();
        }

        private void HandleUseSkillClicked()
        {
            _uiManager.ShowScreen(UIManager.BattleScreen.C_Targeting);
            _enemyPresenter.SetTargetSelectable(true);

            _targetSelectionPresenter.Begin(
                _slotAssignmentPresenter.GetSlots(),
                _slotAssignmentPresenter.GetAliveEnemies(),
                targetIndices => { });
        }

        private void HandleExecuteClicked()
        {
            _enemyPresenter.SetTargetSelectable(false);
            var targetIndices = _targetSelectionPresenter.GetTargetIndices();
            if (targetIndices != null)
                _slotAssignmentPresenter.Confirm(targetIndices);

            // 기술 실행 직후 슬롯을 빈 상태로 표시 — 다음 Roll Dice 전 Screen A에서 사용된 스킬이 잔류하지 않도록.
            _slotAssignmentPresenter.ResetForNewTurn();
            _targetSelectionPresenter?.ResetForNewTurn();
            if (_targetSelectionView != null) _targetSelectionView.ResetForNewTurn();

            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
        }

        // ── IBattleUI ────────────────────────────────────────────────────

        public void OnBattleStart(PlayerState player, List<MonsterInstance> enemies)
        {
            if (_battleLogView != null) _battleLogView.HideResultScreens();

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

            _uiManager.ShowScreen(UIManager.BattleScreen.A_BattleBasic);
            RefreshTopBar();
        }

        public void OnPlayerTurnStarted()
        {
            _slotAssignmentPresenter.ResetForNewTurn();
            _dicePresenter.ResetKeep();
            _targetSelectionPresenter?.ResetForNewTurn();
            if (_targetSelectionView != null) _targetSelectionView.ResetForNewTurn();
            _hasUsableSkills = false;
        }

        public void OnDiceRolled(int[] values, int rerollsLeft)
        {
            _hasUsableSkills = false;
            _dicePresenter.UpdateDice(values, rerollsLeft);
            _slotAssignmentPresenter.OnRerollCountChanged(rerollsLeft);
            _uiManager.ShowScreen(UIManager.BattleScreen.B_DiceTable);
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
            _slotAssignmentPresenter.Begin(usableSkills, allLearnedSkills, aliveEnemies, onComplete);
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

        public void OnIntentUpdated(int enemyIndex, IntentType intent, int value)
        {
            _enemyPresenter.ShowAction(enemyIndex, intent, value);
        }

        public void OnShieldsReset()
        {
            _playerPresenter.SyncShield();
            _enemyPresenter.RefreshAllShields();
            RefreshTopBar();
        }

        public void OnBattleWon()
        {
            // VICTORY 화면 표시 → 사용자가 화면 클릭 또는 일정 시간 경과 시 보상 화면으로 전환.
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
            // WinScreen 표시 중에는 화면 어디든 마우스 클릭하면 즉시 보상 화면으로 넘어간다.
            // UI Button 한 개에만 의존하면 다른 패널이 raycast를 가로챌 때 동작하지 않으므로
            // 글로벌 Mouse 입력을 폴링하여 처리한다.
            float elapsed = 0f;
            // VICTORY 화면 진입 직후 직전 클릭이 잔류해 즉시 닫히는 것 방지용 1프레임 대기.
            yield return null;
            while (elapsed < WIN_SCREEN_DURATION)
            {
                if (_winRewardTransitioned) yield break;
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
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
            GrantReward();
        }

        public void OnBattleLost()
        {
            _battleLogPresenter.ShowBattleLost();
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

            if (_rewardView != null)
            {
                _rewardView.SetReward(reward.Xp, reward.Gold, player.Xp, player.Gold);
                _rewardView.Show();
            }

            _playerPresenter.SyncView();
            RefreshTopBar();
        }

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
