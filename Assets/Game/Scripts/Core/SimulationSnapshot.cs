using System;
using System.Collections.Generic;

namespace Mosquito.Core
{
    [Serializable]
    public sealed class BucketSnapshot
    {
        public long dueTick;
        public string count;
        public BucketSnapshot(long due, string amount) { dueTick = due; count = amount; }
    }

    [Serializable]
    public sealed class SimulationSnapshot
    {
        public int simulationVersion = 1;
        public SimulationConfig config;
        public string runId, initialSeed, randomState;
        public long tick, burstEndTick, eventSequence;
        public RunPhase phase;
        public int unlockFlags;
        public bool testRun;
        public string male, maxAdult, maxEgg, maxFemale;
        public string totalKilled, femaleKilled, totalHatched, femaleBorn, eggsCreated, bestClear;
        public string burstInitialEggs, burstHatched;
        public string eggsKilled = "0";
        public long handAttempts, handHits, zapperUses, incenseUses;
        public long[] readyAt;
        public List<BucketSnapshot> females, eggs;
        public string[] recentLays;
    }
}
