using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public class EncounterTable
    {
        private static readonly string[] POOL_SLIME            = { MonsterDatabase.ID_SLIME };
        private static readonly string[] POOL_SPIDER_SNAKE     = { MonsterDatabase.ID_SPIDER, MonsterDatabase.ID_SNAKE };
        private static readonly string[] POOL_SNAKE_BEAR       = { MonsterDatabase.ID_SNAKE, MonsterDatabase.ID_BEAR };
        private static readonly string[] POOL_SPIDER           = { MonsterDatabase.ID_SPIDER };

        private const int COUNT_MIN = 1;
        private const int COUNT_MAX = 2;

        private readonly IRandom _random;

        public EncounterTable(IRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public List<MonsterData> GenerateEncounter(MapNode node)
        {
            switch (node.Type)
            {
                case NodeType.Boss:
                    return new List<MonsterData> { MonsterDatabase.Get(MonsterDatabase.ID_STONE_GOLEM) };

                case NodeType.Elite:
                    return new List<MonsterData> { MonsterDatabase.Get(MonsterDatabase.ID_ELITE_GOLEM) };

                case NodeType.Shop:
                    throw new InvalidOperationException(
                        "Shop 노드에서는 GenerateEncounter를 호출할 수 없습니다.");

                case NodeType.Combat:
                    return GenerateCombatEncounter(node.Layer);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(node), $"알 수 없는 NodeType: {node.Type}");
            }
        }

        private List<MonsterData> GenerateCombatEncounter(int layer)
        {
            string[] pool;
            switch (layer)
            {
                case 0: pool = POOL_SPIDER;          break;  // 1스테이지: Spider
                case 1: pool = POOL_SPIDER_SNAKE;    break;  // 2스테이지: Spider/Snake
                case 3: pool = POOL_SNAKE_BEAR;      break;  // 3스테이지: Snake/Bear
                case 4: pool = POOL_SNAKE_BEAR;      break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layer),
                        $"Combat 노드에 대한 layer 매핑 없음: layer={layer}");
            }

            int count = _random.Next(COUNT_MIN, COUNT_MAX + 1);
            var result = new List<MonsterData>(count);
            for (int i = 0; i < count; i++)
            {
                int idx = _random.Next(0, pool.Length);
                result.Add(MonsterDatabase.Get(pool[idx]));
            }
            return result;
        }
    }
}
