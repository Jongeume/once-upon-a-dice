using System;
using UnityEngine;

namespace OUD.Unity.Battle
{
    /// <summary>
    /// 몬스터 id → 피격 이펙트 프레임 시퀀스(+틴트) 매핑. MonsterSpriteMap 패턴을 따른다.
    /// 적 공격 적중 시 BattleUIAdapter가 공격자 id로 조회한다.
    /// 전용 아트가 없는 몬스터는 폴백 프레임을 사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "HitEffectLibrary", menuName = "OUD/HitEffectLibrary")]
    public class HitEffectLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("MonsterDatabase.ID_* 와 일치하는 몬스터 식별자.")]
            public string monsterId;

            [Tooltip("12프레임 라인아트 (가로 1열 슬라이스 결과).")]
            public Sprite[] frames;

            [Tooltip("단색 라인아트 틴트. alpha==0이면 미설정 → 흰색(무틴트)으로 취급.")]
            public Color tint;
        }

        [Tooltip("몬스터 id별 피격 이펙트 프레임. 전용 아트가 없는 몬스터는 폴백을 사용한다.")]
        [SerializeField] private Entry[] _entries;

        [Tooltip("매칭되는 entry가 없을 때 사용할 폴백 프레임 (예: Bite).")]
        [SerializeField] private Sprite[] _fallbackFrames;

        [Tooltip("폴백 사용 시 틴트. alpha==0이면 흰색.")]
        [SerializeField] private Color _fallbackTint = Color.white;

        /// <summary>
        /// 몬스터 id로 프레임/틴트를 조회한다. 전용 entry가 없으면 폴백을 반환한다.
        /// 재생 가능한 프레임이 하나도 없으면 false.
        /// </summary>
        public bool TryGet(string monsterId, out Sprite[] frames, out Color tint)
        {
            if (_entries != null)
            {
                foreach (var e in _entries)
                {
                    if (e.monsterId == monsterId && e.frames != null && e.frames.Length > 0)
                    {
                        frames = e.frames;
                        tint   = Normalize(e.tint);
                        return true;
                    }
                }
            }

            if (_fallbackFrames != null && _fallbackFrames.Length > 0)
            {
                frames = _fallbackFrames;
                tint   = Normalize(_fallbackTint);
                return true;
            }

            frames = null;
            tint   = Color.white;
            return false;
        }

        // alpha==0(미설정 struct 기본값)은 무틴트로 간주 → 흰색.
        private static Color Normalize(Color c) => c.a <= 0f ? Color.white : c;
    }
}
