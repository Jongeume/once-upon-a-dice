using System;
using System.Collections;
using OUD.BattleEngine.Core;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>적 1체 UI. HP바 / 실드 / ATK·DEF 메달 / 타겟 버튼.</summary>
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

        // === 구 구조 (UI 방향 변경 시 복원용, 결정 후 정리 예정) ===
        // [Header("공격력 (좌상단)")]
        // [SerializeField] private TMP_Text   _atkText;
        // [Header("방어력 (우상단)")]
        // [SerializeField] private TMP_Text   _defText;
        // [Header("인텐트 (현재 비활성 — 스프라이트로 대체 예정)")]
        // [SerializeField] private TMP_Text   _intentText;
        // [SerializeField] private Image      _intentIcon;

        [Header("ATK 메달 (좌상단)")]
        [SerializeField] private Image    _atkMedal;        // 메달 프레임 배경
        [SerializeField] private TMP_Text _atkValueText;    // 메달 위 수치
        [SerializeField] private Outline  _atkOutline;      // (구) 외곽선 — 펄스 미사용, 보존
        [SerializeField] private Image    _atkSwordImage;   // 검 스프라이트 — 의도 펄스 대상

        [Header("DEF 메달 (우상단)")]
        [SerializeField] private Image    _defMedal;
        [SerializeField] private TMP_Text _defValueText;
        [SerializeField] private Outline  _defOutline;      // (구) 외곽선 — 펄스 미사용, 보존
        [SerializeField] private Image    _defShieldImage;  // 방패 스프라이트 — 의도 펄스 대상

        [Header("Summon 토스트 (보스 전용)")]
        [SerializeField] private GameObject _summonToast;     // EnemyEntry는 null 허용
        [SerializeField] private TMP_Text   _summonToastText;

        [Header("인텐트 강조 색상")]
        [SerializeField] private Color _intentHighlightColor = new Color(1f, 0.85f, 0.2f, 1f);

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

        // 인텐트 강조 펄스 — 검/방패 스프라이트 alpha 깜빡임 (1초 주기, 0.3↔1.0)
        // 외곽선 펄스에서 변경됨: 아이콘 자체 가시성 변화가 의도 알림으로 더 직관적
        private const float PULSE_PERIOD    = 1.0f;
        private const float PULSE_MIN_ALPHA = 0.3f;
        private Coroutine _atkPulseCoroutine;
        private Coroutine _defPulseCoroutine;

        // Summon 토스트 타이밍
        private const float SUMMON_FADE_IN  = 0.2f;
        private const float SUMMON_HOLD     = 1.2f;
        private const float SUMMON_FADE_OUT = 0.4f;
        private Coroutine _summonToastCoroutine;

        // 현재 ATK 캐시 — StrongAttack 의도가 들어올 때 임시로 강공격 데미지를 표시한 뒤
        // 다른 의도(Attack/Shield/Summon 등)로 바뀌면 원본 Atk로 복귀시키기 위해 보관.
        private int _cachedAtk;

        public event Action OnClicked;

        private bool _selectable;
        private bool _highlighted;

        private void Awake()
        {
            if (_targetButton) _targetButton.onClick.AddListener(() => OnClicked?.Invoke());
            EnsureOutline();
            ApplyTargetVisual();
        }

        private void OnDisable()
        {
            // 비활성화 시 모든 의도 효과 정지 — 코루틴이 죽은 GO를 참조하지 않도록.
            StopAllIntentEffects();
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
            _cachedAtk = atk;
            if (_atkValueText) _atkValueText.text = atk.ToString();
            // if (_atkText) _atkText.text = atk.ToString();
        }

        /// <summary>우상단 방어력 표시. (현재 적은 전용 DEF 스탯이 없어 ShieldValue를 전달 — 데이터는 추후 조정.)</summary>
        public void UpdateDef(int def)
        {
            if (_defValueText) _defValueText.text = def.ToString();
            // if (_defText) _defText.text = def.ToString();
        }

        /// <summary>
        /// 다음 턴 의도에 따라 ATK/DEF 메달 강조 토글.
        /// - Attack/StrongAttack/RageWarning → ATK 메달 강조 (Outline 펄스)
        /// - Shield → DEF 메달 강조
        /// - Summon → 두 메달 모두 비강조 + 카드 위에 "Summon" 토스트
        /// StrongAttack은 ATK 수치를 임시로 강공격 데미지(value)로 표시.
        /// </summary>
        public void UpdateIntent(IntentType intent, int value)
        {
            // 구 구조 (참고):
            // if (_intentText) _intentText.text = intent switch { ... };

            switch (intent)
            {
                case IntentType.Attack:
                case IntentType.RageWarning:
                    SetAtkHighlight(true);
                    SetDefHighlight(false);
                    RestoreAtkValueText();
                    break;

                case IntentType.StrongAttack:
                    SetAtkHighlight(true);
                    SetDefHighlight(false);
                    if (_atkValueText) _atkValueText.text = value.ToString();
                    break;

                case IntentType.Shield:
                    SetAtkHighlight(false);
                    SetDefHighlight(true);
                    RestoreAtkValueText();
                    break;

                case IntentType.Summon:
                    SetAtkHighlight(false);
                    SetDefHighlight(false);
                    RestoreAtkValueText();
                    PlaySummonToast();
                    break;

                default:
                    SetAtkHighlight(false);
                    SetDefHighlight(false);
                    RestoreAtkValueText();
                    break;
            }
        }

        /// <summary>StrongAttack 의도였다가 다른 의도로 전환 시 ATK 수치를 캐시된 원본으로 복귀.</summary>
        private void RestoreAtkValueText()
        {
            if (_atkValueText) _atkValueText.text = _cachedAtk.ToString();
        }

        private void SetAtkHighlight(bool active)
        {
            if (_atkSwordImage == null) return;
            if (active && isActiveAndEnabled)
            {
                if (_atkPulseCoroutine == null)
                    _atkPulseCoroutine = StartCoroutine(PulseImageAlpha(_atkSwordImage));
            }
            else
            {
                if (_atkPulseCoroutine != null) { StopCoroutine(_atkPulseCoroutine); _atkPulseCoroutine = null; }
                RestoreImageAlpha(_atkSwordImage);
            }
        }

        private void SetDefHighlight(bool active)
        {
            if (_defShieldImage == null) return;
            if (active && isActiveAndEnabled)
            {
                if (_defPulseCoroutine == null)
                    _defPulseCoroutine = StartCoroutine(PulseImageAlpha(_defShieldImage));
            }
            else
            {
                if (_defPulseCoroutine != null) { StopCoroutine(_defPulseCoroutine); _defPulseCoroutine = null; }
                RestoreImageAlpha(_defShieldImage);
            }
        }

        /// <summary>
        /// 아이콘 스프라이트(검/방패) 알파를 코사인 파동으로 PULSE_MIN_ALPHA↔1.0 깜빡임.
        /// 원본 RGB는 보존(예: ATK 검의 빨강색 그대로 유지).
        /// </summary>
        private IEnumerator PulseImageAlpha(Image image)
        {
            if (image == null) yield break;
            Color baseColor = image.color;     // RGB 원본 캡처 (펄스 시작 시점 기준)
            float t = 0f;
            while (image != null)
            {
                t += Time.deltaTime / PULSE_PERIOD;
                float wave  = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
                float alpha = Mathf.Lerp(PULSE_MIN_ALPHA, 1f, wave);
                image.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        /// <summary>펄스 중단 시 알파 1.0으로 복원.</summary>
        private void RestoreImageAlpha(Image image)
        {
            if (image == null) return;
            Color c = image.color;
            image.color = new Color(c.r, c.g, c.b, 1f);
        }

        // (구) 외곽선 펄스 — 아이콘 알파 펄스로 대체됨, 복원 시 참고용 보존
        // private IEnumerator PulseOutline(Outline outline)
        // {
        //     outline.enabled = true;
        //     Color baseColor = _intentHighlightColor;
        //     float t = 0f;
        //     while (outline != null && outline.enabled)
        //     {
        //         t += Time.deltaTime / PULSE_PERIOD;
        //         float wave  = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
        //         float alpha = Mathf.Lerp(PULSE_MIN_ALPHA, 1f, wave);
        //         outline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        //         yield return null;
        //     }
        // }

        private void PlaySummonToast()
        {
            if (_summonToast == null) return;          // EnemyEntry(일반 적)는 null — 무시.
            if (_summonToastCoroutine != null) return; // 이미 표시 중이면 중복 트리거 무시.
            if (_summonToastText) _summonToastText.text = "Summon";
            _summonToastCoroutine = StartCoroutine(SummonToastRoutine());
        }

        private IEnumerator SummonToastRoutine()
        {
            _summonToast.SetActive(true);
            CanvasGroup cg = _summonToast.GetComponent<CanvasGroup>();
            if (cg == null) cg = _summonToast.AddComponent<CanvasGroup>();

            // 페이드인
            float t = 0f;
            while (t < SUMMON_FADE_IN)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / SUMMON_FADE_IN);
                yield return null;
            }
            cg.alpha = 1f;

            // 유지
            yield return new WaitForSeconds(SUMMON_HOLD);

            // 페이드아웃
            t = 0f;
            while (t < SUMMON_FADE_OUT)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / SUMMON_FADE_OUT);
                yield return null;
            }
            cg.alpha = 0f;
            _summonToast.SetActive(false);
            _summonToastCoroutine = null;
        }

        private void StopAllIntentEffects()
        {
            if (_atkPulseCoroutine    != null) { StopCoroutine(_atkPulseCoroutine);    _atkPulseCoroutine    = null; }
            if (_defPulseCoroutine    != null) { StopCoroutine(_defPulseCoroutine);    _defPulseCoroutine    = null; }
            if (_summonToastCoroutine != null) { StopCoroutine(_summonToastCoroutine); _summonToastCoroutine = null; }
            RestoreImageAlpha(_atkSwordImage);
            RestoreImageAlpha(_defShieldImage);
            if (_summonToast) _summonToast.SetActive(false);
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

        public void ShowTargetBadge(int slotNumber, string skillName, string damageText)
        {
            if (_targetBadgeContainer == null) EnsureTargetBadgeContainer();
            if (_targetBadgeContainer) _targetBadgeContainer.SetActive(true);

            var box = CreateBadgeBox(slotNumber + 1);
            _badgeBoxes.Add(box);
        }

        public void ClearTargetBadge()
        {
            foreach (var box in _badgeBoxes)
                if (box != null) Destroy(box);
            _badgeBoxes.Clear();
            if (_targetBadgeContainer) _targetBadgeContainer.SetActive(false);
        }

        private GameObject CreateBadgeBox(int displayNumber)
        {
            var box = new GameObject($"Badge_{displayNumber}");
            box.transform.SetParent(_targetBadgeContainer.transform, false);

            var rt = box.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(32f, 28f);

            var bg = box.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            bg.raycastTarget = false;

            var outline = box.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.78f, 0.18f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(box.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (_nameText != null)
            {
                tmp.font = _nameText.font;
                tmp.fontSharedMaterial = _nameText.fontSharedMaterial;
            }
            tmp.fontSize = 14f;
            tmp.color = new Color(1f, 0.9f, 0.3f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = $"[{displayNumber}]";
            tmp.raycastTarget = false;

            return box;
        }

        private void EnsureTargetBadgeContainer()
        {
            if (_targetBadgeContainer != null) return;

            var containerGo = new GameObject("TargetBadgeContainer");
            containerGo.transform.SetParent(transform, false);

            var rt = containerGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta = new Vector2(200f, 28f);

            var hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var csf = containerGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            _targetBadgeContainer = containerGo;
            containerGo.SetActive(false);
        }

        public void PlayDeathEffect()
        {
            if (_animator) _animator.SetTrigger(_deathHash);
            StopAllIntentEffects();
        }
    }
}
