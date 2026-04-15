// Dice.cs
// 주사위 1개의 상태(값, 잠금)와 굴림 책임을 담당한다.
// IRandom을 생성자에서 주입받아 System.Random 직접 의존을 차단한다.
namespace OUD.BattleEngine.Dice
{
    using OUD.BattleEngine.Core;

    /// <summary>
    /// 주사위 1개. 값(1~6)과 잠금 상태를 가진다.
    /// Roll() 시 IsKept == true이면 굴리지 않음 (잠금 보호).
    /// </summary>
    public class Dice
    {
        // ── 공개 상태 ──────────────────────────────────────────────
        /// <summary>현재 주사위 눈금 (1~6). 초기값 1.</summary>
        public int  Value  { get; private set; } = 1;

        /// <summary>잠금 여부. true이면 Roll() 호출 시 값이 변하지 않는다.</summary>
        public bool IsKept { get; private set; }

        // ── 의존성 ────────────────────────────────────────────────
        private readonly IRandom _random;

        // ── 생성자 ────────────────────────────────────────────────
        public Dice(IRandom random)
        {
            _random = random;
        }

        // ── 공개 메서드 ───────────────────────────────────────────

        /// <summary>
        /// 주사위를 굴린다.
        /// IsKept == true이면 즉시 반환하여 값을 유지한다 (E-01 처리).
        /// </summary>
        public void Roll()
        {
            if (IsKept) return;
            Value = _random.Next(1, 7); // [1, 7) = 1~6
        }

        /// <summary>잠금 상태를 설정한다.</summary>
        public void SetKept(bool kept) => IsKept = kept;

        /// <summary>잠금을 해제한다. RollAll() 또는 ResetForNewTurn() 시 호출.</summary>
        public void ResetKeep() => IsKept = false;
    }
}
