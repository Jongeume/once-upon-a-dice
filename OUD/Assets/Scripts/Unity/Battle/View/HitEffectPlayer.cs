using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 피격 이펙트 1회 재생기. 프레임을 순차 교체하고 마지막 프레임 후 자기 GameObject를 파괴한다.
    /// 적 공격 적중 시 BattleUIAdapter가 인스턴스화 → Play 호출. Loop 없음.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HitEffectPlayer : MonoBehaviour
    {
        [SerializeField] private Image _image;

        [Tooltip("초당 프레임 수. 12프레임 기준 20fps면 0.6초 — ACTION_DELAY와 맞물린다.")]
        [SerializeField] private float _fps = DEFAULT_FPS;

        private const float DEFAULT_FPS = 20f;

        /// <summary>
        /// 프레임 시퀀스를 1회 재생. tint로 단색 라인아트에 색을 입힌다.
        /// 완료(또는 프레임이 없을 때) 시 GameObject를 파괴한다.
        /// </summary>
        public void Play(Sprite[] frames, Color tint)
        {
            if (_image == null || frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            _image.color         = tint;
            _image.raycastTarget = false;   // 입력 차단 방지
            _image.sprite        = frames[0];
            StartCoroutine(PlayRoutine(frames));
        }

        private IEnumerator PlayRoutine(Sprite[] frames)
        {
            float fps = _fps > 0f ? _fps : DEFAULT_FPS;
            var wait = new WaitForSeconds(1f / fps);

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) _image.sprite = frames[i];
                yield return wait;
            }

            Destroy(gameObject);
        }
    }
}
