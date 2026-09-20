using UnityEngine;
using Mosquito.Core;

namespace Mosquito.Runtime
{
    [CreateAssetMenu(menuName = "Mosquito/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        public SimulationConfig simulation = new SimulationConfig();
        [Range(100, 300)] public int visualLimit = 300;
        [Range(0, 1)] public float masterVolume = 0.65f;
    }
}
