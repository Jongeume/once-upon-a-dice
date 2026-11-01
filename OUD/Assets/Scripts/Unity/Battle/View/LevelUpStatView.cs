// LevelUpStatView.cs
// F-08 레벨업 스탯 선택 UI.
using System;
using OUD.BattleEngine.Core;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class LevelUpStatView : ViewBase
    {
        [SerializeField] private Button _atkButton;
        [SerializeField] private Button _defButton;
        [SerializeField] private Button _hpButton;

        [SerializeField] private TMP_Text _atkText;
        [SerializeField] private TMP_Text _defText;
        [SerializeField] private TMP_Text _hpText;

        public event Action<StatChoice> OnStatChosen;

        private void Awake()
        {
            if (_atkButton != null)
                _atkButton.onClick.AddListener(() => OnStatChosen?.Invoke(StatChoice.AtkUp));
            if (_defButton != null)
                _defButton.onClick.AddListener(() => OnStatChosen?.Invoke(StatChoice.DefUp));
            if (_hpButton != null)
                _hpButton.onClick.AddListener(() => OnStatChosen?.Invoke(StatChoice.HpUp));
        }

        public void SetStats(int atk, int def, int maxHp, int currentHp)
        {
            if (_atkText != null) _atkText.text = $"ATK {atk} → {atk + 1}";
            if (_defText != null) _defText.text = $"DEF {def} → {def + 1}";
            if (_hpText != null) _hpText.text = $"HP {maxHp} → {maxHp + 5} (현재 HP {currentHp}+5)";
        }
    }
}
