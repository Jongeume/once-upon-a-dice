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
            if (_atkText) _atkText.text = $"ATK {atk}";
            if (_defText) _defText.text = $"DEF {def}";
        }

        public void PlayDamageEffect()
        {
            if (_animator) _animator.SetTrigger(_atkHash);
        }

        public void PlayHealEffect()
        {
            if (_animator) _animator.SetTrigger(_healHash);
        }
    }
}
