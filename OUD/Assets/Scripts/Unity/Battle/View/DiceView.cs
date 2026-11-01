using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>주사위 테이블 컨테이너. 리롤 버튼 / 기술 사용 버튼.</summary>
    public class DiceView : ViewBase, IDiceView
    {
        [SerializeField] private Button     _rerollButton;
        [SerializeField] private TMP_Text   _rerollCountText;
        [SerializeField] private Button     _useSkillButton;

        public event System.Action OnRerollClicked;
        public event System.Action OnUseSkillClicked;

        private void Awake()
        {
            if (_rerollButton)    _rerollButton.onClick.AddListener(()   => OnRerollClicked?.Invoke());
            if (_useSkillButton)  _useSkillButton.onClick.AddListener(() => OnUseSkillClicked?.Invoke());
        }

        public void UpdateRerollInfo(int rerollsLeft, bool canReroll)
        {
            if (_rerollButton)
            {
                _rerollButton.gameObject.SetActive(rerollsLeft > 0); // 리롤 남아있을 때만 표시
                _rerollButton.interactable = canReroll;
            }
            if (_rerollCountText) _rerollCountText.text = $"{rerollsLeft}/3";
        }

        public void SetConfirmButtonActive(bool active)
        {
            // 리롤 버튼 상태는 UpdateRerollInfo가 관리 — 여기선 UseSkill(확정) 버튼만 제어
            if (_useSkillButton) _useSkillButton.gameObject.SetActive(active);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
