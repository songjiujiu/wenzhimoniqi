using System;

namespace Mosquito.Core
{
    public enum Weapon { Hand, Zapper, Incense }
    public enum RunPhase { Running, HatchAfterClear, ReproductionEnded }
    public enum SimEventType { Laid, Hatched, Killed, Missed, Unlocked, Ended, EggKilled }

    [Serializable]
    public sealed class SimulationConfig
    {
        public int tickMs = 50;
        public int startMale = 2, startFemale = 2;
        public int initialLayTicks = 120, layIntervalTicks = 120, firstLayTicks = 120;
        public int hatchTicks = 60, burstTicks = 60, smallBatchLimit = 1024;
        public int handUnlock = 10, zapperUnlock = 100, incenseUnlock = 1000;
        public int handCooldown = 6, zapperCooldown = 16, incenseCooldown = 200;
        public int zapperKills = 10;

        public int Cooldown(Weapon weapon) => weapon == Weapon.Hand ? handCooldown : weapon == Weapon.Zapper ? zapperCooldown : incenseCooldown;
        public int UnlockThreshold(Weapon weapon) => weapon == Weapon.Hand ? handUnlock : weapon == Weapon.Zapper ? zapperUnlock : incenseUnlock;
        public SimulationConfig Copy() => (SimulationConfig)MemberwiseClone();

        public void Validate()
        {
            bool legacyPacing = initialLayTicks == 20 && layIntervalTicks == 20 && firstLayTicks == 20 && hatchTicks == 10 && burstTicks == 10;
            bool currentPacing = initialLayTicks == 40 && layIntervalTicks == 40 && firstLayTicks == 40 && hatchTicks == 20 && burstTicks == 20;
            bool slowPacing = initialLayTicks == 120 && layIntervalTicks == 120 && firstLayTicks == 120 && hatchTicks == 60 && burstTicks == 60;
            if (tickMs != 50 || startMale != 2 || startFemale != 2 ||
                !(legacyPacing || currentPacing || slowPacing) || smallBatchLimit != 1024 ||
                handUnlock != 10 || zapperUnlock <= handUnlock || incenseUnlock <= zapperUnlock ||
                handCooldown < 1 || zapperCooldown < 1 || incenseCooldown < 1 || zapperKills != 10)
                throw new ArgumentException("Configuration is incompatible with simulation version 1.");
        }
    }
}
