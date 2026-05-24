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

        [Header("Z축 낙하 (탑뷰 — 앞으로 던지기)")]
        [SerializeField] private float _dropHeight = 250f;
        [SerializeField] private float _zGravity = 2000f;
        [SerializeField] private float _zBounceCoeff = 0.45f;
        [SerializeField] private float _scaleOnTable = 0.65f;
        [SerializeField] private float _impactSquash = 1.15f;
        [SerializeField] private float _squashDuration = 0.06f;

        [Header("테이블 위 미끄러짐 (탑뷰 X/Y)")]
        [SerializeField] private float _slideSpeedMin = 150f;
        [SerializeField] private float _slideSpeedMax = 350f;
        [SerializeField] private float _slideFriction = 3f;
        [SerializeField] private float _wallBounceCoeff = 0.5f;
        [SerializeField] private float _rotationMultiplier = 0.4f;
        [SerializeField] private float _tablePadding = 10f;

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

            // === 정사각형 굴림 영역 계산 (DiceArea 중앙 기준) ===
            CalculateSquareBounds(out float minX, out float maxX, out float minY, out float maxY);

            // === Z축 상태 ===
            float zHeight = _dropHeight + UnityEngine.Random.Range(0f, 80f);
            float zVelocity = -UnityEngine.Random.Range(50f, 150f);
            bool hasLanded = false;

            // === 테이블 X/Y (정사각형 영역 중앙 근처에서 시작) ===
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;
            float rangeX = (maxX - minX) * 0.2f;
            float rangeY = (maxY - minY) * 0.2f;
            Vector2 tablePos = new Vector2(
                cx + UnityEngine.Random.Range(-rangeX, rangeX),
                cy + UnityEngine.Random.Range(-rangeY, rangeY));
            Vector2 tableVel = Vector2.zero;
            float rotation = UnityEngine.Random.Range(0f, 360f);

            // === 스프라이트 ===
            float frameInterval = 1f / _frameRate;
            float frameTimer = 0f;
            int lastSpriteIndex = -1;
            float elapsed = 0f;

            ApplyVisuals(tablePos, zHeight, rotation);

            // === 메인 물리 루프 ===
            while (elapsed < stopDelay)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // --- Z축 물리 ---
                zVelocity -= _zGravity * dt;
                zHeight += zVelocity * dt;

                if (zHeight <= 0f)
                {
                    zHeight = 0f;

                    if (!hasLanded)
                    {
                        hasLanded = true;
                        float slideSpeed = UnityEngine.Random.Range(_slideSpeedMin, _slideSpeedMax);
                        float slideAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        tableVel = new Vector2(
                            Mathf.Cos(slideAngle) * slideSpeed,
                            Mathf.Sin(slideAngle) * slideSpeed);

                        StartCoroutine(SquashEffect());
                    }

                    zVelocity = Mathf.Abs(zVelocity) * _zBounceCoeff;
                    if (zVelocity < 30f) zVelocity = 0f;
                }

                // --- 테이블 X/Y 물리 ---
                if (hasLanded)
                {
                    tablePos += tableVel * dt;

                    if (tablePos.x < minX) { tablePos.x = minX; tableVel.x = -tableVel.x * _wallBounceCoeff; }
                    if (tablePos.x > maxX) { tablePos.x = maxX; tableVel.x = -tableVel.x * _wallBounceCoeff; }
                    if (tablePos.y < minY) { tablePos.y = minY; tableVel.y = -tableVel.y * _wallBounceCoeff; }
                    if (tablePos.y > maxY) { tablePos.y = maxY; tableVel.y = -tableVel.y * _wallBounceCoeff; }

                    tableVel *= Mathf.Exp(-_slideFriction * dt);

                    rotation += tableVel.magnitude * _rotationMultiplier * dt *
                                Mathf.Sign(tableVel.x + tableVel.y * 0.5f);
                }

                ApplyVisuals(tablePos, zHeight, rotation);

                // ���프라이트
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

            // === 슬롯으로 복귀 ===
            yield return SettleRoutine(tablePos, rotation);

            _rollCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 스��일: 높이가 높을수록 큼(1.0=가까움), 테이블에 착지하면 작아짐(_scaleOnTable=멀어짐).
        /// 앞으로 던지는 느낌: 시작(큼) → 착지(작음).
        /// </summary>
        private void ApplyVisuals(Vector2 tablePos, float zHeight, float rotation)
        {
            if (_diceImageRect == null) return;

            // heightRatio: 1=시작(��음/가까움), 0=착지(테이블/멀어짐)
            float heightRatio = Mathf.Clamp01(zHeight / _dropHeight);
            // ���까울 때(높���) = 1.0, 멀어졌을 때(테이블) = _scaleOnTable(0.65)
            float scale = Mathf.Lerp(_scaleOnTable, 1f, heightRatio);

            _diceImageRect.anchoredPosition = tablePos;
            _diceImageRect.localScale = Vector3.one * scale;
            _diceImageRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        /// <summary>
        /// 착지 스쿼시 효과
        /// </summary>
        private IEnumerator SquashEffect()
        {
            if (_diceImageRect == null) yield break;

            float half = _squashDuration * 0.5f;
            float baseScale = _scaleOnTable;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float sx = baseScale * Mathf.Lerp(1f, _impactSquash, t);
                float sy = baseScale * Mathf.Lerp(1f, 1f / _impactSquash, t);
                _diceImageRect.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float sx = baseScale * Mathf.Lerp(_impactSquash, 1f, t);
                float sy = baseScale * Mathf.Lerp(1f / _impactSquash, 1f, t);
                _diceImageRect.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            _diceImageRect.localScale = Vector3.one * baseScale;
        }

        /// <summary>
        /// DiceArea 중앙에 정사각형 굴림판을 설정.
        /// 가로/세로 중 짧은 쪽 ��준으로 정사각형을 만들어 패널 침범 방지.
        /// 각 슬롯의 위치 ��프셋을 보정하여 모��� 주사위가 같은 판에서 움직임.
        /// </summary>
        private void CalculateSquareBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            if (_diceAreaRect == null || _diceImageRect == null)
            {
                minX = -80f; maxX = 80f;
                minY = -80f; maxY = 80f;
                return;
            }

            Vector2 areaSize = _diceAreaRect.rect.size;

            // 정사각형: 가로/세로 중 짧은 쪽 기준 (패딩 적용)
            float side = Mathf.Min(areaSize.x, areaSize.y) - _tablePadding * 2f;
            float halfSide = side * 0.5f;

            // ��� 슬롯의 DiceArea 내 위치 (오프셋 보정)
            RectTransform slotRect = transform as RectTransform;
            Vector2 slotPos = slotRect.anchoredPosition;

            // DiceImage 반경 (스케일 고려)
            float imgRadius = _diceImageRect.rect.size.x * _scaleOnTable * 0.5f;
            float bound = halfSide - imgRadius;

            // 슬롯 위치를 빼서 DiceImage 로컬 좌표 기준으로 변환
            minX = -bound - slotPos.x;
            maxX =  bound - slotPos.x;
            minY = -bound - slotPos.y;
            maxY =  bound - slotPos.y;
        }

        /// <summary>
        /// 테이블 위 위치에서 슬롯 원점(0,0) + 정상 스케일(1.0)로 복귀
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
