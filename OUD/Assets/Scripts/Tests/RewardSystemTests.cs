// RewardSystemTests.cs
// feature-spec F-11 보상 수치 규칙 테스트.
// NUnit 기반 (Unity Test Framework).
using System;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;

namespace OUD.Tests
{
    [TestFixture]
    public class RewardSystemTests
    {
        // ── Mock ─────────────────────────────────────────────────────────────
        /// <summary>호출된 (min, max) 인자를 마지막 1회 캡처하는 IRandom.</summary>
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

        // ── XP ───────────────────────────────────────────────────────────────

        [Test]
        public void CalculateReward_Xp_IsAlways1()
        {
            // Arrange
            var rng = new SequenceRandom(8);
            var sut = new RewardSystem(rng);

            // Act
            RewardResult result = sut.CalculateReward();

            // Assert
            Assert.AreEqual(RewardSystem.XP_PER_BATTLE, result.Xp);
            Assert.AreEqual(1, result.Xp);
        }

        // ── 골드 양 끝 ───────────────────────────────────────────────────────

        [Test]
        public void CalculateReward_RandomReturnsMin_GoldIs8()
        {
            // Arrange: rng가 GOLD_MIN(8)을 반환
            var rng = new SequenceRandom(8);
            var sut = new RewardSystem(rng);

            // Act
            RewardResult result = sut.CalculateReward();

            // Assert
            Assert.AreEqual(8, result.Gold);
        }

        [Test]
        public void CalculateReward_RandomReturnsMax_GoldIs12()
        {
            // Arrange: rng가 GOLD_MAX(12)를 반환
            var rng = new SequenceRandom(12);
            var sut = new RewardSystem(rng);

            // Act
            RewardResult result = sut.CalculateReward();

            // Assert: GOLD_MAX 양 끝 포함 — IRandom.Next(8, 13) 호출로 12 가능해야 함
            Assert.AreEqual(12, result.Gold);
        }

        // ── IRandom 호출 인자 검증 (양 끝 포함 보장) ─────────────────────────

        [Test]
        public void CalculateReward_CallsRandomWithRange8To13_Inclusive()
        {
            // Arrange
            var rng = new RangeCapturingRandom(returnValue: 10);
            var sut = new RewardSystem(rng);

            // Act
            sut.CalculateReward();

            // Assert: IRandom.Next는 max exclusive이므로 12 포함 위해 13으로 호출되어야 함
            Assert.AreEqual(1,  rng.CallCount);
            Assert.AreEqual(8,  rng.LastMinInclusive);
            Assert.AreEqual(13, rng.LastMaxExclusive);
        }

        // ── 생성자 가드 ──────────────────────────────────────────────────────

        [Test]
        public void Constructor_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RewardSystem(null));
        }

        // ── 구조 무결성 ──────────────────────────────────────────────────────

        [Test]
        public void RewardResult_Constructor_StoresValues()
        {
            var result = new RewardResult(xp: 1, gold: 10);

            Assert.AreEqual(1,  result.Xp);
            Assert.AreEqual(10, result.Gold);
        }
    }
}
