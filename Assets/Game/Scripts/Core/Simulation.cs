using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Mosquito.Core
{
    public readonly struct SimEvent
    {
        public readonly SimEventType Type;
        public readonly Weapon Weapon;
        public readonly BigInteger Count, FemaleCount;
        public readonly long Tick, Sequence;
        public SimEvent(SimEventType type, Weapon weapon, BigInteger count, BigInteger female, long tick, long sequence)
        { Type = type; Weapon = weapon; Count = count; FemaleCount = female; Tick = tick; Sequence = sequence; }
    }

    public sealed class Simulation
    {
        private readonly SortedDictionary<long, BigInteger> females = new SortedDictionary<long, BigInteger>();
        private readonly SortedDictionary<long, BigInteger> eggs = new SortedDictionary<long, BigInteger>();
        private readonly Queue<BigInteger> recentLays = new Queue<BigInteger>();
        private readonly long[] readyAt = new long[3];
        private readonly List<SimEvent> events = new List<SimEvent>(16);
        private DeterministicRandom random;
        private long eventSequence;
        private ulong initialSeed;

        public SimulationConfig Config { get; }
        public string RunId { get; private set; }
        public bool TestRun { get; private set; }
        public long Tick { get; private set; }
        public RunPhase Phase { get; private set; }
        public long BurstEndTick { get; private set; }
        public int UnlockFlags { get; private set; }
        public BigInteger Male { get; private set; }
        public BigInteger Female { get; private set; }
        public BigInteger Eggs { get; private set; }
        public BigInteger Adults => Male + Female;
        public BigInteger MaxAdult { get; private set; }
        public BigInteger MaxEgg { get; private set; }
        public BigInteger MaxFemale { get; private set; }
        public BigInteger TotalKilled { get; private set; }
        public BigInteger FemaleKilled { get; private set; }
        public BigInteger TotalHatched { get; private set; }
        public BigInteger FemaleBorn { get; private set; }
        public BigInteger EggsCreated { get; private set; }
        public BigInteger BestClear { get; private set; }
        public BigInteger BurstInitialEggs { get; private set; }
        public BigInteger BurstHatched { get; private set; }
        public long HandAttempts { get; private set; }
        public long HandHits { get; private set; }
        public long ZapperUses { get; private set; }
        public long IncenseUses { get; private set; }
        public IReadOnlyList<SimEvent> Events => events;
        public IReadOnlyDictionary<long, BigInteger> FemaleBuckets => females;
        public IReadOnlyDictionary<long, BigInteger> EggBuckets => eggs;
        public BigInteger LastSecondLaid => recentLays.Aggregate(BigInteger.Zero, (sum, n) => sum + n);

        public Simulation(SimulationConfig config = null, ulong seed = 1)
        {
            Config = (config ?? new SimulationConfig()).Copy(); Config.Validate();
            RunId = Guid.NewGuid().ToString("N"); initialSeed = seed;
            random = new DeterministicRandom(seed);
            Male = Config.startMale; Female = Config.startFemale;
            Add(females, Config.initialLayTicks, Female);
            MaxAdult = Adults; MaxFemale = Female;
            for (int i = 0; i < 20; i++) recentLays.Enqueue(BigInteger.Zero);
        }

        public bool IsUnlocked(Weapon weapon) => (UnlockFlags & (1 << (int)weapon)) != 0;
        public long RemainingCooldown(Weapon weapon) => Math.Max(0, readyAt[(int)weapon] - Tick);
        public bool CanUse(Weapon weapon) => Phase != RunPhase.ReproductionEnded && Adults > 0 &&
            IsUnlocked(weapon) && RemainingCooldown(weapon) == 0 &&
            !(weapon == Weapon.Incense && Phase == RunPhase.HatchAfterClear);

        public void Step(Weapon? command = null)
        {
            events.Clear();
            if (Phase == RunPhase.ReproductionEnded) return;
            Tick = checked(Tick + 1);
            if (command.HasValue) Attack(command.Value);

            var laid = Pop(females, Tick);
            if (laid > 0)
            {
                Add(females, checked(Tick + Config.layIntervalTicks), laid);
                Add(eggs, checked(Tick + Config.hatchTicks), laid);
                Eggs += laid; EggsCreated += laid;
                Emit(SimEventType.Laid, count: laid); UpdateProgress();
            }
            recentLays.Dequeue(); recentLays.Enqueue(laid);

            var hatched = Pop(eggs, Tick);
            if (hatched > 0)
            {
                var girls = SplitFemales(hatched);
                Eggs -= hatched; Male += hatched - girls; Female += girls;
                TotalHatched += hatched; FemaleBorn += girls;
                Add(females, checked(Tick + Config.firstLayTicks), girls);
                if (Phase == RunPhase.HatchAfterClear) BurstHatched += hatched;
                Emit(SimEventType.Hatched, count: hatched, female: girls); UpdateProgress();
            }

            if (Female == 0 && Eggs == 0)
            {
                Phase = RunPhase.ReproductionEnded; Emit(SimEventType.Ended);
            }
            else if (Phase == RunPhase.HatchAfterClear && Tick >= BurstEndTick) Phase = RunPhase.Running;
        }

        private void Attack(Weapon weapon)
        {
            if (!Enum.IsDefined(typeof(Weapon), weapon) || !CanUse(weapon)) return;
            readyAt[(int)weapon] = checked(Tick + Config.Cooldown(weapon));
            if (weapon == Weapon.Hand)
            {
                HandAttempts++;
                if (!random.NextBool()) { Emit(SimEventType.Missed, weapon); return; }
                HandHits++;
            }
            if (weapon == Weapon.Zapper) ZapperUses++;

            BigInteger killed, girls;
            if (weapon == Weapon.Incense)
            {
                killed = Adults; girls = Female;
                Male = Female = BigInteger.Zero; females.Clear(); IncenseUses++;
                BestClear = BigInteger.Max(BestClear, killed);
                BurstInitialEggs = Eggs; BurstHatched = BigInteger.Zero;
                if (Eggs > 0) { Phase = RunPhase.HatchAfterClear; BurstEndTick = checked(Tick + Config.burstTicks); }
            }
            else
            {
                int target = (int)BigInteger.Min(weapon == Weapon.Hand ? 1 : Config.zapperKills, Adults);
                killed = target; girls = 0;
                for (int i = 0; i < target; i++)
                {
                    var rank = random.Below(Adults);
                    if (rank < Male) { Male--; continue; }
                    rank -= Male;
                    long chosen = -1;
                    foreach (var bucket in females)
                    {
                        if (rank < bucket.Value) { chosen = bucket.Key; break; }
                        rank -= bucket.Value;
                    }
                    if (chosen < 0) throw new InvalidOperationException("No female bucket for sampled target.");
                    var remaining = females[chosen] - 1;
                    if (remaining == 0) females.Remove(chosen); else females[chosen] = remaining;
                    Female--; girls++;
                }
            }
            TotalKilled += killed; FemaleKilled += girls;
            Emit(SimEventType.Killed, weapon, killed, girls); UpdateProgress();
        }

        private BigInteger SplitFemales(BigInteger count)
        {
            if (count <= Config.smallBatchLimit)
            {
                int girls = 0;
                for (int i = 0; i < (int)count; i++) if (random.NextBool()) girls++;
                return girls;
            }
            return count / 2 + (!count.IsEven && random.NextBool() ? 1 : 0);
        }

        private void UpdateProgress()
        {
            MaxAdult = BigInteger.Max(MaxAdult, Adults);
            MaxEgg = BigInteger.Max(MaxEgg, Eggs);
            MaxFemale = BigInteger.Max(MaxFemale, Female);
            for (int i = 0; i < 3; i++)
            {
                var weapon = (Weapon)i;
                if (!IsUnlocked(weapon) && MaxAdult >= Config.UnlockThreshold(weapon))
                { UnlockFlags |= 1 << i; Emit(SimEventType.Unlocked, weapon); }
            }
        }

        private void Emit(SimEventType type, Weapon weapon = Weapon.Hand, BigInteger count = default, BigInteger female = default)
            => events.Add(new SimEvent(type, weapon, count, female, Tick, ++eventSequence));
        private static void Add(SortedDictionary<long, BigInteger> buckets, long due, BigInteger count)
        {
            if (count == 0) return;
            buckets.TryGetValue(due, out var old); buckets[due] = old + count;
        }
        private static BigInteger Pop(SortedDictionary<long, BigInteger> buckets, long due)
        {
            if (!buckets.TryGetValue(due, out var count)) return BigInteger.Zero;
            buckets.Remove(due); return count;
        }

        public void Validate()
        {
            if (Male < 0 || Female < 0 || Eggs < 0 || TotalKilled < 0 || FemaleKilled < 0 ||
                TotalHatched < 0 || FemaleBorn < 0 || EggsCreated < 0 || BestClear < 0 ||
                FemaleBorn > TotalHatched || FemaleKilled > TotalKilled || MaxAdult < Adults ||
                MaxFemale < Female || MaxEgg < Eggs || BestClear > TotalKilled ||
                Female != females.Values.Aggregate(BigInteger.Zero, (a, b) => a + b) ||
                Eggs != eggs.Values.Aggregate(BigInteger.Zero, (a, b) => a + b) ||
                Adults != 4 + TotalHatched - TotalKilled || Eggs != EggsCreated - TotalHatched ||
                Female != 2 + FemaleBorn - FemaleKilled ||
                Male != 2 + (TotalHatched - FemaleBorn) - (TotalKilled - FemaleKilled))
                throw new InvalidOperationException("Population conservation failed.");
            foreach (var b in females) if (b.Key <= Tick || b.Key > Tick + 20 || b.Value <= 0) throw new InvalidOperationException("Invalid female deadline.");
            foreach (var b in eggs) if (b.Key <= Tick || b.Key > Tick + 10 || b.Value <= 0) throw new InvalidOperationException("Invalid egg deadline.");
            if (!Enum.IsDefined(typeof(RunPhase), Phase) || Tick < 0 || eventSequence < 0 ||
                (Phase == RunPhase.ReproductionEnded) != (Female == 0 && Eggs == 0) ||
                HandAttempts < 0 || HandHits < 0 || HandHits > HandAttempts || ZapperUses < 0 || IncenseUses < 0 ||
                BurstInitialEggs < 0 || BurstHatched < 0 || BurstHatched > BurstInitialEggs)
                throw new InvalidOperationException("Invalid run state.");
            if (Phase == RunPhase.HatchAfterClear && (BurstEndTick <= Tick || BurstEndTick > Tick + Config.burstTicks))
                throw new InvalidOperationException("Invalid rebound deadline.");
            int flags = 0;
            for (int i = 0; i < 3; i++)
            {
                if (MaxAdult >= Config.UnlockThreshold((Weapon)i)) flags |= 1 << i;
                if (readyAt[i] < 0 || readyAt[i] > Tick + Config.Cooldown((Weapon)i)) throw new InvalidOperationException("Invalid cooldown.");
            }
            if (UnlockFlags != flags || recentLays.Count != 20 || recentLays.Any(n => n < 0)) throw new InvalidOperationException("Invalid progress data.");
        }

        public SimulationSnapshot Capture()
        {
            return new SimulationSnapshot
            {
                config = Config.Copy(), runId = RunId, initialSeed = initialSeed.ToString("X16"), randomState = random.State.ToString("X16"),
                tick = Tick, phase = Phase, burstEndTick = BurstEndTick, eventSequence = eventSequence, unlockFlags = UnlockFlags, testRun = TestRun,
                male = S(Male), maxAdult = S(MaxAdult), maxEgg = S(MaxEgg), maxFemale = S(MaxFemale),
                totalKilled = S(TotalKilled), femaleKilled = S(FemaleKilled), totalHatched = S(TotalHatched), femaleBorn = S(FemaleBorn),
                eggsCreated = S(EggsCreated), bestClear = S(BestClear), burstInitialEggs = S(BurstInitialEggs), burstHatched = S(BurstHatched),
                handAttempts = HandAttempts, handHits = HandHits, zapperUses = ZapperUses, incenseUses = IncenseUses,
                readyAt = (long[])readyAt.Clone(),
                females = females.Select(b => new BucketSnapshot(b.Key, S(b.Value))).ToList(),
                eggs = eggs.Select(b => new BucketSnapshot(b.Key, S(b.Value))).ToList(),
                recentLays = recentLays.Select(S).ToArray()
            };
        }

        public static Simulation Restore(SimulationSnapshot saved)
        {
            if (saved == null || saved.simulationVersion != 1 || saved.config == null ||
                saved.readyAt == null || saved.readyAt.Length != 3 || saved.recentLays == null || saved.recentLays.Length != 20 ||
                saved.females == null || saved.eggs == null || string.IsNullOrEmpty(saved.runId) || saved.females.Count > 20 || saved.eggs.Count > 10)
                throw new ArgumentException("Unsupported or incomplete save.");
            var game = new Simulation(saved.config, ulong.Parse(saved.initialSeed, NumberStyles.HexNumber));
            ulong rng = ulong.Parse(saved.randomState, NumberStyles.HexNumber);
            if (rng == 0) throw new ArgumentException("Invalid RNG state.");
            game.random = new DeterministicRandom(rng);
            game.RunId = saved.runId; game.TestRun = saved.testRun; game.Tick = saved.tick;
            game.Phase = saved.phase; game.BurstEndTick = saved.burstEndTick; game.eventSequence = saved.eventSequence; game.UnlockFlags = saved.unlockFlags;
            game.Male = N(saved.male); game.MaxAdult = N(saved.maxAdult); game.MaxEgg = N(saved.maxEgg); game.MaxFemale = N(saved.maxFemale);
            game.TotalKilled = N(saved.totalKilled); game.FemaleKilled = N(saved.femaleKilled); game.TotalHatched = N(saved.totalHatched);
            game.FemaleBorn = N(saved.femaleBorn); game.EggsCreated = N(saved.eggsCreated); game.BestClear = N(saved.bestClear);
            game.BurstInitialEggs = N(saved.burstInitialEggs); game.BurstHatched = N(saved.burstHatched);
            game.HandAttempts = saved.handAttempts; game.HandHits = saved.handHits; game.ZapperUses = saved.zapperUses; game.IncenseUses = saved.incenseUses;
            Array.Copy(saved.readyAt, game.readyAt, 3);
            game.females.Clear(); game.eggs.Clear(); game.recentLays.Clear();
            foreach (var b in saved.females) game.females.Add(b.dueTick, N(b.count));
            foreach (var b in saved.eggs) game.eggs.Add(b.dueTick, N(b.count));
            foreach (var n in saved.recentLays) game.recentLays.Enqueue(N(n));
            game.Female = game.females.Values.Aggregate(BigInteger.Zero, (a, b) => a + b);
            game.Eggs = game.eggs.Values.Aggregate(BigInteger.Zero, (a, b) => a + b);
            game.Validate(); return game;
        }

        private static string S(BigInteger number) => number.ToString(CultureInfo.InvariantCulture);
        private static BigInteger N(string number)
        {
            if (string.IsNullOrEmpty(number) || number.Length > 100000) throw new ArgumentException("Invalid number length.");
            return BigInteger.Parse(number, NumberStyles.None, CultureInfo.InvariantCulture);
        }
    }
}
