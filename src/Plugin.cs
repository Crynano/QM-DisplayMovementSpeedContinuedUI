using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TinyJson;
using UnityEngine;
using System.Linq;

namespace QM_DisplayMovementSpeedContinuedUIPermanent
{
    public class Plugin
    {
        public static KeyCode toggleKey = KeyCode.Comma;
        public static bool IsEnabled = true;

        public static ConfigDirectories ModDirectories = new ConfigDirectories();

        public static string RootFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static bool IsHealthBarEnabled = true;
        public static bool IsAttackTypeEnabled = true;
        public static bool IsActionPointsEnabled = true;
        public static bool IsUIEnabled = true;

        [Header("Config")]
        public static float minScale = .66f;
        public static float maxScale = 1f;

        internal static DisplayMovementUIPooler pooler;

        internal static GameCamera gameCamera;

        #region MGSC Hooks

        [Hook(ModHookType.AfterBootstrap)]
        public static void Bootstrap(IModContext context)
        {
            string configPath = ModDirectories.ConfigPath;

            // thanks NBK_redspy, i just looked at your code because i had no idea how to do this
            // From NBK_RedSpy:  You are welcome ;)
            // samee
            if (File.Exists(configPath))
            {
                try
                {
                    string fileJson = File.ReadAllText(configPath);
                    Dictionary<string, string> values = fileJson.FromJson<Dictionary<string, string>>();
                    toggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), values["toggleKey"]);
                    IsHealthBarEnabled = bool.Parse(values["IsHealthBarEnabled"]);
                    IsAttackTypeEnabled = bool.Parse(values["IsAttackTypeEnabled"]);
                    IsActionPointsEnabled = bool.Parse(values["IsActionPointsEnabled"]);
                }
                catch (Exception ex)
                {
                    Debug.Log("QM_DisplayMovementSpeedContinuedUIPermanentVersion: Error reading config file");
                    Debug.LogException(ex);
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(ModDirectories.ModPersistenceFolder);

                    var text = "{\"toggleKey\":\"Comma\",";
                    text += "\"IsHealthBarEnabled\":\"true\",";
                    text += "\"IsAttackTypeEnabled\":\"true\",";
                    text += "\"IsActionPointsEnabled\":\"true\"}";
                    File.WriteAllText(configPath, text);
                }
                catch (Exception ex)
                {
                    Debug.Log("QM_DisplayMovementSpeedContinuedUIPermanentVersion: Error writing to config");
                    Debug.LogException(ex);
                }
            }

            // Plugin startup logic
            var harmony = new Harmony("QM_DisplayMovementSpeedContinuedUIPermanentVersion");
            harmony.PatchAll();
        }

        [Hook(ModHookType.DungeonStarted)]
        public static void InstantiateManager(IModContext context)
        {
            gameCamera = GameObject.FindObjectOfType<GameCamera>();
            // This just spawns the manager
            var canvas = GameObject.FindObjectsOfType<Canvas>().First(x => x.name.Contains("UI"));
            var dungeonUI = canvas.transform.Find("Content").gameObject;

            try
            {
                if (dungeonUI.transform.Find("DungeonUI").gameObject != null)
                    dungeonUI = dungeonUI.transform.Find("DungeonUI").gameObject;
            }
            catch(Exception e) { }
            
            //var dungeonUI = GameObject.FindObjectOfType<DungeonUI>().gameObject;

            pooler = dungeonUI.GetComponent<DisplayMovementUIPooler>();

            if (pooler == null)
                pooler = dungeonUI.AddComponent<DisplayMovementUIPooler>();

            pooler.LoadDungeon();
        }

        [Hook(ModHookType.DungeonFinished)]
        public static void Unload(IModContext context)
        {
            pooler?.UnloadDungeon();
        }


        [Hook(ModHookType.DungeonUpdateAfterGameLoop)]
        public static void DungeonUpdateAfterGameLoop(IModContext context)
        {
            // Instead of disabling UI on  ESC or so, why not render it first or behind of the UI.
            //IsUIEnabled = DungeonUI.Instance != null && !DungeonUI.Instance.InventoryScreen.IsActive && !DungeonUI.Instance.MinimapScreen._active;
            UpdateUI(DungeonGameMode.Instance.Creatures.Monsters);
            if (InputHelper.GetKeyDown(toggleKey))
            {
                IsEnabled = !IsEnabled;
                //IsUIEnabled = IsEnabled;
            }
        }
        #endregion

        // New
        public static void UpdateUI(List<Creature> monsters)
        {
            var sizeDelta = maxScale;
            if (gameCamera != null)
            {
                sizeDelta = Mathf.Lerp(maxScale, minScale, (float)gameCamera._currentZoomIndex / (float)gameCamera._zoomLevels.Length);
            }
            foreach (Monster singleMonster in monsters)
            {
                UpdateUI(singleMonster, sizeDelta);
            }
        }

        private static void UpdateUI(Monster monster, float sizeDelta)
        {
            pooler?.GetInstance(monster)?.UpdateElement(sizeDelta);
        }

        public static void ForceDisableUI()
        {
            IsUIEnabled = false;
        }
    }

    //[HarmonyPatch(typeof(ObjHighlightController), nameof(ObjHighlightController.Unhighlight))]
    //public static class Patch_ObjHighlightController_Unhighlight
    //{
    //    public static void Postfix()
    //    {
    //        Plugin.ForceDisableUI();
    //    }
    //}
}
