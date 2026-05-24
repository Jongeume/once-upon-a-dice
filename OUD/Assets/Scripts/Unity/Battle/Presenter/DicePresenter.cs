using System;
using System.Collections.Generic;
using OUD.Unity.Battle;

namespace OUD.Unity.Battle.Presenter
{
    public class DicePresenter
    {
        private readonly IDiceView            _diceView;
        private readonly List<IDiceEntryView> _entryViews;
        private readonly Action<bool[]>       _onRerollRequested;

        private int[]  _values   = new int[5];
        private bool[] _keepMask = new bool[5];
        private int    _rerollsLeft;
        private bool   _isRolling;
        private int    _pendingCount;
        private bool   _isFirstRollOfTurn = true;
        private int    _totalRollsThisTurn;

        public const int   MAX_REROLLS         = 3;
        public const int   DICE_COUNT          = 5;
        public const float BASE_ROLL_DURATION  = 0.8f;
        public const float STOP_STAGGER        = 0.2f;

        public DicePresenter(
            IDiceView diceView,
            List<IDiceEntryView> entryViews,
            Action<bool[]> onRerollRequested)
        {
            _diceView          = diceView;
            _entryViews        = entryViews;
            _onRerollRequested = onRerollRequested;
        }

        public void UpdateDice(int[] values, int rerollsLeft)
        {
            _values      = values;
            _rerollsLeft = rerollsLeft;
            _totalRollsThisTurn++;

            // 첫 롤: 애니메이션 없이 즉시 결과 표시
            if (_isFirstRollOfTurn)
            {
                _isFirstRollOfTurn = false;
                for (int i = 0; i < DICE_COUNT; i++)
                    _entryViews[i].SetResultImmediate(values[i]);

                bool canReroll = _rerollsLeft > 0;
                _diceView.UpdateRerollInfo(_rerollsLeft, canReroll);
                return;
            }

            // 리롤: Keep된 주사위는 즉시, 나머지는 애니메이션
            var rollingIndices = new List<int>();
            for (int i = 0; i < DICE_COUNT; i++)
            {
                if (_keepMask[i])
                    _entryViews[i].SetResultImmediate(values[i]);
                else
                    rollingIndices.Add(i);
            }

            if (rollingIndices.Count == 0)
            {
                bool canReroll = _rerollsLeft > 0;
                _diceView.UpdateRerollInfo(_rerollsLeft, canReroll);
                return;
            }

            _isRolling    = true;
            _pendingCount = rollingIndices.Count;

            _diceView.UpdateRerollInfo(_rerollsLeft, false);
            _diceView.SetConfirmButtonActive(false);

            for (int order = 0; order < rollingIndices.Count; order++)
            {
                int idx   = rollingIndices[order];
                float delay = BASE_ROLL_DURATION + order * STOP_STAGGER;
                _entryViews[idx].PlayRoll(values[idx], delay, OnDieSettled);
            }
        }

        private void OnDieSettled()
        {
            _pendingCount--;
            if (_pendingCount > 0) return;

            _isRolling = false;
            bool canReroll = _rerollsLeft > 0;
            _diceView.UpdateRerollInfo(_rerollsLeft, canReroll);
        }

        public void OnDieToggleKeep(int index)
        {
            if (_isRolling) return;
            if (_totalRollsThisTurn < 2) return; // 리롤 전에는 Keep 불가
            _keepMask[index] = !_keepMask[index];
            _entryViews[index].SetKept(_keepMask[index]);
        }

        public void RequestReroll()
        {
            if (_isRolling) return;
            if (_rerollsLeft <= 0) return;
            _onRerollRequested?.Invoke(_keepMask);
        }

        public int    RerollsLeft   => _rerollsLeft;
        public bool[] GetKeepMask() => _keepMask;
        public bool   IsRolling     => _isRolling;

        public void ResetKeep()
        {
            _isFirstRollOfTurn = true;
            _totalRollsThisTurn = 0;
            for (int i = 0; i < DICE_COUNT; i++)
            {
                _keepMask[i] = false;
                _entryViews[i].SetKept(false);
            }
        }

        public void RefreshConfirmButton(int filledSlots)
        {
            if (_isRolling) return;
            bool slotsFull = filledSlots >= 3;
            bool noReroll  = _rerollsLeft <= 0;
            _diceView.SetConfirmButtonActive(slotsFull || noReroll);
        }
    }
}
