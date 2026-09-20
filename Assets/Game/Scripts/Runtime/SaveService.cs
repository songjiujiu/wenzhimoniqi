using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Mosquito.Core;

namespace Mosquito.Runtime
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int schemaVersion = 1;
        public SimulationSnapshot run;
        public int pendingCatchupTicks;
        public int selectedWeapon;
        public string bestRunKills = "0", bestClear = "0", highestPopulation = "0";
        public float volume = 0.65f, buzzVolume = 0.30f, uiScale = 1f;
        public bool reducedFlash, shake = true;
        public void MergeRecords(Simulation game)
        {
            if (game == null || game.TestRun) return;
            bestRunKills = BigInteger.Max(BigInteger.Parse(bestRunKills), game.TotalKilled).ToString();
            bestClear = BigInteger.Max(BigInteger.Parse(bestClear), game.BestClear).ToString();
            highestPopulation = BigInteger.Max(BigInteger.Parse(highestPopulation), game.MaxAdult).ToString();
        }
    }

    public sealed class SaveService
    {
        [Serializable] private sealed class Envelope { public int version = 1; public long sequence; public string payload, checksum; }
        private readonly string path;
        private long sequence;
        public string Path => path;
        public SaveService(string folder) { Directory.CreateDirectory(folder); path = System.IO.Path.Combine(folder, "profile.json"); }

        public PlayerProfile Load(out string message)
        {
            message = null;
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return null;
            try { return Read(path); }
            catch (Exception)
            {
                try { var recovered = Read(path + ".bak"); message = "主存档无法读取，已恢复上一份备份。"; return recovered; }
                catch (Exception) { message = "存档无法读取，原文件已保留。可以选择开始新局。"; return null; }
            }
        }

        private PlayerProfile Read(string file)
        {
            var info = new FileInfo(file);
            if (!info.Exists || info.Length > 8 * 1024 * 1024) throw new InvalidDataException("Save size invalid.");
            var outer = JsonUtility.FromJson<Envelope>(File.ReadAllText(file, Encoding.UTF8));
            if (outer == null || outer.version != 1 || outer.sequence < 0 || string.IsNullOrEmpty(outer.payload) || outer.checksum != Hash(outer.payload))
                throw new InvalidDataException("Save checksum invalid.");
            var profile = JsonUtility.FromJson<PlayerProfile>(outer.payload);
            Validate(profile);
            sequence = Math.Max(sequence, outer.sequence);
            return profile;
        }

        public void Save(PlayerProfile profile)
        {
            Validate(profile);
            string payload = JsonUtility.ToJson(profile);
            var envelope = new Envelope { sequence = ++sequence, payload = payload, checksum = Hash(payload) };
            string temp = path + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope, true));
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path))
            {
                bool valid = true;
                try { Read(path); } catch { valid = false; }
                if (!valid) File.Copy(path, path + ".corrupt", true);
                File.Replace(temp, path, valid ? path + ".bak" : null);
            }
            else File.Move(temp, path);
        }

        private static void Validate(PlayerProfile profile)
        {
            if (profile == null || profile.schemaVersion != 1 || profile.pendingCatchupTicks < 0 || profile.pendingCatchupTicks > 1000 ||
                profile.selectedWeapon < 0 || profile.selectedWeapon > 2 || !FiniteRange(profile.volume, 0, 1) ||
                !FiniteRange(profile.buzzVolume, 0, 1) || !FiniteRange(profile.uiScale, 1, 1.5f))
                throw new InvalidDataException("Invalid profile.");
            foreach (var count in new[] { profile.bestRunKills, profile.bestClear, profile.highestPopulation })
                if (string.IsNullOrEmpty(count) || count.Length > 100000 || !BigInteger.TryParse(count, out var n) || n < 0)
                    throw new InvalidDataException("Invalid record.");
            if (profile.run != null) Simulation.Restore(profile.run);
        }
        private static bool FiniteRange(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
        private static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
    }
}
