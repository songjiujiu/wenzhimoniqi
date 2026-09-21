using System.Collections;
using System.IO;
using System.Linq;
using Mosquito.Core;
using Mosquito.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mosquito.Tests
{
    public sealed class ReferenceAssetsPlayTests
    {
        [UnityTest]
        public IEnumerator ReferenceModelsAppearInTheActualGameAndWeaponFeedback()
        {
            // CLI supplies an isolated -saveDir; never overwrite the player's normal save.
            Assert.That(System.Environment.GetCommandLineArgs().Contains("-saveDir"), Is.True);
            yield return SceneManager.LoadSceneAsync("Observatory");
            yield return null;
            var game = Object.FindFirstObjectByType<GameController>();
            Assert.That(game, Is.Not.Null);
            var room = game.GetComponent<RoomView>();
            game.NewRun();
            for (int i = 0; i < 140; i++) game.Game.Step();
            game.Resume();
            yield return null;
            Assert.That(game.transform.Find("Blender Room"), Is.Not.Null);
            Assert.That(game.transform.Find("Blender LuckyCat"), Is.Not.Null);
            Assert.That(game.transform.Find("Blender Tower"), Is.Not.Null);
            Assert.That(game.transform.Find("Blender Incense").gameObject.activeInHierarchy, Is.True);
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled), Is.EqualTo(1));
            Assert.That(game.GetComponent<SurfaceEggView>().VisibleCount, Is.GreaterThan(0));
            var recording = Resources.Load<AudioClip>("Audio/MosquitoBuzzLoop");
            Assert.That(recording, Is.Not.Null, "The downloaded recording must be included in the game.");
            Assert.That(recording.length, Is.InRange(7f, 9f));
            var buzz = game.GetComponents<AudioSource>().Single(source => source.loop);
            Assert.That(buzz.clip, Is.SameAs(recording));
            var audioFx = game.GetComponent<GameAudio>();
            audioFx.SetBuzz(.2f, .5f);
            Assert.That(buzz.isPlaying, Is.True, "Even a low mosquito population should be audible.");
            Assert.That(buzz.volume, Is.GreaterThan(0));
            Assert.That(buzz.pitch, Is.EqualTo(1), "Keep the pitch of the real recording.");
            audioFx.SetBuzz(0, .5f);
            Assert.That(buzz.isPlaying, Is.False, "Pause/menu/no adults must silence the loop.");
            audioFx.SetBuzz(.2f, 0);
            Assert.That(buzz.isPlaying, Is.False, "The buzz volume setting must mute the recording.");
            game.Pause("模型接入验证");
            var hud = game.GetComponent<GameHud>();
            game.Resume();
            yield return null;
            Capture(hud, "reference-gameplay");
            foreach (var weapon in new[] { Weapon.Hand, Weapon.Zapper })
            {
                // Use the same visual attack path as gameplay, targeting a visible live mosquito.
                var targets = room.FindTargets(room.VisibleTargetScreenPosition(), weapon);
                Assert.That(targets.Length, Is.GreaterThan(0));
                room.Attack(new SimEvent(SimEventType.Killed, weapon, 1, 0, game.Game.Tick, 0), targets);
                room.Animate(false, false, false);
                string name = weapon == Weapon.Hand ? "Blender Hand feedback" : "Blender Zapper feedback";
                Assert.That(game.transform.Find(name).gameObject.activeInHierarchy, Is.True);
                Capture(hud, weapon == Weapon.Hand ? "reference-hand" : "reference-zapper");
                yield return null;
            }
            room.Attack(new SimEvent(SimEventType.Killed, Weapon.Incense, 1, 0, game.Game.Tick, 0));
            room.Animate(false, false, false);
            Capture(hud, "reference-incense");
        }

        private static void Capture(GameHud hud, string name)
        {
            // Capturing/encoding can stall a frame and trigger the normal >1s safety pause.
            // Resume explicitly for each screenshot without changing that gameplay protection.
            Object.FindFirstObjectByType<GameController>().Resume();
            hud.Refresh();
            var camera = Camera.main;
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                hud.SetCaptureCamera(camera); Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                Directory.CreateDirectory("TestResults"); File.WriteAllBytes("TestResults/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                hud.SetCaptureCamera(null); RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target); Object.Destroy(image);
            }
        }
    }
}
