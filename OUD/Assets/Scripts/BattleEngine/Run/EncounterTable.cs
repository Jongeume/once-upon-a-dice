// EncounterTable.cs
// 노드 인덱스(0~6)별 적 구성을 결정한다.
// IRandom은 생성자 주입 (Dice/DiceHand/RewardSystem 패턴 일관).
// feature-spec F-11, game-design-v2.2 §5.1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 노드 인덱스별 적 구성 생성.
    /// 구간 (0-based 인덱스):
    ///   0~1: 초반 — Slime 풀, 1~2체
    ///   2~3: 중반 — Slime/Skeleton 풀, 1~2체
    ///   4~5: 후반 — Skeleton/Goblin 풀, 2~3체
    ///   6  : 보스 — StoneGolem × 1
    /// 균등 확률로 풀에서 몬스터를 뽑고 수량을 결정한다.
    /// </summary>
    public class EncounterTable
    {
        // ── 구간별 몬스터 풀 ──────────────────────────────────────────────────
        private static readonly string[] EARLY_POOL = { MonsterDatabase.ID_SLIME };
        private static readonly string[] MID_POOL   = { MonsterDatabase.ID_SLIME, MonsterDatabase.ID_SKELETON };
        private static readonly string[] LATE_POOL  = { MonsterDatabase.ID_SKELETON, MonsterDatabase.ID_GOBLIN };

        // ── 구간 경계 (포함, 0-based) ────────────────────────────────────────
        private const int EARLY_END = 1;   // 0~1
        private const int MID_END   = 3;   // 2~3
        private const int LATE_END  = 5;   // 4~5
        // 6 = 보스 (BOSS_NODE)

        // ── 수량 범위 ────────────────────────────────────────────────────────
        private const int EARLY_COUNT_MIN = 1;
        private const int EARLY_COUNT_MAX = 2;  // 양 끝 포함
        private const int MID_COUNT_MIN   = 1;
        private const int MID_COUNT_MAX   = 2;  // 양 끝 포함
        private const int LATE_COUNT_MIN  = 2;
        private const int LATE_COUNT_MAX  = 3;  // 양 끝 포함

        private readonly IRandom _random;

        public EncounterTable(IRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// nodeIndex에 해당하는 적 MonsterData 목록 생성.
        /// 보스 노드(6)는 StoneGolem 단일.
        /// </summary>
        /// <param name="nodeIndex">0~6 (RunState.CurrentNodeIndex)</param>
        public List<MonsterData> GenerateEncounter(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= RunState.TOTAL_NODES)
                throw new ArgumentOutOfRangeException(
                    nameof(nodeIndex),
                    $"nodeIndex must be in [0, {RunState.TOTAL_NODES - 1}]. got: {nodeIndex}");

            // 보스 노드: StoneGolem × 1
            if (nodeIndex == RunState.BOSS_NODE)
            {
                return new List<MonsterData> { MonsterDatabase.Get(MonsterDatabase.ID_STONE_GOLEM) };
            }

            // 일반 노드: 구간 결정 → 풀/수량 결정 → 균등 추첨
            string[] pool;
            int countMin, countMax;

            if (nodeIndex <= EARLY_END)       { pool = EARLY_POOL; countMin = EARLY_COUNT_MIN; countMax = EARLY_COUNT_MAX; }
            else if (nodeIndex <= MID_END)    { pool = MID_POOL;   countMin = MID_COUNT_MIN;   countMax = MID_COUNT_MAX;   }
            else /* nodeIndex <= LATE_END */  { pool = LATE_POOL;  countMin = LATE_COUNT_MIN;  countMax = LATE_COUNT_MAX;  }

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
