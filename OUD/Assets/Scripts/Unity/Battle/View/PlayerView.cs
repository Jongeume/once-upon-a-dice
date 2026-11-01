using System.Collections;
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

        // ── HP 바 anchorMax.x 방식 (EnemyEntryView 패턴) ──
        private const float HP_DRAIN_SPEED = 1.2f;
        private RectTransform _hpFillRect;
        private float _currentHpRatio = 1f;
        private Coroutine _hpDrainCoroutine;

        private void Awake()
        {
            if (_hpFill != null)
                _hpFillRect = _hpFill.GetComponent<RectTransform>();
        }

        /// <summary>anchorMax.x를 이용한 HP바 너비 설정 (HPBg 빨간 배경 노출).</summary>
        private void SetHpBarRatio(RectTransform rt, float ratio)
        {
            if (rt == null) return;
            Vector2 aMax = rt.anchorMax;
            aMax.x = Mathf.Clamp01(ratio);
            rt.anchorMax = aMax;
        }

        public void UpdateHp(float fillAmount, string hpText)
        {
            float previousRatio = _currentHpRatio;
            _currentHpRatio = Mathf.Clamp01(fillAmount);

            if (_hpText)
            {
                _hpText.text      = hpText;
                _hpText.alignment = TextAlignmentOptions.Center;
            }

            // anchorMax.x 방식: HPFill이 줄어들면 뒤의 HPBg(빨간 배경) 노출
            if (_currentHpRatio < previousRatio)
            {
                // 데미지 — 서서히 줄어드는 애니메이션
                if (_hpDrainCoroutine != null) StopCoroutine(_hpDrainCoroutine);
                if (isActiveAndEnabled)
                    _hpDrainCoroutine = StartCoroutine(AnimateHpDrain(_currentHpRatio));
            }
            else
            {
                // 힐 또는 초기 설정 — 즉시 반영
                if (_hpDrainCoroutine != null) { StopCoroutine(_hpDrainCoroutine); _hpDrainCoroutine = null; }
                SetHpBarRatio(_hpFillRect, _currentHpRatio);
            }

            // 체력 비율(0~1)에 따라 Life 아이콘 sprite 교체.
            if (_lifeImage != null)
            {
                Sprite next;
                if (fillAmount > 0.80f)      next = _lifeFull;
                else if (fillAmount > 0.50f) next = _lifeMid;
                else                          next = _lifeLow;
                if (next != null) _lifeImage.sprite = next;
            }
        }

        private IEnumerator AnimateHpDrain(float targetRatio)
        {
            while (_hpFillRect != null && _hpFillRect.anchorMax.x > targetRatio)
            {
                float current = _hpFillRect.anchorMax.x;
                float next = Mathf.MoveTowards(current, targetRatio, HP_DRAIN_SPEED * Time.deltaTime);
                SetHpBarRatio(_hpFillRect, next);
                yield return null;
            }
            SetHpBarRatio(_hpFillRect, targetRatio);
            _hpDrainCoroutine = null;
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
