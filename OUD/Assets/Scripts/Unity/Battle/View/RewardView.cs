// RewardView.cs
// F-13 보상 화면 — 전투 승리 직후 +XP / +Gold 표시 + [계속] 버튼.
// user-flow §3.2 Reward Screen
using System;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 보상 화면. BattleUIAdapter가 OnBattleWon 시점에 Show 호출.
    /// 데이터 흐름: Adapter → SetReward(gainedXp, gainedGold, totalXp, totalGold) → Show()
    /// 사용자가 [계속] 클릭 → OnContinueClicked 발화 → Adapter가 Hide() 또는 다음 화면 전환.
    /// </summary>
    public class RewardView : ViewBase
    {
        [Header("획득 보상")]
        [SerializeField] private TMP_Text _gainedXpText;
        [SerializeField] private TMP_Text _gainedGoldText;

        [Header("누적 보유량 (선택 표시)")]
        [SerializeField] private TMP_Text _totalXpText;
        [SerializeField] private TMP_Text _totalGoldText;

        [Header("진행")]
        [SerializeField] private Button _continueButton;

        public event Action OnContinueClicked;

        private void Awake()
        {
            if (_continueButton != null)
                _continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());
        }

        /// <summary>
        /// 보상 수치 표시. PlayerState 누적값은 Adapter가 AddXp/AddGold 호출 후 전달.
        /// </summary>
        public void SetReward(int gainedXp, int gainedGold, int totalXp, int totalGold)
        {
            if (_gainedXpText)   _gainedXpText.text   = $"<sprite name=\"xp\"> +{gainedXp} XP";
            if (_gainedGoldText) _gainedGoldText.text = $"<sprite name=\"gold\"> +{gainedGold} Gold";
            if (_totalXpText)    _totalXpText.text    = $"<sprite name=\"xp\"> {totalXp}";
            if (_totalGoldText)  _totalGoldText.text  = $"<sprite name=\"gold\"> {totalGold}";
        }
    }
}
