using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;

namespace OUD.Unity.Battle
{
    /// <summary>
    /// BattleEngine.SkillData를 View 표시용으로 가공한 DTO.
    /// Presenter가 생성, View는 읽기만 한다.
    /// </summary>
    public class SkillCardData
    {
        public string SkillId;
        public string DisplayName;
        public HandType RequiredHand;
        public SkillCategory Category;
        public string DescriptionText;
        public string ValueText;
        public bool IsEnabled;
    }
}
