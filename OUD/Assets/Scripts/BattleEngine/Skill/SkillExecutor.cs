// SkillExecutor.cs
// 스킬 1개 실행. 데미지/실드/회복 계산 후 대상에 적용.
// 의존: DamageCalculator (계산), MonsterInstance/PlayerState (상태 변경).
// game-design-v2.2 §2.2, feature-spec F-04
using System.Collections.Generic;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Skill
{
    // ── 결과 타입 ─────────────────────────────────────────────────────────────

    /// <summary>스킬 실행 결과. UI 표시 및 로그용.</summary>
    public class SkillResult
    {
        public SkillData            Skill        { get; set; }
        /// <summary>각 적에게 준 히트 정보 (공격 스킬). 수비 스킬은 빈 목록.</summary>
        public List<DamageEntry>    Damages      { get; set; } = new List<DamageEntry>();
        /// <summary>이번 스킬로 플레이어가 획득한 실드.</summary>
        public int                  ShieldGained { get; set; }
        /// <summary>이번 스킬로 플레이어가 회복한 HP.</summary>
        public int                  HpRecovered  { get; set; }
    }

    /// <summary>단일 히트 결과 (대상 하나에 대한 데미지 정보).</summary>
    public struct DamageEntry
    {
        /// <summary>allEnemies 기준 인덱스.</summary>
        public int  TargetIndex;
        /// <summary>계산된 원본 데미지 (floor 적용 후).</summary>
        public int  Damage;
        public int  ShieldAbsorbed;
        public int  HpDamage;
        public bool TargetDied;
    }

    // ── 실행기 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 스킬 실행 로직. 상태를 보유하지 않으므로 매 전투마다 재사용 가능.
    /// 공격/수비를 별도 메서드로 분리해 단일 책임을 유지한다.
    /// </summary>
    public class SkillExecutor
    {
        // ── 공격 스킬 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 공격 스킬 실행. 대상에 데미지를 적용하고 결과를 반환한다.
        /// TargetType별 분기:
        ///   Single (HitCount=1, Multipliers.Length=1) : 단일 히트
        ///   Single (HitCount=2, Multipliers.Length=2) : Twin Slash — 동일 대상 2회 (개별 판정)
        ///   Single (HitCount=1, Multipliers.Length=2) : Crushing Wave — 주 대상 + 전체 스플래시
        ///   AllEnemies                                 : 전체 AoE (사망한 적 건너뜀)
        /// </summary>
        /// <param name="skill">실행할 스킬 데이터</param>
        /// <param name="enhanceLevel">강화 레벨 (0=Lv1)</param>
        /// <param name="atk">공격자 ATK 스탯</param>
        /// <param name="target">주 대상 (AllEnemies 스킬은 null)</param>
        /// <param name="allEnemies">전체 적 목록 (AoE/스플래시 처리용)</param>
        public SkillResult ExecuteAttack(
            SkillData             skill,
            int                   enhanceLevel,
            int                   atk,
            MonsterInstance       target,
            List<MonsterInstance> allEnemies)
        {
            var result = new SkillResult { Skill = skill };

            if (skill.Target == Core.TargetType.AllEnemies)
            {
                // ── AoE: 전체 적 (사망한 적 건너뜀) ─────────────────────────
                double mult = skill.GetMultiplier(0, enhanceLevel);
                for (int i = 0; i < allEnemies.Count; i++)
                {
                    MonsterInstance enemy = allEnemies[i];
                    if (enemy.IsDead) continue;

                    result.Damages.Add(ApplyHitToEnemy(enemy, i, atk, mult));
                }
            }
            else if (skill.HitCount > 1)
            {
                // ── Twin Slash: 동일 대상 HitCount회, 각 히트 독립 판정 ──────
                if (target == null || target.IsDead)
                    return result; // 대상 사망 시 허공 소멸

                int targetIndex = allEnemies.IndexOf(target);
                for (int hit = 0; hit < skill.HitCount; hit++)
                {
                    double mult = skill.GetMultiplier(hit, enhanceLevel);
                    result.Damages.Add(ApplyHitToEnemy(target, targetIndex, atk, mult));
                    // 중간에 사망해도 남은 히트는 허공 소멸하지 않고 계속
                    // (사망한 적의 HP가 0이므로 추가 데미지는 0이 됨 — 규칙상 허공 소멸은 슬롯 단위)
                }
            }
            else if (skill.Multipliers.Length == 2)
            {
                // ── Crushing Wave: 주 대상 + 전체 스플래시 ───────────────────
                if (target == null || target.IsDead)
                    return result; // 주 대상 사망 시 허공 소멸

                int targetIndex = allEnemies.IndexOf(target);

                // 주 대상
                double mainMult = skill.GetMultiplier(0, enhanceLevel);
                result.Damages.Add(ApplyHitToEnemy(target, targetIndex, atk, mainMult));

                // 스플래시: 주 대상 제외 전체 (사망한 적 건너뜀)
                double splashMult = skill.GetMultiplier(1, enhanceLevel);
                for (int i = 0; i < allEnemies.Count; i++)
                {
                    if (i == targetIndex)  continue;
                    if (allEnemies[i].IsDead) continue;
                    result.Damages.Add(ApplyHitToEnemy(allEnemies[i], i, atk, splashMult));
                }
            }
            else
            {
                // ── 단일 히트 ─────────────────────────────────────────────────
                if (target == null || target.IsDead)
                    return result;

                int targetIndex = allEnemies.IndexOf(target);
                double mult = skill.GetMultiplier(0, enhanceLevel);
                result.Damages.Add(ApplyHitToEnemy(target, targetIndex, atk, mult));
            }

            return result;
        }

        // ── 수비 스킬 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 수비 스킬 실행. 플레이어에 실드 및 HP 회복을 적용하고 결과를 반환한다.
        /// 실드는 합산 (AddShield). HP 회복은 최대 HP 초과 불가 (Heal).
        /// </summary>
        public SkillResult ExecuteDefense(
            SkillData   skill,
            int         enhanceLevel,
            int         def,
            PlayerState player)
        {
            var result = new SkillResult { Skill = skill };

            // 실드 계산 및 적용
            double mult        = skill.GetMultiplier(0, enhanceLevel);
            int    shieldGain  = DamageCalculator.CalcValue(def, mult);
            player.AddShield(shieldGain);
            result.ShieldGained = shieldGain;

            // HP 회복 (있는 경우)
            if (skill.HpRecover > 0)
            {
                player.Heal(skill.HpRecover);
                result.HpRecovered = skill.HpRecover;
            }

            return result;
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        /// <summary>단일 히트를 대상에 적용하고 DamageEntry 반환.</summary>
        private static DamageEntry ApplyHitToEnemy(
            MonsterInstance enemy,
            int             targetIndex,
            int             atk,
            double          multiplier)
        {
            int rawDamage = DamageCalculator.CalcValue(atk, multiplier);

            // MonsterInstance.TakeDamage는 내부에서 실드→HP 처리.
            // 결과를 DamageEntry로 재구성하기 위해 수치를 별도 계산.
            int shieldBefore = enemy.Shield;
            int hpBefore     = enemy.Hp;

            enemy.TakeDamage(rawDamage);

            int shieldAbsorbed = shieldBefore - enemy.Shield;
            int hpDamage       = hpBefore     - enemy.Hp;

            return new DamageEntry
            {
                TargetIndex    = targetIndex,
                Damage         = rawDamage,
                ShieldAbsorbed = shieldAbsorbed,
                HpDamage       = hpDamage,
                TargetDied     = enemy.IsDead
            };
        }
    }
}
