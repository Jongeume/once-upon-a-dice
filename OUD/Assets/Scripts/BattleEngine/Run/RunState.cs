// RunState.cs
// 런 1회의 진행 상태. PlayerState 참조 + 현재 노드 인덱스만 보유한다.
// 누적 XP/Gold/Sp/Level 등은 PlayerState가 보유 — 본 클래스는 노드 진행만 책임.
// feature-spec F-11
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 런 1회의 진행 상태.
    /// 노드 인덱스 0~5: 일반 전투(6개), 6: 보스. TotalNodes=7.
    /// 패배 시 Reset()으로 초기화 (호출자가 새 PlayerState도 함께 교체).
    /// </summary>
    public class RunState
    {
        public const int TOTAL_NODES   = 7;
        public const int BOSS_NODE     = 6;   // 0-based

        public PlayerState Player           { get; private set; }
        public int         CurrentNodeIndex { get; private set; }
        public bool        IsRunComplete    { get; private set; }

        public bool IsBossNode => CurrentNodeIndex == BOSS_NODE;

        public RunState(PlayerState player)
        {
            Player           = player;
            CurrentNodeIndex = 0;
            IsRunComplete    = false;
        }

        /// <summary>
        /// 다음 노드로 진행. 보스 노드에서 호출 시 IsRunComplete=true 전환.
        /// 일반 노드에서는 인덱스만 +1.
        /// </summary>
        public void Advance()
        {
            if (IsRunComplete) return;

            if (IsBossNode)
            {
                IsRunComplete = true;
                return;
            }

            CurrentNodeIndex++;
        }

        /// <summary>
        /// 새 런을 위한 상태 초기화. PlayerState 교체는 외부 책임.
        /// </summary>
        public void Reset(PlayerState player)
        {
            Player           = player;
            CurrentNodeIndex = 0;
            IsRunComplete    = false;
        }
    }
}
