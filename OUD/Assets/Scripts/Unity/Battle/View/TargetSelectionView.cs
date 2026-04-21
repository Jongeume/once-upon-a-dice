using System.Collections.Generic;
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
        [SerializeField] private List<LineRenderer> _targetLines;

        public event System.Action OnExecuteClicked;

        private void Awake()
        {
            if (_executeButton) _executeButton.onClick.AddListener(() => OnExecuteClicked?.Invoke());
        }

        public void ShowSlots(SkillCardData[] slotCards)
        {
            _slotView?.ClearAll();
            if (slotCards == null) return;
            for (int i = 0; i < slotCards.Length; i++)
                if (slotCards[i] != null)
                    _slotView?.SetSlot(i, slotCards[i]);
        }

        public void HighlightSlot(int slotIndex) => _slotView?.HighlightSlot(slotIndex);

        public void ShowTargetLink(int slotIndex, int enemyIndex)
        {
            if (_targetLines == null || slotIndex >= _targetLines.Count) return;
            if (_targetLines[slotIndex] != null)
                _targetLines[slotIndex].enabled = true;
        }

        public void ClearTargetLinks()
        {
            if (_targetLines == null) return;
            foreach (var line in _targetLines)
                if (line != null) line.enabled = false;
        }

        public void SetExecuteButtonActive(bool active)
        {
            if (_executeButton) _executeButton.interactable = active;
        }
    }
}
