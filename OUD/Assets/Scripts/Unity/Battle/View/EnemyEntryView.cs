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

        [Header("실드 (HP 바 좌측)")]
        [SerializeField] private GameObject _shieldDisplay;
        [SerializeField] private Image      _shieldIcon;
        [SerializeField] private TMP_Text   _shieldValueText;

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

        // 의도 표시: 공격 시 검 아이콘만 표시 / 방어 시 방패 아이콘만 표시 (비활성 쪽 숨김)
        // Summon 토스트 펄스용 상수
        private const float PULSE_PERIOD    = 1.0f;
        private const float PULSE_MIN_ALPHA = 0.3f;

        private Coroutine _summonToastCoroutine;

        // HP 바 서서히 감소 애니메이션
        private const float HP_DRAIN_SPEED = 1.2f;    // 초당 anchorMax.x 감소량
        private Coroutine _hpDrainCoroutine;
        private RectTransform _hpFillRect;
        private float _currentHpRatio = 1f;           // 실제 HP 비율 (애니메이션 목표)

        // 데미지 프리뷰 — 타겟팅 시 깎일 부분을 반투명 초록으로 표시
        private const float PREVIEW_ALPHA_MIN = 0.15f;
        private const float PREVIEW_ALPHA_MAX = 0.45f;
        private const float PREVIEW_PULSE_SPEED = 0.8f;   // 초당 펄스 사이클 수
        private Image _hpPreviewFill;
        private RectTransform _hpPreviewRect;
        private bool _isPreviewActive;
        private Coroutine _previewPulseCoroutine;

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
            EnsureHpTrail();
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

        /// <summary>
        /// HP 바 초기화. RectTransform 캐시 + 프리뷰 바 자동 생성.
        /// HP 바는 Sliced Image이므로 anchorMax.x 방식으로 너비 조절.
        /// </summary>
        private void EnsureHpTrail()
        {
            if (_hpFill != null)
                _hpFillRect = _hpFill.GetComponent<RectTransform>();

            if (_hpFill == null) return;

            CreatePreviewBar();
        }

        private void CreatePreviewBar()
        {
            if (_hpPreviewFill != null || _hpFill == null) return;

            GameObject previewGo = Instantiate(_hpFill.gameObject, _hpFill.transform.parent);
            previewGo.name = "HPPreviewFill";
            // 초록 바 바로 뒤(같은 위치)에 배치 → 초록 바가 위, 프리뷰가 아래
            previewGo.transform.SetSiblingIndex(_hpFill.transform.GetSiblingIndex());

            _hpPreviewFill = previewGo.GetComponent<Image>();
            Color previewColor = _hpFill.color;
            previewColor.a = PREVIEW_ALPHA_MAX;
            _hpPreviewFill.color = previewColor;

            _hpPreviewRect = previewGo.GetComponent<RectTransform>();
            previewGo.SetActive(false);
        }

        /// <summary>anchorMax.x를 이용한 HP바 너비 설정 (Sliced Image 호환).</summary>
        private void SetHpBarRatio(RectTransform rt, float ratio)
        {
            if (rt == null) return;
            Vector2 aMax = rt.anchorMax;
            aMax.x = Mathf.Clamp01(ratio);
            rt.anchorMax = aMax;
        }

        /// <summary>초록 HP 바가 부드럽게 목표 비율까지 줄어드는 애니메이션.</summary>
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

        /// <summary>
        /// 타겟팅 시 예상 데미지만큼 HP바를 반투명 표시.
        /// 초록 바를 predictedRatio까지 축소하고, 사이 구간을 반투명 초록으로 보여준다.
        /// </summary>
        public void ShowDamagePreview(float predictedRatio)
        {
            predictedRatio = Mathf.Clamp01(predictedRatio);
            if (predictedRatio >= _currentHpRatio)
            {
                ClearDamagePreview();
                return;
            }

            _isPreviewActive = true;

            // 초록 바 → 피해 후 남을 비율까지 축소
            SetHpBarRatio(_hpFillRect, predictedRatio);

            // 프리뷰 바 → 현재 HP 비율에서 예상 비율 사이를 반투명 초록으로 표시
            if (_hpPreviewRect != null)
            {
                _hpPreviewFill.gameObject.SetActive(true);
                Vector2 aMin = _hpPreviewRect.anchorMin;
                aMin.x = predictedRatio;
                _hpPreviewRect.anchorMin = aMin;
                SetHpBarRatio(_hpPreviewRect, _currentHpRatio);

                // 깜빡이는 펄스 효과 시작
                if (_previewPulseCoroutine != null) StopCoroutine(_previewPulseCoroutine);
                if (isActiveAndEnabled)
                    _previewPulseCoroutine = StartCoroutine(PulsePreviewBar());
            }
        }

        /// <summary>데미지 프리뷰 해제 → 초록 바 원래 비율로 복구.</summary>
        public void ClearDamagePreview()
        {
            if (!_isPreviewActive) return;
            _isPreviewActive = false;

            // 펄스 코루틴 정지
            if (_previewPulseCoroutine != null)
            {
                StopCoroutine(_previewPulseCoroutine);
                _previewPulseCoroutine = null;
            }

            // 초록 바 원래 HP 비율 복구
            SetHpBarRatio(_hpFillRect, _currentHpRatio);

            // 프리뷰 바 숨김
            if (_hpPreviewFill != null)
                _hpPreviewFill.gameObject.SetActive(false);
        }

        /// <summary>프리뷰 바 알파를 부드럽게 오르내리며 깜빡이는 효과.</summary>
        private IEnumerator PulsePreviewBar()
        {
            float t = 0f;
            while (_hpPreviewFill != null)
            {
                t += Time.deltaTime * PREVIEW_PULSE_SPEED;
                // sin 파형으로 부드러운 페이드 (0~1 → min~max)
                float alpha = Mathf.Lerp(PREVIEW_ALPHA_MIN, PREVIEW_ALPHA_MAX,
                    (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);
                Color c = _hpPreviewFill.color;
                c.a = alpha;
                _hpPreviewFill.color = c;
                yield return null;
            }
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
            // 실제 HP 변경 시 프리뷰 해제
            if (_isPreviewActive) ClearDamagePreview();

            float previousRatio = _currentHpRatio;
            _currentHpRatio = Mathf.Clamp01(fillAmount);
            if (_hpText)
            {
                _hpText.text      = hpText;
                _hpText.alignment = TextAlignmentOptions.Center;
            }

            // 초록 바 애니메이션: 데미지 시 서서히 감소, 힐/초기화 시 즉시 반영
            if (_currentHpRatio < previousRatio)
            {
                // 데미지 — 초록 바가 서서히 줄어듦
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
            if (_shieldDisplay) _shieldDisplay.SetActive(visible);
            if (_shieldValueText) _shieldValueText.text = shield.ToString();
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
                    SetSummonHighlight(false);
                    RestoreAtkValueText();
                    break;

                case IntentType.StrongAttack:
                    SetAtkHighlight(true);
                    SetDefHighlight(false);
                    SetSummonHighlight(false);
                    if (_atkValueText) _atkValueText.text = value.ToString();
                    break;

                case IntentType.Shield:
                    SetAtkHighlight(false);
                    SetDefHighlight(true);
                    SetSummonHighlight(false);
                    RestoreAtkValueText();
                    break;

                case IntentType.Summon:
                    SetAtkHighlight(false);
                    SetDefHighlight(false);
                    SetSummonHighlight(true);
                    RestoreAtkValueText();
                    break;

                default:
                    SetAtkHighlight(false);
                    SetDefHighlight(false);
                    SetSummonHighlight(false);
                    RestoreAtkValueText();
                    break;
            }
        }

        /// <summary>StrongAttack 의도였다가 다른 의도로 전환 시 ATK 수치를 캐시된 원본으로 복귀.</summary>
        private void RestoreAtkValueText()
        {
            if (_atkValueText) _atkValueText.text = _cachedAtk.ToString();
        }

        /// <summary>공격 의도: 검 아이콘 표시, 방어 의도가 아니면 방패 숨김.</summary>
        private void SetAtkHighlight(bool active)
        {
            if (_atkSwordImage != null)
                _atkSwordImage.gameObject.SetActive(active);
        }

        /// <summary>방어 의도: 방패 아이콘 표시, 공격 의도가 아니면 검 숨김.</summary>
        private void SetDefHighlight(bool active)
        {
            if (_defShieldImage != null)
                _defShieldImage.gameObject.SetActive(active);
        }

        private void SetSummonHighlight(bool active)
        {
            if (_summonToast == null) return;
            if (active && isActiveAndEnabled)
            {
                if (_summonToastText) _summonToastText.text = "Summon";
                _summonToast.SetActive(true);
                if (_summonToastCoroutine == null)
                    _summonToastCoroutine = StartCoroutine(PulseSummonToast());
            }
            else
            {
                if (_summonToastCoroutine != null) { StopCoroutine(_summonToastCoroutine); _summonToastCoroutine = null; }
                _summonToast.SetActive(false);
            }
        }

        private IEnumerator PulseSummonToast()
        {
            CanvasGroup cg = _summonToast.GetComponent<CanvasGroup>();
            if (cg == null) cg = _summonToast.AddComponent<CanvasGroup>();
            float t = 0f;
            while (_summonToast != null)
            {
                t += Time.deltaTime / PULSE_PERIOD;
                float wave = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
                cg.alpha = Mathf.Lerp(PULSE_MIN_ALPHA, 1f, wave);
                yield return null;
            }
        }

        private void StopAllIntentEffects()
        {
            if (_summonToastCoroutine != null) { StopCoroutine(_summonToastCoroutine); _summonToastCoroutine = null; }
            if (_hpDrainCoroutine     != null) { StopCoroutine(_hpDrainCoroutine);     _hpDrainCoroutine     = null; }
            if (_previewPulseCoroutine != null) { StopCoroutine(_previewPulseCoroutine); _previewPulseCoroutine = null; }
            // 의도 아이콘 양쪽 모두 비활성화 (기본 상태)
            if (_atkSwordImage)  _atkSwordImage.gameObject.SetActive(false);
            if (_defShieldImage) _defShieldImage.gameObject.SetActive(false);
            SetSummonHighlight(false);
        }

        public void SetTargetSelectable(bool selectable)
        {
            _selectable = selectable;
            if (_targetButton) _targetButton.interactable = selectable;
            if (_targetableHint) _targetableHint.SetActive(selectable);
            if (_summonToast) _summonToast.SetActive(!selectable && _summonToastCoroutine != null);
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
            NormalizeBadgeWidths();
        }

        private void NormalizeBadgeWidths()
        {
            const float PADDING = 24f;
            float maxWidth = 0f;
            foreach (var box in _badgeBoxes)
            {
                if (box == null) continue;
                var tmp = box.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    maxWidth = Mathf.Max(maxWidth, tmp.GetPreferredValues(tmp.text).x + PADDING);
            }
            foreach (var box in _badgeBoxes)
            {
                if (box == null) continue;
                var rt = box.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(maxWidth, rt.sizeDelta.y);
            }
            var containerRt = _targetBadgeContainer?.GetComponent<RectTransform>();
            if (containerRt != null) containerRt.sizeDelta = new Vector2(maxWidth, containerRt.sizeDelta.y);
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

            // 너비는 NormalizeBadgeWidths()가 일괄 고정하므로 초기 0으로 설정
            var rt = box.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 44f);

            var bg = box.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            var outline = box.AddComponent<Outline>();
            outline.effectColor    = rimColor;
            outline.effectDistance = new Vector2(1f, -1f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(box.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 6f);
            textRt.offsetMax = new Vector2(-12f, -6f);

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
            vlg.childControlWidth      = false;  // 수동 너비(sizeDelta) 유지
            vlg.childControlHeight     = false;

            var csf = containerGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;  // NormalizeBadgeWidths가 수동 설정
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
