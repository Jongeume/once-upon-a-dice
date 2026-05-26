// LevelUpSkillView.cs
// F-09 SP 해금 선택 UI.
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class LevelUpSkillView : ViewBase
    {
        [SerializeField] private TMP_Text _spText;

        [SerializeField] private Button _smallStraightBtn;
        [SerializeField] private Button _fourOfAKindBtn;
        [SerializeField] private Button _largeStraightBtn;
        [SerializeField] private Button _yahtzeeBtn;
        [SerializeField] private Button _skipButton;

        [SerializeField] private TMP_Text _smallStraightText;
        [SerializeField] private TMP_Text _fourOfAKindText;
        [SerializeField] private TMP_Text _largeStraightText;
        [SerializeField] private TMP_Text _yahtzeeText;

        public event Action<HandType> OnUnlockChosen;
        public event Action OnSkipClicked;

        private void Awake()
        {
            if (_smallStraightBtn != null)
                _smallStraightBtn.onClick.AddListener(() => OnUnlockChosen?.Invoke(HandType.SmallStraight));
            if (_fourOfAKindBtn != null)
                _fourOfAKindBtn.onClick.AddListener(() => OnUnlockChosen?.Invoke(HandType.FourOfAKind));
            if (_largeStraightBtn != null)
                _largeStraightBtn.onClick.AddListener(() => OnUnlockChosen?.Invoke(HandType.LargeStraight));
            if (_yahtzeeBtn != null)
                _yahtzeeBtn.onClick.AddListener(() => OnUnlockChosen?.Invoke(HandType.Yahtzee));
            if (_skipButton != null)
                _skipButton.onClick.AddListener(() => OnSkipClicked?.Invoke());
        }

        public void Bind(int currentSp, HashSet<HandType> unlockedHands)
        {
            if (_spText != null) _spText.text = $"SP: {currentSp}";

            SetupButton(_smallStraightBtn, _smallStraightText, HandType.SmallStraight,
                "Small Straight", 1, currentSp, unlockedHands);
            SetupButton(_fourOfAKindBtn, _fourOfAKindText, HandType.FourOfAKind,
                "Four of a Kind", 2, currentSp, unlockedHands);
            SetupButton(_largeStraightBtn, _largeStraightText, HandType.LargeStraight,
                "Large Straight", 2, currentSp, unlockedHands);
            SetupButton(_yahtzeeBtn, _yahtzeeText, HandType.Yahtzee,
                "Yahtzee", 3, currentSp, unlockedHands);
        }

        private void SetupButton(Button btn, TMP_Text text, HandType hand,
            string displayName, int cost, int currentSp, HashSet<HandType> unlocked)
        {
            if (btn == null) return;

            bool isUnlocked = unlocked.Contains(hand);
            bool canAfford = currentSp >= cost;

            if (text != null)
            {
                if (isUnlocked)
                    text.text = $"{displayName} — Unlocked";
                else
                    text.text = $"{displayName} (SP {cost})";
            }

            btn.interactable = !isUnlocked && canAfford;
        }
    }
}
