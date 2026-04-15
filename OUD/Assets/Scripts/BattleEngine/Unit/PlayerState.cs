// PlayerState.cs
// 전투 중 플레이어의 변동 상태 관리.
// 기본 스탯(PlayerStats)은 불변, 이 클래스는 HP/Shield 등 변동값을 래핑한다.
using System.Collections.Generic;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Unit
{
    /// <summary>
    /// 전투 및 런 진행 중 플레이어의 가변 상태.
    /// 모든 HP/실드 변경은 이 클래스의 메서드를 통해서만 수행한다.
    /// </summary>
    public class PlayerState
    {
        // ── 기본 스탯 (레벨업 시 교체) ────────────────────────────────────────
        public PlayerStats BaseStats { get; private set; }

        // ── 변동 상태 ──────────────────────────────────────────────────────────
        public int Hp     { get; private set; }
        public int Shield { get; private set; }
        public int Level  { get; private set; }   // 0 = Lv1 (시작), 최대 3
        public int Xp     { get; private set; }
        public int Gold   { get; private set; }
        public int Sp     { get; private set; }

        // ── 해금/강화 상태 ────────────────────────────────────────────────────
        /// <summary>해금된 족보. 기본 4종은 시작 시 추가됨.</summary>
        public HashSet<HandType> UnlockedHands   { get; }
        /// <summary>족보별 강화 레벨. 0=Lv1(미강화), 1=Lv2, 2=Lv3.</summary>
        public Dictionary<HandType, int> EnhanceLevels { get; }

        // ── 편의 프로퍼티 ──────────────────────────────────────────────────────
        public int  Atk    => BaseStats.Atk;
        public int  Def    => BaseStats.Def;
        public int  MaxHp  => BaseStats.MaxHp;
        public bool IsAlive => Hp > 0;

        // ── 생성자 ────────────────────────────────────────────────────────────
        /// <param name="stats">초기 기본 스탯 (HP60/ATK6/DEF5 등)</param>
        public PlayerState(PlayerStats stats)
        {
            BaseStats      = stats;
            Hp             = stats.MaxHp;
            Shield         = 0;
            Level          = 0;
            Xp             = 0;
            Gold           = 0;
            Sp             = 0;
            UnlockedHands  = new HashSet<HandType>();
            EnhanceLevels  = new Dictionary<HandType, int>();

            // 기본 4종 족보 해금 (OnePair, TwoPair, Triple, FullHouse)
            UnlockedHands.Add(HandType.OnePair);
            UnlockedHands.Add(HandType.TwoPair);
            UnlockedHands.Add(HandType.Triple);
            UnlockedHands.Add(HandType.FullHouse);
        }

        // ── HP / 실드 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 데미지 수신. 실드 먼저 차감 후 잔여 HP 차감.
        /// game-design-v2.2 §2.2
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;

            // 1. 실드 차감
            int shieldAbsorbed = System.Math.Min(Shield, damage);
            Shield -= shieldAbsorbed;
            int remaining = damage - shieldAbsorbed;

            // 2. 잔여 HP 차감 (0 미만 방지)
            Hp -= remaining;
            if (Hp < 0) Hp = 0;
        }

        /// <summary>실드를 합산. 같은 턴 수비 스킬 중복 허용.</summary>
        public void AddShield(int amount)
        {
            if (amount <= 0) return;
            Shield += amount;
        }

        /// <summary>적 턴 종료 후 플레이어 실드 초기화.</summary>
        public void ResetShield() => Shield = 0;

        /// <summary>
        /// HP 회복. 최대 HP 초과 불가 (초과분 소멸).
        /// game-design-v2.2 §2.2
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            Hp = System.Math.Min(Hp + amount, MaxHp);
        }

        // ── 경험치 / 재화 ──────────────────────────────────────────────────────

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
        }

        /// <summary>골드 소비. 잔고 부족 시 예외.</summary>
        public void SpendGold(int amount)
        {
            if (amount < 0) return;
            if (Gold < amount)
                throw new System.InvalidOperationException(
                    $"골드 부족: 보유 {Gold}, 요구 {amount}");
            Gold -= amount;
        }

        /// <summary>SP 소비. 잔고 부족 시 예외.</summary>
        public void SpendSp(int amount)
        {
            if (amount < 0) return;
            if (Sp < amount)
                throw new System.InvalidOperationException(
                    $"SP 부족: 보유 {Sp}, 요구 {amount}");
            Sp -= amount;
        }

        // ── 레벨업 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 레벨업 실행. 스탯 선택 결과를 반영하고 Level/SP를 갱신.
        /// HP+5 선택 시 현재 HP도 +5 (최대 HP 초과 가능 범위는 MaxHp 증가분까지).
        /// </summary>
        public void LevelUp(PlayerStats newStats)
        {
            int hpIncrease = newStats.MaxHp - BaseStats.MaxHp;
            BaseStats = newStats;
            Level++;
            Sp++;

            // HP+5 선택 시 현재 HP도 동일하게 증가 (min(HP+증가량, 새 MaxHp))
            if (hpIncrease > 0)
                Hp = System.Math.Min(Hp + hpIncrease, MaxHp);
        }

        // ── 스킬 해금/강화 (SkillPointSystem/SkillEnhancer에서 직접 호출) ──────

        public void UnlockHand(HandType hand) => UnlockedHands.Add(hand);

        public void SetEnhanceLevel(HandType hand, int level)
        {
            EnhanceLevels[hand] = level;
        }

        public int GetEnhanceLevel(HandType hand)
        {
            EnhanceLevels.TryGetValue(hand, out int level);
            return level; // 없으면 0 (Lv1, 미강화)
        }
    }
}
