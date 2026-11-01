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

        [Header("실드")]
        [SerializeField] private GameObject _shieldGroup;
        [SerializeField] private TMP_Text   _shieldText;

        [Header("인텐트")]
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
            if (_portrait && sprite != null) _portrait.sprite = sprite;
            if (_hpFill)   _hpFill.fillAmount    = hpFill;
            if (_hpText)   _hpText.text           = hpText;
        }

        public void UpdateHp(float fillAmount, string hpText)
        {
            if (_hpFill) _hpFill.fillAmount = fillAmount;
            if (_hpText) _hpText.text        = hpText;
        }

        public void UpdateShield(int shield, bool visible)
        {
            if (_shieldGroup) _shieldGroup.SetActive(visible);
            if (_shieldText)  _shieldText.text = shield.ToString();
        }

        public void UpdateIntent(IntentType intent, int value)
        {
            if (_intentText == null) return;
            _intentText.text = intent switch
            {
                IntentType.Attack       => $"ATK {value}",
                IntentType.StrongAttack => $"ATK!! {value}",
                IntentType.Shield       => $"DEF {value}",
                IntentType.RageWarning  => $"RAGE {value}",
                _                       => "?"
            };
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

        public void PlayDeathEffect()
        {
            if (_animator) _animator.SetTrigger(_deathHash);
        }
    }
}
