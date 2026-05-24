using System;
using System.Collections;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [Header("영역 참조")]
        [SerializeField] private RectTransform _rollingAreaRect;
        [SerializeField] private RectTransform _keepSlotRect;

        [Header("애니메이션")]
        [SerializeField] private float _frameRate = 12f;
        [SerializeField] private float _decelerationDuration = 0.3f;

        [Header("Z축 낙하 (탑뷰)")]
        [SerializeField] private float _dropHeight = 250f;
        [SerializeField] private float _zGravity = 2000f;
        [SerializeField] private float _zBounceCoeff = 0.45f;
        [SerializeField] private float _scaleOnTable = 0.65f;
        [SerializeField] private float _impactSquash = 1.15f;
        [SerializeField] private float _squashDuration = 0.06f;

        [Header("테이블 위 미끄러짐")]
        [SerializeField] private float _slideSpeedMin = 150f;
        [SerializeField] private float _slideSpeedMax = 350f;
        [SerializeField] private float _slideFriction = 3f;
        [SerializeField] private float _wallBounceCoeff = 0.5f;
        [SerializeField] private float _rotationMultiplier = 0.4f;
        [SerializeField] private float _tablePadding = 10f;

        [Header("이동 애니메이션")]
        [SerializeField] private float _moveToKeepDuration = 0.2f;
        [SerializeField] private float _moveToRollingDuration = 0.15f;

        [Header("Keep 하이라이트")]
        [SerializeField] private Color   _normalColor = new Color(0.09f, 0.06f, 0.04f);
        [SerializeField] private Color   _keptColor   = new Color(0.83f, 0.63f, 0.09f);
        [SerializeField] private Vector2 _borderThickness = new Vector2(4f, 4f);

        // 5개 주사위의 RollingArea 내 기본 배치 (정규화 좌표 0~1)
        private static readonly Vector2[] SLOT_OFFSETS = new Vector2[]
        {
            new Vector2(0.20f, 0.72f),   // Dice1: 좌상
            new Vector2(0.50f, 0.78f),   // Dice2: 중상
            new Vector2(0.80f, 0.68f),   // Dice3: 우상
            new Vector2(0.30f, 0.28f),   // Dice4: 좌하
            new Vector2(0.70f, 0.32f),   // Dice5: 우하
        };
        private const float POSITION_JITTER = 15f;
        private const float SETTLE_DURATION = 0.12f;

        // KeepSlot 동적 할당 (첫 번째 빈 슬롯부터 채움)
        private static readonly bool[] _slotOccupied = new bool[5];
        private int _assignedSlotIndex = -1;
        private RectTransform[] _allKeepSlots;

        private bool    _kept;
        private Outline _outline;
        private Coroutine _rollCoroutine;
        private Coroutine _moveCoroutine;
        private RectTransform _diceImageRect;

        // 롤링 후 대기 위치 (RollingArea 내, 부모 로컬 좌표)
        private Vector2 _restingPosition;
        private float   _restingRotation;

        // 비활성 상태에서 PlayRoll 호출 시 대기용
        private bool   _pendingRoll;
        private int    _pendingResult;
        private float  _pendingDelay;
        private Action _pendingCallback;

        public event Action OnToggled;

        private void Awake()
        {
            if (_diceImage)
                _diceImageRect = _diceImage.GetComponent<RectTransform>();

            // ── DiceImage에 Canvas + GraphicRaycaster + Button 추가 ──
            // DiceArea 슬롯 배경 위에 렌더링 + RollingArea 위치에서 클릭 가능
            if (_diceImageRect != null)
            {
                var imgCanvas = _diceImageRect.gameObject.GetComponent<Canvas>();
                if (imgCanvas == null) imgCanvas = _diceImageRect.gameObject.AddComponent<Canvas>();
                imgCanvas.overrideSorting = true;
                imgCanvas.sortingOrder = 100;

                if (_diceImageRect.gameObject.GetComponent<GraphicRaycaster>() == null)
                    _diceImageRect.gameObject.AddComponent<GraphicRaycaster>();

                // DiceImage 클릭으로 Keep 토글
                _diceImage.raycastTarget = true;
                var imgButton = _diceImageRect.gameObject.GetComponent<Button>();
                if (imgButton == null) imgButton = _diceImageRect.gameObject.AddComponent<Button>();
                imgButton.transition = Selectable.Transition.None;
                imgButton.onClick.AddListener(() => OnToggled?.Invoke());

                // Keep 하이라이트: DiceImage에 Outline
                _outline = _diceImage.GetComponent<Outline>();
                if (_outline == null) _outline = _diceImage.gameObject.AddComponent<Outline>();
                _outline.effectDistance = _borderThickness;
                _outline.useGraphicAlpha = false;
                var hidden = _keptColor;
                hidden.a = 0f;
                _outline.effectColor = hidden;
            }

            // DiceArea 슬롯 배경 숨기기 (주사위가 RollingArea에 표시되므로 불필요)
            if (_background) _background.enabled = false;

            // KeepSlot 탐색: "KeepSlot" 이름의 자식만 (라벨 등 제외)
            if (_keepSlotRect != null)
            {
                Transform slotsParent = _keepSlotRect.parent;
                var slots = new System.Collections.Generic.List<RectTransform>();
                for (int i = 0; i < slotsParent.childCount; i++)
                {
                    var child = slotsParent.GetChild(i);
                    if (child.name.StartsWith("KeepSlot"))
                        slots.Add(child.GetComponent<RectTransform>());
                }
                slots.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                _allKeepSlots = slots.ToArray();
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

            // Keep 상태 해제 (리롤되는 주사위는 Keep이 아님)
            _kept = false;
            if (_outline) { var c = _keptColor; c.a = 0f; _outline.effectColor = c; }

            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }
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
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            if (_resultSprites != null && value >= 1 && value <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[value - 1];

            // Keep 상태면 KeepSlot에 유지, 아니면 RollingArea에 배치
            if (!_kept)
                PlaceInRollingArea();
        }

        public void SetKept(bool kept)
        {
            if (_kept == kept) return;
            _kept = kept;

            if (_outline)
            {
                var c = _keptColor;
                c.a = kept ? 1f : 0f;
                _outline.effectColor = c;
            }

            if (_moveCoroutine != null && gameObject.activeInHierarchy)
                StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;

            if (kept)
            {
                // 첫 번째 빈 KeepSlot 할당
                if (_allKeepSlots != null)
                {
                    for (int i = 0; i < _allKeepSlots.Length && i < _slotOccupied.Length; i++)
                    {
                        if (!_slotOccupied[i])
                        {
                            _assignedSlotIndex = i;
                            _slotOccupied[i] = true;
                            break;
                        }
                    }
                }
                RectTransform target = (_allKeepSlots != null && _assignedSlotIndex >= 0)
                    ? _allKeepSlots[_assignedSlotIndex]
                    : _keepSlotRect;
                if (gameObject.activeInHierarchy)
                    _moveCoroutine = StartCoroutine(MoveToKeepSlot(target));
            }
            else
            {
                // 슬롯 해제
                if (_assignedSlotIndex >= 0 && _assignedSlotIndex < _slotOccupied.Length)
                {
                    _slotOccupied[_assignedSlotIndex] = false;
                    _assignedSlotIndex = -1;
                }
                // 비활성이면 코루틴 안 함 — 다음 활성화 시 PlaceInRollingArea로 배치됨
                if (gameObject.activeInHierarchy)
                    _moveCoroutine = StartCoroutine(MoveToRollingArea());
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 롤링 루틴
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator RollRoutine(int resultValue, float stopDelay, Action onComplete)
        {
            if (_rollingFrames == null || _rollingFrames.Length == 0)
            {
                SetResultImmediate(resultValue);
                onComplete?.Invoke();
                yield break;
            }

            // === RollingArea 경계 계산 ===
            GetRollingBounds(out float minX, out float maxX, out float minY, out float maxY);

            // === Z축 상태 ===
            float zHeight = _dropHeight + UnityEngine.Random.Range(0f, 80f);
            float zVelocity = -UnityEngine.Random.Range(50f, 150f);
            bool hasLanded = false;

            // === 시작 위치 (주사위별 영역 — 겹침 방지) ===
            int diceIdx = Mathf.Clamp(transform.GetSiblingIndex(), 0, SLOT_OFFSETS.Length - 1);
            Vector2 baseNorm = SLOT_OFFSETS[diceIdx];
            float cx = Mathf.Lerp(minX, maxX, baseNorm.x);
            float cy = Mathf.Lerp(minY, maxY, baseNorm.y);
            float rx = (maxX - minX) * 0.10f;
            float ry = (maxY - minY) * 0.10f;
            Vector2 tablePos = new Vector2(
                cx + UnityEngine.Random.Range(-rx, rx),
                cy + UnityEngine.Random.Range(-ry, ry));
            Vector2 tableVel = Vector2.zero;
            float rotation = UnityEngine.Random.Range(0f, 360f);

            float frameInterval = 1f / _frameRate;
            float frameTimer = 0f;
            int lastSpriteIndex = -1;
            float elapsed = 0f;

            // 스케일 초기화
            ApplyVisuals(tablePos, zHeight, rotation);

            // === 메인 물리 루프 ===
            while (elapsed < stopDelay)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                zVelocity -= _zGravity * dt;
                zHeight += zVelocity * dt;

                if (zHeight <= 0f)
                {
                    zHeight = 0f;
                    if (!hasLanded)
                    {
                        hasLanded = true;
                        float speed = UnityEngine.Random.Range(_slideSpeedMin, _slideSpeedMax);
                        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        tableVel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                        StartCoroutine(SquashEffect());
                    }
                    zVelocity = Mathf.Abs(zVelocity) * _zBounceCoeff;
                    if (zVelocity < 30f) zVelocity = 0f;
                }

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
                    rotation += tableVel.magnitude * _rotationMultiplier * 0.5f * dt * Mathf.Sign(tableVel.x);

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

            // === 그리드 슬롯으로 정착 (겹침 방지) ===
            GetRollingBounds(out float sMinX, out float sMaxX, out float sMinY, out float sMaxY);
            Vector2 gridNorm = SLOT_OFFSETS[diceIdx];
            Vector2 gridPos = new Vector2(
                Mathf.Lerp(sMinX, sMaxX, gridNorm.x) + UnityEngine.Random.Range(-POSITION_JITTER, POSITION_JITTER),
                Mathf.Lerp(sMinY, sMaxY, gridNorm.y) + UnityEngine.Random.Range(-POSITION_JITTER, POSITION_JITTER));

            // 현재 위치에서 그리드 위치로 스무스 이동
            Vector2 settleFrom = tablePos;
            float settleElapsed = 0f;
            while (settleElapsed < SETTLE_DURATION)
            {
                settleElapsed += Time.deltaTime;
                float st = settleElapsed / SETTLE_DURATION;
                float ease = 1f - (1f - st) * (1f - st);
                _diceImageRect.anchoredPosition = Vector2.Lerp(settleFrom, gridPos, ease);
                yield return null;
            }

            _restingPosition = gridPos;
            _restingRotation = rotation;
            _diceImageRect.anchoredPosition = gridPos;
            _diceImageRect.localScale = Vector3.one * _scaleOnTable;

            _rollCoroutine = null;
            onComplete?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 이동 애니메이션
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// RollingArea → KeepSlot 이동 (슬롯 크기에 맞춤)
        /// </summary>
        private IEnumerator MoveToKeepSlot(RectTransform targetSlot)
        {
            if (_diceImageRect == null || targetSlot == null) yield break;

            Vector2 fromPos = _diceImageRect.anchoredPosition;
            Quaternion fromRot = _diceImageRect.localRotation;
            Vector3 fromScale = _diceImageRect.localScale;

            // KeepSlot의 월드 위치를 DiceImage 부모 로컬 좌표로 변환
            Vector3 worldTarget = targetSlot.TransformPoint(Vector3.zero);
            Vector3 localTarget = _diceImageRect.parent.InverseTransformPoint(worldTarget);
            Vector2 toPos = new Vector2(localTarget.x, localTarget.y);

            // KeepSlot에서는 정상 스케일로 복원
            Vector3 toScale = Vector3.one;

            float elapsed = 0f;
            while (elapsed < _moveToKeepDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _moveToKeepDuration;
                float ease = 1f - (1f - t) * (1f - t); // ease-out quad

                _diceImageRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, ease);
                _diceImageRect.localRotation = Quaternion.Slerp(fromRot, Quaternion.identity, ease);
                _diceImageRect.localScale = Vector3.Lerp(fromScale, toScale, ease);
                yield return null;
            }

            _diceImageRect.anchoredPosition = toPos;
            _diceImageRect.localRotation = Quaternion.identity;
            _diceImageRect.localScale = toScale;
            _moveCoroutine = null;
        }

        /// <summary>
        /// KeepSlot → RollingArea 복귀
        /// </summary>
        private IEnumerator MoveToRollingArea()
        {
            if (_diceImageRect == null) yield break;

            Vector2 fromPos = _diceImageRect.anchoredPosition;
            Vector3 fromScale = _diceImageRect.localScale;

            // 대기 위치로 복귀
            Vector2 toPos = _restingPosition;
            Vector3 toScale = Vector3.one * _scaleOnTable;

            float elapsed = 0f;
            while (elapsed < _moveToRollingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _moveToRollingDuration;
                float ease = 1f - (1f - t) * (1f - t);

                _diceImageRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, ease);
                _diceImageRect.localScale = Vector3.Lerp(fromScale, toScale, ease);
                yield return null;
            }

            _diceImageRect.anchoredPosition = toPos;
            _diceImageRect.localScale = toScale;
            _moveCoroutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 유틸리티
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 첫 롤(즉시 표시) 시 RollingArea 내 랜덤 위치에 배치
        /// </summary>
        private void PlaceInRollingArea()
        {
            if (_diceImageRect == null) return;

            GetRollingBounds(out float minX, out float maxX, out float minY, out float maxY);

            // 각 주사위별 고정 슬롯 + 약간의 랜덤 오프셋으로 겹침 방지
            int idx = Mathf.Clamp(transform.GetSiblingIndex(), 0, SLOT_OFFSETS.Length - 1);
            Vector2 norm = SLOT_OFFSETS[idx];

            float baseX = Mathf.Lerp(minX, maxX, norm.x);
            float baseY = Mathf.Lerp(minY, maxY, norm.y);

            _restingPosition = new Vector2(
                baseX + UnityEngine.Random.Range(-POSITION_JITTER, POSITION_JITTER),
                baseY + UnityEngine.Random.Range(-POSITION_JITTER, POSITION_JITTER));
            _restingRotation = UnityEngine.Random.Range(-15f, 15f);

            _diceImageRect.anchoredPosition = _restingPosition;
            _diceImageRect.localRotation = Quaternion.Euler(0f, 0f, _restingRotation);
            _diceImageRect.localScale = Vector3.one * _scaleOnTable;
        }

        private void ApplyVisuals(Vector2 tablePos, float zHeight, float rotation)
        {
            if (_diceImageRect == null) return;

            float heightRatio = Mathf.Clamp01(zHeight / _dropHeight);
            float scale = Mathf.Lerp(_scaleOnTable, 1f, heightRatio);

            _diceImageRect.anchoredPosition = tablePos;
            _diceImageRect.localScale = Vector3.one * scale;
            _diceImageRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

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
        /// RollingArea의 경계를 DiceImage 부모(Dice슬롯) 로컬 좌표로 변환
        /// </summary>
        private void GetRollingBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            if (_rollingAreaRect == null || _diceImageRect == null)
            {
                minX = -100f; maxX = 100f;
                minY = -100f; maxY = 100f;
                return;
            }

            // RollingArea의 네 코너를 DiceImage 부모 로컬 좌표로 변환
            Rect areaRect = _rollingAreaRect.rect;
            Transform parentTransform = _diceImageRect.parent;

            // 주사위 크기를 고려한 동적 패딩 (주사위가 영역 밖으로 나가지 않도록)
            float diceHalf = _diceImageRect.rect.width * _scaleOnTable * 0.5f;
            float padding = Mathf.Max(_tablePadding, diceHalf);

            // RollingArea 로컬 좌표의 min/max 코너
            Vector3 worldMin = _rollingAreaRect.TransformPoint(
                new Vector3(areaRect.xMin + padding, areaRect.yMin + padding, 0f));
            Vector3 worldMax = _rollingAreaRect.TransformPoint(
                new Vector3(areaRect.xMax - padding, areaRect.yMax - padding, 0f));

            // 부모 로컬 좌표로 변환
            Vector3 localMin = parentTransform.InverseTransformPoint(worldMin);
            Vector3 localMax = parentTransform.InverseTransformPoint(worldMax);

            minX = Mathf.Min(localMin.x, localMax.x);
            maxX = Mathf.Max(localMin.x, localMax.x);
            minY = Mathf.Min(localMin.y, localMax.y);
            maxY = Mathf.Max(localMin.y, localMax.y);
        }
    }
}
