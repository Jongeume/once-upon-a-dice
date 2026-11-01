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
        [SerializeField] private Button     _backButton;

        private CanvasGroup _rerollCanvasGroup;

        public event System.Action OnRerollClicked;
        public event System.Action OnUseSkillClicked;

        private const float DISABLED_ALPHA = 0.4f;

        private void Awake()
        {
            if (_rerollButton)
            {
                _rerollButton.onClick.AddListener(() => OnRerollClicked?.Invoke());
                _rerollCanvasGroup = _rerollButton.GetComponent<CanvasGroup>();
                if (!_rerollCanvasGroup)
                    _rerollCanvasGroup = _rerollButton.gameObject.AddComponent<CanvasGroup>();
            }
            if (_useSkillButton) _useSkillButton.onClick.AddListener(() => OnUseSkillClicked?.Invoke());
        }

        public void UpdateRerollInfo(int rerollsLeft, bool canReroll)
        {
            if (_rerollButton)
            {
                _rerollButton.interactable = canReroll;
                if (_rerollCanvasGroup)
                    _rerollCanvasGroup.alpha = rerollsLeft > 0 ? 1f : DISABLED_ALPHA;
            }
            if (_rerollCountText) _rerollCountText.text = $"{rerollsLeft}/3";
        }

        public void SetConfirmButtonActive(bool active)
        {
            if (_useSkillButton) _useSkillButton.gameObject.SetActive(active);
            if (_backButton)     _backButton.gameObject.SetActive(!active);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
