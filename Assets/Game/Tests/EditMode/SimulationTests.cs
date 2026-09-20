using System;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using UnityEngine;
using Mosquito.Core;
using Mosquito.Runtime;

namespace Mosquito.Tests
{
    public sealed class SimulationTests
    {
        private static void Advance(Simulation game, int ticks) { for (int i = 0; i < ticks; i++) { game.Step(); game.Validate(); } }
        private static Simulation Fixture(BigInteger male, BigInteger female, BigInteger eggs, int eggDelay = 10, int layDelay = 20, ulong rng = 1)
        {
            var snapshot = new Simulation().Capture(); snapshot.tick = 100; snapshot.testRun = true;
            snapshot.randomState = rng.ToString("X16"); snapshot.male = male.ToString();
            snapshot.females.Clear(); snapshot.eggs.Clear();
            if (female > 0) snapshot.females.Add(new BucketSnapshot(100 + layDelay, female.ToString()));
            if (eggs > 0) snapshot.eggs.Add(new BucketSnapshot(100 + eggDelay, eggs.ToString()));
            snapshot.totalHatched = (male + female).ToString(); snapshot.femaleBorn = female.ToString();
            snapshot.totalKilled = "4"; snapshot.femaleKilled = "2"; snapshot.eggsCreated = (male + female + eggs).ToString();
            snapshot.maxAdult = BigInteger.Max(1000, male + female).ToString(); snapshot.maxFemale = BigInteger.Max(2, female).ToString(); snapshot.maxEgg = eggs.ToString();
            snapshot.unlockFlags = 7; snapshot.phase = female == 0 && eggs == 0 ? RunPhase.ReproductionEnded : RunPhase.Running;
            return Simulation.Restore(snapshot);
        }

        [Test] public void InitialMothersLayAtOneSecond_ThenEggsHatchAtOneAndHalf()
        {
            var game = new Simulation(seed: 7); Advance(game, 19);
            Assert.That(game.Eggs, Is.EqualTo(BigInteger.Zero)); game.Step();
            Assert.That(game.Adults, Is.EqualTo(new BigInteger(4))); Assert.That(game.Eggs, Is.EqualTo(new BigInteger(2)));
            Assert.That(game.EggBuckets.Keys.Single(), Is.EqualTo(30)); Advance(game, 10);
            Assert.That(game.Adults, Is.EqualTo(new BigInteger(6))); Assert.That(game.TotalHatched, Is.EqualTo(new BigInteger(2)));
            Assert.That(game.FemaleBuckets.Keys.All(k => k == 40 || k == 50), Is.True);
        }
        [Test] public void AttackBeforeDueLayCancelsMotherButPreservesExistingEggs()
        {
            var game = Fixture(0, 1, 3, eggDelay: 10, layDelay: 1); var before = game.EggsCreated;
            game.Step(Weapon.Hand); game.Validate();
            Assert.That(game.Female, Is.EqualTo(BigInteger.Zero)); Assert.That(game.EggsCreated, Is.EqualTo(before));
            Assert.That(game.Eggs, Is.EqualTo(new BigInteger(3))); Advance(game, 9);
            Assert.That(game.TotalHatched, Is.EqualTo(new BigInteger(4)));
        }
        [Test] public void HandMissConsumesCooldownButDoesNotKill()
        {
            var game = Fixture(998, 2, 0, rng: 2); game.Step(Weapon.Hand);
            Assert.That(game.Adults, Is.EqualTo(new BigInteger(1000))); Assert.That(game.HandAttempts, Is.EqualTo(1));
            Assert.That(game.HandHits, Is.Zero); Assert.That(game.RemainingCooldown(Weapon.Hand), Is.EqualTo(6));
        }
        [Test] public void CooldownBoundaryAndIndependentWeapons()
        {
            var game = Fixture(998, 2, 10); game.Step(Weapon.Hand); long attempts = game.HandAttempts;
            for (int i = 0; i < 5; i++) game.Step(Weapon.Hand);
            Assert.That(game.HandAttempts, Is.EqualTo(attempts)); game.Step(Weapon.Hand);
            Assert.That(game.HandAttempts, Is.EqualTo(attempts + 1)); game.Step(Weapon.Zapper);
            Assert.That(game.ZapperUses, Is.EqualTo(1)); game.Validate();
        }
        [Test] public void ZapperKillsOnlyAvailableAdultsAndEndsReproduction()
        {
            var game = Fixture(4, 2, 0); game.Step(Weapon.Zapper); game.Validate();
            Assert.That(game.Adults, Is.EqualTo(BigInteger.Zero)); Assert.That(game.TotalKilled, Is.EqualTo(new BigInteger(10)));
            Assert.That(game.Phase, Is.EqualTo(RunPhase.ReproductionEnded));
        }
        [Test] public void IncensePreservesDeadlinesAndAllEggsHatchExactlyOnce()
        {
            var game = Fixture(998, 2, 30, 6); long due = game.EggBuckets.Keys.Single();
            game.Step(Weapon.Incense); game.Validate();
            Assert.That(game.Adults, Is.EqualTo(BigInteger.Zero)); Assert.That(game.Eggs, Is.EqualTo(new BigInteger(30)));
            Assert.That(game.EggBuckets.Keys.Single(), Is.EqualTo(due)); Assert.That(game.FemaleBuckets, Is.Empty);
            Assert.That(game.RemainingCooldown(Weapon.Incense), Is.EqualTo(200)); Advance(game, 5);
            Assert.That(game.Adults, Is.EqualTo(new BigInteger(30))); Assert.That(game.BurstHatched, Is.EqualTo(new BigInteger(30)));
            Advance(game, 5); Assert.That(game.Phase, Is.EqualTo(RunPhase.Running));
        }
        [Test] public void SameTickIncenseDoesNotKillNewborns()
        {
            var game = Fixture(998, 2, 2, 1, 1); var created = game.EggsCreated;
            game.Step(Weapon.Incense); game.Validate();
            Assert.That(game.Adults, Is.EqualTo(new BigInteger(2))); Assert.That(game.EggsCreated, Is.EqualTo(created));
            Assert.That(game.Events.First(e => e.Type == SimEventType.Killed).Sequence,
                Is.LessThan(game.Events.First(e => e.Type == SimEventType.Hatched).Sequence));
        }
        [Test] public void EmptyAttackDoesNotAdvanceRngOrCooldown()
        {
            var game = Fixture(0, 0, 10); string rng = game.Capture().randomState;
            game.Step(Weapon.Incense);
            Assert.That(game.Capture().randomState, Is.EqualTo(rng)); Assert.That(game.IncenseUses, Is.Zero);
            Assert.That(game.RemainingCooldown(Weapon.Incense), Is.Zero);
        }
        [Test] public void NoEggClearEndsWithoutHiddenRepopulation()
        {
            var game = Fixture(998, 2, 0); game.Step(Weapon.Incense); long tick = game.Tick;
            Advance(game, 500); Assert.That(game.Tick, Is.EqualTo(tick)); Assert.That(game.Adults, Is.EqualTo(BigInteger.Zero));
        }
        [Test] public void AllMaleFinalHatchEndsOnlyAfterHatching()
        {
            var game = Fixture(1, 0, 1, eggDelay: 1, rng: 2);
            Assert.That(game.Phase, Is.EqualTo(RunPhase.Running)); game.Step(); game.Validate();
            Assert.That(game.Phase, Is.EqualTo(RunPhase.ReproductionEnded)); Assert.That(game.Male, Is.EqualTo(new BigInteger(2)));
        }
        [Test] public void UnlocksPersistAfterPopulationFallsButResetOnNewRun()
        {
            var game = new Simulation(seed: 73); Advance(game, 420);
            Assert.That(game.IsUnlocked(Weapon.Incense), Is.True); game.Step(Weapon.Incense);
            Assert.That(game.IsUnlocked(Weapon.Incense), Is.True); Assert.That(new Simulation().UnlockFlags, Is.Zero);
        }
        [TestCase(1024)] [TestCase(1025)] [TestCase(1026)]
        public void SexSplitConservesPopulationAtBoundary(int count)
        {
            var game = Fixture(0, 0, count, eggDelay: 1); game.Step(); game.Validate();
            Assert.That(game.Adults, Is.EqualTo(new BigInteger(count)));
            if (count > 1024) Assert.That(BigInteger.Abs(game.Male - game.Female), Is.LessThanOrEqualTo(BigInteger.One));
        }
        [Test] public void HugeCountsKeepUnitPrecision()
        {
            BigInteger huge = BigInteger.Pow(10, 300); var game = Fixture(huge, huge, huge);
            game.Step(Weapon.Hand); Assert.That(game.Adults, Is.EqualTo(huge * 2 - 1));
            game.Step(Weapon.Zapper); Assert.That(game.Adults, Is.EqualTo(huge * 2 - 11)); game.Validate();
            game.Step(Weapon.Incense); Assert.That(game.Adults, Is.EqualTo(BigInteger.Zero)); Assert.That(game.Eggs, Is.EqualTo(huge));
        }
        [Test] public void SaveResumeKeepsRngAndBucketOrdering()
        {
            var uninterrupted = new Simulation(seed: 31); Advance(uninterrupted, 210);
            var snapshot = uninterrupted.Capture(); snapshot.females.Reverse();
            var restored = Simulation.Restore(snapshot);
            for (int i = 0; i < 200; i++)
            {
                Weapon? command = i % 7 == 0 ? Weapon.Hand : i % 17 == 0 ? Weapon.Zapper : (Weapon?)null;
                uninterrupted.Step(command); restored.Step(command); uninterrupted.Validate(); restored.Validate();
            }
            Assert.That(JsonUtility.ToJson(restored.Capture()), Is.EqualTo(JsonUtility.ToJson(uninterrupted.Capture())));
        }
        [Test] public void SaveDuringReboundDoesNotDuplicateHatches()
        {
            var a = Fixture(998, 2, 50, eggDelay: 6); a.Step(Weapon.Incense);
            var b = Simulation.Restore(a.Capture()); Advance(a, 25); Advance(b, 25);
            Assert.That(JsonUtility.ToJson(a.Capture()), Is.EqualTo(JsonUtility.ToJson(b.Capture())));
        }
        [Test] public void ManyOperationsPreservePopulationAndBoundedBucketCount()
        {
            var game = new Simulation(seed: 85);
            for (int i = 0; i < 6000 && game.Phase != RunPhase.ReproductionEnded; i++)
            {
                game.Step(i % 221 == 0 ? Weapon.Incense : i % 17 == 0 ? Weapon.Zapper : (Weapon?)null); game.Validate();
                Assert.That(game.FemaleBuckets.Count, Is.LessThanOrEqualTo(20)); Assert.That(game.EggBuckets.Count, Is.LessThanOrEqualTo(10));
            }
        }
        [Test] public void InvalidConservationAndExpiredBucketsAreRejected()
        {
            var snapshot = new Simulation().Capture(); snapshot.male = "12";
            Assert.Throws<InvalidOperationException>(() => Simulation.Restore(snapshot));
            snapshot = new Simulation().Capture(); snapshot.females[0].dueTick = 0;
            Assert.Throws<InvalidOperationException>(() => Simulation.Restore(snapshot));
        }
        [Test] public void RandomHasStableKnownVectorAndHalfOutcomes()
        {
            var rng = new DeterministicRandom(1); Assert.That(rng.NextUInt64(), Is.EqualTo(5180492295206395165UL));
            int girls = 0; for (int i = 0; i < 10000; i++) if (rng.NextBool()) girls++;
            Assert.That(girls, Is.InRange(4700, 5300));
        }
        [Test] public void RandomBigIntegerRankNeverExceedsRange()
        {
            var rng = new DeterministicRandom(42);
            foreach (var max in new[] { new BigInteger(3), new BigInteger(256), BigInteger.Pow(10, 300) })
                for (int i = 0; i < 100; i++) { var rank = rng.Below(max); Assert.That(rank >= 0 && rank < max, Is.True); }
        }
        [TestCase("999", "999")] [TestCase("1000", "1.00K")] [TestCase("999999", "1.00M")]
        [TestCase("12345", "12.3K")] [TestCase("1000000000000000", "1.00e15")]
        public void FormatsAtMagnitudeBoundaries(string value, string expected)
            => Assert.That(CountFormatter.Format(BigInteger.Parse(value)), Is.EqualTo(expected));

        [Test] public void AtomicSaveBackupAndPausedBacklogRoundTrip()
        {
            string testRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/save-tests"));
            string directory = Path.Combine(testRoot, Guid.NewGuid().ToString("N"));
            try
            {
                var service = new SaveService(directory);
                var game = new Simulation(); Advance(game, 100);
                var profile = new PlayerProfile { run = game.Capture(), pendingCatchupTicks = 24 };
                service.Save(profile); game.Step(); profile.run = game.Capture(); service.Save(profile);
                var loaded = service.Load(out var notice);
                Assert.That(notice, Is.Null); Assert.That(loaded.pendingCatchupTicks, Is.EqualTo(24)); Assert.That(loaded.run.tick, Is.EqualTo(101));
                File.WriteAllText(service.Path, "corrupt"); loaded = service.Load(out notice);
                Assert.That(notice, Does.Contain("备份")); Assert.That(loaded.run.tick, Is.EqualTo(100));
                service.Save(loaded); Assert.That(service.Load(out _).run.tick, Is.EqualTo(100));
            }
            finally
            {
                string resolved = Path.GetFullPath(directory);
                if (!resolved.StartsWith(testRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Cleanup target escaped the test workspace.");
                if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            }
        }
        [Test] public void RecordsMergeByMaximumNotBySummingRepeatedSaves()
        {
            var game = new Simulation(seed: 15); Advance(game, 400); game.Step(Weapon.Incense);
            var profile = new PlayerProfile(); profile.MergeRecords(game); string best = profile.bestRunKills;
            profile.MergeRecords(game); Assert.That(profile.bestRunKills, Is.EqualTo(best));
        }
    }
}
