// RunState.cs
// 런 1회의 진행 상태. PlayerState 참조 + 현재 노드 위치(Id + Layer) + 클리어 플래그만 보유.
// 누적 XP/Gold/Sp/Level 등은 PlayerState가 보유 — 본 클래스는 진행 상태만 책임.
// Phase D-2 (sprint MVP — 노드맵 UI 도입): 1-2-1 구조 + Boss 합류 (총 4노드, 3 layer).
//   - CurrentNodeIndex = layer index (0~2). 기존 호환 의미 유지.
//   - CurrentNodeId   = RunMap 노드 고유 ID (0~3). 분기 노드 식별용.
// 노드 이동/클리어 결정은 RunManager의 책임 — 본 클래스는 데이터 보유자.
// feature-spec F-11 (Phase D-2 갱신)
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 런 1회의 진행 상태.
    /// <para>
    /// CurrentNodeIndex = layer index (0~2): 0=시작 / 1=분기 / 2=Boss.
    /// CurrentNodeId    = RunMap 노드 고유 ID (0~3, 1-2-1 구조).
    /// </para>
    /// 마지막 layer(2 = Boss) 클리어 시 IsRunComplete=true.
    /// </summary>
    public class RunState
    {
        public const int TOTAL_NODES = 13;
        public const int LAST_NODE   = 12;

        public PlayerState Player           { get; private set; }
        public int         CurrentNodeIndex { get; private set; }   // layer index
        public int         CurrentNodeId    { get; private set; }   // RunMap 노드 ID
        public bool        IsRunComplete    { get; private set; }

        /// <summary>현재 노드가 마지막 layer(Boss layer)인지.</summary>
        public bool IsLastNode => CurrentNodeId == LAST_NODE;

        public RunState(PlayerState player)
        {
            Player           = player;
            CurrentNodeIndex = 0;
            CurrentNodeId    = RunMap.START_NODE_ID;
            IsRunComplete    = false;
        }

        /// <summary>
        /// 지정 노드로 이동. 이미 IsRunComplete=true면 무시.
        /// nodeId/layer 검증은 호출자(RunManager)가 RunMap 기반으로 수행.
        /// </summary>
        public void MoveTo(int nodeId, int layer)
        {
            if (IsRunComplete) return;
            CurrentNodeId    = nodeId;
            CurrentNodeIndex = layer;
        }

        /// <summary>
        /// 마지막 노드(Boss) 클리어 시 호출. 이후 MoveTo는 무시된다.
        /// </summary>
        public void MarkComplete()
        {
            IsRunComplete = true;
        }

        /// <summary>
        /// 새 런을 위한 상태 초기화. PlayerState 교체는 외부 책임.
        /// </summary>
        public void Reset(PlayerState player)
        {
            Player           = player;
            CurrentNodeIndex = 0;
            CurrentNodeId    = RunMap.START_NODE_ID;
            IsRunComplete    = false;
        }
    }
}
