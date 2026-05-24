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

        [Header("Z축 낙하 (탑뷰)")]
        [SerializeField] private float _dropHeight = 250f;
        [SerializeField] private float _zGravity = 2000f;
        [SerializeField] private float _zBounceCoeff = 0.45f;
        [SerializeField] private float _scaleAtMaxHeight = 0.3f;
        [SerializeField] private float _impactSquash = 1.15f;
        [SerializeField] private float _squashDuration = 0.06f;

        [Header("테이블 위 미끄러짐 (탑뷰 X/Y)")]
        [SerializeField] private float _slideSpeedMin = 150f;
        [SerializeField] private float _slideSpeedMax = 350f;
        [SerializeField] private float _slideFriction = 3f;
        [SerializeField] private float _wallBounceCoeff = 0.5f;
        [SerializeField] private float _rotationMultiplier = 0.4f;

        [Header("복귀")]
        [SerializeField] private float _settleTime = 0.25f;

        [Header("Keep 하이라이트")]
        [SerializeField] private Color   _normalColor = new Color(0.09f, 0.06f, 0.04f);
        [SerializeField] private Color   _keptColor   = new Color(0.83f, 0.63f, 0.09f);
        [SerializeField] private Vector2 _borderThickness = new Vector2(4f, 4f);

        private bool    _kept;
        private Outline _outline;
        private Coroutine _rollCoroutine;
        private RectTransform _diceImageRect;
        private RectTransform _diceAreaRect;

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

            // DiceArea = 부모 (Dice1~5의 부모)
            if (transform.parent != null)
                _diceAreaRect = transform.parent.GetComponent<RectTransform>();

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

            // === 경계 계산 (테이블 표면 = DiceArea) ===
            Vector2 tableBounds = CalculateTableBounds();
            float halfW = tableBounds.x;
            float halfH = tableBounds.y;

            // === Z축 상태 (높이: 0 = 테이블 위) ===
            float zHeight = _dropHeight + UnityEngine.Random.Range(0f, 80f);
            float zVelocity = -UnityEngine.Random.Range(50f, 150f); // 초기 약간 하강
            bool hasLanded = false;
            int bounceCount = 0;

            // === 테이블 X/Y 상태 (탑뷰 평면 이동) ===
            Vector2 tablePos = new Vector2(
                UnityEngine.Random.Range(-halfW * 0.3f, halfW * 0.3f),
                UnityEngine.Random.Range(-halfH * 0.3f, halfH * 0.3f));
            Vector2 tableVel = Vector2.zero;
            float rotation = UnityEngine.Random.Range(0f, 360f);

            // === 스프라이트 ===
            float frameInterval = 1f / _frameRate;
            float frameTimer = 0f;
            int lastSpriteIndex = -1;
            float elapsed = 0f;

            // 낙하 전 초기 스케일 적용
            ApplyVisuals(tablePos, zHeight, rotation);

            // === 메인 물리 루프 ===
            while (elapsed < stopDelay)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // --- Z축 물리 (낙하/바운스) ---
                zVelocity -= _zGravity * dt;
                zHeight += zVelocity * dt;

                if (zHeight <= 0f)
                {
                    zHeight = 0f;
                    bounceCount++;

                    if (!hasLanded)
                    {
                        // 첫 착지: 테이블 위 랜덤 방향으로 미끄러짐 시작
                        hasLanded = true;
                        float slideSpeed = UnityEngine.Random.Range(_slideSpeedMin, _slideSpeedMax);
                        float slideAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        tableVel = new Vector2(
                            Mathf.Cos(slideAngle) * slideSpeed,
                            Mathf.Sin(slideAngle) * slideSpeed);

                        // 착지 스쿼시 효과
                        StartCoroutine(SquashEffect());
                    }

                    // Z 바운스 (점점 약해짐)
                    zVelocity = Mathf.Abs(zVelocity) * _zBounceCoeff;
                    if (zVelocity < 30f) zVelocity = 0f; // 미세 바운스 제거
                }

                // --- 테이블 X/Y 물리 (착지 후에만) ---
                if (hasLanded)
                {
                    tablePos += tableVel * dt;

                    // 벽 충돌
                    if (tablePos.x < -halfW) { tablePos.x = -halfW; tableVel.x = -tableVel.x * _wallBounceCoeff; }
                    if (tablePos.x >  halfW) { tablePos.x =  halfW; tableVel.x = -tableVel.x * _wallBounceCoeff; }
                    if (tablePos.y < -halfH) { tablePos.y = -halfH; tableVel.y = -tableVel.y * _wallBounceCoeff; }
                    if (tablePos.y >  halfH) { tablePos.y =  halfH; tableVel.y = -tableVel.y * _wallBounceCoeff; }

                    // 마찰 감속
                    tableVel *= Mathf.Exp(-_slideFriction * dt);

                    // 이동 방향에 따른 회전
                    rotation += tableVel.magnitude * _rotationMultiplier * dt *
                                Mathf.Sign(tableVel.x + tableVel.y * 0.5f);
                }

                // --- 비주얼 적용 ---
                ApplyVisuals(tablePos, zHeight, rotation);

                // --- 스프라이트 프레임 순환 ---
                frameTimer += dt;
                if (frameTimer >= frameInterval)
                {
                    frameTimer -= frameInterval;
                    int index;
                    do { index = UnityEngine.Random.Range(0, _rollingFrames.Length); }
                    while (index == lastSpriteIndex && _rollingFrames.Length > 1);
                    lastSpriteIndex = index;
                    _diceImage.sprite = _rollingFrames[index];
                }

                yield return null;
            }

            // === 감속 구간 ===
            float[] decelFps = { _frameRate * 0.66f, _frameRate * 0.33f };
            float stepDuration = _decelerationDuration / decelFps.Length;

            for (int step = 0; step < decelFps.Length; step++)
            {
                float stepElapsed = 0f;
                float interval = 1f / decelFps[step];
                float stepTimer = 0f;
                float dampMult = 1f + step * 2f; // 점점 더 강한 감쇠

                while (stepElapsed < stepDuration)
                {
                    float dt = Time.deltaTime;
                    stepElapsed += dt;

                    // 테이블 물리 (강한 감쇠)
                    tablePos += tableVel * dt;
                    if (tablePos.x < -halfW) { tablePos.x = -halfW; tableVel.x = -tableVel.x * 0.3f; }
                    if (tablePos.x >  halfW) { tablePos.x =  halfW; tableVel.x = -tableVel.x * 0.3f; }
                    if (tablePos.y < -halfH) { tablePos.y = -halfH; tableVel.y = -tableVel.y * 0.3f; }
                    if (tablePos.y >  halfH) { tablePos.y =  halfH; tableVel.y = -tableVel.y * 0.3f; }
                    tableVel *= Mathf.Exp(-_slideFriction * dampMult * dt);

                    rotation += tableVel.magnitude * _rotationMultiplier * 0.5f * dt *
                                Mathf.Sign(tableVel.x);

                    // Z는 이미 0에 안착
                    ApplyVisuals(tablePos, 0f, rotation);

                    stepTimer += dt;
                    if (stepTimer >= interval)
                    {
                        stepTimer -= interval;
                        int index;
                        do { index = UnityEngine.Random.Range(0, _rollingFrames.Length); }
                        while (index == lastSpriteIndex && _rollingFrames.Length > 1);
                        lastSpriteIndex = index;
                        _diceImage.sprite = _rollingFrames[index];
                    }

                    yield return null;
                }
            }

            // === 결과 스프라이트 ===
            if (_resultSprites != null && resultValue >= 1 && resultValue <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[resultValue - 1];

            // === 슬롯 위치로 복귀 ===
            yield return SettleRoutine(tablePos, rotation);

            _rollCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Z축 높이 → 스케일, 테이블 위치 → anchoredPosition, 회전 적용
        /// </summary>
        private void ApplyVisuals(Vector2 tablePos, float zHeight, float rotation)
        {
            if (_diceImageRect == null) return;

            // Z 높이에 따른 스케일 (높을수록 작음 = 멀리 있음)
            float heightRatio = Mathf.Clamp01(zHeight / _dropHeight);
            float scale = Mathf.Lerp(1f, _scaleAtMaxHeight, heightRatio);

            _diceImageRect.anchoredPosition = tablePos;
            _diceImageRect.localScale = Vector3.one * scale;
            _diceImageRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        /// <summary>
        /// 착지 순간 스쿼시 효과 (납작해졌다 복원)
        /// </summary>
        private IEnumerator SquashEffect()
        {
            if (_diceImageRect == null) yield break;

            float half = _squashDuration * 0.5f;
            float elapsed = 0f;

            // 스쿼시 (가로 넓고 세로 납작)
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float sx = Mathf.Lerp(1f, _impactSquash, t);
                float sy = Mathf.Lerp(1f, 1f / _impactSquash, t);
                _diceImageRect.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            // 복원
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float sx = Mathf.Lerp(_impactSquash, 1f, t);
                float sy = Mathf.Lerp(1f / _impactSquash, 1f, t);
                _diceImageRect.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            _diceImageRect.localScale = Vector3.one;
        }

        /// <summary>
        /// DiceArea 기준 테이블 표면 이동 영역 (half-extents)
        /// </summary>
        private Vector2 CalculateTableBounds()
        {
            if (_diceAreaRect == null || _diceImageRect == null)
                return new Vector2(100f, 60f);

            Vector2 areaSize = _diceAreaRect.rect.size;
            Vector2 imgSize = _diceImageRect.rect.size;

            // DiceImage가 DiceArea 안에서 이동 가능한 범위
            float halfW = (areaSize.x - imgSize.x) * 0.5f;
            float halfH = (areaSize.y - imgSize.y) * 0.5f;

            return new Vector2(Mathf.Max(halfW, 20f), Mathf.Max(halfH, 20f));
        }

        /// <summary>
        /// 현재 위치에서 원래 슬롯(0,0)으로 부드럽게 복귀
        /// </summary>
        private IEnumerator SettleRoutine(Vector2 fromPos, float fromRotation)
        {
            if (_diceImageRect == null) yield break;

            float elapsed = 0f;
            Quaternion startRot = Quaternion.Euler(0f, 0f, fromRotation);
            Vector3 startScale = _diceImageRect.localScale;

            while (elapsed < _settleTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _settleTime;
                float ease = 1f - (1f - t) * (1f - t) * (1f - t); // ease-out cubic

                _diceImageRect.anchoredPosition = Vector2.Lerp(fromPos, Vector2.zero, ease);
                _diceImageRect.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, ease);
                _diceImageRect.localScale = Vector3.Lerp(startScale, Vector3.one, ease);
                yield return null;
            }

            ResetMotion();
        }

        private void ResetMotion()
        {
            if (_diceImageRect == null) return;
            _diceImageRect.anchoredPosition = Vector2.zero;
            _diceImageRect.localRotation = Quaternion.identity;
            _diceImageRect.localScale = Vector3.one;
        }
    }
}
