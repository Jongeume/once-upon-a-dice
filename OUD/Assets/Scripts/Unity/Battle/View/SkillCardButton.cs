using System;
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
        [SerializeField] private Color _enabledColor  = new Color(0.18f, 0.12f, 0.08f, 0.95f);
        [SerializeField] private Color _disabledColor = new Color(0.10f, 0.08f, 0.06f, 0.45f);

        [Header("외곽선")]
        [SerializeField] private Color _enabledOutline  = new Color(0.95f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color _disabledOutline = new Color(0.30f, 0.25f, 0.18f, 0.5f);

        private string _skillId;
        private Outline _outline;

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
            if (_nameText) _nameText.text = card.DisplayName;
            if (_handText) _handText.text = card.ValueText ?? $"({card.RequiredHand})";
            SetEnabled(card.IsEnabled);
            if (_button) _button.onClick.AddListener(() => onClick?.Invoke(_skillId));
        }

        public void SetEnabled(bool enabled)
        {
            if (_button)     _button.interactable = enabled;
            if (_background) _background.color    = enabled ? _enabledColor : _disabledColor;
            EnsureOutline();
            if (_outline)
            {
                _outline.effectColor = enabled ? _enabledOutline : _disabledOutline;
                _outline.enabled     = true;
            }
        }
    }
}
