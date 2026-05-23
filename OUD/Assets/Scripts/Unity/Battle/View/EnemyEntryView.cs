using System;
using OUD.BattleEngine.Core;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>적 1체 UI. HP바 / 실드 / Intent / 타겟 버튼.</summary>
    public class EnemyEntryView : ViewBase, IEnemyEntryView
    {
        [Header("기본")]
        [SerializeField] private Image    _portrait;
        [SerializeField] private TMP_Text _nameText;

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

        [Header("공격력 (좌상단)")]
        [SerializeField] private TMP_Text   _atkText;

        [Header("방어력 (우상단)")]
        [SerializeField] private TMP_Text   _defText;

        [Header("인텐트 (현재 비활성 — 스프라이트로 대체 예정)")]
        [SerializeField] private TMP_Text   _intentText;
        [SerializeField] private Image      _intentIcon;

        [Header("타겟")]
        [SerializeField] private Button     _targetButton;
        [SerializeField] private Image      _targetHighlight;
        [SerializeField] private Outline    _targetOutline;
        [SerializeField] private GameObject _targetableHint;

        [Header("타겟 색상")]
        [SerializeField] private Color _selectableOutlineColor = new Color(0.95f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color _hoverOutlineColor      = new Color(1f,    0.45f, 0.20f, 1f);

        [Header("타겟 뱃지")]
        [SerializeField] private GameObject _targetBadgeContainer;

        [Header("분노 (보스)")]
        [SerializeField] private GameObject _rageGroup;
        [SerializeField] private Image      _rageIcon;
        [SerializeField] private TMP_Text   _rageLabel;

        [Header("이펙트")]
        [SerializeField] private Animator   _animator;

        private static readonly int _deathHash = Animator.StringToHash("Death");

        public event Action OnClicked;

        private bool _selectable;
        private bool _highlighted;

        private void Awake()
        {
            if (_targetButton) _targetButton.onClick.AddListener(() => OnClicked?.Invoke());
            EnsureOutline();
            ApplyTargetVisual();
        }

        private void EnsureOutline()
        {
            if (_targetOutline != null) return;
            _targetOutline = GetComponent<Outline>();
            if (_targetOutline == null) _targetOutline = gameObject.AddComponent<Outline>();
            _targetOutline.effectDistance = new Vector2(3f, -3f);
        }

        public void Setup(string name, Sprite sprite, float hpFill, string hpText)
        {
            if (_nameText) _nameText.text        = name;
            // sprite=null도 그대로 할당 — MonsterSpriteMap에 매핑 없는 몬스터는 흰 박스로 표시.
            if (_portrait) _portrait.sprite = sprite;
            UpdateHp(hpFill, hpText);
        }

        public void UpdateHp(float fillAmount, string hpText)
        {
            if (_hpFill) _hpFill.fillAmount = fillAmount;
            if (_hpText) _hpText.text        = hpText;

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

        /// <summary>좌상단 공격력(현재 실효 ATK) 표시. 분노 등으로 ATK 변동 시 갱신.</summary>
        public void UpdateAtk(int atk)
        {
            if (_atkText) _atkText.text = atk.ToString();
        }

        /// <summary>우상단 방어력 표시. (현재 적은 전용 DEF 스탯이 없어 ShieldValue를 전달 — 데이터는 추후 조정.)</summary>
        public void UpdateDef(int def)
        {
            if (_defText) _defText.text = def.ToString();
        }

        public void UpdateIntent(IntentType intent, int value)
        {
            bool isAttack = intent == IntentType.Attack
                         || intent == IntentType.StrongAttack
                         || intent == IntentType.RageWarning;
            bool isShield = intent == IntentType.Shield;

            if (_atkText)
            {
                _atkText.gameObject.SetActive(isAttack);
                if (isAttack) _atkText.text = value.ToString();
            }
            if (_defText)
            {
                _defText.gameObject.SetActive(isShield);
                if (isShield) _defText.text = value.ToString();
            }
        }

        public void SetTargetSelectable(bool selectable)
        {
            _selectable = selectable;
            if (_targetButton) _targetButton.interactable = selectable;
            if (_targetableHint) _targetableHint.SetActive(selectable);
            ApplyTargetVisual();
        }

        public void SetTargetHighlight(bool highlighted)
        {
            _highlighted = highlighted;
            if (_targetHighlight) _targetHighlight.enabled = highlighted;
            ApplyTargetVisual();
        }

        private void ApplyTargetVisual()
        {
            if (_targetOutline == null) return;
            if (_highlighted)
            {
                _targetOutline.enabled     = true;
                _targetOutline.effectColor = _hoverOutlineColor;
            }
            else if (_selectable)
            {
                _targetOutline.enabled     = true;
                _targetOutline.effectColor = _selectableOutlineColor;
            }
            else
            {
                _targetOutline.enabled = false;
            }
        }

        public void SetRageActive(bool active)
        {
            if (_rageGroup) _rageGroup.SetActive(active);
        }

        private readonly System.Collections.Generic.List<GameObject> _badgeBoxes = new();

        public void ShowTargetBadge(string skillName, SkillCategory category, bool isAoe)
        {
            if (_targetBadgeContainer == null) EnsureTargetBadgeContainer();
            if (_targetBadgeContainer) _targetBadgeContainer.SetActive(true);

            var box = CreateBadgeBox(skillName, category, isAoe);
            _badgeBoxes.Add(box);
        }

        public void ClearTargetBadge()
        {
            foreach (var box in _badgeBoxes)
                if (box != null) Destroy(box);
            _badgeBoxes.Clear();
            if (_targetBadgeContainer) _targetBadgeContainer.SetActive(false);
        }

        private GameObject CreateBadgeBox(string skillName, SkillCategory category, bool isAoe)
        {
            bool isAttack = category == SkillCategory.Attack;

            Color bgColor   = isAttack ? new Color(0.11f, 0.04f, 0f,  0.95f)
                                       : new Color(0.02f, 0.05f, 0.11f, 0.95f);
            Color rimColor  = isAttack ? new Color(0.94f, 0.27f, 0.27f, 1f)
                                       : new Color(0.23f, 0.51f, 0.96f, 1f);
            Color textColor = isAttack ? new Color(0.99f, 0.64f, 0.64f, 1f)
                                       : new Color(0.58f, 0.77f, 0.99f, 1f);
            string label = isAoe ? $"{skillName} 전체" : skillName;

            var box = new GameObject($"Badge_{skillName}");
            box.transform.SetParent(_targetBadgeContainer.transform, false);

            var rt = box.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 44f);

            var bg = box.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            var outline = box.AddComponent<Outline>();
            outline.effectColor    = rimColor;
            outline.effectDistance = new Vector2(1f, -1f);

            // HorizontalLayoutGroup이 텍스트 preferred width를 box로 전파 → ContentSizeFitter가 올바른 너비 계산
            var hlg = box.AddComponent<HorizontalLayoutGroup>();
            hlg.padding                = new RectOffset(12, 12, 6, 6);
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;

            var csf = box.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(box.transform, false);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (_nameText != null)
            {
                tmp.font               = _nameText.font;
                tmp.fontSharedMaterial = _nameText.fontSharedMaterial;
            }
            tmp.fontSize           = 22f;
            tmp.color              = textColor;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.text               = label;
            tmp.raycastTarget      = false;
            tmp.enableWordWrapping = false;

            return box;
        }

        private void EnsureTargetBadgeContainer()
        {
            if (_targetBadgeContainer != null) return;

            var containerGo = new GameObject("TargetBadgeContainer");
            containerGo.transform.SetParent(transform, false);

            var rt = containerGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);  // 중앙 상단
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta        = new Vector2(0f, 0f);

            var vlg = containerGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 2f;
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;

            var csf = containerGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            _targetBadgeContainer = containerGo;
            containerGo.SetActive(false);
        }

        public void PlayDeathEffect()
        {
            if (_animator) _animator.SetTrigger(_deathHash);
        }
    }
}
