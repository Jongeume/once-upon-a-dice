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

        [Header("이펙트")]
        [SerializeField] private Animator   _animator;

        private static readonly int _deathHash = Animator.StringToHash("Death");

        public event Action OnClicked;

        private void Awake()
        {
            if (_targetButton) _targetButton.onClick.AddListener(() => OnClicked?.Invoke());
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
                IntentType.Attack       => $"⚔ {value}",
                IntentType.StrongAttack => $"⚔⚔ {value}",
                IntentType.Shield       => $"🛡 {value}",
                _                       => "?"
            };
        }

        public void SetTargetSelectable(bool selectable)
        {
            if (_targetButton) _targetButton.interactable = selectable;
        }

        public void SetTargetHighlight(bool highlighted)
        {
            if (_targetHighlight) _targetHighlight.enabled = highlighted;
        }

        public void PlayDeathEffect()
        {
            if (_animator) _animator.SetTrigger(_deathHash);
        }
    }
}
