// RestView.cs
// F-10 휴식 UI — 골드 → HP 회복 교환.
using System;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class RestView : ViewBase
    {
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _investText;

        [SerializeField] private Button _minusButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Button _restButton;
        [SerializeField] private Button _skipButton;

        public event Action<int> OnRestConfirmed;
        public event Action OnRestSkipped;

        private int _investGold;
        private int _maxGold;
        private int _currentHp;
        private int _maxHp;

        private const int UNIT = 10;
        private const int HP_PER_UNIT = 5;

        private void Awake()
        {
            if (_minusButton != null)
                _minusButton.onClick.AddListener(OnMinusClicked);
            if (_plusButton != null)
                _plusButton.onClick.AddListener(OnPlusClicked);
            if (_restButton != null)
                _restButton.onClick.AddListener(() => OnRestConfirmed?.Invoke(_investGold));
            if (_skipButton != null)
                _skipButton.onClick.AddListener(() => OnRestSkipped?.Invoke());
        }

        private int _totalGold;

        public void Bind(int currentHp, int maxHp, int gold)
        {
            _currentHp = currentHp;
            _maxHp = maxHp;
            _totalGold = gold;
            _maxGold = (gold / UNIT) * UNIT;
            _investGold = 0;
            RefreshUI();
        }

        private void OnMinusClicked()
        {
            if (_investGold >= UNIT)
            {
                _investGold -= UNIT;
                RefreshUI();
            }
        }

        private void OnPlusClicked()
        {
            if (_investGold < _maxGold)
            {
                _investGold += UNIT;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            int previewHeal = (_investGold / UNIT) * HP_PER_UNIT;
            int previewHp = Math.Min(_currentHp + previewHeal, _maxHp);

            if (_hpText != null)
                _hpText.text = $"HP: {_currentHp}/{_maxHp} → {previewHp}";
            if (_goldText != null)
                _goldText.text = $"Gold: {_totalGold - _investGold}";
            if (_investText != null)
                _investText.text = $"투자: {_investGold} Gold → +{previewHeal} HP";

            if (_minusButton != null) _minusButton.interactable = _investGold > 0;
            if (_plusButton != null) _plusButton.interactable = _investGold < _maxGold;
            if (_restButton != null) _restButton.interactable = _investGold > 0;
        }
    }
}
