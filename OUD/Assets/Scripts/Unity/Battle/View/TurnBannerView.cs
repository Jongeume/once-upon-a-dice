using System.Collections;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>턴 전환 시 화면 중앙 상단에 "Your Turn" / "Enemy Turn" 배너를 잠깐 띄운다.
    /// 더불어 내 턴 시작 ~ 주사위 굴리기 전 정지 구간 동안 Roll Dice 버튼에 펄스 글로우를 건다.
    /// (연출/글로우 패턴은 TutorialOverlayView를 재사용)</summary>
    public class TurnBannerView : ViewBase
    {
        [Header("배너")]
        [Tooltip("페이드를 제어할 CanvasGroup. 항상 활성 상태로 두고 alpha만 조절한다.")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text    _bannerText;

        [Header("Roll Dice 글로우 (선택)")]
        [Tooltip("내 턴 시작 시 글로우를 걸 Roll Dice 버튼 오브젝트(BattlePanel/ActionButtonA).")]
        [SerializeField] private GameObject _rollDiceButton;

        // ── 연출 타이밍 ──
        private const float FADE_IN_DURATION  = 0.25f;
        private const float HOLD_DURATION     = 0.70f;
        private const float FADE_OUT_DURATION = 0.35f;

        // ── 강조 색 ──
        private static readonly Color ALLY_COLOR  = new Color(0.32f, 0.85f, 0.70f, 1f); // 청록
        private static readonly Color ENEMY_COLOR = new Color(0.90f, 0.28f, 0.28f, 1f); // 빨강

        private const string YOUR_TURN_TEXT  = "Your Turn";
        private const string ENEMY_TURN_TEXT = "Enemy Turn";

        // ── 글로우 (TutorialOverlayView 패턴 재사용) ──
        private static readonly Color GLOW_COLOR = new Color(0.95f, 0.78f, 0.18f, 1f);
        private const float GLOW_PULSE_PERIOD    = 1.0f;
        private const float GLOW_PULSE_MIN_ALPHA = 0.3f;

        private Coroutine _bannerCoroutine;
        private Coroutine _glowCoroutine;
        private Outline   _glowOutline;

        private void Awake()
        {
            // 입력을 절대 막지 않도록 보장 (배너는 순수 시각 효과).
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        // ── 배너 ──────────────────────────────────────────────────────────

        public void ShowYourTurn()  => ShowBanner(YOUR_TURN_TEXT,  ALLY_COLOR);
        public void ShowEnemyTurn() => ShowBanner(ENEMY_TURN_TEXT, ENEMY_COLOR);

        public void ShowBanner(string text, Color accent)
        {
            if (_bannerText != null)
            {
                _bannerText.text  = text;
                _bannerText.color = accent;
            }
            if (!isActiveAndEnabled) return; // 코루틴 실행 불가 시 무시.
            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(PlayBanner());
        }

        private IEnumerator PlayBanner()
        {
            yield return Fade(0f, 1f, FADE_IN_DURATION);
            yield return new WaitForSeconds(HOLD_DURATION);
            yield return Fade(1f, 0f, FADE_OUT_DURATION);
            _bannerCoroutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;
            float elapsed = 0f;
            _canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        public void HideImmediate()
        {
            if (_bannerCoroutine != null) { StopCoroutine(_bannerCoroutine); _bannerCoroutine = null; }
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        // ── Roll Dice 글로우 ─────────────────────────────────────────────

        public void SetRollDiceGlow(bool active)
        {
            if (!active) { ClearGlow(); return; }
            if (_rollDiceButton == null) return;

            _glowOutline = _rollDiceButton.GetComponent<Outline>();
            if (_glowOutline == null) _glowOutline = _rollDiceButton.AddComponent<Outline>();
            _glowOutline.effectDistance = new Vector2(2f, -2f);
            _glowOutline.enabled = true;

            if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
            if (isActiveAndEnabled)
                _glowCoroutine = StartCoroutine(PulseGlow(_glowOutline));
        }

        private void ClearGlow()
        {
            if (_glowCoroutine != null) { StopCoroutine(_glowCoroutine); _glowCoroutine = null; }
            if (_glowOutline != null) _glowOutline.enabled = false;
        }

        private IEnumerator PulseGlow(Outline outline)
        {
            float t = 0f;
            while (outline != null && outline.enabled)
            {
                t += Time.deltaTime / GLOW_PULSE_PERIOD;
                float wave  = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
                float alpha = Mathf.Lerp(GLOW_PULSE_MIN_ALPHA, 1f, wave);
                outline.effectColor = new Color(GLOW_COLOR.r, GLOW_COLOR.g, GLOW_COLOR.b, alpha);
                yield return null;
            }
        }

        private void OnDisable()
        {
            ClearGlow();
            if (_bannerCoroutine != null) { StopCoroutine(_bannerCoroutine); _bannerCoroutine = null; }
        }
    }
}
