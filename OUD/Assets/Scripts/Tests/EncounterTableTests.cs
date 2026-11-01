// EncounterTableTests.cs
// feature-spec F-11 적 구성 규칙 테스트 (sprint MVP — 3노드 압축, 보스 제거).
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
        /// <summary>호출 순서대로 결과를 반환하는 IRandom (배열 소진 시 마지막 반복).</summary>
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
            Assert.Throws<ArgumentOutOfRangeException>(() => sut.GenerateEncounter(3));
        }

        // ── 노드 0 (초반): Slime 풀, 1~2체 ──────────────────────────────────

        [Test]
        public void GenerateEncounter_Node0_AllSlimes_Count1()
        {
            // 첫 Next: count=1, 두번째 Next: pool index=0 (Slime)
            var sut = new EncounterTable(new ScriptedRandom(1, 0));
            List<MonsterData> result = sut.GenerateEncounter(0);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SLIME, result[0].Id);
        }

        [Test]
        public void GenerateEncounter_Node0_TwoSlimes()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 0));
            List<MonsterData> result = sut.GenerateEncounter(0);

            Assert.AreEqual(2, result.Count);
            foreach (var m in result)
                Assert.AreEqual(MonsterDatabase.ID_SLIME, m.Id);
        }

        // ── 노드 1 (중반): Slime/Skeleton 풀 ─────────────────────────────────

        [Test]
        public void GenerateEncounter_Node1_PoolIsSlimeOrSkeleton()
        {
            // count=2, pool[0]=Slime, pool[1]=Skeleton
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 1));
            List<MonsterData> result = sut.GenerateEncounter(1);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SLIME,    result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_SKELETON, result[1].Id);
        }

        // ── 노드 2 (후반): Skeleton/Goblin 풀, 2~3체 ─────────────────────────

        [Test]
        public void GenerateEncounter_Node2_PoolIsSkeletonOrGoblin_Count3()
        {
            // count=3, pool[0]=Skeleton, pool[1]=Goblin
            var sut = new EncounterTable(new ScriptedRandom(3, 0, 1, 0));
            List<MonsterData> result = sut.GenerateEncounter(2);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SKELETON, result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_GOBLIN,   result[1].Id);
            Assert.AreEqual(MonsterDatabase.ID_SKELETON, result[2].Id);
        }

        // ── 풀 외 적 미등장 ──────────────────────────────────────────────────

        [Test]
        public void GenerateEncounter_Node0_NeverContainsSkeletonOrGoblin()
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

        // ── 보스(StoneGolem) 미등장 ──────────────────────────────────────────

        [Test]
        public void GenerateEncounter_NoNode_ContainsStoneGolem()
        {
            // sprint MVP: 보스 제거 — 모든 노드에서 StoneGolem이 나오면 안 됨
            var sut = new EncounterTable(new ScriptedRandom(3, 0, 1, 0));

            for (int n = 0; n < RunState.TOTAL_NODES; n++)
            {
                List<MonsterData> result = sut.GenerateEncounter(n);
                foreach (var m in result)
                    Assert.AreNotEqual(MonsterDatabase.ID_STONE_GOLEM, m.Id,
                        $"node {n} contained StoneGolem");
            }
        }
    }
}
