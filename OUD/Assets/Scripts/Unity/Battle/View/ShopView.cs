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
        [SerializeField] private TMP_Text _xpInvestText;
        [SerializeField] private Button   _xpMinusButton;
        [SerializeField] private Button   _xpPlusButton;
        [SerializeField] private Button _buyXpButton;

        [Header("Skip")]
        [SerializeField] private Button _skipButton;

        public event Action<int> OnHpRecoveryConfirmed;
        public event Action<int> OnXpPurchased;
        public event Action OnShopSkipped;

        private int _investGold;
        private int _maxGold;
        private int _currentHp;
        private int _maxHp;
        private int _totalGold;
        private int _currentXp;
        private int _xpBuyCount;

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
            if (_xpMinusButton != null)
                _xpMinusButton.onClick.AddListener(OnXpMinusClicked);
            if (_xpPlusButton != null)
                _xpPlusButton.onClick.AddListener(OnXpPlusClicked);
            if (_buyXpButton != null)
                _buyXpButton.onClick.AddListener(() => OnXpPurchased?.Invoke(_xpBuyCount));
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
            _xpBuyCount = 0;
            RefreshUI();
        }

        public void RefreshGold(int newGold, int newHp, int newXp)
        {
            _totalGold = newGold;
            _currentHp = newHp;
            _currentXp = newXp;
            _maxGold = (newGold / HP_UNIT) * HP_UNIT;
            _investGold = 0;
            _xpBuyCount = 0;
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
            int remaining = _totalGold - _xpBuyCount * XP_COST;
            int capByGold = (remaining / HP_UNIT) * HP_UNIT;
            if (_investGold < capByGold)
            {
                _investGold += HP_UNIT;
                RefreshUI();
            }
        }

        private void OnXpMinusClicked()
        {
            if (_xpBuyCount > 0)
            {
                _xpBuyCount--;
                RefreshUI();
            }
        }

        private void OnXpPlusClicked()
        {
            int remaining = _totalGold - _investGold;
            int maxBuy = remaining / XP_COST;
            if (_xpBuyCount < maxBuy)
            {
                _xpBuyCount++;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            // ── HP 회복 영역 ──────────────────────────────────────────────
            int previewHeal = (_investGold / HP_UNIT) * HP_PER_UNIT;
            int previewHp = Math.Min(_currentHp + previewHeal, _maxHp);

            if (_hpText != null)
                _hpText.text = $"HP: {_currentHp}/{_maxHp} → {previewHp}";
            if (_goldText != null)
                _goldText.text = $"Gold: {_totalGold - _investGold - _xpBuyCount * XP_COST}";
            if (_investText != null)
                _investText.text = $"투자: {_investGold} Gold → +{previewHeal} HP";

            // HP 만피 시 회복 패널은 시각적으로 유지하되 모든 버튼 클릭 비활성.
            bool hpFull = _currentHp >= _maxHp;
            int hpRemaining = _totalGold - _xpBuyCount * XP_COST;
            int hpCapByGold = (hpRemaining / HP_UNIT) * HP_UNIT;
            if (_minusButton != null) _minusButton.interactable = !hpFull && _investGold > 0;
            if (_plusButton != null)  _plusButton.interactable  = !hpFull && _investGold < hpCapByGold;
            if (_healButton != null)  _healButton.interactable  = !hpFull && _investGold > 0;

            // ── XP 구매 영역 ──────────────────────────────────────────────
            int xpCost = _xpBuyCount * XP_COST;
            int xpPreview = _currentXp + _xpBuyCount;
            int xpRemaining = _totalGold - _investGold - xpCost;
            int xpCapByGold = xpRemaining / XP_COST;

            if (_xpInfoText != null)
                _xpInfoText.text = $"XP: {_currentXp} → {xpPreview}";
            if (_xpInvestText != null)
                _xpInvestText.text = $"투자: {xpCost} Gold → +{_xpBuyCount} XP";

            if (_xpMinusButton != null) _xpMinusButton.interactable = _xpBuyCount > 0;
            if (_xpPlusButton != null)  _xpPlusButton.interactable  = xpCapByGold > 0;
            if (_buyXpButton != null)   _buyXpButton.interactable   = _xpBuyCount > 0;
        }
    }
}
