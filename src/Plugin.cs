using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TinyJson;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinued
{
    public class Plugin
    {
        public static KeyCode toggleKey = KeyCode.Comma;
        public static bool IsEnabled = true;

        public static ConfigDirectories ModDirectories = new ConfigDirectories();

        // New
        public static GameObject uiPrefab;
        public static DisplayMovementController uiController;

        public static string RootFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static bool IsHealthBarEnabled = true;
        public static bool IsAttackTypeEnabled = true;
        public static bool IsActionPointsEnabled = true;

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
                    Debug.Log("QM_DisplayMovementSpeedContinuedUI: Error reading config file");
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
                    Debug.Log("QM_DisplayMovementSpeedContinuedUI: Error writing to config");
                    Debug.LogException(ex);
                }
            }

            // Plugin startup logic
            var harmony = new Harmony("QM_DisplayMovementSpeedContinuedUI");
            harmony.PatchAll();
        }

        // New
        [Hook(ModHookType.DungeonStarted)]
        public static void SpawnUI(IModContext context)
        {
            // This just spawns the manager
            var canvas = GameObject.FindObjectsOfType<Canvas>().First(x => x.name.Contains("UI"));
            var dungeonUI = canvas.transform.Find("Content").gameObject;

            try
            {
                if (dungeonUI.transform.Find("DungeonUI").gameObject != null)
                    dungeonUI = dungeonUI.transform.Find("DungeonUI").gameObject;
            }
            catch (Exception e) { }

            uiController = GameObject.FindObjectOfType<DisplayMovementController>();
            uiPrefab = DataLoader.LoadFileFromBundle<GameObject>("apcontrollerbundle", "ControllerPrefab");
            if (uiPrefab == null)
            {
                Debug.LogError($"Could not spawn, UI PREFAB is null");
            }
            else if (dungeonUI.transform != null && uiController == null)
            {
                uiController = GameObject.Instantiate(uiPrefab, dungeonUI.transform).AddComponent<DisplayMovementController>();
                uiController.transform.SetAsFirstSibling();
                uiController.LoadComponents("apcontrollerbundle");
                uiController.name = $"[UI] DisplayMovementSpeedContinued";
                uiController.DisableUI();
                Debug.Log($"UI for DisplayMovement Controller has instantiated correctly");
            }
            else
            {
                Debug.LogError($"unsupported error?");
            }
        }

        [Hook(ModHookType.DungeonFinished)]
        public static void CleanUI(IModContext context)
        {
            GameObject.Destroy(uiController);
        }

        #endregion

        // New
        [Hook(ModHookType.DungeonUpdateBeforeGameLoop)]
        public static void DungeonUpdateBeforeGameLoop(IModContext context)
        {
            if (InputHelper.GetKeyDown(toggleKey))
            {
                IsEnabled = !IsEnabled;
                uiController.gameObject.SetActive(IsEnabled);
            }
        }

        public static void UpdateUI(CellPosition mapCell, ObjHighlightController __instance)
        {
            Monster monster = __instance._creatures.GetMonster(mapCell.X, mapCell.Y);
            if (monster != null)
            {
                uiController.SetEnemy(monster, monster.transform.position);
            }
            else
            {
                uiController.DisableUI();
            }
        }

        public static void ForceDisableUI()
        {
            if (uiController != null)
            {
                uiController.DisableUI();
            }
        }
    }

    // Custom new patch for UI
    [HarmonyPatch(typeof(ObjHighlightController), nameof(ObjHighlightController.Process))]
    public static class Patch_ObjHighlightController_Process
    {
        public static void Postfix(CellPosition cellUnderCursor, ObjHighlightController __instance)
        {
            Plugin.UpdateUI(cellUnderCursor, __instance);
        }
    }

    [HarmonyPatch(typeof(ObjHighlightController), nameof(ObjHighlightController.Unhighlight))]
    public static class Patch_ObjHighlightController_Unhighlight
    {
        public static void Postfix()
        {
            Plugin.ForceDisableUI();
        }
    }
}
