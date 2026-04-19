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
        [SerializeField] private Color _enabledColor  = new Color(0.09f, 0.06f, 0.04f, 0.9f);
        [SerializeField] private Color _disabledColor = new Color(0.16f, 0.13f, 0.09f, 0.6f);

        private string _skillId;

        public void Setup(SkillCardData card, Action<string> onClick)
        {
            _skillId = card.SkillId;
            if (_nameText) _nameText.text = card.DisplayName;
            if (_handText) _handText.text = $"({card.RequiredHand})";
            SetEnabled(card.IsEnabled);
            if (_button) _button.onClick.AddListener(() => onClick?.Invoke(_skillId));
        }

        public void SetEnabled(bool enabled)
        {
            if (_button)     _button.interactable = enabled;
            if (_background) _background.color    = enabled ? _enabledColor : _disabledColor;
        }
    }
}
