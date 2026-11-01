using System.Collections;
using TMPro;
using UnityEngine;

namespace OUD.Unity.Battle.View
{
    /// <summary>데미지/회복 팝업. 위로 떠오르다 페이드아웃.</summary>
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float    _duration  = 1.0f;
        [SerializeField] private float    _riseSpeed = 80f;

        public void Play(Vector3 worldPos, string message, Color color)
        {
            transform.position = worldPos;
            if (_text) { _text.text = message; _text.color = color; }
            gameObject.SetActive(true);
            StartCoroutine(AnimRoutine());
        }

        private IEnumerator AnimRoutine()
        {
            float elapsed = 0f;
            var startColor = _text ? _text.color : Color.white;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                transform.position += Vector3.up * _riseSpeed * Time.deltaTime;
                if (_text)
                {
                    float alpha = 1f - (elapsed / _duration);
                    _text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
