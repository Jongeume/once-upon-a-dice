// EncounterTableTests.cs
// feature-spec F-11 적 구성 규칙 테스트 (Phase D-2 — 1-2-1 + Boss 합류, StoneGolem 부활).
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

        // ── 노드 헬퍼 ────────────────────────────────────────────────────────
        private static MapNode CombatNode(int layer, int column = 0) =>
            new MapNode(id: 100 + layer * 10 + column, type: NodeType.Combat,
                        layer: layer, column: column, nextNodeIds: new[] { 0 });

        private static MapNode BossNode() =>
            new MapNode(id: 999, type: NodeType.Boss,
                        layer: RunMap.LAST_LAYER, column: 0, nextNodeIds: new int[0]);

        // ── 생성자 가드 ──────────────────────────────────────────────────────

        [Test]
        public void Constructor_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EncounterTable(null));
        }

        // ── Combat: layer 0 (시작) — Slime 풀 1~2체 ─────────────────────────

        [Test]
        public void GenerateEncounter_Layer0Combat_AllSlimes_Count1()
        {
            // 첫 Next: count=1, 두번째 Next: pool index=0 (Slime)
            var sut = new EncounterTable(new ScriptedRandom(1, 0));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 0));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SLIME, result[0].Id);
        }

        [Test]
        public void GenerateEncounter_Layer0Combat_TwoSlimes()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 0));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 0));

            Assert.AreEqual(2, result.Count);
            foreach (var m in result)
                Assert.AreEqual(MonsterDatabase.ID_SLIME, m.Id);
        }

        [Test]
        public void GenerateEncounter_Layer0Combat_NeverContainsSkeletonOrGoblin()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 0));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 0));

            foreach (var m in result)
            {
                Assert.AreNotEqual(MonsterDatabase.ID_SKELETON,    m.Id);
                Assert.AreNotEqual(MonsterDatabase.ID_GOBLIN,      m.Id);
                Assert.AreNotEqual(MonsterDatabase.ID_STONE_GOLEM, m.Id);
            }
        }

        // ── Combat: layer 1 (분기) — Slime/Skeleton 풀 1~2체 ────────────────

        [Test]
        public void GenerateEncounter_Layer1Combat_PoolIsSlimeOrSkeleton()
        {
            // count=2, pool[0]=Slime, pool[1]=Skeleton
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 1));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 1, column: 0));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SLIME,    result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_SKELETON, result[1].Id);
        }

        [Test]
        public void GenerateEncounter_Layer1Combat_NeverContainsGoblinOrBoss()
        {
            // sprint MVP — layer 1은 Slime/Skeleton 풀만 (Goblin/StoneGolem 미등장)
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 1));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 1, column: 1));

            foreach (var m in result)
            {
                Assert.AreNotEqual(MonsterDatabase.ID_GOBLIN,      m.Id);
                Assert.AreNotEqual(MonsterDatabase.ID_STONE_GOLEM, m.Id);
            }
        }

        // ── Boss: StoneGolem 1체 고정 ────────────────────────────────────────

        [Test]
        public void GenerateEncounter_BossNode_ReturnsStoneGolemOnly()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));
            List<MonsterData> result = sut.GenerateEncounter(BossNode());

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_STONE_GOLEM, result[0].Id);
        }

        [Test]
        public void GenerateEncounter_BossNode_DoesNotConsumeRandom()
        {
            // Boss는 고정 구성이므로 IRandom 호출 없음. 호출 시 예외나면 검출됨.
            var emptyRandom = new ScriptedRandom(); // 인덱스 초과 시 IndexOutOfRange
            var sut = new EncounterTable(emptyRandom);

            // 예외 없이 통과해야 함
            Assert.DoesNotThrow(() => sut.GenerateEncounter(BossNode()));
        }

        // ── Combat 노드 layer 범위 검증 ──────────────────────────────────────

        [Test]
        public void GenerateEncounter_Layer2Combat_Throws()
        {
            // sprint MVP: layer 2는 Boss 노드만 — Combat 타입은 layer 0/1만 허용
            var sut = new EncounterTable(new ScriptedRandom(0));
            var invalidNode = new MapNode(id: 100, type: NodeType.Combat,
                                          layer: 2, column: 0, nextNodeIds: new int[0]);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => sut.GenerateEncounter(invalidNode));
        }
    }
}
