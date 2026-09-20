using System;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Mosquito.Core;
using Vector2 = UnityEngine.Vector2;

namespace Mosquito.Runtime
{
    public sealed class GameHud : MonoBehaviour
    {
        private GameController game;
        private RectTransform canvasRoot, overlay, modal, settingsPanel;
        private TMP_FontAsset font;
        private readonly Color ink = new Color(.045f, .08f, .10f, .94f);
        private readonly Color panel = new Color(.08f, .135f, .155f, .92f);
        private readonly Color line = new Color(.19f, .28f, .29f);
        private readonly Color ivory = new Color(.91f, .93f, .85f);
        private readonly Color muted = new Color(.53f, .65f, .65f);
        private readonly Color amber = new Color(1, .69f, .30f);
        private readonly Dictionary<TMP_Text, float> fontSizes = new Dictionary<TMP_Text, float>();
        private TMP_Text adult, females, males, eggs, rate, kills, bestClear, status, time, phase, record, modalTitle, modalBody, milestone;
        private readonly TMP_Text[] weaponTitles = new TMP_Text[3], weaponDetails = new TMP_Text[3];
        private readonly Image[] weaponPanels = new Image[3], progress = new Image[3];
        private Button primary, secondary, settingsButton, quitButton;
        private TMP_Text primaryText, secondaryText;
        private bool confirmRestart;
        private float toastUntil;
        private string currentModalKey;

        public void Initialize(GameController controller)
        {
            game = controller;
            font = TMP_FontAsset.CreateFontAsset("Microsoft YaHei", "Regular") ??
                TMP_FontAsset.CreateFontAsset("Microsoft YaHei UI", "Regular") ?? TMP_FontAsset.CreateFontAsset("Arial", "Regular");
            if (font == null) throw new InvalidOperationException("Unable to create Windows UI font.");
            var canvas = new GameObject("Game HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot = canvas.GetComponent<RectTransform>();
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var events = new GameObject("Input UI", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            events.GetComponent<EventSystem>().sendNavigationEvents = false;

            var header = Box(canvasRoot, "Telemetry", 0, 0, 1920, 192, ink);
            Text(header, "MOSQUITO  /  OBSERVATORY", 60, 25, 520, 32, 20, amber);
            Text(header, "蚊群观察室", 60, 70, 430, 53, 39, ivory);
            Text(header, "无限繁殖 · 清场实验", 62, 133, 420, 30, 19, muted);
            Divider(header, 520, 38, 1, 116);
            Text(header, "当前成蚊", 562, 31, 260, 28, 18, muted);
            adult = Text(header, "4", 557, 67, 355, 83, 65, ivory);
            females = Text(header, "母蚊  2", 938, 64, 235, 37, 25, amber);
            males = Text(header, "公蚊  2", 938, 112, 235, 32, 22, muted);
            Divider(header, 1205, 40, 1, 110);
            Text(header, "当前虫卵", 1250, 33, 240, 28, 18, muted);
            eggs = Text(header, "0", 1247, 77, 220, 65, 44, ivory);
            Button(header, "暂停  ESC", 1660, 60, 198, 61, () => game.Pause("模拟已暂停"), line);

            var side = Box(canvasRoot, "Run journal", 1510, 232, 350, 520, panel);
            Text(side, "本局档案", 28, 25, 285, 39, 26, ivory);
            Divider(side, 28, 83, 294, 1);
            Text(side, "累计击杀", 28, 109, 280, 30, 18, muted);
            kills = Text(side, "0", 25, 148, 295, 61, 44, amber);
            Text(side, "最大单次清场", 28, 236, 285, 28, 18, muted);
            bestClear = Text(side, "—", 27, 273, 293, 47, 31, ivory);
            Divider(side, 28, 341, 294, 1);
            rate = Text(side, "最近 1 秒产卵  0", 28, 365, 290, 38, 18, muted);
            time = Text(side, "模拟时长  00:00", 28, 415, 290, 37, 18, muted);
            phase = Text(canvasRoot, "● 观察中", 62, 235, 640, 40, 22, amber);
            Text(canvasRoot, "数量超过 300 后，画面展示部分蚊群", 64, 284, 810, 28, 17, muted);

            var bottom = Box(canvasRoot, "Weapon tray", 0, 830, 1920, 250, ink);
            status = Text(bottom, "", 64, 18, 1750, 37, 23, ivory);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var button = Button(bottom, "", 60 + i * 610, 80, 582, 116, () => game.Select((Weapon)index), panel);
                var rect = button.GetComponent<RectTransform>(); weaponPanels[i] = button.GetComponent<Image>();
                Text(rect, "0" + (i + 1), 22, 18, 52, 47, 31, muted);
                weaponTitles[i] = Text(rect, WeaponName((Weapon)i), 92, 16, 325, 40, 27, ivory);
                weaponDetails[i] = Text(rect, "", 93, 66, 405, 28, 17, muted);
                progress[i] = Box(rect, "Cooldown", 0, 111, 582, 5, amber).GetComponent<Image>();
            }
            Text(bottom, "1 / 2 / 3 选择武器     点击场景或 SPACE 使用     按住左键连续拍击", 64, 211, 1700, 26, 17, muted);
            milestone = Text(canvasRoot, "", 520, 735, 880, 60, 30, amber); milestone.alignment = TextAlignmentOptions.Center;

            overlay = Box(canvasRoot, "Modal backdrop", 0, 0, 1920, 1080, new Color(.025f, .05f, .065f, .82f));
            modal = Box(overlay, "Dialog", 570, 170, 780, 738, panel);
            Text(modal, "MOSQUITO  /  A SMALL EXPERIMENT", 50, 40, 675, 35, 18, amber);
            modalTitle = Text(modal, "从四只开始。", 46, 103, 690, 85, 56, ivory);
            modalBody = Text(modal, "", 50, 215, 675, 125, 23, muted); modalBody.textWrappingMode = TextWrappingModes.Normal;
            record = Text(modal, "", 50, 356, 670, 47, 19, amber);
            primary = Button(modal, "开始观察", 50, 430, 680, 76, Primary, amber); primaryText = primary.GetComponentInChildren<TMP_Text>(); primaryText.color = ink;
            secondary = Button(modal, "继续上次实验", 50, 526, 680, 65, Secondary, line); secondaryText = secondary.GetComponentInChildren<TMP_Text>();
            settingsButton = Button(modal, "设置", 50, 629, 318, 59, game.OpenSettings, line);
            quitButton = Button(modal, "退出", 390, 629, 340, 59, game.RequestQuit, line);
            BuildSettings();
        }

        private void BuildSettings()
        {
            settingsPanel = Box(overlay, "Settings", 470, 110, 980, 860, panel);
            Text(settingsPanel, "实验室设置", 50, 35, 700, 67, 40, ivory);
            Text(settingsPanel, "调整舒适度，模拟保持暂停。", 52, 112, 850, 40, 22, muted);
            Slider(settingsPanel, "总音量", 195, game.Profile.volume, value => { game.Profile.volume = value; game.ApplyPreferences(); });
            Slider(settingsPanel, "嗡鸣强度", 292, game.Profile.buzzVolume, value => game.Profile.buzzVolume = value);
            Button(settingsPanel, "切换闪光强度", 50, 394, 420, 62, () => { game.Profile.reducedFlash = !game.Profile.reducedFlash; currentModalKey = null; }, line);
            Button(settingsPanel, "切换震屏", 505, 394, 420, 62, () => game.Profile.shake = !game.Profile.shake, line);
            Text(settingsPanel, "分辨率", 50, 490, 200, 33, 22, muted);
            Button(settingsPanel, "1280 × 720", 50, 535, 272, 60, () => Screen.SetResolution(1280, 720, FullScreenMode.Windowed), line);
            Button(settingsPanel, "1920 × 1080", 350, 535, 272, 60, () => Screen.SetResolution(1920, 1080, FullScreenMode.Windowed), line);
            Button(settingsPanel, "无边框全屏", 650, 535, 274, 60, () => Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow), line);
            Button(settingsPanel, "文字 100%", 50, 625, 272, 60, () => SetScale(1), line);
            Button(settingsPanel, "文字 125%", 350, 625, 272, 60, () => SetScale(1.25f), line);
            Button(settingsPanel, "文字 150%", 650, 625, 274, 60, () => SetScale(1.5f), line);
            Button(settingsPanel, "保存并返回", 50, 744, 874, 68, game.CloseSettings, amber).GetComponentInChildren<TMP_Text>().color = ink;
        }

        private void SetScale(float scale) { game.Profile.uiScale = scale; game.ApplyPreferences(); }
        public void ApplyScale(float scale)
        {
            foreach (var pair in fontSizes)
            {
                pair.Key.enableAutoSizing = true; pair.Key.fontSizeMin = pair.Value * .8f;
                pair.Key.fontSizeMax = pair.Value * scale; pair.Key.fontSize = pair.Value * scale;
            }
        }
        public void ShowMilestone(string text) { milestone.text = text; toastUntil = Time.unscaledTime + 3; }
        public void SetCaptureCamera(Camera camera)
        {
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = camera == null ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = .5f;
        }

        public void Refresh()
        {
            var sim = game.Game;
            adult.text = sim == null ? "4" : CountFormatter.Format(sim.Adults);
            females.text = "母蚊  " + (sim == null ? "2" : CountFormatter.Format(sim.Female));
            males.text = "公蚊  " + (sim == null ? "2" : CountFormatter.Format(sim.Male));
            eggs.text = sim == null ? "0" : CountFormatter.Format(sim.Eggs);
            kills.text = sim == null ? "0" : CountFormatter.Format(sim.TotalKilled);
            bestClear.text = sim == null ? "—" : CountFormatter.Format(sim.BestClear);
            rate.text = "最近 1 秒产卵  " + (sim == null ? "0" : CountFormatter.Format(sim.LastSecondLaid));
            long seconds = sim == null ? 0 : sim.Tick / 20;
            time.text = "模拟时长  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            phase.text = game.Recovering ? "● 正在恢复进度" : game.Paused ? "Ⅱ 模拟暂停" : sim?.Phase == RunPhase.HatchAfterClear ? "● 幸存虫卵正在回弹" : "● 观察中";
            status.text = game.Status;
            milestone.gameObject.SetActive(Time.unscaledTime < toastUntil);
            for (int i = 0; i < 3; i++)
            {
                var weapon = (Weapon)i; bool unlocked = sim != null && sim.IsUnlocked(weapon);
                bool selected = unlocked && game.Selected == weapon;
                weaponPanels[i].color = selected ? new Color(.18f, .24f, .23f) : panel;
                weaponTitles[i].color = unlocked ? ivory : muted;
                float fraction = unlocked ? 1 - sim.RemainingCooldown(weapon) / (float)sim.Config.Cooldown(weapon) : 0;
                progress[i].rectTransform.sizeDelta = new Vector2(582 * fraction, 5);
                weaponDetails[i].text = !unlocked ? "本局峰值 " + new[] { "10", "100", "1,000" }[i] + " 只解锁" :
                    sim.RemainingCooldown(weapon) > 0 ? "冷却 " + (sim.RemainingCooldown(weapon) * .05f).ToString("F1") + "s" :
                    weapon == Weapon.Hand ? "50% 命中 · 单次 1 只" : weapon == Weapon.Zapper ? "单次最多 10 只 · 按住连发" : "清除成蚊 · 保留虫卵";
            }
            bool ended = sim != null && sim.Phase == RunPhase.ReproductionEnded;
            bool showModal = game.InMenu || game.Paused || ended || confirmRestart || game.SettingsOpen;
            overlay.gameObject.SetActive(showModal);
            settingsPanel.gameObject.SetActive(game.SettingsOpen);
            modal.gameObject.SetActive(!game.SettingsOpen);
            string key = confirmRestart ? "confirm" : game.InMenu ? "menu" : ended ? "ended" : "paused";
            if (key != currentModalKey)
            {
                currentModalKey = key;
                modalTitle.text = key == "menu" ? "从四只开始。" : key == "confirm" ? "开始新的实验？" : key == "ended" ? "这次，安静了。" : "让房间安静一会。";
                modalBody.text = key == "menu" ? "观察蚊群繁殖，在失控时拍下手掌、挥动电拍。\n蚊香能清掉成蚊，但故事可能还在虫卵里。" :
                    key == "confirm" ? "当前局进度将被替换。\n你的历史最高纪录会保留。" : key == "ended" ? game.Status : "数量、孵化与武器冷却都已暂停。\n继续后，从刚才的时刻接着观察。";
                primaryText.text = key == "menu" ? "开始观察" : key == "confirm" ? "确定开始新局" : key == "ended" ? "重新开始" : "继续观察";
                secondaryText.text = key == "menu" ? "继续上次实验" : key == "confirm" ? "取消" : "保存并返回主菜单";
            }
            secondary.interactable = key != "menu" || game.CanContinue;
            record.text = "历史单局击杀  " + CountFormatter.Format(BigInteger.Parse(game.Profile.bestRunKills)) + "    /    最大清场  " + CountFormatter.Format(BigInteger.Parse(game.Profile.bestClear));
        }

        private void Primary()
        {
            if (game.InMenu && game.CanContinue && !confirmRestart) { confirmRestart = true; currentModalKey = null; return; }
            if (confirmRestart || game.InMenu || game.Game?.Phase == RunPhase.ReproductionEnded)
            { confirmRestart = false; game.NewRun(); currentModalKey = null; }
            else game.Resume();
        }
        private void Secondary()
        {
            if (confirmRestart) { confirmRestart = false; currentModalKey = null; }
            else if (game.InMenu) game.ContinueRun(); else game.ReturnToMenu();
        }
        public static string WeaponName(Weapon weapon) => weapon == Weapon.Hand ? "手掌" : weapon == Weapon.Zapper ? "电蚊拍" : "蚊香";

        private RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        private RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        { var rect = Rect(parent, name, x, y, w, h); rect.gameObject.AddComponent<Image>().color = color; return rect; }
        private void Divider(Transform parent, float x, float y, float w, float h) => Box(parent, "Divider", x, y, w, h, line);
        private TMP_Text Text(Transform parent, string value, float x, float y, float w, float h, float size, Color color)
        {
            var label = Rect(parent, value.Length > 22 ? value.Substring(0, 22) : value, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = value; label.fontSize = size; label.color = color;
            label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft; fontSizes.Add(label, size); return label;
        }
        private Button Button(Transform parent, string value, float x, float y, float w, float h, Action action, Color color)
        {
            var rect = Box(parent, "Button " + value, x, y, w, h, color);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.06f); colors.pressedColor = new Color(.78f, .85f, .82f); colors.disabledColor = new Color(.4f, .4f, .4f); button.colors = colors;
            button.onClick.AddListener(() => action());
            var label = Text(rect, value, 18, 4, w - 36, h - 8, 23, ivory); label.alignment = TextAlignmentOptions.Center; return button;
        }
        private void Slider(Transform parent, string value, float y, float initial, Action<float> change)
        {
            Text(parent, value, 50, y, 235, 42, 24, ivory);
            var area = Box(parent, value + " slider", 300, y + 4, 620, 35, line);
            var fill = Box(area, "Fill", 0, 0, 620, 35, amber);
            var slider = area.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.fillRect = fill;
            slider.minValue = 0; slider.maxValue = 1; slider.value = initial;
            slider.onValueChanged.AddListener(v => change(v));
        }
    }
}
