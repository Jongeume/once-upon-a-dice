using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Common
{
    /// <summary>
    /// 화면 A/B/C 패널 전환 관리.
    /// MVP: SetActive 즉시 전환. 폴리싱 단계에서 슬라이드업으로 교체.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public enum BattleScreen { A_BattleBasic, B_DiceTable, C_Targeting, D_Shop }

        [SerializeField] private GameObject _battlePanel;
        [SerializeField] private GameObject _diceTablePanel;
        [SerializeField] private GameObject _targetingOverlay;

        [Header("Roll Dice 버튼 (Screen A에서만 표시 — 타겟팅 중에는 End Turn과 겹치지 않도록 숨김)")]
        [SerializeField] private GameObject _rollDiceButton;

        [Header("배경 (D_Shop 진입 시 교체)")]
        [SerializeField] private Image  _canvasBackground;
        [SerializeField] private Sprite _battleBackground;
        [SerializeField] private Sprite _shopBackground;

        private BattleScreen _current = BattleScreen.A_BattleBasic;

        /// <summary>현재 전투 배경 스프라이트. SetBattleBackground()로 변경.</summary>
        private Sprite _activeBattleBackground;

        public BattleScreen Current => _current;

        private void Awake()
        {
            _activeBattleBackground = _battleBackground;
        }

        /// <summary>전투 배경 스프라이트를 교체한다. 이후 ShowScreen 호출 시 이 스프라이트가 사용된다.</summary>
        public void SetBattleBackground(Sprite sprite)
        {
            _activeBattleBackground = sprite != null ? sprite : _battleBackground;
            // 현재 상점이 아닌 화면이면 즉시 반영
            if (_canvasBackground != null && _current != BattleScreen.D_Shop)
            {
                _canvasBackground.sprite = _activeBattleBackground;
                _canvasBackground.color  = Color.white;
            }
        }

        public void ShowScreen(BattleScreen screen)
        {
            _current = screen;
            // BattlePanel: 화면 A, C에서 표시 (B/D는 비활성)
            _battlePanel.SetActive(screen == BattleScreen.A_BattleBasic || screen == BattleScreen.C_Targeting);
            // DiceTablePanel: 화면 B에서만 표시
            _diceTablePanel.SetActive(screen == BattleScreen.B_DiceTable);
            // TargetingOverlay: 화면 C에서만 활성
            if (_targetingOverlay != null)
                _targetingOverlay.SetActive(screen == BattleScreen.C_Targeting);
            // Roll Dice 버튼: Screen A에서만 노출.
            // Screen C에서는 같은 위치의 End Turn(Execute) 버튼이 보이도록 Roll Dice를 숨긴다.
            // (End Turn은 TargetSelectionPresenter가 interactable로 활성/비활성 토글)
            if (_rollDiceButton != null)
                _rollDiceButton.SetActive(screen == BattleScreen.A_BattleBasic);
            // 배경: 상점 화면이면 상점 배경, 그 외엔 현재 활성 전투 배경
            if (_canvasBackground != null)
                _canvasBackground.sprite = (screen == BattleScreen.D_Shop) ? _shopBackground : _activeBattleBackground;
        }
    }
}
