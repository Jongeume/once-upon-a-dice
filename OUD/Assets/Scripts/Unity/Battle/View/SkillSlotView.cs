using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 기술 슬롯 3개 표시. 화면 A/B/C에서 동일 위치 유지 (B1).
    /// SlotAssignmentView와 TargetSelectionView 양쪽에서 참조된다.
    /// </summary>
    public class SkillSlotView : ViewBase
    {
        [SerializeField] private List<SkillSlotEntry> _slots;

        public void SetSlot(int index, SkillCardData card)
        {
            if (index < 0 || index >= _slots.Count) return;
            _slots[index].SetCard(card);
        }

        public void ClearAll()
        {
            foreach (var s in _slots) s.Clear();
        }

        public void HighlightSlot(int index)
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetHighlight(i == index);
        }

        public void SetSlotClickable(bool clickable)
        {
            foreach (var s in _slots) s.SetClickable(clickable);
        }

        public event System.Action<int> OnSlotClicked;

        private void Awake()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                int idx = i;
                _slots[i].OnClicked += () => OnSlotClicked?.Invoke(idx);
            }
        }
    }

    [System.Serializable]
    public class SkillSlotEntry
    {
        [SerializeField] private TMP_Text   _nameText;
        [SerializeField] private TMP_Text   _handText;
        [SerializeField] private Image      _background;
        [SerializeField] private Image      _highlight;
        [SerializeField] private Button     _button;

        [Header("색상")]
        [SerializeField] private Color _emptyColor    = new Color(0.16f, 0.10f, 0.06f);
        [SerializeField] private Color _atkColor      = new Color(0.75f, 0.25f, 0.19f, 0.8f);
        [SerializeField] private Color _defColor      = new Color(0.16f, 0.38f, 0.75f, 0.8f);
        [SerializeField] private Color _highlightColor = new Color(0.83f, 0.63f, 0.09f, 0.6f);

        public event System.Action OnClicked;

        private bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;
            if (_button) _button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        public void SetCard(SkillCardData card)
        {
            Init();
            if (_nameText) _nameText.text = card.DisplayName;
            if (_handText) _handText.text = card.RequiredHand.ToString();
            if (_background)
            {
                _background.color = card.Category == SkillCategory.Attack
                    ? _atkColor : _defColor;
            }
        }

        public void Clear()
        {
            Init();
            if (_nameText)   _nameText.text   = "빈 슬롯";
            if (_handText)   _handText.text   = "";
            if (_background) _background.color = _emptyColor;
            SetHighlight(false);
        }

        public void SetHighlight(bool on)
        {
            if (_highlight) _highlight.enabled = on;
        }

        public void SetClickable(bool clickable)
        {
            if (_button) _button.interactable = clickable;
        }
    }
}
