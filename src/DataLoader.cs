using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public static class DataLoader
    {
        public static T LoadFileFromBundle<T>(string bundleName, string fileName) where T : class
        {
            var fullPath = Path.Combine(Plugin.RootFolder, bundleName);
            if (!File.Exists(fullPath)) { Debug.LogError($"Could not load bundle at {fullPath}"); return null; }
            var loadedBundle = AssetBundle.LoadFromFile(fullPath);
            var loadedAsset = loadedBundle.LoadAsset(fileName, typeof(T)) as T;
            loadedBundle.Unload(false);
            if (loadedAsset != null)
            {
                return loadedAsset;
            }
            else
            {
                Debug.Log($"Returning null asset from {bundleName} and {fileName}");
                return null;
            }
        }

        public static T[] LoadFilesFromBundle<T>(string bundleName, List<string> fileNames) where T : class
        {
            var fullPath = Path.Combine(Plugin.RootFolder, bundleName);
            if (!File.Exists(fullPath)) { Debug.LogError($"Could not load bundle at {fullPath}"); return null; }
            var loadedBundle = AssetBundle.LoadFromFile(fullPath);
            T[] loadedAssets = new T[fileNames.Count];
            for (int i = 0; i < fileNames.Count; i++)
            {
                loadedAssets[i] = loadedBundle.LoadAsset(fileNames[i], typeof(T)) as T;
            }
            loadedBundle.Unload(false);
            return loadedAssets;
        }

        public static IEnumerable<T> LoadFilesFromMemory<T>(string resourceName, List<string> fileNames) where T : class
        {
            List<T> result = new List<T>();
            foreach (var fileName in fileNames)
            {
                result.Add(LoadFileFromMemory<T>(resourceName, fileName));
            }
            return result;
        }
        
        public static T LoadFileFromMemory<T>(string resourceName, string fileName) where T : class
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(resourceName))
            {
                return null;
            }

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                Logger.LogError("ASSETBUNDLE COULD NOT BE LOADED");
                return null;
            }

            AssetBundle loadedBundle = AssetBundle.LoadFromStream(stream);
            var loadedAsset = loadedBundle.LoadAsset(fileName, typeof(T)) as T;
            loadedBundle.Unload(false);

            stream.Position = 0;

            if (loadedAsset != null)
            {
                //Logger.LogInfo($"Loaded asset correctly! Returning {loadedAsset.GetType()}");
                return loadedAsset;
            }
            return null;
        }
    }
}