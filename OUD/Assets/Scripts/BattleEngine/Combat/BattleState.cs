// BattleState.cs
// 전투 1회의 전체 상태 컨테이너.
// TurnManager가 이 객체를 읽고 쓰며, IBattleUI는 읽기 전용으로 참조한다.
using System.Collections.Generic;
using System.Linq;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Dice;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Combat
{
    /// <summary>
    /// 전투 진행 중 모든 가변 상태를 하나의 객체로 묶는다.
    /// TurnManager에서 단일 진실 소스(SSOT)로 사용한다.
    /// </summary>
    public class BattleState
    {
        // ── 페이즈 / 턴 ───────────────────────────────────────────────────────
        public BattlePhase Phase      { get; set; } = BattlePhase.BattleStart;
        public int         TurnNumber { get; set; } = 0;

        // ── 참가자 ────────────────────────────────────────────────────────────
        public PlayerState            Player  { get; }
        public List<MonsterInstance>  Enemies { get; }

        // ── 주사위 ────────────────────────────────────────────────────────────
        public DiceHand DiceHand { get; }

        // ── 턴 내 추적 상태 ───────────────────────────────────────────────────
        /// <summary>이번 턴 사용된 족보 집합 (공+수 합산 1회 제한).</summary>
        public HashSet<HandType> UsedHandsThisTurn { get; } = new HashSet<HandType>();

        /// <summary>족보 판정 결과 (현재 턴).</summary>
        public List<HandType> AchievedHands { get; set; } = new List<HandType>();

        // ── 종료 조건 ─────────────────────────────────────────────────────────
        public bool IsPlayerDead      => Player.Hp <= 0;
        public bool AreAllEnemiesDead => Enemies.All(e => e.IsDead);

        /// <summary>생존한 적만 반환.</summary>
        public List<MonsterInstance> AliveEnemies
            => Enemies.FindAll(e => !e.IsDead);

        // ── 생성자 ────────────────────────────────────────────────────────────
        public BattleState(PlayerState player, List<MonsterInstance> enemies, DiceHand diceHand)
        {
            Player   = player;
            Enemies  = enemies;
            DiceHand = diceHand;
        }

        // ── 턴 리셋 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 새 플레이어 턴 시작 시 턴 내 추적 상태를 초기화.
        /// DiceHand 리셋, 사용 족보 목록 초기화, 족보 판정 초기화.
        /// </summary>
        public void ResetForNewTurn()
        {
            TurnNumber++;
            DiceHand.ResetForNewTurn();
            UsedHandsThisTurn.Clear();
            AchievedHands.Clear();
        }
    }
}
