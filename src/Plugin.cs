using HarmonyLib;
using MGSC;
using ModConfigMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ModConfigMenu.Objects;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public static class Plugin
    {
        public static ModConfig Config { get; private set; }

        private static PixelPerfectCamera _gameCamera;
        private static DisplayMovementController _controller;

        public static string RootFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string ModAssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
        private static string ModPersistenceFolder =>
            Path.Combine($"{Application.persistentDataPath}/../Quasimorph_ModConfigs", ModAssemblyName);
        private static string ConfigPath => Path.Combine(ModPersistenceFolder, "config.json");
        //private static string ConfigPathIni => Path.Combine(ModPersistenceFolder, "config.ini");

        //
        public static string BundleName = "QM_DisplayMovementSpeedContinuedUI.Resources.apcontrollerbundle";
        
        // Camera related
        private static int _perfectPPU = 78;
        
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
            // Check for old ini file.
            // Load into memory the ini file.
            // Create config file with defaults if new config does not exist.
            // Load created config
            // Load MCM
            // If MCM fails fuck it
            // Proceed.
            Directory.CreateDirectory(ModPersistenceFolder);
            
            Config = ModConfig.LoadConfigJson(ConfigPath);
            
            if (!File.Exists(ConfigPath))
                Config.SaveConfigJson(ConfigPath);
            
            // Work with newModConfig from now on
            List<ConfigValue> configValues = new List<ConfigValue>()
            {
                new ConfigValue("EnabledAttackType", Config.EnabledAttackType, "Global", true,
                    "Toggle the attack type (melee/ranged) icon", "Enable Attack Icon"),
                
                new ConfigValue("EnabledDamageType", Config.EnabledDamageType, "Global", true,
                    "Toggle the icon showing the enemy's weapon damage type. (Blunt, Fire, Cold, etc.)", "Enable Damage Type Icon"),
                
                new ConfigValue("EnabledActionPoints", Config.EnabledActionPoints, "Global", true,
                    "Toggle the numeric action points (AP) display", "Enable Action Points Display"),
                
                new ConfigValue("EnabledNumericHealth", Config.EnabledNumericHealth, "Global", false,
                    "Toggle the text displaying the numeric health.", "Enable Numeric Health Display"),
                
                new ConfigValue("EnabledHealthBar", Config.EnabledHealthBar, "Health Bar", true,
                    "Toggle the healthbar on the UI", "Enable Health Bar"),
                
                new ConfigValue("EnableRemainingHealthPreviewBar", Config.EnableRemainingHealthPreviewBar, "Health Bar",
                    true,
                    "Toggles a bar that displays the average damage you would deal to that unit.",
                    "Enable Damage Preview Bar"),
                
                new ConfigValue("HealthBarBlink", Config.HealthBarBlink, "Health Bar", true,
                    "Toggle the health bar blink.", "Enable Health Bar Blink"),
                
                new ConfigValue("HealthBarBlinkSpeed", Config.HealthBarBlinkSpeed, 
                    header:"Health Bar", 
                    min : 0.5f, max : 10f, defaultValue : 2f,
                    tooltip:"Sets the health bar blinking speed.", 
                    label: "Blinking Speed"),
                
                new ConfigValue("CurrentHealthColor", Config.CurrentHealthColor, "Health Bar", Color.red,
                    "Color for the current amount of health a unit has.", "Current Health Color"),
                
                new ConfigValue("RemainingHealthColor", Config.RemainingHealthColor, "Health Bar", Color.yellow,
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
            
            ModConfigMenuAPI.RegisterModConfig("Display Movement Speed UI", configValues,
                (Dictionary<string, object> config, out string message) =>
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
                });
        }
        
        [Hook(ModHookType.DungeonStarted)]
        public static void SpawnUI(IModContext context)
        {
            _controller = UI.Get<DisplayMovementController>();
            _controller.LoadComponents();
            _gameCamera = GameObject.FindObjectOfType<PixelPerfectCamera>();
        }

        [Hook(ModHookType.ResourcesLoad)]
        public static object ResourcesLoad(string path)
        {
            if (path.Contains(nameof(DisplayMovementController)))
            {
                var pref = DataLoader.LoadFileFromMemory<GameObject>(BundleName, "ControllerPrefab");
                pref.AddComponent<DisplayMovementController>();
                //pref.gameObject.SetActive(false);
                return pref;
            }
            return null;
        }

        #endregion


        public static void UpdateUI(CellPosition mapCell, ObjHighlightController __instance)
        {
            Monster monster = __instance._creatures.GetMonster(mapCell.X, mapCell.Y);
            if (monster != null)
            {
                float scaleSize = (float)_gameCamera.assetsPPU / (float)_perfectPPU;
                _controller.SetEnemy(monster, monster.transform.position, scaleSize);
            }
            else
            {
                _controller.DisableUI();
            }
        }

        public static void ForceDisableUI()
        {
            _controller?.DisableUI();
        }
    }
}