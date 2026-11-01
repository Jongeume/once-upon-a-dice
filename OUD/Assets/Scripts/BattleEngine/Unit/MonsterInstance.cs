// MonsterInstance.cs
// 전투 중 몬스터 1체의 변동 상태.
// MonsterData는 불변 설계 데이터, 이 클래스는 전투 내 실시간 상태를 래핑한다.
using System;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Unit
{
    /// <summary>
    /// 전투에 참가한 몬스터 인스턴스.
    /// HP, Shield, Atk, 패턴 인덱스, 분노 여부를 추적한다.
    /// </summary>
    public class MonsterInstance
    {
        public MonsterData Data { get; }

        // ── 변동 상태 ──────────────────────────────────────────────────────────
        public int  Hp        { get; private set; }
        public int  Shield    { get; private set; }
        /// <summary>현재 실효 ATK. 분노 시 RageAtkBonus 누적.</summary>
        public int  Atk       { get; private set; }
        public bool IsEnraged { get; private set; }
        public bool IsDead    => Hp <= 0;

        private int _patternIndex;

        // ── 생성자 ────────────────────────────────────────────────────────────
        public MonsterInstance(MonsterData data)
        {
            Data          = data;
            Hp            = data.MaxHp;
            Shield        = 0;
            Atk           = data.BaseAtk;
            IsEnraged     = false;
            _patternIndex = 0;
        }

        // ── Intent / 패턴 ─────────────────────────────────────────────────────

        /// <summary>
        /// 현재 패턴 인덱스에 해당하는 Intent 반환.
        /// 분노 시 RagePattern, 평시 Pattern 사용.
        /// </summary>
        public IntentType GetCurrentIntent()
        {
            IntentType[] pattern = IsEnraged ? Data.RagePattern : Data.Pattern;
            return pattern[_patternIndex % pattern.Length];
        }

        /// <summary>
        /// 현재 Intent의 수치값 반환.
        /// Attack / StrongAttack → ATK 기반, Shield → ShieldValue.
        /// 강공격 배율 계산은 DamageCalculator.CalcValue 사용 (floor).
        /// </summary>
        public int GetIntentValue()
        {
            IntentType intent = GetCurrentIntent();
            return intent switch
            {
                IntentType.Attack       => Atk,
                IntentType.StrongAttack => (int)Math.Floor(Atk * Data.StrongAttackMultiplier),
                IntentType.Shield       => Data.ShieldValue,
                IntentType.RageWarning  => Atk, // 경고용, 실제 행동은 Attack
                _ => 0
            };
        }

        /// <summary>행동 실행 후 패턴 인덱스를 한 칸 전진.</summary>
        public void AdvancePattern()
        {
            IntentType[] pattern = IsEnraged ? Data.RagePattern : Data.Pattern;
            _patternIndex = (_patternIndex + 1) % pattern.Length;
        }

        /// <summary>
        /// 적 턴 시작 시 분노 조건 체크.
        /// HP ≤ RageHpThreshold 이면 ATK 영구 버프 + 패턴 리셋.
        /// 이미 분노한 경우 재발동 없음.
        /// game-design-v2.2 §4.4
        /// </summary>
        public void CheckRage()
        {
            if (!Data.HasRage || IsEnraged) return;
            if (Hp <= Data.RageHpThreshold)
            {
                IsEnraged     = true;
                Atk          += Data.RageAtkBonus;
                _patternIndex = 0; // 분노 패턴 처음부터
            }
        }

        // ── HP / 실드 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 데미지 수신. 실드 먼저 차감 후 잔여 HP 차감.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;

            int shieldAbsorbed = Math.Min(Shield, damage);
            Shield -= shieldAbsorbed;
            int remaining = damage - shieldAbsorbed;

            Hp -= remaining;
            if (Hp < 0) Hp = 0;
        }

        /// <summary>플레이어 턴 종료 후 적 실드 초기화.</summary>
        public void ResetShield() => Shield = 0;

        /// <summary>수비 Intent 실행 시 실드 획득.</summary>
        public void GainShield(int amount)
        {
            if (amount <= 0) return;
            Shield += amount;
        }
    }
}
