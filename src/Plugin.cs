using HarmonyLib;
using MGSC;
using ModConfigMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public class Plugin
    {
        public static string RootFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static float MinScale = 0.66f;
        public static float MaxScale = 1.0f;

        public static GameCamera GameCamera;

        private static DisplayMovementController _controller;

        public static string ModAssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
        public static string ModPersistenceFolder => Path.Combine($"{Application.persistentDataPath}/../Quasimorph_ModConfigs", ModAssemblyName);
        public static string ConfigPath => Path.Combine(ModPersistenceFolder, ConfigName);
        private const string ConfigName = "config.ini";
        public static ModConfig Config { get; private set; } = new ModConfig();

        public static string BundleName = "apcontrollerbundle";

        private const string OriginalModConfigB64 = "W0dlbmVyYWxdCiN0b29sdGlwIEVuYWJsZXMgdGhlIGhlYWx0aGJhciBwYXJ0IG9mIHRoZSBVSS4KI2xhYmVsIEVuYWJsZSBIZWFsdGggQmFyCiNkZWZhdWx0IHRydWUKRW5hYmxlZEhlYWx0aEJhciA9IFRydWUKCiN0b29sdGlwIEVuYWJsZXMgdGhlIGF0dGFjayB0eXBlIGljb24gcGFydCBvZiB0aGUgVUkuCiNsYWJlbCBFbmFibGUgQXR0YWNrIEljb24KI2RlZmF1bHQgdHJ1ZQpFbmFibGVkQXR0YWNrVHlwZSA9IFRydWUKCiN0b29sdGlwIEVuYWJsZXMgdGhlIG51bWVyaWMgYWN0aW9uIHBvaW50cyBkaXNwbGF5IHBhcnQgb2YgdGhlIFVJLgojbGFiZWwgRW5hYmxlIEFjdGlvbiBQb2ludHMgRGlzcGxheQojZGVmYXVsdCB0cnVlCkVuYWJsZWRBY3Rpb25Qb2ludHMgPSBUcnVlCgpbSGVhbHRoIEJhcl0KI3Rvb2x0aXAgRW5hYmxlcyB0aGUgYmxpbmtpbmcgb2YgdGhlIGF2ZXJhZ2UgZGFtYWdlIGRpc3BsYXkuCiNsYWJlbCBFbmFibGUgSGVhbHRoIEJhciBCbGluawojZGVmYXVsdCB0cnVlCkhlYWx0aEJhckJsaW5rID0gVHJ1ZQoKI3Rvb2x0aXAgRW5hYmxlcyBhIGJhciB0byBkaXNwbGF5IHRoZSByZW1haW5pbmcgaGVhbHRoIHRoZSBlbmVteSBjb3VsZCBoYXZlIGFmdGVyIGFuIGF2ZXJhZ2UgaGl0LgojbGFiZWwgRW5hYmxlIFJlbWFpbmluZyBIZWFsdGggUHJldmlldyBCYXIKI2RlZmF1bHQgdHJ1ZQpFbmFibGVSZW1haW5pbmdIZWFsdGhQcmV2aWV3QmFyID0gVHJ1ZQoKI3Rvb2x0aXAgQ29sb3IgZm9yIHRoZSBjdXJyZW50IGFtb3VudCBvZiBoZWFsdGggYSB1bml0IGhhcy4KI2xhYmVsIEN1cnJlbnQgSGVhbHRoIENvbG9yCiNkZWZhdWx0ICNGRjAwMDAKQ3VycmVudEhlYWx0aENvbG9yID0gI0ZGMDAwMAoKI3Rvb2x0aXAgQ29sb3IgZm9yIHRoZSByZW1haW5pbmcgaGVhbHRoIGEgdW5pdCB3b3VsZCBoYXZlIGFmdGVyIGFuIGF2ZXJhZ2UgaGl0LgojbGFiZWwgUmVtYWluaW5nIEhlYWx0aCBDb2xvcgojZGVmYXVsdCAjRkZBNjAwClJlbWFpbmluZ0hlYWx0aENvbG9yID0gI0ZGQTYwMAoKI3Rvb2x0aXAgQ29sb3IgZm9yIHRoZSBiYWNrZ3JvdW5kIG9mIHRoZSBoZWFsdGhiYXIuCiNsYWJlbCBCYWNrZ3JvdW5kIEhlYWx0aGJhciBDb2xvcgojZGVmYXVsdCAjMDAwMDAwCkJhY2tncm91bmRIZWFsdGhDb2xvciA9ICMwMDAwMDAKCiN0b29sdGlwIEVuYWJsZXMgdGhlIGRpc3BsYXkgb2Ygc21hbGwgYmFycyB2aXN1YWxseSBkZWxpbWl0aW5nIGEgY2VydGFpbiBhbW91bnQgb2YgaGVhbHRoLgojbGFiZWwgRW5hYmxlIEhlYWx0aCBDaHVuayBEaXZpZGVyCiNkZWZhdWx0IHRydWUKSGVhbHRoQ2h1bmtFbmFibGVkID0gVHJ1ZQoKI3Rvb2x0aXAgSG93IG11Y2ggaGVhbHRoIGEgY2h1bmsgcmVwcmVzZW50cy4KI2xhYmVsIEhlYWx0aCBDaHVuayBEaXZpZGVyIFZhbHVlCiNtaW4gNQojbWF4IDUwCiNkZWZhdWx0IDIwCkhlYWx0aENodW5rVmFsdWUgPSAyMAoKI3Rvb2x0aXAgQ29sb3IgZm9yIHRoZSBoZWFsdGggY2h1bmsgZGl2aWRlci4KI2xhYmVsIEhlYWx0aCBDaHVuayBEaXZpZGVyIENvbG9yCiNkZWZhdWx0ICNGRkZGRkYKSGVhbHRoQ2h1bmtEaXZpZGVyQ29sb3IgPSAjRkZGRkZG";

        #region MGSC Hooks

        [Hook(ModHookType.AfterBootstrap)]
        public static void Bootstrap(IModContext context)
        {
            var harmony = new Harmony("QM_DisplayMovementSpeedContinuedUI");
            harmony.PatchAll();
        }

        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfig(IModContext context)
        {
            Directory.CreateDirectory(ModPersistenceFolder);
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, EncoderHelper.Base64Decode(OriginalModConfigB64));
            }

            bool IsMCMOn = false;
            string mcmConfigPath = string.Empty;

            try
            {
                IsMCMOn = RegisterToMCM();
                mcmConfigPath = GetMCMPath();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Loading without MCM.");
                IsMCMOn = false;
                mcmConfigPath = string.Empty;
            }

#if DEBUG
            Debug.Log($"MCM for Display Movement UI [HOVER] report. On? {IsMCMOn}. File Path? {mcmConfigPath}. Exists? {File.Exists(mcmConfigPath)}");
#endif
            if (IsMCMOn && File.Exists(mcmConfigPath))
            {
#if DEBUG
                Debug.Log("Config should launch from MCM");
#endif
                Config.LoadConfig(mcmConfigPath);
            }
            else
            {
#if DEBUG
                Debug.Log("Config should launch from default");
#endif
                Config.LoadConfig(ConfigPath);
            }
        }

        private static bool RegisterToMCM()
        {
            ModConfigMenuAPI.RegisterModConfig("Display Movement UI [HOVER]", ConfigPath, delegate (Dictionary<string, object> properties)
            {
#if DEBUG
                    Debug.Log("Applying the changes in Display Movement UI [HOVER] mod!");
#endif
                Config.LoadConfig(properties);
            });
            return true;
        }

        private static string GetMCMPath()
        {
            return ModConfigMenuAPI.GetNameForConfigFile(ConfigPath);
        }

        // New
        [Hook(ModHookType.DungeonStarted)]
        public static void SpawnUI(IModContext context)
        {
            GameCamera = GameObject.FindObjectOfType<GameCamera>();
            // This just spawns the manager

            Transform dungeonUI = GameObject.FindObjectOfType<UI>().transform.Find("Content");
            try
            {
                _controller = UI.Get<DisplayMovementController>();
            }
            catch (Exception e) { Debug.LogWarning($"Warning in DisplayMovement SpawnUI\n{e}"); }
            /*
            //try
            //{
            //    if (dungeonUI.transform.Find("DungeonUI").gameObject != null)
            //        dungeonUI = dungeonUI.transform.Find("DungeonUI").gameObject;
            //}
            //catch (Exception e) { }
       

            uiController = GameObject.FindObjectOfType<DisplayMovementController>();
            uiPrefab = DataLoader.LoadFileFromBundle<GameObject>(BundleName, "ControllerPrefab");
            if (uiPrefab == null)
            {
                Debug.LogError($"Could not spawn, UI PREFAB is null");
            }
            else if (dungeonUI != null && uiController == null)
            {
                uiController = GameObject.Instantiate(uiPrefab, dungeonUI).AddComponent<DisplayMovementController>();
                uiController.transform.SetAsFirstSibling();
                uiController.name = $"[UI] DisplayMovementSpeedContinued";
                uiController.DisableUI();
#if DEBUG
                Debug.Log($"UI for DisplayMovement Controller has instantiated correctly");
#endif
            }
            else
            {
                Debug.LogError($"unsupported error?");
            }
            */
        }

        [Hook(ModHookType.ResourcesLoad)]
        public static object ResourcesLoad(string path)
        {
            if (path.Contains(nameof(DisplayMovementController)))
            {
                var pref = DataLoader.LoadFileFromBundle<GameObject>(BundleName, "ControllerPrefab");
                pref.AddComponent<DisplayMovementController>();
                return pref;
            }

            return null;
        }

        #endregion


        public static void UpdateUI(CellPosition mapCell, ObjHighlightController __instance)
        {
            var scaleSize = MaxScale;
            if (GameCamera != null)
            {
                // We can set the scale of the UI according to the current level and the UI sizes we know that fit.
                scaleSize = Mathf.Lerp(MaxScale, MinScale,
                    (float)GameCamera._currentZoomIndex / (float)GameCamera._zoomLevels.Length);
            }

            Monster monster = __instance._creatures.GetMonster(mapCell.X, mapCell.Y);
            if (monster != null)
            {
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
