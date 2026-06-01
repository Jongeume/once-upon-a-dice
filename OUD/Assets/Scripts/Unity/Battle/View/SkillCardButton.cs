using System;
using OUD.BattleEngine.Core;
using OUD.Unity.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>기술 목록 한 줄 버튼. 활성화 여부에 따라 색 변경.</summary>
    public class SkillCardButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _handText;
        [SerializeField] private Button   _button;
        [SerializeField] private Image    _background;

        [Header("색상")]
        [SerializeField] private Color _atkEnabledColor = new Color(0.55f, 0.15f, 0.12f, 0.95f);
        [SerializeField] private Color _defEnabledColor = new Color(0.10f, 0.25f, 0.55f, 0.95f);
        [SerializeField] private Color _disabledColor = new Color(0.10f, 0.08f, 0.06f, 0.45f);

        [Header("외곽선")]
        [SerializeField] private Color _enabledOutline  = new Color(0.95f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color _disabledOutline = new Color(0.30f, 0.25f, 0.18f, 0.5f);

        private string _skillId;
        private Outline _outline;
        private Color _resolvedEnabledColor;
        private TMP_Text _valueText;

        private void Awake()
        {
            EnsureOutline();
        }

        private void EnsureOutline()
        {
            if (_outline != null) return;
            _outline = GetComponent<Outline>();
            if (_outline == null) _outline = gameObject.AddComponent<Outline>();
            _outline.effectDistance = new Vector2(2.5f, -2.5f);
        }

        public void Setup(SkillCardData card, Action<string> onClick)
        {
            _skillId = card.SkillId;
            _resolvedEnabledColor = card.Category == SkillCategory.Attack
                ? _atkEnabledColor
                : _defEnabledColor;
            if (_nameText)
            {
                _nameText.text = card.DisplayName;
                _nameText.color = Color.white;
                _nameText.enableAutoSizing = true;
                _nameText.fontSizeMin = 14f;
                _nameText.fontSizeMax = 24f;
                _nameText.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (_handText)
            {
                _handText.text = $"({card.RequiredHand})";
                _handText.alignment = TextAlignmentOptions.MidlineLeft;
                _handText.color = Color.white;
                _handText.enableAutoSizing = true;
                _handText.fontSizeMin = 10f;
                _handText.fontSizeMax = 16f;
                _handText.overflowMode = TextOverflowModes.Ellipsis;
            }
            SetValueText(card.ValueText);
            if (_valueText) _valueText.color = Color.white;
            SetEnabled(card.IsEnabled);
            if (_button) _button.onClick.AddListener(() => onClick?.Invoke(_skillId));
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
            _valueText.enableAutoSizing = true;
            _valueText.fontSizeMin = 10f;
            _valueText.fontSizeMax = 16f;
            _valueText.overflowMode = TextOverflowModes.Ellipsis;
            _valueText.raycastTarget = false;

            var rt = _valueText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void SetEnabled(bool enabled)
        {
            if (_button)     _button.interactable = enabled;
            var bgColor = _resolvedEnabledColor.a > 0 ? _resolvedEnabledColor : _atkEnabledColor;
            if (_background) _background.color    = enabled ? bgColor : _disabledColor;
            EnsureOutline();
            if (_outline)
            {
                _outline.effectColor = enabled ? _enabledOutline : _disabledOutline;
                _outline.enabled     = true;
            }
        }
    }
}
