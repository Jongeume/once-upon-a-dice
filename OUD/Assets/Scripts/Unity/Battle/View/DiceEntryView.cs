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

        [Header("Keep 하이라이트")]
        [SerializeField] private Color   _normalColor = new Color(0.09f, 0.06f, 0.04f);
        [SerializeField] private Color   _keptColor   = new Color(0.83f, 0.63f, 0.09f);
        [SerializeField] private Vector2 _borderThickness = new Vector2(4f, 4f);

        private bool    _kept;
        private Outline _outline;
        private Coroutine _rollCoroutine;

        public event Action OnToggled;

        private void Awake()
        {
            if (_button) _button.onClick.AddListener(() => OnToggled?.Invoke());

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

        public void PlayRoll(int resultValue, float stopDelay, Action onComplete)
        {
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

            float elapsed = 0f;
            float frameInterval = 1f / _frameRate;
            float timer = 0f;
            int lastIndex = -1;

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
                elapsed += Time.deltaTime;
                yield return null;
            }

            float[] decelFps = { _frameRate * 0.66f, _frameRate * 0.33f };
            float stepDuration = _decelerationDuration / decelFps.Length;

            for (int step = 0; step < decelFps.Length; step++)
            {
                float stepElapsed = 0f;
                float interval = 1f / decelFps[step];
                float stepTimer = 0f;
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
                    stepElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (_resultSprites != null && resultValue >= 1 && resultValue <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[resultValue - 1];

            _rollCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
