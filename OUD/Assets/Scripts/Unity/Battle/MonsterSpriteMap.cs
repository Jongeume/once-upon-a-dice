using System;
using UnityEngine;

namespace OUD.Unity.Battle
{
    [CreateAssetMenu(fileName = "MonsterSpriteMap", menuName = "OUD/MonsterSpriteMap")]
    public class MonsterSpriteMap : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string monsterId;
            public Sprite sprite;
        }

        public Entry[] entries;

        public Sprite GetSprite(string id)
        {
            foreach (var e in entries)
                if (e.monsterId == id) return e.sprite;
            return null;
        }
    }
}
