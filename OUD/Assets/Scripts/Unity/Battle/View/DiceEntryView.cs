using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float _scaleOnTable = 0.5f;
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

        // ── 주사위 간 충돌 공유 상태 ──
        private static readonly Vector2[] s_positions = new Vector2[5];
        private static readonly bool[] s_active = new bool[5];
        private const float DICE_COLLISION_RADIUS = 25f;
        private const float COLLISION_REPULSION   = 600f;
        private const float KEEP_SCALE            = 0.75f;

        // KeepSlot 동적 할당 (첫 번째 빈 슬롯부터 채움)
        private static readonly bool[] _slotOccupied = new bool[5];
        private int _assignedSlotIndex = -1;
        private RectTransform[] _allKeepSlots;

        private bool    _kept;
        private Outline _outline;
        private Coroutine _rollCoroutine;
        private Coroutine _moveCoroutine;
        private RectTransform _diceImageRect;
        private int _diceIdx;

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

            _diceIdx = Mathf.Clamp(transform.GetSiblingIndex(), 0, 4);

            // ── DiceImage에 Canvas + GraphicRaycaster + Button 추가 ──
            if (_diceImageRect != null)
            {
                var imgCanvas = _diceImageRect.gameObject.GetComponent<Canvas>();
                if (imgCanvas == null) imgCanvas = _diceImageRect.gameObject.AddComponent<Canvas>();
                imgCanvas.overrideSorting = true;
                imgCanvas.sortingOrder = 100;

                if (_diceImageRect.gameObject.GetComponent<GraphicRaycaster>() == null)
                    _diceImageRect.gameObject.AddComponent<GraphicRaycaster>();

                _diceImage.raycastTarget = true;
                var imgButton = _diceImageRect.gameObject.GetComponent<Button>();
                if (imgButton == null) imgButton = _diceImageRect.gameObject.AddComponent<Button>();
                imgButton.transition = Selectable.Transition.None;
                imgButton.onClick.AddListener(() => OnToggled?.Invoke());

                _outline = _diceImage.GetComponent<Outline>();
                if (_outline == null) _outline = _diceImage.gameObject.AddComponent<Outline>();
                _outline.effectDistance = _borderThickness;
                _outline.useGraphicAlpha = false;
                var hidden = _keptColor;
                hidden.a = 0f;
                _outline.effectColor = hidden;
            }

            if (_background) _background.enabled = false;

            // KeepSlot 탐색
            if (_keepSlotRect != null)
            {
                Transform slotsParent = _keepSlotRect.parent;
                var slots = new List<RectTransform>();
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

        // ─────────────────────────────────────────────────────────────────────
        // 퍼블릭 인터페이스
        // ─────────────────────────────────────────────────────────────────────

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

            _kept = false;
            if (_outline) { var c = _keptColor; c.a = 0f; _outline.effectColor = c; }

            // KeepSlot 해제 (첫 롤에서 배치된 슬롯)
            if (_assignedSlotIndex >= 0 && _assignedSlotIndex < _slotOccupied.Length)
            {
                _slotOccupied[_assignedSlotIndex] = false;
                _assignedSlotIndex = -1;
            }

            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }
            if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
            _rollCoroutine = StartCoroutine(RollRoutine(resultValue, stopDelay, onComplete));
        }

        public void SetResultImmediate(int value)
        {
            if (_rollCoroutine != null) { StopCoroutine(_rollCoroutine); _rollCoroutine = null; }
            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }

            if (_resultSprites != null && value >= 1 && value <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[value - 1];

            // Keep 상태면 KeepSlot에 유지, 아니면 KeepSlot에 배치 (첫 롤)
            if (!_kept)
                PlaceInKeepSlot();
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
                if (_assignedSlotIndex >= 0 && _assignedSlotIndex < _slotOccupied.Length)
                {
                    _slotOccupied[_assignedSlotIndex] = false;
                    _assignedSlotIndex = -1;
                }
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

            // === RollingArea 경계 ===
            GetRollingBounds(out float minX, out float maxX, out float minY, out float maxY);

            // === Z축 상태 ===
            float zHeight = _dropHeight + UnityEngine.Random.Range(0f, 80f);
            float zVelocity = -UnityEngine.Random.Range(50f, 150f);
            bool hasLanded = false;

            // === 시작 위치: RollingArea 중앙 (약간 퍼짐) ===
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float spreadAngle = _diceIdx * (360f / 5f) * Mathf.Deg2Rad;
            float spreadRadius = 12f;
            Vector2 tablePos = new Vector2(
                centerX + Mathf.Cos(spreadAngle) * spreadRadius,
                centerY + Mathf.Sin(spreadAngle) * spreadRadius);
            Vector2 tableVel = Vector2.zero;
            float rotation = UnityEngine.Random.Range(0f, 360f);

            // 충돌 시스템에 등록
            s_positions[_diceIdx] = tablePos;
            s_active[_diceIdx] = true;

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

                // Z축 물리
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
                    // 주사위 간 충돌
                    ApplyDiceCollision(ref tablePos, ref tableVel, dt);

                    // 이동 + 벽 바운스
                    tablePos += tableVel * dt;
                    ClampToBounds(ref tablePos, ref tableVel, minX, maxX, minY, maxY, _wallBounceCoeff);

                    // 마찰 + 회전
                    tableVel *= Mathf.Exp(-_slideFriction * dt);
                    rotation += tableVel.magnitude * _rotationMultiplier * dt *
                                Mathf.Sign(tableVel.x + tableVel.y * 0.5f);
                }

                // 공유 위치 갱신
                s_positions[_diceIdx] = tablePos;

                ApplyVisuals(tablePos, zHeight, rotation);

                // 스프라이트 순환
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

                    ApplyDiceCollision(ref tablePos, ref tableVel, dt);

                    tablePos += tableVel * dt;
                    ClampToBounds(ref tablePos, ref tableVel, minX, maxX, minY, maxY, 0.3f);
                    tableVel *= Mathf.Exp(-_slideFriction * dampMult * dt);
                    rotation += tableVel.magnitude * _rotationMultiplier * 0.5f * dt * Mathf.Sign(tableVel.x);

                    s_positions[_diceIdx] = tablePos;
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

            // === 정지 후 겹침 해소 ===
            s_active[_diceIdx] = false;
            ResolveOverlap(ref tablePos, minX, maxX, minY, maxY);
            s_positions[_diceIdx] = tablePos;

            // === 최종 위치에서 대기 ===
            _restingPosition = tablePos;
            _restingRotation = rotation;
            _diceImageRect.anchoredPosition = tablePos;
            _diceImageRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            _diceImageRect.localScale = Vector3.one * _scaleOnTable;

            _rollCoroutine = null;
            onComplete?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 충돌 시스템
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>실시간 충돌: 다른 주사위와 반발력 적용</summary>
        private void ApplyDiceCollision(ref Vector2 pos, ref Vector2 vel, float dt)
        {
            float minDist = DICE_COLLISION_RADIUS * 2f;
            for (int other = 0; other < 5; other++)
            {
                if (other == _diceIdx) continue;
                if (!s_active[other]) continue;

                Vector2 delta = pos - s_positions[other];
                float dist = delta.magnitude;
                if (dist < minDist && dist > 0.01f)
                {
                    Vector2 dir = delta / dist;
                    vel += dir * COLLISION_REPULSION * dt;
                    pos += dir * (minDist - dist) * 0.3f;
                }
            }
        }

        /// <summary>정지 후 겹침 해소 (반복 밀어내기)</summary>
        private void ResolveOverlap(ref Vector2 pos, float minX, float maxX, float minY, float maxY)
        {
            float minDist = DICE_COLLISION_RADIUS * 2f;
            for (int iter = 0; iter < 15; iter++)
            {
                bool moved = false;
                for (int other = 0; other < 5; other++)
                {
                    if (other == _diceIdx) continue;
                    Vector2 delta = pos - s_positions[other];
                    float dist = delta.magnitude;
                    if (dist < minDist && dist > 0.01f)
                    {
                        Vector2 dir = delta / dist;
                        pos += dir * (minDist - dist) * 0.6f;
                        moved = true;
                    }
                    else if (dist <= 0.01f)
                    {
                        // 완전 겹침 — 랜덤 방향으로 밀기
                        float a = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        pos += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * minDist;
                        moved = true;
                    }
                }
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
                if (!moved) break;
            }
        }

        /// <summary>벽 바운스 + 클램핑</summary>
        private static void ClampToBounds(ref Vector2 pos, ref Vector2 vel,
            float minX, float maxX, float minY, float maxY, float bounce)
        {
            if (pos.x < minX) { pos.x = minX; vel.x = -vel.x * bounce; }
            if (pos.x > maxX) { pos.x = maxX; vel.x = -vel.x * bounce; }
            if (pos.y < minY) { pos.y = minY; vel.y = -vel.y * bounce; }
            if (pos.y > maxY) { pos.y = maxY; vel.y = -vel.y * bounce; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 이동 애니메이션
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator MoveToKeepSlot(RectTransform targetSlot)
        {
            if (_diceImageRect == null || targetSlot == null) yield break;

            Vector2 fromPos = _diceImageRect.anchoredPosition;
            Quaternion fromRot = _diceImageRect.localRotation;
            Vector3 fromScale = _diceImageRect.localScale;

            Vector3 worldTarget = targetSlot.TransformPoint(Vector3.zero);
            Vector3 localTarget = _diceImageRect.parent.InverseTransformPoint(worldTarget);
            Vector2 toPos = new Vector2(localTarget.x, localTarget.y);
            Vector3 toScale = Vector3.one * KEEP_SCALE;

            float elapsed = 0f;
            while (elapsed < _moveToKeepDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _moveToKeepDuration;
                float ease = 1f - (1f - t) * (1f - t);

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

        private IEnumerator MoveToRollingArea()
        {
            if (_diceImageRect == null) yield break;

            Vector2 fromPos = _diceImageRect.anchoredPosition;
            Vector3 fromScale = _diceImageRect.localScale;
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

        /// <summary>첫 롤: KeepSlot에 배치</summary>
        private void PlaceInKeepSlot()
        {
            if (_diceImageRect == null) return;

            RectTransform target = _keepSlotRect;
            if (_allKeepSlots != null && _diceIdx < _allKeepSlots.Length)
                target = _allKeepSlots[_diceIdx];

            if (target != null)
            {
                Vector3 worldTarget = target.TransformPoint(Vector3.zero);
                Vector3 localTarget = _diceImageRect.parent.InverseTransformPoint(worldTarget);
                _restingPosition = new Vector2(localTarget.x, localTarget.y);
            }

            _restingRotation = 0f;
            _diceImageRect.anchoredPosition = _restingPosition;
            _diceImageRect.localRotation = Quaternion.identity;
            _diceImageRect.localScale = Vector3.one * KEEP_SCALE;

            s_positions[_diceIdx] = _restingPosition;
            s_active[_diceIdx] = false;
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

        private void GetRollingBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            if (_rollingAreaRect == null || _diceImageRect == null)
            {
                minX = -100f; maxX = 100f;
                minY = -100f; maxY = 100f;
                return;
            }

            Rect areaRect = _rollingAreaRect.rect;
            Transform parentTransform = _diceImageRect.parent;

            float diceHalf = _diceImageRect.rect.width * _scaleOnTable * 0.5f;
            float padding = Mathf.Max(_tablePadding, diceHalf);

            Vector3 worldMin = _rollingAreaRect.TransformPoint(
                new Vector3(areaRect.xMin + padding, areaRect.yMin + padding, 0f));
            Vector3 worldMax = _rollingAreaRect.TransformPoint(
                new Vector3(areaRect.xMax - padding, areaRect.yMax - padding, 0f));

            Vector3 localMin = parentTransform.InverseTransformPoint(worldMin);
            Vector3 localMax = parentTransform.InverseTransformPoint(worldMax);

            minX = Mathf.Min(localMin.x, localMax.x);
            maxX = Mathf.Max(localMin.x, localMax.x);
            minY = Mathf.Min(localMin.y, localMax.y);
            maxY = Mathf.Max(localMin.y, localMax.y);
        }
    }
}
