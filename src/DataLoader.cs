using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUIPermanent
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
    }
}