using System;

namespace Mosquito.Core
{
    public enum Weapon { Hand, Zapper, Incense }
    public enum RunPhase { Running, HatchAfterClear, ReproductionEnded }
    public enum SimEventType { Laid, Hatched, Killed, Missed, Unlocked, Ended }

    [Serializable]
    public sealed class SimulationConfig
    {
        public int tickMs = 50;
        public int startMale = 2, startFemale = 2;
        public int initialLayTicks = 20, layIntervalTicks = 20, firstLayTicks = 20;
        public int hatchTicks = 10, burstTicks = 10, smallBatchLimit = 1024;
        public int handUnlock = 10, zapperUnlock = 100, incenseUnlock = 1000;
        public int handCooldown = 6, zapperCooldown = 16, incenseCooldown = 200;
        public int zapperKills = 10;

        public int Cooldown(Weapon weapon) => weapon == Weapon.Hand ? handCooldown : weapon == Weapon.Zapper ? zapperCooldown : incenseCooldown;
        public int UnlockThreshold(Weapon weapon) => weapon == Weapon.Hand ? handUnlock : weapon == Weapon.Zapper ? zapperUnlock : incenseUnlock;
        public SimulationConfig Copy() => (SimulationConfig)MemberwiseClone();

        public void Validate()
        {
            if (tickMs != 50 || startMale != 2 || startFemale != 2 ||
                initialLayTicks != 20 || layIntervalTicks != 20 || firstLayTicks != 20 ||
                hatchTicks != 10 || burstTicks != 10 || smallBatchLimit != 1024 ||
                handUnlock != 10 || zapperUnlock <= handUnlock || incenseUnlock <= zapperUnlock ||
                handCooldown < 1 || zapperCooldown < 1 || incenseCooldown < 1 || zapperKills != 10)
                throw new ArgumentException("Configuration is incompatible with simulation version 1.");
        }
    }
}
