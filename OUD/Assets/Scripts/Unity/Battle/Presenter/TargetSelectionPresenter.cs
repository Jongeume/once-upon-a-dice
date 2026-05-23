using System;
using System.Collections.Generic;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;
using OUD.Unity.Battle;

namespace OUD.Unity.Battle.Presenter
{
    /// <summary>
    /// 화면 C — 슬롯별 타겟 지정 흐름.
    /// 공격 기술 → 적 클릭 대기 / 수비 기술 → 자동 스킵.
    /// 타겟 변경 허용. 모든 타겟 확정 시 턴 종료 버튼 활성화.
    /// </summary>
    public class TargetSelectionPresenter
    {
        private readonly ITargetSelectionView _view;
        private EnemyPresenter _enemyPresenter;
        private IPlayerView    _playerView;

        private SkillData[]   _slots;
        private int[]         _targetIndices;
        private int           _activeSlotIndex;
        private List<MonsterInstance> _aliveEnemies;
        private Action<int[]> _onAllTargetsConfirmed;
        private PlayerState   _playerState;

        public TargetSelectionPresenter(ITargetSelectionView view) => _view = view;

        public void SetEnemyPresenter(EnemyPresenter enemyPresenter) => _enemyPresenter = enemyPresenter;

        public void SetPlayerView(IPlayerView playerView) => _playerView = playerView;

        /// <summary>새 턴/새 전투 시작 시 내부 상태 초기화. 이전 턴 슬롯이 클릭 처리에 영향 주지 않도록.</summary>
        public void ResetForNewTurn()
        {
            _slots                 = null;
            _targetIndices         = null;
            _activeSlotIndex       = -1;
            _aliveEnemies          = null;
            _onAllTargetsConfirmed = null;
            _enemyPresenter?.ClearAllTargetBadges();
            _playerView?.ClearDefenseBadges();
        }

        public void Begin(
            SkillData[]            slots,
            List<MonsterInstance>  aliveEnemies,
            Action<int[]>          onAllTargetsConfirmed,
            PlayerState            playerState = null)
        {
            _slots                 = slots;
            _aliveEnemies          = aliveEnemies;
            _onAllTargetsConfirmed = onAllTargetsConfirmed;
            if (playerState != null) _playerState = playerState;
            _targetIndices         = new int[slots.Length];
            for (int i = 0; i < _targetIndices.Length; i++) _targetIndices[i] = -1;

            _view.ShowSlots(ToSlotCards(slots));
            _view.ClearTargetLinks();
            _view.SetExecuteButtonActive(false);
            _view.SetSlotClickable(true);

            // AllEnemies / Self 자동 배정 뱃지
            ShowAutoAssignedBadges();

            AdvanceToNextAttackSlot(0);
        }

        private SkillCardData[] ToSlotCards(SkillData[] slots)
        {
            var cards = new SkillCardData[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                SkillData s = slots[i];
                string valueText = null;
                if (_playerState != null)
                {
                    int enhLv = _playerState.GetEnhanceLevel(s.Hand);
                    valueText = SkillValueHelper.BuildValueText(
                        s, _playerState.Atk, _playerState.Def, enhLv);
                }
                cards[i] = new SkillCardData
                {
                    SkillId      = s.Id,
                    DisplayName  = s.Name,
                    RequiredHand = s.Hand,
                    Category     = s.Category,
                    ValueText    = valueText,
                    IsEnabled    = true
                };
            }
            return cards;
        }

        /// <summary>적 클릭 시 View에서 호출.</summary>
        public void OnEnemyClicked(int enemyIndex)
        {
            if (_activeSlotIndex < 0 || _activeSlotIndex >= _slots.Length) return;
            if (_slots[_activeSlotIndex] == null) return;
            if (_slots[_activeSlotIndex].Target != TargetType.Single) return;

            _targetIndices[_activeSlotIndex] = enemyIndex;
            RefreshAllTargetBadges();

            // 다음 미확정 공격 슬롯으로 이동
            int next = FindNextUnconfirmedAttackSlot(_activeSlotIndex + 1);
            if (next < 0)
            {
                // 모든 타겟 확정
                _activeSlotIndex = -1;
                _view.SetExecuteButtonActive(true);
                _onAllTargetsConfirmed?.Invoke(_targetIndices);
            }
            else
            {
                AdvanceToNextAttackSlot(next);
            }
        }

        /// <summary>슬롯 클릭 시 해당 슬롯으로 타겟 재선택.</summary>
        public void OnSlotClicked(int slotIndex)
        {
            // 타겟 선택 모드(Begin 호출 후)가 아니면 무시 — 빈 슬롯 클릭으로 인한 오작동 방지.
            if (_slots == null) return;
            if (slotIndex < 0 || slotIndex >= _slots.Length) return;
            if (_slots[slotIndex] == null) return;
            if (_slots[slotIndex].Target != TargetType.Single) return;
            _activeSlotIndex = slotIndex;
            _view.HighlightSlot(slotIndex);
            _view.SetExecuteButtonActive(false);
        }

        private void AdvanceToNextAttackSlot(int startFrom)
        {
            for (int i = startFrom; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;
                if (_slots[i].Target == TargetType.Single)
                {
                    _activeSlotIndex = i;
                    _view.HighlightSlot(i);
                    return;
                }
            }
            // Single 슬롯 없음 → 즉시 확정
            _activeSlotIndex = -1;
            _view.SetExecuteButtonActive(true);
            _onAllTargetsConfirmed?.Invoke(_targetIndices);
        }

        /// <summary>현재까지 확정된 타겟 인덱스 배열 반환. Execute 버튼 클릭 시 Confirm()에 전달.</summary>
        public int[] GetTargetIndices() => _targetIndices;

        private void ShowAutoAssignedBadges()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;

                if (_slots[i].Target == TargetType.AllEnemies && _enemyPresenter != null)
                {
                    for (int e = 0; e < _aliveEnemies.Count; e++)
                    {
                        if (_aliveEnemies[e].IsDead) continue;
                        _enemyPresenter.ShowTargetBadge(e, _slots[i].Name, SkillCategory.Attack, isAoe: true);
                    }
                }
                else if (_slots[i].Target == TargetType.Self && _playerView != null)
                {
                    _playerView.ShowDefenseBadge(_slots[i].Name);
                }
            }
        }

        private void RefreshAllTargetBadges()
        {
            if (_enemyPresenter == null) return;
            _enemyPresenter.ClearAllTargetBadges();
            if (_slots == null || _targetIndices == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;

                if (_slots[i].Target == TargetType.AllEnemies)
                {
                    for (int e = 0; e < _aliveEnemies.Count; e++)
                    {
                        if (_aliveEnemies[e].IsDead) continue;
                        _enemyPresenter.ShowTargetBadge(e, _slots[i].Name, SkillCategory.Attack, isAoe: true);
                    }
                }
                else if (_slots[i].Target == TargetType.Single && _targetIndices[i] >= 0)
                {
                    _enemyPresenter.ShowTargetBadge(_targetIndices[i], _slots[i].Name, SkillCategory.Attack, isAoe: false);
                }
            }
        }

        private int FindNextUnconfirmedAttackSlot(int startFrom)
        {
            for (int i = startFrom; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;
                if (_slots[i].Target == TargetType.Single && _targetIndices[i] < 0)
                    return i;
            }
            return -1;
        }
    }
}
