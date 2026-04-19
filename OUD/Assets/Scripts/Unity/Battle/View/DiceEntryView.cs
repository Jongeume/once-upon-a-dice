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

        private bool _kept;

        public event Action OnToggled;

        private void Awake()
        {
            if (_button) _button.onClick.AddListener(() =>
            {
                OnToggled?.Invoke();
            });
        }

        public void UpdateValue(int value)
        {
            if (_valueText) _valueText.text = value.ToString();
        }

        public void SetKept(bool kept)
        {
            _kept = kept;
            if (_background) _background.color = kept ? _keptColor : _normalColor;
        }
    }
}
