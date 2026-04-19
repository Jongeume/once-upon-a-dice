using OUD.Unity.Common;
using UnityEngine;

namespace OUD.Unity.Battle.View
{
    /// <summary>EnemyEntryView 프리팹들의 부모 컨테이너.</summary>
    public class EnemyView : ViewBase
    {
        [SerializeField] private Transform        _container;
        [SerializeField] private EnemyEntryView   _entryPrefab;

        public EnemyEntryView SpawnEntry()
        {
            return Instantiate(_entryPrefab, _container);
        }

        public Transform Container => _container;
    }
}
