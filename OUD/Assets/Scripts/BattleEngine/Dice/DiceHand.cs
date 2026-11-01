// DiceHand.cs
// 주사위 5개 묶음과 리롤 카운트를 관리한다.
// 첫 굴림(RollAll)은 리롤 카운트를 소비하지 않는다.
// 리롤(Reroll)은 IsKept가 false인 주사위만 다시 굴리고 카운트를 1 감소시킨다.
namespace OUD.BattleEngine.Dice
{
    using OUD.BattleEngine.Core;

    /// <summary>
    /// 주사위 5개의 상태와 리롤 잔여 횟수를 관리하는 클래스.
    /// 턴 시작 → RollAll() → (SetKept + Reroll) × N → GetValues() → HandEvaluator 순으로 호출한다.
    /// </summary>
    public class DiceHand
    {
        // ── 상수 ──────────────────────────────────────────────────
        private const int MAX_REROLLS = 2;
        private const int DICE_COUNT  = 5;

        // ── 공개 상태 ──────────────────────────────────────────────
        /// <summary>주사위 5개 배열. 인덱스 0~4.</summary>
        public Dice[] Dices       { get; }

        /// <summary>남은 리롤 횟수. 0이면 Reroll() 거부.</summary>
        public int    RerollsLeft { get; private set; }

        // ── 생성자 ────────────────────────────────────────────────
        /// <summary>
        /// IRandom을 공유하여 5개의 Dice를 생성한다.
        /// 모든 주사위가 동일한 IRandom 인스턴스를 쓰므로
        /// 시드 고정 시 결정론적 결과를 보장한다.
        /// </summary>
        public DiceHand(IRandom random)
        {
            Dices = new Dice[DICE_COUNT];
            for (int i = 0; i < DICE_COUNT; i++)
                Dices[i] = new Dice(random);

            RerollsLeft = MAX_REROLLS;
        }

        // ── 공개 메서드 ───────────────────────────────────────────

        /// <summary>
        /// 첫 굴림. 잠금을 전부 해제하고 5개 모두 굴린다.
        /// 리롤 카운트를 소비하지 않는다 (첫 굴림은 무료).
        /// </summary>
        public void RollAll()
        {
            for (int i = 0; i < DICE_COUNT; i++)
            {
                Dices[i].ResetKeep(); // 잠금 해제 후 굴림
                Dices[i].Roll();
            }
            // RerollsLeft 변경 없음 — 첫 굴림은 리롤이 아님
        }

        /// <summary>
        /// 리롤을 시도한다.
        /// IsKept가 false인 주사위만 다시 굴리고 RerollsLeft를 1 감소.
        /// 리롤 잔여가 0이면 false를 반환하고 상태를 변경하지 않는다 (E-02).
        /// 5개 전부 잠겼어도 카운트는 소비된다 (E-01 — 의도적 설계).
        /// </summary>
        /// <returns>리롤 성공 여부</returns>
        public bool Reroll()
        {
            if (RerollsLeft <= 0) return false;

            for (int i = 0; i < DICE_COUNT; i++)
                Dices[i].Roll(); // 내부에서 IsKept 체크하므로 별도 분기 불필요

            RerollsLeft--;
            return true;
        }

        /// <summary>
        /// 턴 시작 시 초기화. 리롤 카운트를 MAX_REROLLS로 복구하고 잠금을 해제한다.
        /// TurnManager가 플레이어 턴 시작 시 호출한다.
        /// </summary>
        public void ResetForNewTurn()
        {
            RerollsLeft = MAX_REROLLS;
            for (int i = 0; i < DICE_COUNT; i++)
                Dices[i].ResetKeep();
        }

        /// <summary>
        /// 현재 5개 주사위 값을 배열로 반환한다.
        /// HandEvaluator로 전달하는 용도.
        /// </summary>
        public int[] GetValues()
        {
            int[] values = new int[DICE_COUNT];
            for (int i = 0; i < DICE_COUNT; i++)
                values[i] = Dices[i].Value;
            return values;
        }
    }
}
