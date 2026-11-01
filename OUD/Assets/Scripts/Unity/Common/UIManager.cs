using UnityEngine;

namespace OUD.Unity.Common
{
    /// <summary>
    /// 화면 A/B/C 패널 전환 관리.
    /// MVP: SetActive 즉시 전환. 폴리싱 단계에서 슬라이드업으로 교체.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public enum BattleScreen { A_BattleBasic, B_DiceTable, C_Targeting }

        [SerializeField] private GameObject _battlePanel;
        [SerializeField] private GameObject _diceTablePanel;
        [SerializeField] private GameObject _targetingOverlay;

        private BattleScreen _current = BattleScreen.A_BattleBasic;

        public BattleScreen Current => _current;

        public void ShowScreen(BattleScreen screen)
        {
            _current = screen;
            // BattlePanel: 화면 A, C에서 표시
            _battlePanel.SetActive(screen != BattleScreen.B_DiceTable);
            // DiceTablePanel: 화면 B에서만 표시
            _diceTablePanel.SetActive(screen == BattleScreen.B_DiceTable);
            // TargetingOverlay: 화면 C에서만 활성
            if (_targetingOverlay != null)
                _targetingOverlay.SetActive(screen == BattleScreen.C_Targeting);
        }
    }
}
