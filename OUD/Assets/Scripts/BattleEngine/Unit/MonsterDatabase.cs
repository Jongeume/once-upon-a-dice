// MonsterDatabase.cs
// 몬스터 종류별 정적 데이터 + 인스턴스 팩토리 제공.
// 수치 출처: feature-spec-sprint-mvp.md F-06, F-07
// [가정 기반] 수치: B-1~B-2 (Sprint-1 밸런스 시뮬에서 검증 예정)
using System.Collections.Generic;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Unit
{
    /// <summary>
    /// 전체 몬스터 데이터 보유. 정적 조회 + 인스턴스 생성 팩토리 제공.
    /// </summary>
    public static class MonsterDatabase
    {
        // ── 몬스터 ID 상수 ────────────────────────────────────────────────────
        public const string ID_SLIME       = "Slime";
        public const string ID_SKELETON    = "Skeleton";
        public const string ID_GOBLIN      = "Goblin";
        public const string ID_STONE_GOLEM = "StoneGolem";

        // ── 내부 테이블 ───────────────────────────────────────────────────────
        private static readonly Dictionary<string, MonsterData> _table;

        static MonsterDatabase()
        {
            _table = new Dictionary<string, MonsterData>
            {
                [ID_SLIME]       = BuildSlime(),
                [ID_SKELETON]    = BuildSkeleton(),
                [ID_GOBLIN]      = BuildGoblin(),
                [ID_STONE_GOLEM] = BuildStoneGolem(),
            };
        }

        // ── 공개 조회 ─────────────────────────────────────────────────────────

        /// <summary>ID로 MonsterData 조회. 없으면 null.</summary>
        public static MonsterData Get(string id)
        {
            _table.TryGetValue(id, out MonsterData data);
            return data;
        }

        /// <summary>ID로 MonsterInstance 즉시 생성.</summary>
        public static MonsterInstance Create(string id)
        {
            MonsterData data = Get(id);
            if (data == null)
                throw new System.ArgumentException($"알 수 없는 몬스터 ID: {id}");
            return new MonsterInstance(data);
        }

        // ── Sprint-0 인카운터 팩토리 ─────────────────────────────────────────

        /// <summary>
        /// Sprint-0 기준 1스테이지 인카운터 생성.
        /// 구성: Slime × 2 + Skeleton × 1 (mvp-sprint0-next-week.md §1.1 S0-8)
        /// </summary>
        public static List<MonsterInstance> CreateSprint0Encounter()
        {
            return new List<MonsterInstance>
            {
                Create(ID_SLIME),
                Create(ID_SLIME),
                Create(ID_SKELETON),
            };
        }

        // ── 몬스터 정의 ───────────────────────────────────────────────────────

        /// <summary>
        /// Slime: HP=20, ATK=8, 실드=0.
        /// 패턴: 공격 → 공격 → 공격 (순환).
        /// feature-spec F-06 [가정 기반] B-2
        /// </summary>
        private static MonsterData BuildSlime() => new MonsterData(
            id:          ID_SLIME,
            name:        "Slime",
            maxHp:       20,
            baseAtk:     8,
            shieldValue: 0,
            pattern:     new[]
            {
                IntentType.Attack,
                IntentType.Attack,
                IntentType.Attack,
            });

        /// <summary>
        /// Skeleton: HP=25, ATK=10, 실드=5.
        /// 패턴: 공격 → 수비 → 공격 (순환).
        /// feature-spec F-06 [가정 기반] B-2
        /// </summary>
        private static MonsterData BuildSkeleton() => new MonsterData(
            id:          ID_SKELETON,
            name:        "Skeleton",
            maxHp:       25,
            baseAtk:     10,
            shieldValue: 5,
            pattern:     new[]
            {
                IntentType.Attack,
                IntentType.Shield,
                IntentType.Attack,
            });

        /// <summary>
        /// Goblin: HP=15, ATK=12, 실드=0.
        /// 패턴: 공격 → 공격 → 공격 (순환).
        /// feature-spec F-06 [가정 기반] B-2
        /// </summary>
        private static MonsterData BuildGoblin() => new MonsterData(
            id:          ID_GOBLIN,
            name:        "Goblin",
            maxHp:       15,
            baseAtk:     12,
            shieldValue: 0,
            pattern:     new[]
            {
                IntentType.Attack,
                IntentType.Attack,
                IntentType.Attack,
            });

        /// <summary>
        /// Stone Golem (보스): HP=80, ATK=14, 실드=10, 강공격배율=1.5x.
        /// 기본 패턴: 공격(14) → 실드(10) → 공격(14) → 강공격(21) → 반복.
        /// 분노 전환: HP ≤ 40 → ATK+4(=18), 분노 패턴 적용.
        /// 분노 패턴: 공격(18) → 강공격(27) → 공격(18) → 반복.
        /// feature-spec F-07
        /// </summary>
        private static MonsterData BuildStoneGolem() => new MonsterData(
            id:                     ID_STONE_GOLEM,
            name:                   "Stone Golem",
            maxHp:                  80,
            baseAtk:                14,
            shieldValue:            10,
            strongAttackMultiplier: 1.5,
            pattern: new[]
            {
                IntentType.Attack,
                IntentType.Shield,
                IntentType.Attack,
                IntentType.StrongAttack,
            },
            rageHpThreshold: 40,   // HP ≤ 40 (MaxHp 80의 50%) 시 분노 전환
            rageAtkBonus:    4,    // ATK 14 → 18
            ragePattern: new[]
            {
                IntentType.Attack,
                IntentType.StrongAttack,
                IntentType.Attack,
            });
    }
}
