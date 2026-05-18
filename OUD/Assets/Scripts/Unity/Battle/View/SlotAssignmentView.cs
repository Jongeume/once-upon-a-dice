using System.Collections.Generic;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 화면 B 전용 뷰. 기술 목록(공격/수비 2열) + 슬롯 배분 제어.
    /// ISlotAssignmentView 구현.
    /// </summary>
    public class SlotAssignmentView : ViewBase, ISlotAssignmentView
    {
        [Header("기술 목록 컨테이너")]
        [SerializeField] private Transform _attackColumn;
        [SerializeField] private Transform _defenseColumn;

        [Header("기술 카드 프리팹")]
        [SerializeField] private SkillCardButton _skillCardPrefab;

        [Header("슬롯 (B1 - 화면A와 동일 위치)")]
        [SerializeField] private SkillSlotView _slotView;

        [Header("버튼")]
        [SerializeField] private Button   _rerollButton;
        [SerializeField] private TMP_Text _rerollCountText;
        [SerializeField] private Button   _useSkillButton;

        private readonly Dictionary<string, SkillCardButton> _cardButtons = new();

        public event System.Action<string> OnSkillCardClicked;
        public event System.Action         OnRerollClicked;
        public event System.Action         OnUseSkillClicked;

        private void Awake()
        {
            if (_rerollButton)   _rerollButton.onClick.AddListener(()   => OnRerollClicked?.Invoke());
            if (_useSkillButton) _useSkillButton.onClick.AddListener(() => OnUseSkillClicked?.Invoke());
        }

        public void ShowSkillList(List<SkillCardData> attackSkills, List<SkillCardData> defenseSkills)
        {
            ClearColumn(_attackColumn);
            ClearColumn(_defenseColumn);
            _cardButtons.Clear();

            SpawnCards(attackSkills,  _attackColumn);
            SpawnCards(defenseSkills, _defenseColumn);
        }

        public void SetSkillCardEnabled(string skillId, bool enabled)
        {
            if (_cardButtons.TryGetValue(skillId, out var btn))
                btn.SetEnabled(enabled);
        }

        public void UpdateSlot(int slotIndex, SkillCardData skill) =>
            _slotView?.SetSlot(slotIndex, skill);

        public void ClearSlots() => _slotView?.ClearAll();

        public void SetRerollButtonActive(bool active, int rerollsLeft)
        {
            if (_rerollButton)    _rerollButton.gameObject.SetActive(active);
            if (_rerollCountText) _rerollCountText.text = $"리롤 {rerollsLeft}/3";
        }

        public void SetUseSkillButtonActive(bool active)
        {
            if (_useSkillButton) _useSkillButton.gameObject.SetActive(active);
        }

        private void SpawnCards(List<SkillCardData> cards, Transform column)
        {
            foreach (var card in cards)
            {
                var btn = Instantiate(_skillCardPrefab, column);
                btn.Setup(card, id => OnSkillCardClicked?.Invoke(id));
                _cardButtons[card.SkillId] = btn;
            }
        }

        private static void ClearColumn(Transform column)
        {
            if (column == null) return;
            foreach (Transform child in column)
                Destroy(child.gameObject);
        }
    }
}
