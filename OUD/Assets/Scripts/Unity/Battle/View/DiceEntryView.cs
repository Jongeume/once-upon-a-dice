using System;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>주사위 1개 UI. 눈 표시 + Keep 토글 하이라이트.</summary>
    public class DiceEntryView : ViewBase, IDiceEntryView
    {
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Image    _background;
        [SerializeField] private Button   _button;

        [Header("색상")]
        [SerializeField] private Color _normalColor  = new Color(0.09f, 0.06f, 0.04f);
        [SerializeField] private Color _keptColor    = new Color(0.83f, 0.63f, 0.09f);

        [Header("선택 보더")]
        [SerializeField] private Vector2 _borderThickness = new Vector2(4f, 4f);

        private bool _kept;
        private Outline _outline;

        public event Action OnToggled;

        private void Awake()
        {
            if (_button) _button.onClick.AddListener(() =>
            {
                OnToggled?.Invoke();
            });

            if (_background)
            {
                _background.color = _normalColor;

                _outline = _background.GetComponent<Outline>();
                if (_outline == null) _outline = _background.gameObject.AddComponent<Outline>();
                _outline.effectDistance = _borderThickness;
                _outline.useGraphicAlpha = false;

                var hidden = _keptColor;
                hidden.a = 0f;
                _outline.effectColor = hidden;
            }
        }

        public void UpdateValue(int value)
        {
            if (_valueText) _valueText.text = value > 0 ? value.ToString() : "-";
        }

        public void SetKept(bool kept)
        {
            _kept = kept;
            if (_outline)
            {
                var c = _keptColor;
                c.a = kept ? 1f : 0f;
                _outline.effectColor = c;
            }
        }
    }
}
