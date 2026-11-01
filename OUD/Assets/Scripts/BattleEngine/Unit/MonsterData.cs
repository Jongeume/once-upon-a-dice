// MonsterData.cs
// 몬스터 종류별 정적 정의. 불변 데이터.
// 실제 전투 중 변동 상태는 MonsterInstance가 담당한다.
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Unit
{
    /// <summary>
    /// 몬스터 1종의 설계 데이터 (불변).
    /// 전투 시작 시 MonsterInstance로 인스턴스화된다.
    /// </summary>
    public class MonsterData
    {
        // ── 식별자 ──────────────────────────────────────────────────────────
        /// <summary>코드 식별자. 예: "Slime", "Skeleton"</summary>
        public string Id   { get; }
        public string Name { get; }

        /// <summary>등급 (Normal/Elite/Boss). UI 카드틀 분기에 사용.</summary>
        public MonsterTier Tier { get; }

        // ── 기본 스탯 ────────────────────────────────────────────────────────
        public int MaxHp       { get; }
        public int BaseAtk     { get; }
        /// <summary>수비 행동 시 획득하는 실드 고정값.</summary>
        public int ShieldValue { get; }

        // ── 강공격 배율 (보스용) ──────────────────────────────────────────────
        /// <summary>
        /// 강공격(StrongAttack) Intent 시 사용할 배율.
        /// 일반 몬스터는 사용하지 않으므로 0.0 또는 1.0으로 설정.
        /// 매직 넘버 금지 — 보스 MonsterData 생성자에서 명시적으로 전달.
        /// </summary>
        public double StrongAttackMultiplier { get; }

        // ── 기본 패턴 ─────────────────────────────────────────────────────────
        /// <summary>고정 순환 패턴. 인덱스 순서대로 반복.</summary>
        public IntentType[] Pattern { get; }

        // ── 분노 패턴 (보스용) ───────────────────────────────────────────────
        public bool        HasRage          { get; }
        /// <summary>HP ≤ 이 값일 때 분노 전환 (game-design-v2.2 §4.4).</summary>
        public int         RageHpThreshold  { get; }
        /// <summary>분노 시 ATK에 영구 추가되는 보너스.</summary>
        public int         RageAtkBonus     { get; }
        /// <summary>분노 전환 후 사용할 패턴.</summary>
        public IntentType[] RagePattern     { get; }

        // ── 생성자 (일반 몬스터) ─────────────────────────────────────────────
        public MonsterData(
            string id,
            string name,
            int maxHp,
            int baseAtk,
            int shieldValue,
            IntentType[] pattern,
            MonsterTier tier = MonsterTier.Normal)
        {
            Id                     = id;
            Name                   = name;
            Tier                   = tier;
            MaxHp                  = maxHp;
            BaseAtk                = baseAtk;
            ShieldValue            = shieldValue;
            StrongAttackMultiplier = 1.0;
            Pattern                = pattern;
            HasRage                = false;
            RageHpThreshold        = 0;
            RageAtkBonus           = 0;
            RagePattern            = System.Array.Empty<IntentType>();
        }

        // ── 생성자 (분노/강공격 보스) ────────────────────────────────────────
        public MonsterData(
            string id,
            string name,
            int maxHp,
            int baseAtk,
            int shieldValue,
            double strongAttackMultiplier,
            IntentType[] pattern,
            int rageHpThreshold,
            int rageAtkBonus,
            IntentType[] ragePattern,
            MonsterTier tier = MonsterTier.Boss)
        {
            Id                     = id;
            Name                   = name;
            Tier                   = tier;
            MaxHp                  = maxHp;
            BaseAtk                = baseAtk;
            ShieldValue            = shieldValue;
            StrongAttackMultiplier = strongAttackMultiplier;
            Pattern                = pattern;
            HasRage                = true;
            RageHpThreshold        = rageHpThreshold;
            RageAtkBonus           = rageAtkBonus;
            RagePattern            = ragePattern;
        }
    }
}
