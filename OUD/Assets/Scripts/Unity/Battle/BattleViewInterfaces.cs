using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.Unity.Battle;

namespace OUD.Unity.Battle
{
    public interface IPlayerView
    {
        void UpdateHp(float fillAmount, string hpText);
        void UpdateShield(int shield, bool visible);
        void UpdateStats(int atk, int def);
        void PlayDamageEffect();
        void PlayHealEffect();
    }

    public interface IEnemyEntryView
    {
        event System.Action OnClicked;
        void Setup(string name, UnityEngine.Sprite sprite, float hpFill, string hpText);
        void UpdateHp(float fillAmount, string hpText);
        void UpdateShield(int shield, bool visible);
        void UpdateIntent(IntentType intent, int value);
        void SetTargetSelectable(bool selectable);
        void SetTargetHighlight(bool highlighted);
        void PlayDeathEffect();
    }

    public interface IDiceEntryView
    {
        void UpdateValue(int value);
        void SetKept(bool kept);
    }

    public interface IDiceView
    {
        void UpdateRerollInfo(int rerollsLeft, bool canReroll);
        void SetConfirmButtonActive(bool active);
        void SetVisible(bool visible);
    }

    public interface ISlotAssignmentView
    {
        void ShowSkillList(List<SkillCardData> attackSkills, List<SkillCardData> defenseSkills);
        void SetSkillCardEnabled(string skillId, bool enabled);
        void UpdateSlot(int slotIndex, SkillCardData skill);
        void ClearSlots();
        void SetRerollButtonActive(bool active, int rerollsLeft);
        void SetUseSkillButtonActive(bool active);
    }

    public interface ITargetSelectionView
    {
        event System.Action<int> OnSlotClicked;
        void ShowSlots(SkillCardData[] slotCards);
        void HighlightSlot(int slotIndex);
        void ShowTargetLink(int slotIndex, int enemyIndex);
        void ClearTargetLinks();
        void SetExecuteButtonActive(bool active);
        void SetSlotClickable(bool clickable);
    }

    public interface IBattleLogView
    {
        void ShowDamagePopup(UnityEngine.Vector3 worldPos, int damage);
        void ShowHealPopup(UnityEngine.Vector3 worldPos, int amount);
        void ShowWinScreen();
        void ShowLoseScreen();
    }
}
