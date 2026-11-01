using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>플레이어 HP바 / 실드 / 스탯 표시.</summary>
    public class PlayerView : ViewBase, IPlayerView
    {
        [Header("HP")]
        [SerializeField] private Image    _hpFill;
        [SerializeField] private TMP_Text _hpText;

        [Header("Life Icon")]
        [SerializeField] private Image  _lifeImage;
        [SerializeField] private Sprite _lifeFull;     // 100~81%
        [SerializeField] private Sprite _lifeMid;      // 80~51%
        [SerializeField] private Sprite _lifeLow;      // 50~1%

        [Header("실드")]
        [SerializeField] private GameObject _shieldGroup;
        [SerializeField] private TMP_Text   _shieldText;

        [Header("실드 (HP 바 옆)")]
        [SerializeField] private GameObject _shieldDisplay;
        [SerializeField] private Image      _shieldIcon;
        [SerializeField] private TMP_Text   _shieldValueText;

        [Header("스탯")]
        [SerializeField] private TMP_Text _atkText;
        [SerializeField] private TMP_Text _defText;

        [Header("이펙트")]
        [SerializeField] private Animator _animator;

        private static readonly int _atkHash    = Animator.StringToHash("Damage");
        private static readonly int _healHash   = Animator.StringToHash("Heal");

        public void UpdateHp(float fillAmount, string hpText)
        {
            if (_hpFill)  _hpFill.fillAmount = fillAmount;
            if (_hpText)
            {
                _hpText.text      = hpText;
                _hpText.alignment = TextAlignmentOptions.Center;
            }

            // 체력 비율(0~1)에 따라 Life 아이콘 sprite 교체.
            // 100~81%: Full / 80~51%: Mid / 50~1%: Low
            if (_lifeImage != null)
            {
                Sprite next;
                if (fillAmount > 0.80f)      next = _lifeFull;
                else if (fillAmount > 0.50f) next = _lifeMid;
                else                          next = _lifeLow;
                if (next != null) _lifeImage.sprite = next;
            }
        }

        public void UpdateShield(int shield, bool visible)
        {
            if (_shieldGroup) _shieldGroup.SetActive(visible);
            if (_shieldText)  _shieldText.text = shield.ToString();
            // HP 바 옆 방어 아이콘 (EnemyEntry 프리팹과 동일 패턴)
            if (_shieldDisplay) _shieldDisplay.SetActive(visible);
            if (_shieldValueText) _shieldValueText.text = shield.ToString();
        }

        public void UpdateStats(int atk, int def)
        {
            if (_atkText) _atkText.text = $"{atk}";
            if (_defText) _defText.text = $"{def}";
        }

        public void PlayDamageEffect()
        {
            if (_animator) _animator.SetTrigger(_atkHash);
        }

        public void PlayHealEffect()
        {
            if (_animator) _animator.SetTrigger(_healHash);
        }

        // 방어 뱃지
        private GameObject _defenseBadgeContainer;
        private readonly System.Collections.Generic.List<GameObject> _defenseBadgeBoxes = new();
        private TMP_FontAsset _cachedBadgeFont;

        public void ShowDefenseBadge(string skillName)
        {
            if (_defenseBadgeContainer == null) EnsureDefenseBadgeContainer();
            if (_defenseBadgeContainer) _defenseBadgeContainer.SetActive(true);

            var box = new GameObject($"DefBadge_{skillName}");
            box.transform.SetParent(_defenseBadgeContainer.transform, false);

            var rt = box.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 44f);

            var bg = box.AddComponent<Image>();
            bg.color         = new Color(0.02f, 0.05f, 0.11f, 0.95f);
            bg.raycastTarget = false;

            var outline = box.AddComponent<Outline>();
            outline.effectColor    = new Color(0.23f, 0.51f, 0.96f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(box.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 6f);
            textRt.offsetMax = new Vector2(-12f, -6f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (_cachedBadgeFont == null && _atkText != null)
                _cachedBadgeFont = _atkText.font;
            if (_cachedBadgeFont != null)
                tmp.font = _cachedBadgeFont;
            tmp.fontSize           = 30f;
            tmp.color              = new Color(0.58f, 0.77f, 0.99f, 1f);
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.text               = skillName;
            tmp.raycastTarget      = false;
            tmp.enableWordWrapping = false;

            _defenseBadgeBoxes.Add(box);
            NormalizeDefenseBadgeWidths();
        }

        private void NormalizeDefenseBadgeWidths()
        {
            const float BADGE_WIDTH = 200f;
            foreach (var box in _defenseBadgeBoxes)
            {
                if (box == null) continue;
                var rt = box.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(BADGE_WIDTH, rt.sizeDelta.y);
            }
            var containerRt = _defenseBadgeContainer?.GetComponent<RectTransform>();
            if (containerRt != null) containerRt.sizeDelta = new Vector2(BADGE_WIDTH, containerRt.sizeDelta.y);
        }

        public void ClearDefenseBadges()
        {
            foreach (var box in _defenseBadgeBoxes)
                if (box != null) Destroy(box);
            _defenseBadgeBoxes.Clear();
            if (_defenseBadgeContainer) _defenseBadgeContainer.SetActive(false);
        }

        private void EnsureDefenseBadgeContainer()
        {
            if (_defenseBadgeContainer != null) return;

            var go = new GameObject("DefenseBadgeContainer");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, -30f);
            rt.sizeDelta        = new Vector2(0f, 0f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 2f;
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = false;  // 수동 너비(sizeDelta) 유지
            vlg.childControlHeight     = false;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;  // NormalizeDefenseBadgeWidths가 수동 설정
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            _defenseBadgeContainer = go;
            go.SetActive(false);
        }
    }
}
