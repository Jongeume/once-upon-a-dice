// RunState.cs
// 런 1회의 진행 상태. PlayerState 참조 + 현재 노드 인덱스만 보유한다.
// 누적 XP/Gold/Sp/Level 등은 PlayerState가 보유 — 본 클래스는 노드 진행만 책임.
// Phase D-1 (sprint MVP 데모): 보스 제거, 3전투 자동 진행.
// 노드맵 UI / 보스 메커니즘은 후속 작업(Phase D-2)에서 별도 설계.
// feature-spec F-11 (sprint MVP 갱신)
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 런 1회의 진행 상태.
    /// 노드 인덱스 0~2: 일반 전투(3개). 보스 노드는 sprint MVP 범위 외.
    /// 마지막 노드(2) 종료 시 IsRunComplete=true.
    /// </summary>
    public class RunState
    {
        public const int TOTAL_NODES = 3;
        public const int LAST_NODE   = 2;   // 0-based, 마지막 노드 인덱스

        public PlayerState Player           { get; private set; }
        public int         CurrentNodeIndex { get; private set; }
        public bool        IsRunComplete    { get; private set; }

        /// <summary>현재 노드가 런의 마지막 노드인지 (보스 노드의 단순화 대체).</summary>
        public bool IsLastNode => CurrentNodeIndex == LAST_NODE;

        public RunState(PlayerState player)
        {
            Player           = player;
            CurrentNodeIndex = 0;
            IsRunComplete    = false;
        }

        /// <summary>
        /// 다음 노드로 진행. 마지막 노드에서 호출 시 IsRunComplete=true 전환.
        /// </summary>
        public void Advance()
        {
            if (IsRunComplete) return;

            if (IsLastNode)
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
