using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static T LoadFileFromMemory<T>(string resourceName, string fileName) where T : class
        {
            return LoadFilesFromMemory<T>(resourceName, new List<string>() {fileName}).ToList()[0];
        }
        
        public static IEnumerable<T> LoadFilesFromMemory<T>(string resourceName, List<string> fileNames) where T : class
        {
            if (string.IsNullOrEmpty(resourceName) || fileNames == null || fileNames.Count < 1)
            {
                return null;
            }

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                Logger.LogError("ASSETBUNDLE COULD NOT BE LOADED");
                return null;
            }
            
            //foreach
            AssetBundle loadedBundle = AssetBundle.LoadFromStream(stream);
            var result = new List<T>();
            
            // Optimize asset recovery by only opening/closing bundle once not N.
            foreach (var singleAsset in fileNames)
            {
                if (loadedBundle.LoadAsset(singleAsset, typeof(T)) is T loadedAsset)
                {
                    result.Add(loadedAsset);
                }
            }
            
            loadedBundle.Unload(false);

            stream.Position = 0;
            return result;
        }
    }
}