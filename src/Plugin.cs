using HarmonyLib;
using MGSC;
using ModConfigMenu.Contracts;
using ModConfigMenu.Implementations;
using ModConfigMenu.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using static ModConfigMenu.ModConfigMenuAPI;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public static class Plugin
    {
        public static ModConfig Config { get; private set; }

        private const string GLOBAL_HEADER = "Global";
        private const string HEALTHBAR_HEADER = "Health Bar";

        private static DisplayMovementController _controller;
        private static Pooler _uiPool;

        private static PixelPerfectCamera GameCamera;
        private static readonly int _perfectPPU = 78;
        public static Vector3 ScaleSize
        {
            get
            {
                if (GameCamera == null)
                    return Vector3.one;

                float scale = (float)GameCamera.assetsPPU / _perfectPPU;
                return new Vector3(scale, scale, scale);
            }
        }

        public static string RootFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string ModAssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
        private static string ModPersistenceFolder =>
            Path.Combine($"{Application.persistentDataPath}/../Quasimorph_ModConfigs", ModAssemblyName);

        private static string ConfigPath => Path.Combine(ModPersistenceFolder, "config.json");
        public static readonly string BundleName = "QM_DisplayMovementSpeedContinuedUI.Resources.apcontrollerbundle";

        #region Shader Properties
        public static readonly int DamagedHealthbarColor = Shader.PropertyToID("_DamagedHealthbarColor");
        public static readonly int CurrentHealthbarColor = Shader.PropertyToID("_CurrentHealthbarColor");
        public static readonly int EnableCurrentHealthBlink = Shader.PropertyToID("_EnableCurrentHealthBlink");
        public static readonly int CurrentHealthBlinkingSpeed = Shader.PropertyToID("_CurrentHealthBlinkingSpeed");
        public static readonly int ChunkColor = Shader.PropertyToID("_ChunkColor");
        public static readonly int CurrentHealthPercent = Shader.PropertyToID("_CurrentHealthPercent");
        public static readonly int DamagedHealthPercent = Shader.PropertyToID("_DamagedHealthPercent");
        public static readonly int ChunkAmount = Shader.PropertyToID("_ChunkAmount");
        public static readonly int CurrentHealthBlink = Shader.PropertyToID("_EnableCurrentHealthBlink");
        public static readonly int EnableChunk = Shader.PropertyToID("_EnableChunk");
        public static readonly int EnableDamagePreview = Shader.PropertyToID("_EnableDamagePreview");
        #endregion

        #region MGSC Hooks

        [Hook(ModHookType.AfterBootstrap)]
        public static void Bootstrap(IModContext context)
        {
            var harmony = new Harmony("Crynano_DisplayMovementSpeedContinuedUI");
            harmony.PatchAll();
        }

        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfig(IModContext context)
        {
            Directory.CreateDirectory(ModPersistenceFolder);

            Config = ModConfig.LoadConfigJson(ConfigPath);

            if (!File.Exists(ConfigPath))
                Config.SaveConfigJson(ConfigPath);

            // Work with newModConfig from now on
            List<IConfigValue> configValues = new List<IConfigValue>()
            {
                new DropdownConfig("UIMode", Config.UIMode.ToString(), GLOBAL_HEADER, "OnlyWhenFocused",
                    "Configure display mode for UI.",
                    "UI Mode",
                    Enum.GetNames(typeof(ModConfig.UiMode)).ToList<object>()),

                new ConfigValue("EnabledAttackType", Config.EnabledAttackType, GLOBAL_HEADER, true,
                    "Toggle the attack type (melee/ranged) icon", "Enable Attack Icon"),

                new ConfigValue("EnabledDamageType", Config.EnabledDamageType, GLOBAL_HEADER, true,
                    "Toggle the icon showing the enemy's weapon damage type. (Blunt, Fire, Cold, etc.)", "Enable Damage Type Icon"),

                new ConfigValue("EnabledActionPoints", Config.EnabledActionPoints, GLOBAL_HEADER, true,
                    "Toggle the numeric action points (AP) display", "Enable Action Points Display"),

                new ConfigValue("EnabledNumericHealth", Config.EnabledNumericHealth, GLOBAL_HEADER, false,
                    "Toggle the text displaying the numeric health.", "Enable Numeric Health Display"),

                new ConfigValue("EnabledHealthBar", Config.EnabledHealthBar, HEALTHBAR_HEADER, true,
                    "Toggle the healthbar on the UI", "Enable Health Bar"),

                new ConfigValue("EnableRemainingHealthPreviewBar", Config.EnableRemainingHealthPreviewBar, HEALTHBAR_HEADER,
                    true,
                    "Toggles a bar that displays the average damage you would deal to that unit.",
                    "Enable Damage Preview Bar"),

                new ConfigValue("HealthBarBlink", Config.HealthBarBlink, HEALTHBAR_HEADER, true,
                    "Toggle the health bar blink.", "Enable Health Bar Blink"),

                new RangeConfig<float>("HealthBarBlinkSpeed", Config.HealthBarBlinkSpeed,
                    header:HEALTHBAR_HEADER,
                    min : 0.5f, max : 10f, defaultValue : 2f,
                    tooltip:"Sets the health bar blinking speed.",
                    label: "Blinking Speed"),

                new ConfigValue("CurrentHealthColor", Config.CurrentHealthColor, HEALTHBAR_HEADER, Color.red,
                    "Color for the current amount of health a unit has.", "Current Health Color"),

                new ConfigValue("RemainingHealthColor", Config.RemainingHealthColor, HEALTHBAR_HEADER, Color.yellow,
                    "Color for the amount of health the unit would have after an average hit from your merc.",
                    "Remaining Health Color"),

                new ConfigValue("HealthChunkEnabled", Config.HealthChunkEnabled, "Dividers", true,
                    "Toggles the health dividers overlaying the health bar.", "Enable Health Chunk Divider"),

                new ConfigValue("HealthChunkValue", Config.HealthChunkValue,
                    header: "Dividers", min: 5, max: 50, defaultValue: 20, label: "Health Chunk Divider Value",
                    tooltip: "How much health a chunk represents."),

                new ConfigValue("HealthChunkDividerColor", Config.HealthChunkDividerColor, "Dividers", Color.white,
                    "Color for the chunk divider.",
                    "Chunk Divider Color"),

                new ConfigValue("DebugMode", Config.DebugMode, "Debug", false,
                    "Toggles debug messages.", "Toggle debug mode"),
            };

            RegisterModConfig("Display Movement Speed UI", configValues, ConfigChangedCallback);
        }

        private static bool ConfigChangedCallback(Dictionary<string, object> config, out string message)
        {
            try
            {
                message = "All good";
                Config.LoadConfig(config);
                Config.SaveConfigJson(ConfigPath);
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        [Hook(ModHookType.DungeonStarted)]
        public static void SpawnUI(IModContext context)
        {
            _controller = UI.Get<DisplayMovementController>();

            GameCamera = GameObject.FindObjectOfType<PixelPerfectCamera>();

            var poolObject = new GameObject("DisplayMovementUIPool");
            poolObject.transform.SetParent(_controller.transform.parent.transform.parent);

            _uiPool = poolObject.AddComponent<Pooler>();
            _uiPool.Initialize(_controller.gameObject, _controller.transform.parent);
        }

        [Hook(ModHookType.ResourcesLoad)]
        public static object ResourcesLoad(string path)
        {
            if (path.Contains(nameof(DisplayMovementController)))
            {
                var pref = DataLoader.LoadFileFromMemory<GameObject>(BundleName, "ControllerPrefab");
                pref.AddComponent<DisplayMovementController>();
                Logger.LogDebug($"Loaded DisplayMovementController prefab from bundle.");
                return pref;
            }
            return null;
        }

        #endregion

        public static void UpdateMonsterUI(Monster monster)
        {
            if (GameCamera == null)
            {
                Logger.LogWarning($"Game camera not found.");
                return;
            }

            if (_uiPool != null)
            {
                var pooledController = _uiPool.GetController(monster);
                pooledController.SetEnemy(monster);
            }
        }

        public static void ReleaseMonsterUI(Creature monster)
        {
            _uiPool?.ReturnController(monster);
        }

        public static Pooler GetUIPool()
        {
            return _uiPool;
        }

        public static void SetEnemyFocus(Monster monster)
        {
            var controller = GetUIPool().GetController(monster);
            if (controller != null)
            {
                controller.Focused = true;
            }
        }
    }
}