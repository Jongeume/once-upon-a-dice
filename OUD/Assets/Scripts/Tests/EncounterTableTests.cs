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

        private static MapNode CombatNode(int layer, int column = 0) =>
            new MapNode(id: 100 + layer * 10 + column, type: NodeType.Combat,
                        layer: layer, column: column, nextNodeIds: new[] { 0 });

        private static MapNode BossNode() =>
            new MapNode(id: 12, type: NodeType.Boss,
                        layer: RunMap.LAST_LAYER, column: 0, nextNodeIds: new int[0]);

        private static MapNode EliteNode() =>
            new MapNode(id: 10, type: NodeType.Elite,
                        layer: 6, column: 0, nextNodeIds: new[] { 11 });

        private static MapNode ShopNode() =>
            new MapNode(id: 5, type: NodeType.Shop,
                        layer: 3, column: 0, nextNodeIds: new[] { 6 });

        [Test]
        public void Constructor_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EncounterTable(null));
        }

        // ── Layer 0: Spider only ──────────────────────────────────────

        [Test]
        public void Layer0Combat_OneSpider()
        {
            var sut = new EncounterTable(new ScriptedRandom(1, 0));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 0));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SPIDER, result[0].Id);
        }

        [Test]
        public void Layer0Combat_TwoSpiders()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 0));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 0));

            Assert.AreEqual(2, result.Count);
            foreach (var m in result)
                Assert.AreEqual(MonsterDatabase.ID_SPIDER, m.Id);
        }

        // ── Layer 1: Spider / Snake ───────────────────────────────────

        [Test]
        public void Layer1Combat_SpiderAndSnake()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 1));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 1));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SPIDER, result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_SNAKE,  result[1].Id);
        }

        // ── Layer 2: Spider / Snake (X자 교차 분기) ───────────────────

        [Test]
        public void Layer2Combat_SpiderAndSnake()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 1));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 2));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SPIDER, result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_SNAKE,  result[1].Id);
        }

        // ── Layer 3: Shop — Combat 불가 ───────────────────────────────

        [Test]
        public void Layer3Combat_Throws()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));
            var invalidNode = new MapNode(id: 100, type: NodeType.Combat,
                                          layer: 3, column: 0, nextNodeIds: new int[0]);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => sut.GenerateEncounter(invalidNode));
        }

        // ── Layer 4: Snake / Bear ─────────────────────────────────────

        [Test]
        public void Layer4Combat_SnakeOrBear()
        {
            var sut = new EncounterTable(new ScriptedRandom(2, 0, 1));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 4));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SNAKE, result[0].Id);
            Assert.AreEqual(MonsterDatabase.ID_BEAR,  result[1].Id);
        }

        // ── Layer 5: Snake / Bear (X자 교차 분기) ────────────────────

        [Test]
        public void Layer5Combat_SnakeOrBear()
        {
            var sut = new EncounterTable(new ScriptedRandom(1, 0));
            List<MonsterData> result = sut.GenerateEncounter(CombatNode(layer: 5));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_SNAKE, result[0].Id);
        }

        // ── Boss: StoneGolem ───────────────────────────────────────────

        [Test]
        public void BossNode_ReturnsStoneGolem()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));
            List<MonsterData> result = sut.GenerateEncounter(BossNode());

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_STONE_GOLEM, result[0].Id);
        }

        // ── Elite: EliteGolem ──────────────────────────────────────────

        [Test]
        public void EliteNode_ReturnsEliteGolem()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));
            List<MonsterData> result = sut.GenerateEncounter(EliteNode());

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_ELITE_GOLEM, result[0].Id);
        }

        // ── Shop: Throws ──────────────────────────────────────────────

        [Test]
        public void ShopNode_Throws()
        {
            var sut = new EncounterTable(new ScriptedRandom(0));
            Assert.Throws<InvalidOperationException>(() => sut.GenerateEncounter(ShopNode()));
        }
    }
}
