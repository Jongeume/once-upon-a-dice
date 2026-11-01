// Stats.cs
// 플레이어 기본 스탯을 담는 불변 값 타입(struct).
// 레벨업 시 새 인스턴스를 반환하여 기존 값을 보호한다 (Value Object 패턴).
namespace OUD.BattleEngine.Core
{
    /// <summary>
    /// 플레이어 기본 스탯. readonly struct이므로 생성 후 변경 불가.
    /// 레벨업 등 스탯 변경은 With* 메서드로 새 값을 반환받아 교체한다.
    /// </summary>
    public readonly struct PlayerStats
    {
        public int MaxHp { get; }
        public int Atk   { get; }
        public int Def   { get; }

        public PlayerStats(int maxHp, int atk, int def)
        {
            MaxHp = maxHp;
            Atk   = atk;
            Def   = def;
        }

        // 각 스탯을 올린 새 PlayerStats 반환 (원본 불변 유지)
        public PlayerStats WithAtkUp(int amount) => new PlayerStats(MaxHp, Atk + amount, Def);
        public PlayerStats WithDefUp(int amount) => new PlayerStats(MaxHp, Atk, Def + amount);
        public PlayerStats WithHpUp(int amount)  => new PlayerStats(MaxHp + amount, Atk, Def);
    }
}
