// EncounterTable.cs
// 노드 인덱스(0~2)별 적 구성을 결정한다.
// IRandom은 생성자 주입 (Dice/DiceHand/RewardSystem 패턴 일관).
// Phase D-1 (sprint MVP 데모): 3전투 압축, 보스 제거.
// feature-spec F-11 (sprint MVP 갱신), game-design-v2.2 §5.1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 노드 인덱스별 적 구성 생성.
    /// sprint MVP 데모 매핑 (0-based):
    ///   0: 초반 — Slime 풀, 1~2체
    ///   1: 중반 — Slime/Skeleton 풀, 1~2체
    ///   2: 후반 — Skeleton/Goblin 풀, 2~3체
    /// 보스(StoneGolem)는 sprint MVP 범위 외 — 노드맵 UI 도입 시 별도 메커니즘으로 추가.
    /// 균등 확률로 풀에서 몬스터를 뽑고 수량을 결정한다.
    /// </summary>
    public class EncounterTable
    {
        // ── 노드별 몬스터 풀 ──────────────────────────────────────────────────
        private static readonly string[] EARLY_POOL = { MonsterDatabase.ID_SLIME };
        private static readonly string[] MID_POOL   = { MonsterDatabase.ID_SLIME, MonsterDatabase.ID_SKELETON };
        private static readonly string[] LATE_POOL  = { MonsterDatabase.ID_SKELETON, MonsterDatabase.ID_GOBLIN };

        // ── 노드 인덱스 매핑 (0-based, sprint MVP 3전투 압축) ────────────────
        private const int EARLY_NODE = 0;
        private const int MID_NODE   = 1;
        private const int LATE_NODE  = 2;

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
        /// nodeIndex(0~2)에 해당하는 적 MonsterData 목록 생성.
        /// </summary>
        public List<MonsterData> GenerateEncounter(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= RunState.TOTAL_NODES)
                throw new ArgumentOutOfRangeException(
                    nameof(nodeIndex),
                    $"nodeIndex must be in [0, {RunState.TOTAL_NODES - 1}]. got: {nodeIndex}");

            string[] pool;
            int countMin, countMax;

            switch (nodeIndex)
            {
                case EARLY_NODE: pool = EARLY_POOL; countMin = EARLY_COUNT_MIN; countMax = EARLY_COUNT_MAX; break;
                case MID_NODE:   pool = MID_POOL;   countMin = MID_COUNT_MIN;   countMax = MID_COUNT_MAX;   break;
                case LATE_NODE:  pool = LATE_POOL;  countMin = LATE_COUNT_MIN;  countMax = LATE_COUNT_MAX;  break;
                default:
                    // TOTAL_NODES 범위 검증을 통과했으므로 여기 도달 불가 — 방어용
                    throw new InvalidOperationException($"unmapped nodeIndex: {nodeIndex}");
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
