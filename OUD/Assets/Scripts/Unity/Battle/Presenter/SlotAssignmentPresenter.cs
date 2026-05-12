using System;
using System.Collections.Generic;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;
using OUD.Unity.Battle;

namespace OUD.Unity.Battle.Presenter
{
    /// <summary>
    /// 화면 B — 기술 목록 표시 + 슬롯 배분 관리.
    /// 기술 클릭 → 가장 왼쪽 빈 슬롯 자동 배치 / 취소 불가.
    /// 동일 족보 턴당 1회 제한 (원페어 포함 예외 없음).
    /// </summary>
    public class SlotAssignmentPresenter
    {
        private readonly ISlotAssignmentView _view;
        private readonly DicePresenter       _dicePresenter;
        private readonly Action              _onUseSkillClicked;

        private List<SkillData>            _usableSkills;
        private List<MonsterInstance>      _aliveEnemies;
        private Action<List<SlotAssignment>> _onComplete;

        private SkillData[]   _slots      = new SkillData[SlotManager.MAX_SLOTS];
        private HashSet<HandType> _usedHands = new HashSet<HandType>();
        private int           _filledCount;

        public SlotAssignmentPresenter(
            ISlotAssignmentView view,
            DicePresenter       dicePresenter,
            Action              onUseSkillClicked)
        {
            _view              = view;
            _dicePresenter     = dicePresenter;
            _onUseSkillClicked = onUseSkillClicked;
        }

        /// <summary>새 플레이어 턴 시작 시 1회 호출. 슬롯/족보 상태 완전 초기화.</summary>
        public void ResetForNewTurn()
        {
            _slots       = new SkillData[SlotManager.MAX_SLOTS];
            _usedHands   = new HashSet<HandType>();
            _filledCount = 0;
            _usableSkills = null;
            _view.ClearSlots();
            // 잡패가 다음 턴에 나와 RebuildSkillList가 호출되지 않을 경우 대비해 기술 목록도 즉시 비운다.
            _view.ShowSkillList(new List<SkillCardData>(), new List<SkillCardData>());
            // 이전 턴 끝물에 활성화된 "기술 사용" 버튼이 새 턴까지 남아있지 않도록 비활성으로 강제.
            _view.SetUseSkillButtonActive(false);
        }

        /// <summary>
        /// 초기 롤 또는 리롤 후 사용 가능한 스킬 목록을 갱신한다.
        /// 슬롯 상태는 보존된다 — ResetForNewTurn()이 초기화를 담당.
        /// </summary>
        public void Begin(
            List<SkillData>            usableSkills,
            List<MonsterInstance>      aliveEnemies,
            Action<List<SlotAssignment>> onComplete)
        {
            _usableSkills = usableSkills;
            _aliveEnemies = aliveEnemies;
            _onComplete   = onComplete;

            RebuildSkillList();
            int rerollsLeft = _dicePresenter?.RerollsLeft ?? 0;
            _view.SetRerollButtonActive(rerollsLeft > 0, rerollsLeft);
            _view.SetUseSkillButtonActive(rerollsLeft == 0 || _filledCount >= SlotManager.MAX_SLOTS);
        }

        public void OnSkillClicked(string skillId)
        {
            if (_filledCount >= SlotManager.MAX_SLOTS) return;

            SkillData skill = _usableSkills.Find(s => s.Id == skillId);
            if (skill == null) return;
            if (_usedHands.Contains(skill.Hand)) return;

            // 가장 왼쪽 빈 슬롯에 배치
            for (int i = 0; i < SlotManager.MAX_SLOTS; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = skill;
                    _filledCount++;
                    _usedHands.Add(skill.Hand);

                    _view.UpdateSlot(i, ToCardData(skill, true));
                    _view.SetSkillCardEnabled(skill.Id, false);
                    // 같은 족보의 다른 기술도 비활성화
                    DisableSameHand(skill.Hand);
                    break;
                }
            }

            bool slotsFull  = _filledCount >= SlotManager.MAX_SLOTS;
            bool noRerolls  = _dicePresenter != null &&
                              false; // DicePresenter의 rerollsLeft를 외부에서 전달받아 판단
            _dicePresenter?.RefreshConfirmButton(_filledCount);

            if (slotsFull)
            {
                _view.SetRerollButtonActive(false, 0);
                _view.SetUseSkillButtonActive(true);
            }
        }

        /// <summary>리롤 소진 시 외부(DicePresenter)에서 호출. 기술 사용 버튼 활성화.</summary>
        public void OnRerollsExhausted()
        {
            _view.SetRerollButtonActive(false, 0);
            _view.SetUseSkillButtonActive(true);
        }

        public void OnRerollCountChanged(int rerollsLeft)
        {
            bool hasRerolls = rerollsLeft > 0;
            _view.SetRerollButtonActive(hasRerolls, rerollsLeft);
            if (!hasRerolls) _view.SetUseSkillButtonActive(true);
        }

        /// <summary>✅ 기술 사용 버튼 클릭 → 콜백 호출 없이 화면 C로 전환 신호만 보냄.</summary>
        public void OnUseSkillClicked() => _onUseSkillClicked?.Invoke();

        /// <summary>화면 C에서 실행 확정 시 호출. SlotAssignment 목록 조립 → 콜백 전달.</summary>
        public void Confirm(int[] targetIndices)
        {
            var assignments = new List<SlotAssignment>();
            for (int i = 0; i < SlotManager.MAX_SLOTS; i++)
            {
                if (_slots[i] != null)
                    assignments.Add(new SlotAssignment { Skill = _slots[i], TargetIndex = targetIndices[i] });
            }
            _onComplete?.Invoke(assignments);
        }

        public SkillData[] GetSlots() => _slots;
        public List<MonsterInstance> GetAliveEnemies() => _aliveEnemies;

        private void RebuildSkillList()
        {
            var attack  = new List<SkillCardData>();
            var defense = new List<SkillCardData>();

            foreach (var s in _usableSkills)
            {
                bool enabled = !_usedHands.Contains(s.Hand);
                var card = ToCardData(s, enabled);
                if (s.Category == SkillCategory.Attack) attack.Add(card);
                else                                      defense.Add(card);
            }

            _view.ShowSkillList(attack, defense);
        }

        private void DisableSameHand(HandType hand)
        {
            foreach (var s in _usableSkills)
                if (s.Hand == hand)
                    _view.SetSkillCardEnabled(s.Id, false);
        }

        private static SkillCardData ToCardData(SkillData s, bool enabled) =>
            new SkillCardData
            {
                SkillId         = s.Id,
                DisplayName     = s.Name,
                RequiredHand    = s.Hand,
                Category        = s.Category,
                DescriptionText = $"{s.Name} ({s.Hand})",
                IsEnabled       = enabled
            };
    }
}
