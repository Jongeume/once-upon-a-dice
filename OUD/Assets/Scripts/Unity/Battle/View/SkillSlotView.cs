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
            // 씬에 직접 입력된 텍스트(예: "Empty Slot") 대신 코드 일관 라벨로 강제 초기화
            ClearAll();
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
        [SerializeField] private Color _emptyColor    = new Color(0.16f, 0.10f, 0.06f, 1f);
        [SerializeField] private Color _atkColor      = new Color(0.75f, 0.25f, 0.19f, 0.95f);
        [SerializeField] private Color _defColor      = new Color(0.16f, 0.38f, 0.75f, 0.95f);
        [SerializeField] private Color _highlightColor = new Color(0.95f, 0.78f, 0.18f, 1f);

        // 색상 fallback (SerializeField가 직렬화된 0,0,0,0으로 덮인 경우 대비)
        private static readonly Color FallbackEmpty     = new Color(0.16f, 0.10f, 0.06f, 1f);
        private static readonly Color FallbackAttack    = new Color(0.75f, 0.25f, 0.19f, 0.95f);
        private static readonly Color FallbackDefense   = new Color(0.16f, 0.38f, 0.75f, 0.95f);
        private static readonly Color FallbackHighlight = new Color(0.95f, 0.78f, 0.18f, 1f);

        public event System.Action OnClicked;

        private bool _initialized;
        private Color _baseColor;
        private bool _highlighted;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;
            if (_button) _button.onClick.AddListener(() => OnClicked?.Invoke());
            _baseColor = ResolveColor(_emptyColor, FallbackEmpty);
            ApplyBackground();
        }

        public void SetCard(SkillCardData card)
        {
            Init();
            if (_nameText) _nameText.text = card.DisplayName;
            if (_handText) _handText.text = card.RequiredHand.ToString();
            _baseColor = card.Category == SkillCategory.Attack
                ? ResolveColor(_atkColor, FallbackAttack)
                : ResolveColor(_defColor, FallbackDefense);
            ApplyBackground();
        }

        public void Clear()
        {
            Init();
            if (_nameText)   _nameText.text   = "빈 슬롯";
            if (_handText)   _handText.text   = "";
            _baseColor = ResolveColor(_emptyColor, FallbackEmpty);
            SetHighlight(false);
            ApplyBackground();
        }

        public void SetHighlight(bool on)
        {
            _highlighted = on;
            if (_highlight) _highlight.enabled = on;
            ApplyBackground();
        }

        public void SetClickable(bool clickable)
        {
            if (_button) _button.interactable = clickable;
        }

        private void ApplyBackground()
        {
            if (!_background) return;
            // Highlight Image가 없는 경우(_highlight=null) background 색을 강조색으로 대체
            if (_highlighted && _highlight == null)
                _background.color = ResolveColor(_highlightColor, FallbackHighlight);
            else
                _background.color = _baseColor;
        }

        private static Color ResolveColor(Color serialized, Color fallback)
        {
            // 알파가 0이거나 모든 채널이 0이면 직렬화 누락으로 간주 → fallback
            if (serialized.a <= 0.001f) return fallback;
            if (serialized.r <= 0.001f && serialized.g <= 0.001f && serialized.b <= 0.001f && serialized.a <= 0.001f)
                return fallback;
            return serialized;
        }
    }
}
