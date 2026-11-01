// EncounterTableTests.cs
// feature-spec F-11 적 구성 규칙 테스트.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class EncounterTableTests
    {
        // ── Mock ─────────────────────────────────────────────────────────────
        /// <summary>
        /// (min, max) 호출 순서대로 결과를 반환하는 IRandom.
        /// 각 Next 호출이 시퀀스의 다음 값을 사용한다 (배열 소진 시 마지막 반복).
        /// 범위는 무시.
        /// </summary>
        private class ScriptedRandom : IRandom
        {
            private readonly int[] _values;
            private int _index;

            public ScriptedRandom(params int[] values) { _values = values; }

            public int Next(int minInclusive, int maxExclusive)
            {
                int v = _values[_index];
                if (_index < _values.Length - 1) _index++;
                return v;
            }
        }

        // ── 생성자 가드 ──────────────────────────────────────────────────────

        [Test]
        public void Constructor_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EncounterTable(null));
        }

        // ── 노드 인덱스 범위 검증 ────────────────────────────────────────────

        [Test]
        public void GenerateEncounter_NegativeIndex_Throws()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => sut.GenerateEncounter(-1));
        }

        [Test]
        public void GenerateEncounter_IndexAboveTotal_Throws()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => sut.GenerateEncounter(7));
        }

        // ── 보스 노드 ────────────────────────────────────────────────────────

        [Test]
        public void GenerateEncounter_BossNode_ReturnsSingleStoneGolem()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));

            List<MonsterData> result = sut.GenerateEncounter(RunState.BOSS_NODE);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_STONE_GOLEM, result[0].Id);
        }

        // ── 초반 (0~1): Slime, 1~2체 ─────────────────────────────────────────

        [Test]
        public void GenerateEncounter_EarlyNode0_AllSlimes()
        {
            // 첫 Next(count): 1, 두번째 Next(pool index): 0 (Slime)
            var sut = new EncounterTable(new ScriptedRandom(1, 0));
            List<MonsterData> result = sut.GenerateEncounter(0);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SLIME, result[0].Id);
        }

        [Test]
        public void GenerateEncounter_EarlyNode1_TwoSlimes()
        {
            // count=2 (max=2, +1 호출이지만 ScriptedRandom는 범위 무시 → 첫 값 2)
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 0));
            List<MonsterData> result = sut.GenerateEncounter(1);

            Assert.AreEqual(2, result.Count);
            foreach (var m in result)
                Assert.AreEqual(MonsterDatabase.ID_SLIME, m.Id);
        }

        // ── 중반 (2~3): Slime/Skeleton, 1~2체 ────────────────────────────────

        [Test]
        public void GenerateEncounter_MidNode2_PoolIsSlimeOrSkeleton()
        {
            // count=2, pool[0]=Slime, pool[1]=Skeleton
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 1));
            List<MonsterData> result = sut.GenerateEncounter(2);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SLIME,    result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_SKELETON, result[1].Id);
        }

        // ── 후반 (4~5): Skeleton/Goblin, 2~3체 ───────────────────────────────

        [Test]
        public void GenerateEncounter_LateNode5_PoolIsSkeletonOrGoblin()
        {
            // count=3, pool[0]=Skeleton, pool[1]=Goblin
            var sut = new EncounterTable(new ScriptedRandom(3, 0, 1, 0));
            List<MonsterData> result = sut.GenerateEncounter(5);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SKELETON, result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_GOBLIN,   result[1].Id);
            Assert.AreEqual(MonsterDatabase.ID_SKELETON, result[2].Id);
        }

        // ── 일반 노드 풀 외 적 미등장 ────────────────────────────────────────

        [Test]
        public void GenerateEncounter_EarlyNode_NeverContainsSkeletonOrGoblin()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 0));
            List<MonsterData> result = sut.GenerateEncounter(0);

            foreach (var m in result)
            {
                Assert.AreNotEqual(MonsterDatabase.ID_SKELETON,    m.Id);
                Assert.AreNotEqual(MonsterDatabase.ID_GOBLIN,      m.Id);
                Assert.AreNotEqual(MonsterDatabase.ID_STONE_GOLEM, m.Id);
            }
        }
    }
}
