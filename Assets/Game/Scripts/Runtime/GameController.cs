using System;
using System.Collections;
using System.IO;
using System.Numerics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mosquito.Core;

namespace Mosquito.Runtime
{
    [DefaultExecutionOrder(100)]
    public sealed class GameController : MonoBehaviour
    {
        public Simulation Game { get; private set; }
        public PlayerProfile Profile { get; private set; }
        public Weapon Selected { get; private set; }
        public bool Paused { get; private set; }
        public bool InMenu { get; private set; } = true;
        public bool Recovering { get; private set; }
        public int PendingTicks { get; private set; }
        public string Status { get; private set; } = "从四只蚊子开始，观察一场失控。";
        public double LastTickMilliseconds { get; private set; }
        public bool CanContinue => Profile.run != null;
        public bool SettingsOpen { get; private set; }
        private SaveService saves;
        private GameHud hud;
        private RoomView room;
        private GameAudio audioFx;
        private GameSettings settings;
        private double remainder, lastRealTime;
        private Weapon? queuedCommand;
        private bool holdArmed, exitAuthorized;
        private int lastMilestone;
        private bool saveRequested;
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        private void Awake()
        {
            Application.targetFrameRate = 60;
            settings = Resources.Load<GameSettings>("GameSettings");
            saves = new SaveService(Argument("-saveDir") ?? Application.persistentDataPath);
            Profile = saves.Load(out var notice) ?? new PlayerProfile();
            if (notice != null) Status = notice;
            room = gameObject.AddComponent<RoomView>(); room.Initialize();
            audioFx = gameObject.AddComponent<GameAudio>(); audioFx.Initialize();
            hud = gameObject.AddComponent<GameHud>(); hud.Initialize(this);
            ApplyPreferences();
            lastRealTime = Time.realtimeSinceStartupAsDouble;
            Application.wantsToQuit += WantsToQuit;
            if (HasArgument("-auto-play")) NewRun();
            if (Argument("-capture") != null) StartCoroutine(CaptureRun());
        }

        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            double delta = now - lastRealTime; lastRealTime = now;
            if (!InMenu && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !Recovering)
            { if (SettingsOpen) CloseSettings(); else if (!Paused && Game.Phase != RunPhase.ReproductionEnded) Pause("模拟已暂停"); }
            if (!InMenu && !Paused && Game != null && Game.Phase != RunPhase.ReproductionEnded)
            {
                if (!Recovering)
                {
                    if (delta > 1) { Pause("检测到时间跳变，已暂停；不会补算离线时间。"); }
                    else
                    {
                        ReadInput(); remainder += Math.Max(0, delta);
                        int due = (int)(remainder / .05); remainder -= due * .05; PendingTicks += due;
                        if (PendingTicks > 20) Pause("积欠较多，模拟已保护暂停。点击继续恢复。");
                    }
                }
                if (!Paused)
                {
                    for (int i = 0; i < 5 && PendingTicks > 0; i++)
                    {
                        PendingTicks--;
                        Weapon? command = queuedCommand; queuedCommand = null;
                        if (!command.HasValue && holdArmed && !Recovering && Selected != Weapon.Incense) command = Selected;
                        stopwatch.Restart(); Game.Step(command); stopwatch.Stop();
                        LastTickMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                        ProcessEvents();
                        Profile.MergeRecords(Game);
                        if (Game.Phase == RunPhase.ReproductionEnded)
                        { PendingTicks = 0; holdArmed = false; saveRequested = true; }
                        if (Game.Tick % 200 == 0 || saveRequested) { saveRequested = false; Save(); }
                        if (Game.Phase == RunPhase.ReproductionEnded) break;
                    }
                    if (Recovering && PendingTicks == 0) { Recovering = false; Status = "进度已恢复。"; }
                }
            }
            room.SetPopulation(Game, settings == null ? 300 : settings.visualLimit, InMenu);
            room.Animate(!Paused && !InMenu, Profile.reducedFlash, Profile.shake);
            audioFx.SetBuzz(Game == null || InMenu || Paused || Game.Phase == RunPhase.ReproductionEnded ? 0 :
                Mathf.Clamp01((float)BigInteger.Log10(BigInteger.Max(1, Game.Adults)) / 6f), Profile.volume, Profile.buzzVolume);
            hud.Refresh();
        }

        private void ReadInput()
        {
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) Select(Weapon.Hand);
                if (keyboard.digit2Key.wasPressedThisFrame) Select(Weapon.Zapper);
                if (keyboard.digit3Key.wasPressedThisFrame) Select(Weapon.Incense);
                if (keyboard.spaceKey.wasPressedThisFrame) Queue(Selected);
            }
            if (mouse == null) return;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse.leftButton.wasReleasedThisFrame || overUi || !Application.isFocused) holdArmed = false;
            if (mouse.leftButton.wasPressedThisFrame && !overUi)
            { Queue(Selected); holdArmed = Selected != Weapon.Incense; }
        }
        private void Queue(Weapon weapon) { if (!queuedCommand.HasValue) queuedCommand = weapon; }

        public void Select(Weapon weapon)
        {
            if (Game == null || !Game.IsUnlocked(weapon)) { Status = "本局峰值达到 " + (Game?.Config.UnlockThreshold(weapon) ?? 10) + " 只后解锁。"; return; }
            Selected = weapon; holdArmed = false; queuedCommand = null;
            Status = "已选择" + GameHud.WeaponName(weapon) + " · 点击场景或按 Space 使用";
        }
        public void NewRun()
        {
            if (Game != null) Profile.MergeRecords(Game);
            Game = new Simulation(settings == null ? null : settings.simulation, unchecked((ulong)DateTime.UtcNow.Ticks));
            Selected = Weapon.Hand; PendingTicks = 0; remainder = 0; lastMilestone = 0;
            InMenu = Paused = Recovering = SettingsOpen = false; queuedCommand = null; holdArmed = false;
            room.ResetVisuals(); Status = "母蚊每 2 秒产卵，虫卵 1 秒后孵化。"; lastRealTime = Time.realtimeSinceStartupAsDouble;
            Save();
        }
        public void ContinueRun()
        {
            if (!CanContinue) return;
            try
            {
                Game = Simulation.Restore(Profile.run);
                bool pacingUpdated = Game.UpgradeReproductionPacing();
                PendingTicks = Profile.pendingCatchupTicks;
                Selected = (Weapon)Profile.selectedWeapon; remainder = 0;
                InMenu = Paused = SettingsOpen = false; Recovering = PendingTicks > 0;
                holdArmed = false; queuedCommand = null; room.ResetVisuals();
                Status = Recovering ? "正在恢复已积欠的进度…" : "已恢复存档；离线期间没有繁殖。";
                if (pacingUpdated) { Status += " 繁殖速度已调慢。"; Save(); }
            }
            catch (Exception e) { Status = "无法恢复此存档：" + e.Message; }
        }
        public void Pause(string reason)
        { if (Game == null || InMenu) return; Paused = true; holdArmed = false; queuedCommand = null; Status = reason; }
        public void Resume()
        { if (Game == null) return; Paused = SettingsOpen = false; Recovering = PendingTicks > 0; holdArmed = false; queuedCommand = null; }
        public void OpenSettings() { if (!InMenu) Pause("设置"); SettingsOpen = true; }
        public void CloseSettings() { SettingsOpen = false; ApplyPreferences(); Save(); }
        public void ReturnToMenu()
        { if (!Save()) return; InMenu = true; SettingsOpen = false; holdArmed = false; queuedCommand = null; }
        public void RequestQuit() { if (Save()) { exitAuthorized = true; Application.Quit(); } }
        private bool WantsToQuit() => exitAuthorized || Save();
        private void OnDestroy() { Application.wantsToQuit -= WantsToQuit; }
        private void OnApplicationFocus(bool focused)
        { if (!focused && !InMenu && !HasArgument("-capture")) Pause("窗口失去焦点，模拟已暂停。返回后点击继续。"); }

        public bool Save()
        {
            try
            {
                if (Game != null)
                {
                    Profile.MergeRecords(Game); Profile.run = Game.Capture();
                    Profile.pendingCatchupTicks = PendingTicks; Profile.selectedWeapon = (int)Selected;
                }
                saves.Save(Profile); return true;
            }
            catch (Exception e) { Status = "保存失败，请重试：" + e.Message; Debug.LogError(Status); return false; }
        }
        public void ApplyPreferences() { AudioListener.volume = HasArgument("-silent") ? 0 : Profile.volume; hud?.ApplyScale(Profile.uiScale); }

        private void ProcessEvents()
        {
            bool save = false;
            foreach (var e in Game.Events)
            {
                if (e.Type == SimEventType.Unlocked)
                { Status = "已解锁" + GameHud.WeaponName(e.Weapon) + " · 按 " + ((int)e.Weapon + 1) + " 选择"; audioFx.PlayUnlock(); save = true; }
                if (e.Type == SimEventType.Missed) { Status = "MISS · 手掌有 50% 的几率落空"; room.Attack(e); audioFx.PlayAttack(e.Weapon, false); }
                if (e.Type == SimEventType.Killed)
                {
                    Status = "清除 " + CountFormatter.Format(e.Count) + " 只";
                    if (e.Weapon == Weapon.Incense) { Status += " · 幸存虫卵 " + CountFormatter.Format(Game.BurstInitialEggs); save = true; }
                    room.Attack(e); audioFx.PlayAttack(e.Weapon, true);
                }
                if (e.Type == SimEventType.Ended) Status = Game.Adults == 0 ? "全部蚊子已清除。" : "繁殖已停止，剩余 " + CountFormatter.Format(Game.Male) + " 只公蚊。";
            }
            int milestone = Game.TotalKilled >= 1000000000 ? 3 : Game.TotalKilled >= 1000000 ? 2 : Game.TotalKilled >= 1000 ? 1 : 0;
            if (milestone > lastMilestone) { lastMilestone = milestone; hud.ShowMilestone("累计击杀突破 " + new[] { "", "1K", "1M", "1B" }[milestone]); }
            if (save) saveRequested = true;
        }

        private IEnumerator CaptureRun()
        {
            if (HasArgument("-smoke-test"))
            {
                // Let the first render finish before timing the unattended gameplay check.
                // Resume a startup protection pause just as a player would, without disabling it.
                yield return new WaitForEndOfFrame();
                yield return new WaitForSecondsRealtime(1);
                if (Paused) Resume();
                float timeout = Time.realtimeSinceStartup + 45;
                while (!Game.IsUnlocked(Weapon.Hand) && Time.realtimeSinceStartup < timeout) yield return null;
                Select(Weapon.Hand); Queue(Weapon.Hand);
                while (!Game.IsUnlocked(Weapon.Zapper) && Time.realtimeSinceStartup < timeout) yield return null;
                Select(Weapon.Zapper); Queue(Weapon.Zapper);
                while (!Game.IsUnlocked(Weapon.Incense) && Time.realtimeSinceStartup < timeout) yield return null;
                Select(Weapon.Incense); Queue(Weapon.Incense);
                yield return new WaitForSecondsRealtime(.7f);
                Pause("自动运行验证：暂停与存档");
                long pausedTick = Game.Tick;
                yield return new WaitForSecondsRealtime(.2f);
                bool pausedCorrectly = Game.Tick == pausedTick;
                bool saved = Save();
                var restored = Simulation.Restore(Profile.run);
                bool roundTrip = JsonUtility.ToJson(Game.Capture()) == JsonUtility.ToJson(restored.Capture());
                Resume();
                Status = "本次清除 " + CountFormatter.Format(Game.BestClear) + " 只 · 幸存虫卵 " + CountFormatter.Format(Game.BurstInitialEggs);
                string reportPath = Path.Combine(Path.GetDirectoryName(Argument("-capture")), "runtime-smoke.json");
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, JsonUtility.ToJson(new RuntimeSmokeReport {
                    success = Game.HandAttempts > 0 && Game.ZapperUses > 0 && Game.IncenseUses > 0 && pausedCorrectly && saved && roundTrip,
                    tick = Game.Tick, handAttempts = Game.HandAttempts, zapperUses = Game.ZapperUses, incenseUses = Game.IncenseUses,
                    pausedCorrectly = pausedCorrectly, saved = saved, roundTrip = roundTrip,
                    adults = Game.Adults.ToString(), eggs = Game.Eggs.ToString(), tickMilliseconds = LastTickMilliseconds
                }, true));
                yield return new WaitForSecondsRealtime(1);
            }
            else yield return new WaitForSecondsRealtime(Argument("-capture-delay") is string delay && float.TryParse(delay, out float seconds) ? seconds : 12);
            yield return new WaitForEndOfFrame();
            string file = Argument("-capture"); Directory.CreateDirectory(Path.GetDirectoryName(file));
            // An explicit render target also works when Windows suppresses a hidden swap chain.
            var camera = Camera.main;
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            hud.SetCaptureCamera(camera);
            Canvas.ForceUpdateCanvases();
            room.Animate(false, Profile.reducedFlash, Profile.shake);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            var capture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); capture.Apply();
            File.WriteAllBytes(file, capture.EncodeToPNG());
            Destroy(capture); RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
            hud.SetCaptureCamera(null);
            yield return new WaitForSecondsRealtime(1);
            if (HasArgument("-quit-after-capture")) RequestQuit();
        }
        [Serializable]
        private sealed class RuntimeSmokeReport
        {
            public bool success, pausedCorrectly, saved, roundTrip;
            public long tick, handAttempts, zapperUses, incenseUses;
            public string adults, eggs;
            public double tickMilliseconds;
        }
        private static bool HasArgument(string key) => Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0;
        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
