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
            return Instantiate(prefab, _container);
        }

        public Transform Container => _container;
    }
}
