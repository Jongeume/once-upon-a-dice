// DebugBattleUI.cs
// 개발 검증용 IBattleUI 구현체.
// 전투 이벤트를 Debug.Log로 출력하고 자동 진행 플래그를 제공한다.
// BattleBootstrapper의 코루틴이 플래그를 폴링하여 턴을 자동으로 진행시킨다.
using System;
using System.Collections.Generic;
using UnityEngine;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;

namespace OUD.Unity.Adapter
{
    /// <summary>
    /// Sprint-0 검증용 IBattleUI.
    /// UI 없이 Console 로그만으로 전투 흐름을 확인할 수 있다.
    /// </summary>
    public class DebugBattleUI : IBattleUI
    {
        // ── 자동 진행 플래그 (BattleBootstrapper 코루틴이 폴링) ──────────────
        public bool DiceRolled            { get; private set; }
        public bool SlotAssignmentPending { get; private set; }
        public bool BattleOver            { get; private set; }

        // ── 슬롯 배분 대기 정보 ────────────────────────────────────────────────
        public List<SkillData>              PendingSkills   { get; private set; }
        public List<MonsterInstance>        PendingEnemies  { get; private set; }
        public Action<List<SlotAssignment>> PendingCallback { get; private set; }

        // ── 플래그 클리어 (BattleBootstrapper 코루틴에서 호출) ────────────────
        public void ClearDiceRolled()            => DiceRolled = false;
        public void ClearSlotAssignmentPending() => SlotAssignmentPending = false;

        // ── IBattleUI 구현 ────────────────────────────────────────────────────

        public void OnPlayerTurnStarted() { }

        public void OnBattleStart(PlayerState player, List<MonsterInstance> enemies)
        {
            var names = string.Join(", ", enemies.ConvertAll(e => $"{e.Data.Name}(HP:{e.Hp})"));
            Debug.Log($"[전투 시작] 플레이어 HP:{player.Hp}/{player.MaxHp} ATK:{player.Atk} DEF:{player.Def}\n  적: {names}");
        }

        public void OnDiceRolled(int[] values, int rerollsLeft)
        {
            Debug.Log($"[주사위] [{string.Join(", ", values)}]  리롤 잔여: {rerollsLeft}");
            DiceRolled = true;
        }

        public void OnHandsEvaluated(List<HandType> hands, List<SkillData> usableSkills)
        {
            if (usableSkills.Count == 0)
            {
                Debug.Log("[족보] 잡패 — 스킬 없음. 적 턴으로 진행");
                return;
            }
            var handNames  = string.Join(", ", hands.ConvertAll(h => h.ToString()));
            var skillNames = string.Join(", ", usableSkills.ConvertAll(s => $"{s.Name}({s.Category})"));
            Debug.Log($"[족보] {handNames}\n  사용 가능 스킬: {skillNames}");
        }

        public void RequestSlotAssignment(
            List<SkillData> usableSkills,
            List<MonsterInstance> aliveEnemies,
            Action<List<SlotAssignment>> onComplete)
        {
            var enemyInfo = string.Join(", ", aliveEnemies.ConvertAll(e => $"{e.Data.Name}(HP:{e.Hp})"));
            Debug.Log($"[슬롯 배분 대기] 스킬 {usableSkills.Count}개  생존 적: {enemyInfo}");
            PendingSkills   = usableSkills;
            PendingEnemies  = aliveEnemies;
            PendingCallback = onComplete;
            SlotAssignmentPending = true;
        }

        public void OnSlotExecuted(int slotIndex, SkillResult result)
        {
            string skillName = result.Skill?.Name ?? "?";
            if (result.Damages.Count > 0)
            {
                var dmgLog = new System.Text.StringBuilder();
                foreach (var d in result.Damages)
                    dmgLog.Append($" 적[{d.TargetIndex}] {d.Damage}dmg(실드{d.ShieldAbsorbed}/HP{d.HpDamage}){(d.TargetDied ? " 사망!" : "")}");
                Debug.Log($"[슬롯{slotIndex + 1}] {skillName}(공격) →{dmgLog}");
            }
            else
            {
                Debug.Log($"[슬롯{slotIndex + 1}] {skillName}(수비) → 실드+{result.ShieldGained}  HP회복+{result.HpRecovered}");
            }
        }

        public void OnEnemyAction(int enemyIndex, IntentType intent, int value)
        {
            Debug.Log($"[적 행동] 적[{enemyIndex}] {intent}  수치:{value}");
        }

        public void OnShieldsReset()
        {
            Debug.Log("[실드 초기화]");
        }

        public void OnBattleWon()
        {
            Debug.Log("=== 전투 승리! ===");
            BattleOver = true;
        }

        public void OnBattleLost()
        {
            Debug.Log("=== 전투 패배! ===");
            BattleOver = true;
        }
    }
}
