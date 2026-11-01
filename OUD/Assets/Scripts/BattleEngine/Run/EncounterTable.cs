// EncounterTable.cs
// MapNode(layer + NodeType)별 적 구성 결정.
// IRandom은 생성자 주입 (Dice/DiceHand/RewardSystem 패턴 일관).
// Phase D-2 (sprint MVP — 노드맵 UI 도입): 1-2-1 구조 + Boss 합류 (StoneGolem 부활).
//   - layer 0 Combat: 시작 — Slime 1~2체
//   - layer 1 Combat: 분기 — Slime/Skeleton 풀 1~2체
//   - layer 2 Boss  : 클라이맥스 — StoneGolem 1체
// feature-spec F-11 (Phase D-2 갱신), game-design-v2.2 §5.1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// MapNode 기반 적 구성 생성기.
    /// 결정 순서: NodeType → Boss는 StoneGolem 1체. Combat은 Layer로 풀 결정.
    /// </summary>
    public class EncounterTable
    {
        // ── Layer별 Combat 풀 / 수량 (sprint MVP 1-2-1 구조) ─────────────────
        private static readonly string[] LAYER0_POOL = { MonsterDatabase.ID_SLIME };
        private static readonly string[] LAYER1_POOL = { MonsterDatabase.ID_SLIME, MonsterDatabase.ID_SKELETON };

        private const int LAYER0_COUNT_MIN = 1;
        private const int LAYER0_COUNT_MAX = 2;   // 양 끝 포함
        private const int LAYER1_COUNT_MIN = 1;
        private const int LAYER1_COUNT_MAX = 2;   // 양 끝 포함

        private const int LAYER0 = 0;
        private const int LAYER1 = 1;

        // ── Boss 구성 ────────────────────────────────────────────────────────
        private const string BOSS_ID = MonsterDatabase.ID_STONE_GOLEM;

        private readonly IRandom _random;

        public EncounterTable(IRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// MapNode에 해당하는 적 MonsterData 목록 생성.
        /// Boss 노드: StoneGolem 1체 고정.
        /// Combat 노드: layer별 풀에서 균등 랜덤, 수량 균등 랜덤.
        /// </summary>
        public List<MonsterData> GenerateEncounter(MapNode node)
        {
            if (node.Type == NodeType.Boss)
                return new List<MonsterData> { MonsterDatabase.Get(BOSS_ID) };

            // Combat
            string[] pool;
            int countMin, countMax;
            switch (node.Layer)
            {
                case LAYER0: pool = LAYER0_POOL; countMin = LAYER0_COUNT_MIN; countMax = LAYER0_COUNT_MAX; break;
                case LAYER1: pool = LAYER1_POOL; countMin = LAYER1_COUNT_MIN; countMax = LAYER1_COUNT_MAX; break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(node),
                        $"Combat 노드는 layer 0 또는 1만 허용. got: layer={node.Layer}, type={node.Type}");
            }

            // IRandom.Next(min, max)는 max exclusive이므로 +1
            int count = _random.Next(countMin, countMax + 1);

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
