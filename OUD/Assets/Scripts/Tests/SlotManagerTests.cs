// SlotManagerTests.cs
// feature-spec F-03 슬롯 배분 유효성 검증 테스트.
// NUnit 기반 (Unity Test Framework).
using System.Collections.Generic;
using NUnit.Framework;
using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class SlotManagerTests
    {
        private SlotManager _slotManager;

        // ── 테스트 픽스처 ─────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _slotManager = new SlotManager();
        }

        // ── 공용 헬퍼 ─────────────────────────────────────────────────────────

        /// <summary>생존한 Slime 인스턴스 생성.</summary>
        private static MonsterInstance AliveSlime()
        {
            var data = new MonsterData(
                id: "Slime", name: "Slime", maxHp: 20, baseAtk: 8, shieldValue: 0,
                pattern: new[] { IntentType.Attack, IntentType.Attack, IntentType.Attack });
            return new MonsterInstance(data);
        }

        /// <summary>HP 0인 (사망) Slime 인스턴스 생성.</summary>
        private static MonsterInstance DeadSlime()
        {
            var inst = AliveSlime();
            inst.TakeDamage(999); // 즉시 사망
            return inst;
        }

        private static SkillData AttackSkill(HandType hand)
            => SkillDatabase.Get(hand, SkillCategory.Attack);

        private static SkillData DefenseSkill(HandType hand)
            => SkillDatabase.Get(hand, SkillCategory.Defense);

        private static SlotAssignment AttackSlot(HandType hand, int targetIndex = 0)
            => new SlotAssignment { Skill = AttackSkill(hand), TargetIndex = targetIndex };

        private static SlotAssignment DefenseSlot(HandType hand)
            => new SlotAssignment { Skill = DefenseSkill(hand), TargetIndex = -1 };

        // ── Validate: 정상 케이스 ──────────────────────────────────────────────

        [Test]
        public void Validate_EmptySlots_ReturnsTrue()
        {
            // 슬롯 전부 빈 상태로 실행 → 허용 (적 턴으로 넘어감)
            var slots   = new List<SlotAssignment>();
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsTrue(_slotManager.Validate(slots, hands, enemies));
        }

        [Test]
        public void Validate_NullSlots_ReturnsTrue()
        {
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsTrue(_slotManager.Validate(null, hands, enemies));
        }

        [Test]
        public void Validate_SingleAttackSlot_ValidTarget_ReturnsTrue()
        {
            var slots   = new List<SlotAssignment> { AttackSlot(HandType.OnePair, 0) };
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsTrue(_slotManager.Validate(slots, hands, enemies));
        }

        [Test]
        public void Validate_AttackAndDefense_DifferentHands_ReturnsTrue()
        {
            // OnePair 공격 + TwoPair 수비 → 서로 다른 족보이므로 허용
            var slots = new List<SlotAssignment>
            {
                AttackSlot(HandType.OnePair, 0),
                DefenseSlot(HandType.TwoPair)
            };
            var hands   = new List<HandType> { HandType.OnePair, HandType.TwoPair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsTrue(_slotManager.Validate(slots, hands, enemies));
        }

        [Test]
        public void Validate_TwoSlots_OneEmpty_ReturnsTrue()
        {
            // 빈 슬롯 1개 + 스킬 2개 → 실행 허용
            var slots = new List<SlotAssignment>
            {
                AttackSlot(HandType.OnePair, 0),
                DefenseSlot(HandType.TwoPair),
                new SlotAssignment { Skill = null } // 빈 슬롯
            };
            var hands   = new List<HandType> { HandType.OnePair, HandType.TwoPair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsTrue(_slotManager.Validate(slots, hands, enemies));
        }

        // ── Validate: 동일 족보 제한 ──────────────────────────────────────────

        [Test]
        public void Validate_SameHand_AttackAndDefense_ReturnsFalse()
        {
            // OnePair 공격 + OnePair 수비 → 동일 족보 1회 제한 위반
            var slots = new List<SlotAssignment>
            {
                AttackSlot(HandType.OnePair, 0),
                DefenseSlot(HandType.OnePair)
            };
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsFalse(_slotManager.Validate(slots, hands, enemies));
        }

        [Test]
        public void Validate_SameHand_AttackTwice_ReturnsFalse()
        {
            var slots = new List<SlotAssignment>
            {
                AttackSlot(HandType.OnePair, 0),
                AttackSlot(HandType.OnePair, 0)
            };
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsFalse(_slotManager.Validate(slots, hands, enemies));
        }

        // ── Validate: 달성하지 못한 족보 ─────────────────────────────────────

        [Test]
        public void Validate_HandNotAchieved_ReturnsFalse()
        {
            // TwoPair를 달성하지 않았는데 사용 시도
            var slots   = new List<SlotAssignment> { AttackSlot(HandType.TwoPair, 0) };
            var hands   = new List<HandType> { HandType.OnePair }; // TwoPair 없음
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsFalse(_slotManager.Validate(slots, hands, enemies));
        }

        // ── Validate: 사망한 적 대상 지정 ─────────────────────────────────────

        [Test]
        public void Validate_AttackDeadEnemy_ReturnsFalse()
        {
            // 사망한 적에게 공격 스킬 지정 → 거부
            var slots   = new List<SlotAssignment> { AttackSlot(HandType.OnePair, 0) };
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance> { DeadSlime() };

            Assert.IsFalse(_slotManager.Validate(slots, hands, enemies));
        }

        [Test]
        public void Validate_AttackOutOfRangeIndex_ReturnsFalse()
        {
            var slots   = new List<SlotAssignment> { AttackSlot(HandType.OnePair, 99) };
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsFalse(_slotManager.Validate(slots, hands, enemies));
        }

        [Test]
        public void Validate_DefenseSkill_NoTargetNeeded_ReturnsTrue()
        {
            // 수비 스킬은 대상 인덱스 -1이어도 허용
            var slots   = new List<SlotAssignment> { DefenseSlot(HandType.OnePair) };
            var hands   = new List<HandType> { HandType.OnePair };
            var enemies = new List<MonsterInstance>(); // 적 없어도 수비는 가능

            Assert.IsTrue(_slotManager.Validate(slots, hands, enemies));
        }

        // ── Validate: 슬롯 초과 ───────────────────────────────────────────────

        [Test]
        public void Validate_MoreThanMaxSlots_ReturnsFalse()
        {
            var slots = new List<SlotAssignment>
            {
                DefenseSlot(HandType.OnePair),
                DefenseSlot(HandType.TwoPair),
                DefenseSlot(HandType.Triple),
                DefenseSlot(HandType.FullHouse) // 4번째 슬롯
            };
            var hands   = new List<HandType> { HandType.OnePair, HandType.TwoPair, HandType.Triple, HandType.FullHouse };
            var enemies = new List<MonsterInstance> { AliveSlime() };

            Assert.IsFalse(_slotManager.Validate(slots, hands, enemies));
        }

        // ── CanAssign ─────────────────────────────────────────────────────────

        [Test]
        public void CanAssign_EmptySlots_ValidHand_ReturnsTrue()
        {
            var current = new List<SlotAssignment>();
            var hands   = new List<HandType> { HandType.OnePair };

            Assert.IsTrue(_slotManager.CanAssign(current, HandType.OnePair, hands));
        }

        [Test]
        public void CanAssign_HandAlreadyUsed_ReturnsFalse()
        {
            var current = new List<SlotAssignment> { AttackSlot(HandType.OnePair, 0) };
            var hands   = new List<HandType> { HandType.OnePair };

            Assert.IsFalse(_slotManager.CanAssign(current, HandType.OnePair, hands));
        }

        [Test]
        public void CanAssign_HandNotAchieved_ReturnsFalse()
        {
            var current = new List<SlotAssignment>();
            var hands   = new List<HandType> { HandType.OnePair };

            Assert.IsFalse(_slotManager.CanAssign(current, HandType.TwoPair, hands));
        }

        [Test]
        public void CanAssign_AllSlotsFull_ReturnsFalse()
        {
            var current = new List<SlotAssignment>
            {
                AttackSlot(HandType.OnePair, 0),
                DefenseSlot(HandType.TwoPair),
                DefenseSlot(HandType.Triple)
            };
            var hands = new List<HandType>
            {
                HandType.OnePair, HandType.TwoPair, HandType.Triple, HandType.FullHouse
            };

            Assert.IsFalse(_slotManager.CanAssign(current, HandType.FullHouse, hands));
        }
    }
}
