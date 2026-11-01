using System;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class ClearView : ViewBase
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private Button   _restartButton;

        public event Action OnRestartClicked;

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }

        public void SetSummary(int level, int gold)
        {
            if (_levelText != null) _levelText.text = $"Level: {level}";
            if (_goldText != null)  _goldText.text  = $"Gold: {gold}";
        }
    }
}
