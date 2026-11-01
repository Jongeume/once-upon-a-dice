// NodeView.cs
// F-11 Phase D-2 노드맵 UI — 단일 노드 표현.
// NodeMapView가 4개를 보유. 상태(Cleared/Current/Available/Locked)에 따라
// 알파/Outline 두께/라벨 텍스트/클릭 활성 여부를 갱신한다.
// feature-spec F-11 (Phase D-2), user-flow §1
using System;
using OUD.BattleEngine.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 노드의 시각 상태. NodeMapView가 Bind 시점에 결정해 SetState로 주입한다.
    /// </summary>
    public enum NodeVisualState
    {
        Cleared,    // 이미 클리어한 노드 (알파 0.5, 흐릿)
        Current,    // 방금 클리어한 노드 = 현재 위치 (알파 1.0, 흰색 외곽)
        Available,  // 다음 진입 가능 (밝은 시안 외곽 + 두꺼움, 클릭 가능)
        Locked,     // 미해금 (알파 0.4, 어둡고 ?만 표시)
    }

    /// <summary>
    /// 단일 노드 UI. Inspector에서 Image 프레임 + Outline + TMP_Text 라벨 + Button을 바인딩.
    /// NodeMapView가 4개를 자식으로 두고 Bind 시점에 SetNode + SetState 호출.
    /// </summary>
    public class NodeView : MonoBehaviour
    {
        // ── Inspector 바인딩 ─────────────────────────────────────────────────

        [Header("프레임 (Image + Outline)")]
        [SerializeField] private Image   _frameImage;
        [SerializeField] private Outline _frameOutline;

        [Header("라벨 (중앙) — Combat='전투' / Boss='보스' / Locked='?'")]
        [SerializeField] private TMP_Text _labelText;

        [Header("클리어 오버레이 (우상단 ✓)")]
        [SerializeField] private TMP_Text _clearedOverlay;

        [Header("클릭 (Available 상태에서만 활성)")]
        [SerializeField] private Button _clickButton;

        // ── 상수 (CLAUDE.md hard rule: 매직 넘버 금지) ─────────────────────

        // 시각 색상 ─ 이미지 참고 (시안 글로우 + 다크 블루 베이스)
        private static readonly Color FRAME_COLOR_COMBAT  = new Color(0.20f, 0.30f, 0.50f, 1f); // 어두운 청회색
        private static readonly Color FRAME_COLOR_BOSS    = new Color(0.55f, 0.10f, 0.10f, 1f); // 진한 빨강
        private static readonly Color OUTLINE_AVAILABLE   = new Color(0.00f, 0.85f, 1.00f, 1f); // 밝은 시안 (#00D8FF)
        private static readonly Color OUTLINE_CURRENT     = new Color(1.00f, 1.00f, 1.00f, 1f); // 흰색
        private static readonly Color OUTLINE_CLEARED     = new Color(0.40f, 0.40f, 0.40f, 1f); // 회색
        private static readonly Color OUTLINE_LOCKED      = new Color(0.15f, 0.15f, 0.20f, 1f); // 어두움

        private static readonly Color LABEL_COLOR_COMBAT = Color.white;
        private static readonly Color LABEL_COLOR_BOSS   = new Color(1.00f, 0.85f, 0.20f, 1f); // 보스: 노란빛 (붉은 프레임 위에서 가독성)

        // Outline 두께 (px) ─ Available 시 두껍게 강조
        private static readonly Vector2 OUTLINE_THICK = new Vector2(5f, 5f);
        private static readonly Vector2 OUTLINE_NORM  = new Vector2(2f, 2f);

        // 알파 (상태별)
        private const float ALPHA_FULL    = 1.0f;
        private const float ALPHA_CLEARED = 0.5f;
        private const float ALPHA_LOCKED  = 0.4f;

        // 라벨 텍스트
        private const string LABEL_COMBAT = "전투";
        private const string LABEL_BOSS   = "보스";
        private const string LABEL_LOCKED = "?";
        private const string OVERLAY_CHECK = "✓";

        // ── 상태 ─────────────────────────────────────────────────────────────

        public int NodeId { get; private set; }
        public event Action<int> OnClicked;

        private void Awake()
        {
            if (_clickButton != null)
                _clickButton.onClick.AddListener(() => OnClicked?.Invoke(NodeId));
        }

        // ── 외부 API ─────────────────────────────────────────────────────────

        /// <summary>
        /// 노드 데이터 주입. NodeId / Type 별 라벨 + 베이스 컬러 설정.
        /// SetState 호출 전 1회 사용.
        /// </summary>
        public void SetNode(MapNode node)
        {
            NodeId = node.Id;

            if (_frameImage != null)
                _frameImage.color = (node.Type == NodeType.Boss) ? FRAME_COLOR_BOSS : FRAME_COLOR_COMBAT;

            if (_labelText != null)
            {
                _labelText.text = (node.Type == NodeType.Boss) ? LABEL_BOSS : LABEL_COMBAT;
                _labelText.color = (node.Type == NodeType.Boss) ? LABEL_COLOR_BOSS : LABEL_COLOR_COMBAT;
                _labelText.fontStyle = (node.Type == NodeType.Boss) ? FontStyles.Bold : FontStyles.Normal;
            }

            if (_clearedOverlay != null)
            {
                _clearedOverlay.text = OVERLAY_CHECK;
                _clearedOverlay.gameObject.SetActive(false);  // SetState에서 토글
            }
        }

        /// <summary>
        /// 시각 상태 전환. SetNode 이후 매번 호출 가능.
        /// </summary>
        public void SetState(NodeVisualState state)
        {
            switch (state)
            {
                case NodeVisualState.Cleared:
                    ApplyAlpha(ALPHA_CLEARED);
                    SetOutline(OUTLINE_CLEARED, OUTLINE_NORM);
                    SetClickable(false);
                    SetClearedOverlay(true);
                    SetLabelTextOverride(null);  // 원래 라벨 유지
                    break;

                case NodeVisualState.Current:
                    ApplyAlpha(ALPHA_FULL);
                    SetOutline(OUTLINE_CURRENT, OUTLINE_NORM);
                    SetClickable(false);
                    SetClearedOverlay(true);  // 방금 클리어한 노드는 ✓ 표시
                    SetLabelTextOverride(null);
                    break;

                case NodeVisualState.Available:
                    ApplyAlpha(ALPHA_FULL);
                    SetOutline(OUTLINE_AVAILABLE, OUTLINE_THICK);
                    SetClickable(true);
                    SetClearedOverlay(false);
                    SetLabelTextOverride(null);
                    break;

                case NodeVisualState.Locked:
                    ApplyAlpha(ALPHA_LOCKED);
                    SetOutline(OUTLINE_LOCKED, OUTLINE_NORM);
                    SetClickable(false);
                    SetClearedOverlay(false);
                    SetLabelTextOverride(LABEL_LOCKED);  // ? 로 덮어쓰기
                    break;
            }
        }

        // ── 내부 헬퍼 ───────────────────────────────────────────────────────

        private void ApplyAlpha(float alpha)
        {
            if (_frameImage != null)
            {
                Color c = _frameImage.color;
                c.a = alpha;
                _frameImage.color = c;
            }
            if (_labelText != null)
            {
                Color c = _labelText.color;
                c.a = alpha;
                _labelText.color = c;
            }
        }

        private void SetOutline(Color color, Vector2 distance)
        {
            if (_frameOutline == null) return;
            _frameOutline.effectColor    = color;
            _frameOutline.effectDistance = distance;
        }

        private void SetClickable(bool clickable)
        {
            if (_clickButton == null) return;
            _clickButton.interactable = clickable;
        }

        private void SetClearedOverlay(bool show)
        {
            if (_clearedOverlay == null) return;
            _clearedOverlay.gameObject.SetActive(show);
        }

        /// <summary>Locked 상태에서 라벨을 "?"로 덮어쓰기. null이면 SetNode에서 설정한 원본 유지.</summary>
        private void SetLabelTextOverride(string overrideText)
        {
            if (_labelText == null) return;
            if (overrideText != null)
                _labelText.text = overrideText;
            // null인 경우 SetNode에서 설정한 텍스트 그대로 유지 (별도 작업 없음)
        }
    }
}
