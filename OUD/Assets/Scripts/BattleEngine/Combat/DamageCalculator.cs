// DamageCalculator.cs
// 데미지/실드 계산 순수 함수 모음.
// 모든 계산은 Math.Floor(내림) 적용. Math.Round 사용 금지.
// game-design-v2.2 §2.2
using System;

namespace OUD.BattleEngine.Combat
{
    /// <summary>
    /// 데미지/실드 수치 계산 및 HP 적용 순수 함수.
    /// 상태를 보유하지 않으므로 static class로 제공한다.
    /// </summary>
    public static class DamageCalculator
    {
        // ── 수치 계산 ─────────────────────────────────────────────────────────

        /// <summary>
        /// floor(stat × multiplier).
        /// 공격 데미지, 실드, 강공격 모두 이 함수를 통한다.
        /// </summary>
        /// <param name="stat">기준 스탯 (ATK or DEF)</param>
        /// <param name="multiplier">스킬 배율 (강화 레벨 반영 후)</param>
        /// <returns>내림 적용된 정수 결과</returns>
        public static int CalcValue(int stat, double multiplier)
            => (int)Math.Floor(stat * multiplier);

        // ── 데미지 적용 ───────────────────────────────────────────────────────

        /// <summary>
        /// 원본 데미지를 실드 → HP 순서로 적용한다.
        /// 참조 파라미터로 실드/HP를 직접 갱신하고, 흡수량과 HP 손실량을 반환한다.
        /// game-design-v2.2 §2.2
        ///   실드 = max(실드 - 원본데미지, 0)
        ///   잔여  = max(원본데미지 - 기존실드, 0)
        ///   HP   -= 잔여
        /// </summary>
        /// <param name="rawDamage">계산된 원본 데미지 (CalcValue 결과)</param>
        /// <param name="targetShield">대상 현재 실드 (ref, 직접 갱신)</param>
        /// <param name="targetHp">대상 현재 HP (ref, 직접 갱신)</param>
        /// <returns>(실드가 흡수한 양, HP에 실제로 들어간 손실량)</returns>
        public static (int shieldAbsorbed, int hpDamage) ApplyDamage(
            int rawDamage, ref int targetShield, ref int targetHp)
        {
            if (rawDamage <= 0)
                return (0, 0);

            // 1. 실드 차감
            int shieldAbsorbed = Math.Min(targetShield, rawDamage);
            targetShield -= shieldAbsorbed;

            // 2. 잔여 데미지 → HP 차감
            int remaining = rawDamage - shieldAbsorbed;
            int hpDamage  = Math.Min(remaining, targetHp); // HP 0 미만 방지
            targetHp     -= hpDamage;

            return (shieldAbsorbed, hpDamage);
        }

        // ── HP 회복 ───────────────────────────────────────────────────────────

        /// <summary>
        /// HP 회복 적용. 최대 HP 초과 불가 (초과분 소멸).
        /// game-design-v2.2 §2.2: min(현재HP + 회복량, 최대HP)
        /// </summary>
        /// <param name="currentHp">현재 HP (ref, 직접 갱신)</param>
        /// <param name="amount">회복량</param>
        /// <param name="maxHp">최대 HP 상한</param>
        /// <returns>실제로 회복된 양</returns>
        public static int ApplyHeal(ref int currentHp, int amount, int maxHp)
        {
            if (amount <= 0) return 0;
            int before   = currentHp;
            currentHp    = Math.Min(currentHp + amount, maxHp);
            return currentHp - before;
        }
    }
}
