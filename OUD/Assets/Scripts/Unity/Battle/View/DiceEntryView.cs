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
        [SerializeField] private float _scaleAtMaxHeight = 1.8f;
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

            // === 경계 계산 (부모 슬롯 위치 보정 포함) ===
            CalculateTableBounds(out float minX, out float maxX, out float minY, out float maxY);

            // === Z축 상태 (높이: 0 = 테이블 위) ===
            float zHeight = _dropHeight + UnityEngine.Random.Range(0f, 80f);
            float zVelocity = -UnityEngine.Random.Range(50f, 150f);
            bool hasLanded = false;

            // === 테이블 X/Y 상태 (탑뷰 평면 이동) ===
            // 시작 위치: DiceArea 중앙 부근 (슬롯 보정 적용)
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float rangeX = (maxX - minX) * 0.3f;
            float rangeY = (maxY - minY) * 0.3f;
            Vector2 tablePos = new Vector2(
                centerX + UnityEngine.Random.Range(-rangeX, rangeX),
                centerY + UnityEngine.Random.Range(-rangeY, rangeY));
            Vector2 tableVel = Vector2.zero;
            float rotation = UnityEngine.Random.Range(0f, 360f);

            // === 스프라이트 ===
            float frameInterval = 1f / _frameRate;
            float frameTimer = 0f;
            int lastSpriteIndex = -1;
            float elapsed = 0f;

            // 초기 스케일 적용
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

                    // Z 바운스
                    zVelocity = Mathf.Abs(zVelocity) * _zBounceCoeff;
                    if (zVelocity < 30f) zVelocity = 0f;
                }

                // --- 테이블 X/Y 물리 (착지 후에만) ---
                if (hasLanded)
                {
                    tablePos += tableVel * dt;

                    // 벽 충돌 (부모 슬롯 위치 보정된 경계)
                    if (tablePos.x < minX) { tablePos.x = minX; tableVel.x = -tableVel.x * _wallBounceCoeff; }
                    if (tablePos.x > maxX) { tablePos.x = maxX; tableVel.x = -tableVel.x * _wallBounceCoeff; }
                    if (tablePos.y < minY) { tablePos.y = minY; tableVel.y = -tableVel.y * _wallBounceCoeff; }
                    if (tablePos.y > maxY) { tablePos.y = maxY; tableVel.y = -tableVel.y * _wallBounceCoeff; }

                    // 마찰 감속
                    tableVel *= Mathf.Exp(-_slideFriction * dt);

                    // 이동에 따른 회전
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
                float dampMult = 1f + step * 2f;

                while (stepElapsed < stepDuration)
                {
                    float dt = Time.deltaTime;
                    stepElapsed += dt;

                    // 테이블 물리 (강한 감쇠)
                    tablePos += tableVel * dt;
                    if (tablePos.x < minX) { tablePos.x = minX; tableVel.x = -tableVel.x * 0.3f; }
                    if (tablePos.x > maxX) { tablePos.x = maxX; tableVel.x = -tableVel.x * 0.3f; }
                    if (tablePos.y < minY) { tablePos.y = minY; tableVel.y = -tableVel.y * 0.3f; }
                    if (tablePos.y > maxY) { tablePos.y = maxY; tableVel.y = -tableVel.y * 0.3f; }
                    tableVel *= Mathf.Exp(-_slideFriction * dampMult * dt);

                    rotation += tableVel.magnitude * _rotationMultiplier * 0.5f * dt *
                                Mathf.Sign(tableVel.x);

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
        /// Z축 높이 → 스케일 (높을수록 큼 = 카메라에 가까움), 위치/회전 적용
        /// </summary>
        private void ApplyVisuals(Vector2 tablePos, float zHeight, float rotation)
        {
            if (_diceImageRect == null) return;

            // 높을수록 크게 (카메라 근처 → 테이블로 떨어짐)
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

            // 스쿼시
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
        /// DiceArea 전체 영역에서 이동 가능한 경계를 부모 슬롯 위치 기준으로 계산.
        /// 각 Dice 슬롯의 오프셋을 보정하여 DiceImage가 DiceArea 전체를 활용.
        /// </summary>
        private void CalculateTableBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            if (_diceAreaRect == null || _diceImageRect == null)
            {
                minX = -100f; maxX = 100f;
                minY = -60f;  maxY = 60f;
                return;
            }

            Vector2 areaSize = _diceAreaRect.rect.size;
            Vector2 areaPivot = _diceAreaRect.pivot;

            // DiceArea 로컬 좌표계에서의 경계
            float areaLeft   = -areaSize.x * areaPivot.x;
            float areaRight  =  areaSize.x * (1f - areaPivot.x);
            float areaBottom = -areaSize.y * areaPivot.y;
            float areaTop    =  areaSize.y * (1f - areaPivot.y);

            // 이 슬롯(Dice1~5)의 DiceArea 내 위치
            RectTransform slotRect = transform as RectTransform;
            Vector2 slotPos = slotRect.anchoredPosition;

            // 다이스 이미지 절반 크기 (여유분)
            Vector2 imgHalf = _diceImageRect.rect.size * 0.5f;

            // DiceImage의 anchoredPosition은 부모(슬롯) 기준이므로
            // DiceArea 전체를 쓰려면 슬롯 오프셋만큼 보정 필요
            minX = areaLeft   + imgHalf.x - slotPos.x;
            maxX = areaRight  - imgHalf.x - slotPos.x;
            minY = areaBottom + imgHalf.y - slotPos.y;
            maxY = areaTop    - imgHalf.y - slotPos.y;
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
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);

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
