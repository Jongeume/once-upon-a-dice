using OUD.BattleEngine.Unit;
using OUD.Unity.Common;
using UnityEngine;

namespace OUD.Unity.Battle.View
{
    /// <summary>EnemyEntryView 프리팹들의 부모 컨테이너. 등급별 프리팹 분기.</summary>
    public class EnemyView : ViewBase
    {
        [SerializeField] private Transform        _container;
        [SerializeField] private EnemyEntryView   _entryPrefab;       // Normal
        [SerializeField] private EnemyEntryView   _elitePrefab;       // Elite (null이면 Normal 대체)
        [SerializeField] private EnemyEntryView   _bossPrefab;        // Boss  (null이면 Normal 대체)

        public EnemyEntryView SpawnEntry(MonsterTier tier = MonsterTier.Normal)
        {
            EnemyEntryView prefab = tier switch
            {
                MonsterTier.Boss  => _bossPrefab  != null ? _bossPrefab  : _entryPrefab,
                MonsterTier.Elite => _elitePrefab != null ? _elitePrefab : _entryPrefab,
                _                 => _entryPrefab,
            };
            EnemyEntryView entry = Instantiate(prefab, _container);
            entry.gameObject.SetActive(true);
            // 보스는 일반 몬스터 대비 카드 크기가 커서 pivot.y=1(상단 기준)로 맞춰야 베이스라인이 정렬됨
            if (tier == MonsterTier.Boss)
            {
                RectTransform rt = entry.transform as RectTransform;
                if (rt != null)
                {
                    Vector2 pivot = rt.pivot;
                    pivot.y = 1f;
                    rt.pivot = pivot;
                }
            }
            return entry;
        }

        public Transform Container => _container;
    }
}
