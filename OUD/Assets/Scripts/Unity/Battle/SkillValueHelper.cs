using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;

namespace OUD.Unity.Battle
{
    public static class SkillValueHelper
    {
        public static string BuildValueText(SkillData s, int atk, int def, int enhanceLevel)
        {
            if (s.Category == SkillCategory.Attack)
            {
                int dmg = DamageCalculator.CalcValue(atk, s.GetMultiplier(0, enhanceLevel));
                if (s.HitCount > 1)
                    return $"ATK {dmg}×{s.HitCount}";
                if (s.Multipliers.Length >= 2 && s.Target == TargetType.Single)
                {
                    int splash = DamageCalculator.CalcValue(atk, s.GetMultiplier(1, enhanceLevel));
                    return $"ATK {dmg}+{splash}";
                }
                if (s.Target == TargetType.AllEnemies)
                    return $"ATK {dmg} ALL";
                return $"ATK {dmg}";
            }
            else
            {
                int shd = DamageCalculator.CalcValue(def, s.GetMultiplier(0, enhanceLevel));
                if (s.HpRecover > 0)
                    return $"DEF {shd} +{s.HpRecover}HP";
                return $"DEF {shd}";
            }
        }
    }
}
