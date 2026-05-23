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
            if (_hpText)  _hpText.text        = hpText;

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

        public void ShowDefenseBadge(string skillName)
        {
            if (_defenseBadgeContainer == null) EnsureDefenseBadgeContainer();
            if (_defenseBadgeContainer) _defenseBadgeContainer.SetActive(true);

            var box = new GameObject($"DefBadge_{skillName}");
            box.transform.SetParent(_defenseBadgeContainer.transform, false);

            var rt = box.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 20f);

            var bg = box.AddComponent<Image>();
            bg.color         = new Color(0.02f, 0.05f, 0.11f, 0.95f);
            bg.raycastTarget = false;

            var csf = box.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var outline = box.AddComponent<Outline>();
            outline.effectColor    = new Color(0.23f, 0.51f, 0.96f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(box.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4f, 2f);
            textRt.offsetMax = new Vector2(-4f, -2f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize      = 11f;
            tmp.color         = new Color(0.58f, 0.77f, 0.99f, 1f);
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.text          = skillName;
            tmp.raycastTarget = false;

            _defenseBadgeBoxes.Add(box);
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
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta        = new Vector2(0f, 0f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 2f;
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            _defenseBadgeContainer = go;
            go.SetActive(false);
        }
    }
}
