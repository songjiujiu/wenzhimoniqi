using System;
using UnityEngine;
using Mosquito.Core;

namespace Mosquito.Runtime
{
    public sealed class GameAudio : MonoBehaviour
    {
        private AudioSource buzz, effects;
        private AudioClip hand, miss, zapper, incense, unlock;
        public void Initialize()
        {
            buzz = gameObject.AddComponent<AudioSource>(); effects = gameObject.AddComponent<AudioSource>();
            buzz.loop = true; buzz.spatialBlend = effects.spatialBlend = 0;
            buzz.clip = Tone("Soft mosquito ambience", 1, 220, .12f, false);
            buzz.volume = 0; buzz.Play();
            hand = Tone("Palm impact", .12f, 110, .7f, true);
            miss = Tone("Palm miss", .10f, 350, .15f, true);
            zapper = Tone("Electric snap", .20f, 880, .5f, true);
            incense = Tone("Incense sweep", .55f, 75, .4f, true);
            unlock = Tone("Unlock", .25f, 660, .22f, false);
        }
        public void SetBuzz(float intensity, float master, float level)
        { buzz.volume = Mathf.Lerp(buzz.volume, intensity * level, Time.unscaledDeltaTime * 5); buzz.pitch = 1 + intensity * .35f; }
        public void PlayAttack(Weapon weapon, bool hit) => effects.PlayOneShot(!hit ? miss : weapon == Weapon.Hand ? hand : weapon == Weapon.Zapper ? zapper : incense, .45f);
        public void PlayUnlock() => effects.PlayOneShot(unlock, .5f);
        private static AudioClip Tone(string name, float seconds, float frequency, float amplitude, bool noise)
        {
            const int rate = 44100; int count = (int)(rate * seconds); var samples = new float[count];
            var random = new System.Random(123);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = seconds == 1 ? 1 : Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * 10);
                samples[i] = amplitude * envelope * ((float)Math.Sin(t * frequency * Math.PI * 2) * .6f +
                    (noise ? ((float)random.NextDouble() * 2 - 1) * .4f : (float)Math.Sin(t * frequency * Math.PI * 4) * .1f));
            }
            var clip = AudioClip.Create(name, count, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
    }
}
