using System;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;

namespace OUD.Tests
{
    [TestFixture]
    public class RewardSystemTests
    {
        private class RangeCapturingRandom : IRandom
        {
            public int LastMinInclusive;
            public int LastMaxExclusive;
            public int CallCount;
            private readonly int _returnValue;

            public RangeCapturingRandom(int returnValue) { _returnValue = returnValue; }

            public int Next(int minInclusive, int maxExclusive)
            {
                LastMinInclusive = minInclusive;
                LastMaxExclusive = maxExclusive;
                CallCount++;
                return _returnValue;
            }
        }

        [Test]
        public void CalculateReward_Combat_XpIs1()
        {
            var rng = new SequenceRandom(8);
            var sut = new RewardSystem(rng);

            RewardResult result = sut.CalculateReward(NodeType.Combat);

            Assert.AreEqual(1, result.Xp);
        }

        [Test]
        public void CalculateReward_Combat_GoldRange8To12()
        {
            var rng = new RangeCapturingRandom(returnValue: 10);
            var sut = new RewardSystem(rng);

            sut.CalculateReward(NodeType.Combat);

            Assert.AreEqual(8, rng.LastMinInclusive);
            Assert.AreEqual(13, rng.LastMaxExclusive);
        }

        [Test]
        public void CalculateReward_Elite_XpIs2()
        {
            var rng = new SequenceRandom(18);
            var sut = new RewardSystem(rng);

            RewardResult result = sut.CalculateReward(NodeType.Elite);

            Assert.AreEqual(2, result.Xp);
        }

        [Test]
        public void CalculateReward_Elite_GoldRange18To24()
        {
            var rng = new RangeCapturingRandom(returnValue: 20);
            var sut = new RewardSystem(rng);

            sut.CalculateReward(NodeType.Elite);

            Assert.AreEqual(18, rng.LastMinInclusive);
            Assert.AreEqual(25, rng.LastMaxExclusive);
        }

        [Test]
        public void CalculateReward_DefaultParam_IsCombat()
        {
            var rng = new SequenceRandom(8);
            var sut = new RewardSystem(rng);

            RewardResult result = sut.CalculateReward();

            Assert.AreEqual(1, result.Xp);
        }

        [Test]
        public void Constructor_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RewardSystem(null));
        }

        [Test]
        public void RewardResult_Constructor_StoresValues()
        {
            var result = new RewardResult(xp: 2, gold: 20);

            Assert.AreEqual(2, result.Xp);
            Assert.AreEqual(20, result.Gold);
        }
    }
}
