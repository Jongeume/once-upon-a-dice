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
        [SerializeField] private Color _emptyColor = new Color(0.16f, 0.10f, 0.06f, 1f);
        [SerializeField] private Color _atkColor = new Color(0.55f, 0.15f, 0.12f, 0.95f);
        [SerializeField] private Color _defColor = new Color(0.10f, 0.25f, 0.55f, 0.95f);
        [SerializeField] private Color _highlightColor = new Color(0.95f, 0.78f, 0.18f, 1f);

        // 색상 fallback (SerializeField가 직렬화된 0,0,0,0으로 덮인 경우 대비)
        private static readonly Color FallbackEmpty = new Color(0.16f, 0.10f, 0.06f, 1f);
        private static readonly Color FallbackAttack = new Color(0.55f, 0.15f, 0.12f, 0.95f);
        private static readonly Color FallbackDefense = new Color(0.10f, 0.25f, 0.55f, 0.95f);
        private static readonly Color FallbackHighlight = new Color(0.95f, 0.78f, 0.18f, 1f);

        public event System.Action OnClicked;

        private bool _initialized;
        private Color _baseColor;
        private bool _highlighted;
        private bool _isEmpty = true;
        private TMP_Text _valueText;

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
            _isEmpty = false;
            if (_nameText) _nameText.text = card.DisplayName;
            if (_handText)
            {
                _handText.text = card.RequiredHand.ToString();
                _handText.alignment = TextAlignmentOptions.MidlineLeft;
            }
            SetValueText(card.ValueText);
            _baseColor = card.Category == SkillCategory.Attack
                ? ResolveColor(_atkColor, FallbackAttack)
                : ResolveColor(_defColor, FallbackDefense);
            ApplyBackground();
            if (_nameText) _nameText.color = Color.white;
            if (_handText) _handText.color = Color.white;
            if (_valueText) _valueText.color = Color.white;
        }

        // 빈 슬롯 텍스트 색: 어두운 회색 (배경과 구분되되 눈에 띄지 않게)
        private static readonly Color EmptyTextColor = new Color(0.35f, 0.28f, 0.22f, 1f);

        public void Clear()
        {
            Init();
            _isEmpty = true;
            if (_nameText)   { _nameText.text = "Empty Slot"; _nameText.color = EmptyTextColor; }
            if (_handText)   { _handText.text = "";           _handText.color = EmptyTextColor; }
            if (_valueText)  _valueText.text  = "";
            _baseColor = ResolveColor(_emptyColor, FallbackEmpty);
            SetHighlight(false);
            ApplyBackground();
        }

        private void SetValueText(string text)
        {
            if (_handText == null) return;
            if (string.IsNullOrEmpty(text))
            {
                if (_valueText) _valueText.text = "";
                return;
            }
            EnsureValueText();
            if (_valueText) _valueText.text = text;
        }

        private void EnsureValueText()
        {
            if (_valueText) return;
            if (_handText == null) return;

            var go = new GameObject("ValueText");
            go.transform.SetParent(_handText.transform, false);
            _valueText = go.AddComponent<TextMeshProUGUI>();
            _valueText.font = _handText.font;
            _valueText.fontSharedMaterial = _handText.fontSharedMaterial;
            _valueText.fontSize = _handText.fontSize;
            _valueText.color = _handText.color;
            _valueText.alignment = TextAlignmentOptions.MidlineRight;
            _valueText.overflowMode = TextOverflowModes.Overflow;
            _valueText.raycastTarget = false;

            var rt = _valueText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void SetHighlight(bool on)
        {
            // 빈 슬롯이면 하이라이트 무시 — 버튼 클릭은 받지만 시각 변화 없음.
            _highlighted = on && !_isEmpty;
            if (_highlight) _highlight.enabled = _highlighted;
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
