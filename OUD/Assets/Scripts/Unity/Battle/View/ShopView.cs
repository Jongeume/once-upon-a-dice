using System;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class ShopView : ViewBase
    {
        [Header("HP Recovery")]
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _investText;
        [SerializeField] private Button _minusButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Button _healButton;

        [Header("XP Purchase")]
        [SerializeField] private TMP_Text _xpInfoText;
        [SerializeField] private Button _buyXpButton;

        [Header("Skip")]
        [SerializeField] private Button _skipButton;

        public event Action<int> OnHpRecoveryConfirmed;
        public event Action OnXpPurchased;
        public event Action OnShopSkipped;

        private int _investGold;
        private int _maxGold;
        private int _currentHp;
        private int _maxHp;
        private int _totalGold;
        private int _currentXp;

        private const int HP_UNIT = 10;
        private const int HP_PER_UNIT = 5;
        private const int XP_COST = 20;

        private void Awake()
        {
            if (_minusButton != null)
                _minusButton.onClick.AddListener(OnMinusClicked);
            if (_plusButton != null)
                _plusButton.onClick.AddListener(OnPlusClicked);
            if (_healButton != null)
                _healButton.onClick.AddListener(() => OnHpRecoveryConfirmed?.Invoke(_investGold));
            if (_buyXpButton != null)
                _buyXpButton.onClick.AddListener(() => OnXpPurchased?.Invoke());
            if (_skipButton != null)
                _skipButton.onClick.AddListener(() => OnShopSkipped?.Invoke());
        }

        public void Bind(int currentHp, int maxHp, int gold, int xp)
        {
            _currentHp = currentHp;
            _maxHp = maxHp;
            _totalGold = gold;
            _currentXp = xp;
            _maxGold = (gold / HP_UNIT) * HP_UNIT;
            _investGold = 0;
            RefreshUI();
        }

        public void RefreshGold(int newGold, int newHp, int newXp)
        {
            _totalGold = newGold;
            _currentHp = newHp;
            _currentXp = newXp;
            _maxGold = (newGold / HP_UNIT) * HP_UNIT;
            _investGold = 0;
            RefreshUI();
        }

        private void OnMinusClicked()
        {
            if (_investGold >= HP_UNIT)
            {
                _investGold -= HP_UNIT;
                RefreshUI();
            }
        }

        private void OnPlusClicked()
        {
            if (_investGold < _maxGold)
            {
                _investGold += HP_UNIT;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            int previewHeal = (_investGold / HP_UNIT) * HP_PER_UNIT;
            int previewHp = Math.Min(_currentHp + previewHeal, _maxHp);

            if (_hpText != null)
                _hpText.text = $"HP: {_currentHp}/{_maxHp} → {previewHp}";
            if (_goldText != null)
                _goldText.text = $"Gold: {_totalGold - _investGold}";
            if (_investText != null)
                _investText.text = $"투자: {_investGold} Gold → +{previewHeal} HP";

            if (_minusButton != null) _minusButton.interactable = _investGold > 0;
            if (_plusButton != null) _plusButton.interactable = _investGold < _maxGold && _currentHp < _maxHp;
            if (_healButton != null) _healButton.interactable = _investGold > 0;

            bool canBuyXp = _totalGold - _investGold >= XP_COST;
            if (_xpInfoText != null)
                _xpInfoText.text = $"XP: {_currentXp}  Gold: {_totalGold - _investGold}";
            if (_buyXpButton != null) _buyXpButton.interactable = canBuyXp && _investGold == 0;
        }
    }
}
