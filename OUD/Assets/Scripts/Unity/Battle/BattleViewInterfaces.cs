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
        void ShowDefenseBadge(string skillName);
        void ClearDefenseBadges();
    }

    public interface IEnemyEntryView
    {
        event System.Action OnClicked;
        void Setup(string name, UnityEngine.Sprite sprite, float hpFill, string hpText);
        void UpdateHp(float fillAmount, string hpText);
        void UpdateShield(int shield, bool visible);
        /// <summary>좌상단 공격력(현재 실효 ATK) 표시 갱신.</summary>
        void UpdateAtk(int atk);
        /// <summary>우상단 방어력 표시 갱신. (현재 ShieldValue 전달 — 데이터 조정 예정.)</summary>
        void UpdateDef(int def);
        void UpdateIntent(IntentType intent, int value);
        void SetTargetSelectable(bool selectable);
        void SetTargetHighlight(bool highlighted);
        /// <summary>분노 상태 표시 토글 (보스 전용 — HP 임계치 도달 시 영구 활성).</summary>
        void SetRageActive(bool active);
        void PlayDeathEffect();
        void ShowTargetBadge(string skillName, SkillCategory category, bool isAoe);
        void ClearTargetBadge();
        /// <summary>타겟팅 시 예상 피해를 HP 바에 반투명 표시. predictedRatio = 피해 후 남을 HP 비율.</summary>
        void ShowDamagePreview(float predictedRatio);
        /// <summary>데미지 프리뷰 해제.</summary>
        void ClearDamagePreview();
    }

    public interface IDiceEntryView
    {
        void PlayRoll(int resultValue, float stopDelay, System.Action onComplete);
        void SetResultImmediate(int value);
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
