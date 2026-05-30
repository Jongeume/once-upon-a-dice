using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public class EncounterTable
    {
        // Layer 0, 1
        private static readonly string[][] EARLY_PRESETS =
        {
            new[] { MonsterDatabase.ID_SPIDER },
            new[] { MonsterDatabase.ID_SPIDER, MonsterDatabase.ID_SPIDER },
        };

        // Layer 2
        private static readonly string[][] MID_PRESETS =
        {
            new[] { MonsterDatabase.ID_SNAKE },
            new[] { MonsterDatabase.ID_SPIDER, MonsterDatabase.ID_SPIDER, MonsterDatabase.ID_SPIDER },
            new[] { MonsterDatabase.ID_SNAKE, MonsterDatabase.ID_SPIDER },
        };

        // Layer 4, 5
        private static readonly string[][] LATE_PRESETS =
        {
            new[] { MonsterDatabase.ID_BEAR },
            new[] { MonsterDatabase.ID_SNAKE, MonsterDatabase.ID_SNAKE },
            new[] { MonsterDatabase.ID_SNAKE, MonsterDatabase.ID_SPIDER, MonsterDatabase.ID_SPIDER },
        };

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
                    return new List<MonsterData> { MonsterDatabase.Get(MonsterDatabase.ID_EVIL_QUEEN) };

                case NodeType.Elite:
                    return new List<MonsterData> { MonsterDatabase.Get(MonsterDatabase.ID_CROW_KNIGHT) };

                case NodeType.Shop:
                    throw new InvalidOperationException(
                        "Shop 노드에서는 GenerateEncounter를 호출할 수 없습니다.");

                case NodeType.Start:
                    throw new InvalidOperationException(
                        "Start 노드에서는 GenerateEncounter를 호출할 수 없습니다. (전투 없음)");

                case NodeType.Combat:
                    return GenerateCombatEncounter(node.Layer);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(node), $"알 수 없는 NodeType: {node.Type}");
            }
        }

        private List<MonsterData> GenerateCombatEncounter(int layer)
        {
            string[][] presets;
            switch (layer)
            {
                case 0:
                case 1:
                    presets = EARLY_PRESETS;
                    break;
                case 2:
                    presets = MID_PRESETS;
                    break;
                case 4:
                case 5:
                    presets = LATE_PRESETS;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layer),
                        $"Combat 노드에 대한 layer 매핑 없음: layer={layer}");
            }

            string[] preset = presets[_random.Next(0, presets.Length)];
            var result = new List<MonsterData>(preset.Length);
            foreach (string id in preset)
                result.Add(MonsterDatabase.Get(id));
            return result;
        }
    }
}
