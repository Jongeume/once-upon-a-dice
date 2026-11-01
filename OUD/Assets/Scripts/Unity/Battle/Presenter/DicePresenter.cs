using System;
using System.Collections.Generic;
using OUD.Unity.Battle;

namespace OUD.Unity.Battle.Presenter
{
    /// <summary>
    /// 주사위 값 배열 + Keep 토글 관리.
    /// 리롤 요청은 Action 콜백으로 Adapter에 전달.
    /// </summary>
    public class DicePresenter
    {
        private readonly IDiceView         _diceView;
        private readonly List<IDiceEntryView> _entryViews;
        private readonly Action<bool[]>    _onRerollRequested;

        private int[]  _values     = new int[5];
        private bool[] _keepMask   = new bool[5];
        private int    _rerollsLeft;

        public const int MAX_REROLLS = 2;
        public const int DICE_COUNT  = 5;

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

            for (int i = 0; i < DICE_COUNT; i++)
                _entryViews[i].UpdateValue(values[i]);

            bool canReroll = _rerollsLeft > 0;
            _diceView.UpdateRerollInfo(_rerollsLeft, canReroll);
        }

        public void OnDieToggleKeep(int index)
        {
            _keepMask[index] = !_keepMask[index];
            _entryViews[index].SetKept(_keepMask[index]);
        }

        /// <summary>리롤 버튼 클릭 시 호출.</summary>
        public void RequestReroll()
        {
            if (_rerollsLeft <= 0) return;
            _onRerollRequested?.Invoke(_keepMask);
        }

        public int    RerollsLeft   => _rerollsLeft;
        public bool[] GetKeepMask() => _keepMask;

        public void ResetKeep()
        {
            for (int i = 0; i < DICE_COUNT; i++)
            {
                _keepMask[i] = false;
                _entryViews[i].SetKept(false);
            }
        }

        /// <summary>슬롯이 꽉 차거나 리롤 소진 시 확정 버튼 활성화 요청.</summary>
        public void RefreshConfirmButton(int filledSlots)
        {
            bool slotsFull = filledSlots >= 3;
            bool noReroll  = _rerollsLeft <= 0;
            _diceView.SetConfirmButtonActive(slotsFull || noReroll);
        }
    }
}
