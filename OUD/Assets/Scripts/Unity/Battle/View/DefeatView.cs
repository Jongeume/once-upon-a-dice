using System;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class DefeatView : ViewBase
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _reachedNodeText;
        [SerializeField] private Button   _restartButton;

        public event Action OnRestartClicked;

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }

        public void SetDefeatInfo(int reachedNode, int totalNodes)
        {
            if (_reachedNodeText != null)
                _reachedNodeText.text = $"Node: {reachedNode}/{totalNodes}";
        }
    }
}
