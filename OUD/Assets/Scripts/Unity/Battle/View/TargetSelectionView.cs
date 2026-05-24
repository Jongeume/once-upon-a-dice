using OUD.Unity.Battle;
using OUD.Unity.Common;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 화면 C 전용. 타겟 연결선 + 턴 종료 버튼.
    /// ITargetSelectionView 구현.
    /// </summary>
    public class TargetSelectionView : ViewBase, ITargetSelectionView
    {
        [SerializeField] private Button      _executeButton;
        [SerializeField] private SkillSlotView _slotView;

        public event System.Action OnExecuteClicked;
        public event System.Action<int> OnSlotClicked;

        private void Awake()
        {
            if (_executeButton) _executeButton.onClick.AddListener(() => OnExecuteClicked?.Invoke());
            if (_slotView != null)
                _slotView.OnSlotClicked += idx => OnSlotClicked?.Invoke(idx);
        }

        public void SetSlotClickable(bool clickable) => _slotView?.SetSlotClickable(clickable);

        public void ShowSlots(SkillCardData[] slotCards)
        {
            _slotView?.ClearAll();
            if (slotCards == null) return;
            for (int i = 0; i < slotCards.Length; i++)
                if (slotCards[i] != null)
                    _slotView?.SetSlot(i, slotCards[i]);
        }

        public void HighlightSlot(int slotIndex) => _slotView?.HighlightSlot(slotIndex);

        public void ShowTargetLink(int slotIndex, int enemyIndex) { }

        public void ClearTargetLinks() { }

        public void SetExecuteButtonActive(bool active)
        {
            if (_executeButton) _executeButton.interactable = active;
        }

        /// <summary>새 플레이어 턴 시작 시 호출. Screen A 슬롯 패널과 타겟 선을 비운다.</summary>
        public void ResetForNewTurn()
        {
            _slotView?.ClearAll();
            ClearTargetLinks();
            SetExecuteButtonActive(false);
        }
    }
}
