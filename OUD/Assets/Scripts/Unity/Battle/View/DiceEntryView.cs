using System;
using System.Collections;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class DiceEntryView : ViewBase, IDiceEntryView
    {
        [SerializeField] private Image    _diceImage;
        [SerializeField] private Image    _background;
        [SerializeField] private Button   _button;

        [Header("스프라이트")]
        [SerializeField] private Sprite[] _rollingFrames;
        [SerializeField] private Sprite[] _resultSprites;

        [Header("애니메이션")]
        [SerializeField] private float _frameRate = 12f;
        [SerializeField] private float _decelerationDuration = 0.3f;

        [Header("굴림 모션")]
        [SerializeField] private float _shakeAmplitude = 8f;
        [SerializeField] private float _shakeFrequency = 15f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _settleTime = 0.15f;

        [Header("Keep 하이라이트")]
        [SerializeField] private Color   _normalColor = new Color(0.09f, 0.06f, 0.04f);
        [SerializeField] private Color   _keptColor   = new Color(0.83f, 0.63f, 0.09f);
        [SerializeField] private Vector2 _borderThickness = new Vector2(4f, 4f);

        private bool    _kept;
        private Outline _outline;
        private Coroutine _rollCoroutine;
        private RectTransform _diceImageRect;

        // 비활성 상태에서 PlayRoll 호출 시 대기용
        private bool   _pendingRoll;
        private int    _pendingResult;
        private float  _pendingDelay;
        private Action _pendingCallback;

        public event Action OnToggled;

        private void Awake()
        {
            if (_button) _button.onClick.AddListener(() => OnToggled?.Invoke());

            if (_diceImage)
                _diceImageRect = _diceImage.GetComponent<RectTransform>();

            if (_background)
            {
                _background.color = _normalColor;
                _outline = _background.GetComponent<Outline>();
                if (_outline == null) _outline = _background.gameObject.AddComponent<Outline>();
                _outline.effectDistance = _borderThickness;
                _outline.useGraphicAlpha = false;
                var hidden = _keptColor;
                hidden.a = 0f;
                _outline.effectColor = hidden;
            }
        }

        private void OnEnable()
        {
            if (_pendingRoll)
            {
                _pendingRoll = false;
                if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
                _rollCoroutine = StartCoroutine(RollRoutine(_pendingResult, _pendingDelay, _pendingCallback));
            }
        }

        public void PlayRoll(int resultValue, float stopDelay, Action onComplete)
        {
            if (!gameObject.activeInHierarchy)
            {
                _pendingRoll = true;
                _pendingResult = resultValue;
                _pendingDelay = stopDelay;
                _pendingCallback = onComplete;
                return;
            }

            if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
            _rollCoroutine = StartCoroutine(RollRoutine(resultValue, stopDelay, onComplete));
        }

        public void SetResultImmediate(int value)
        {
            if (_rollCoroutine != null)
            {
                StopCoroutine(_rollCoroutine);
                _rollCoroutine = null;
            }

            // 모션 초기화
            ResetMotion();

            if (_resultSprites != null && value >= 1 && value <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[value - 1];
        }

        public void SetKept(bool kept)
        {
            _kept = kept;
            if (_outline)
            {
                var c = _keptColor;
                c.a = kept ? 1f : 0f;
                _outline.effectColor = c;
            }
        }

        private IEnumerator RollRoutine(int resultValue, float stopDelay, Action onComplete)
        {
            if (_rollingFrames == null || _rollingFrames.Length == 0)
            {
                SetResultImmediate(resultValue);
                onComplete?.Invoke();
                yield break;
            }

            // 각 주사위마다 고유한 모션 오프셋
            float phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float rotDir = UnityEngine.Random.value > 0.5f ? 1f : -1f;

            float elapsed = 0f;
            float frameInterval = 1f / _frameRate;
            float timer = 0f;
            int lastIndex = -1;

            // === 메인 롤링 ===
            while (elapsed < stopDelay)
            {
                timer += Time.deltaTime;
                if (timer >= frameInterval)
                {
                    timer -= frameInterval;
                    int index;
                    do { index = UnityEngine.Random.Range(0, _rollingFrames.Length); }
                    while (index == lastIndex && _rollingFrames.Length > 1);
                    lastIndex = index;
                    _diceImage.sprite = _rollingFrames[index];
                }

                // 위치 떨림 + 회전
                ApplyMotion(elapsed, phaseOffset, rotDir, 1f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // === 감속 구간 ===
            float[] decelFps = { _frameRate * 0.66f, _frameRate * 0.33f };
            float stepDuration = _decelerationDuration / decelFps.Length;
            float totalDecelElapsed = 0f;

            for (int step = 0; step < decelFps.Length; step++)
            {
                float stepElapsed = 0f;
                float interval = 1f / decelFps[step];
                float stepTimer = 0f;
                // 감속 비율: 점점 줄어듦
                float motionScale = 1f - (float)(step + 1) / (decelFps.Length + 1);

                while (stepElapsed < stepDuration)
                {
                    stepTimer += Time.deltaTime;
                    if (stepTimer >= interval)
                    {
                        stepTimer -= interval;
                        int index;
                        do { index = UnityEngine.Random.Range(0, _rollingFrames.Length); }
                        while (index == lastIndex && _rollingFrames.Length > 1);
                        lastIndex = index;
                        _diceImage.sprite = _rollingFrames[index];
                    }

                    totalDecelElapsed += Time.deltaTime;
                    ApplyMotion(elapsed + totalDecelElapsed, phaseOffset, rotDir, motionScale);

                    stepElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // === 결과 스프라이트 설정 ===
            if (_resultSprites != null && resultValue >= 1 && resultValue <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[resultValue - 1];

            // === 제자리 복귀 애니메이션 ===
            yield return SettleRoutine();

            _rollCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 롤링 중 위치 떨림 + Z회전 적용
        /// </summary>
        private void ApplyMotion(float time, float phaseOffset, float rotDir, float scale)
        {
            if (_diceImageRect == null) return;

            float t = time * _shakeFrequency;
            float x = Mathf.Sin(t + phaseOffset) * _shakeAmplitude * scale;
            float y = Mathf.Cos(t * 1.3f + phaseOffset) * _shakeAmplitude * 0.7f * scale;
            _diceImageRect.anchoredPosition = new Vector2(x, y);

            float rot = time * _rotationSpeed * rotDir * scale;
            _diceImageRect.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// <summary>
        /// 결과 확정 후 부드럽게 제자리 복귀
        /// </summary>
        private IEnumerator SettleRoutine()
        {
            if (_diceImageRect == null) yield break;

            Vector2 startPos = _diceImageRect.anchoredPosition;
            Quaternion startRot = _diceImageRect.localRotation;
            float elapsed = 0f;

            while (elapsed < _settleTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _settleTime;
                // ease-out cubic
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);

                _diceImageRect.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, ease);
                _diceImageRect.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, ease);
                yield return null;
            }

            ResetMotion();
        }

        private void ResetMotion()
        {
            if (_diceImageRect == null) return;
            _diceImageRect.anchoredPosition = Vector2.zero;
            _diceImageRect.localRotation = Quaternion.identity;
        }
    }
}
