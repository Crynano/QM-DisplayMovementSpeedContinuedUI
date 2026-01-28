using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public class ModConfig
    {
        public bool DebugMode { get; set; } = false;
        public bool EnabledHealthBar { get; set; } = true;
        public bool EnabledAttackType { get; set; } = true;
        public bool EnabledDamageType { get; set; } = true;
        public bool EnabledActionPoints { get; set; } = true;
        public bool EnabledNumericHealth { get; set; } = true;
        public bool HealthBarBlink { get; set; } = true;
        public float HealthBarBlinkSpeed { get; set; } = 2f;
        public bool EnableRemainingHealthPreviewBar { get; set; } = true;
        
        //public Color BackgroundHealthColor { get; set; } = Color.black; // WONT BE USED.
        public bool HealthChunkEnabled { get; set; } = true;
        public int HealthChunkValue { get; set; } = 20;

        [JsonProperty("RemainingHealthColor")]
        private string _remainingHealthColor;
        [JsonProperty("HealthChunkDividerColor")]
        private string _healthChunkDividerColor;

        
        [JsonProperty("CurrentHealthColor")]
        private string _currentHealthColor;
        
        [JsonIgnore]
        public Color CurrentHealthColor
        {
            get
            {
                ColorUtility.TryParseHtmlString($"#{_currentHealthColor}", out Color result);
                return result;
            }
            set => _currentHealthColor = ColorUtility.ToHtmlStringRGBA(value);
        }

        [JsonIgnore]
        public Color RemainingHealthColor
        {
            get
            {
                ColorUtility.TryParseHtmlString($"#{_remainingHealthColor}", out Color result);
                return result;
            }
            set => _remainingHealthColor = ColorUtility.ToHtmlStringRGBA(value);
        }
        
        [JsonIgnore]public Color HealthChunkDividerColor
        {
            get
            {
                ColorUtility.TryParseHtmlString($"#{_healthChunkDividerColor}", out Color result);
                return result;
            }
            set => _healthChunkDividerColor = ColorUtility.ToHtmlStringRGBA(value);
        }
       

        // [NonSerialized] [JsonIgnore] private static readonly JsonSerializerSettings _options = new JsonSerializerSettings()
        // {
        //     Formatting = Formatting.Indented,
        //     ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        // };

        public ModConfig()
        {
            CurrentHealthColor = Color.red;
            ColorUtility.TryParseHtmlString("#FFA600", out var remainingNewColor);
            RemainingHealthColor = remainingNewColor;
            HealthChunkDividerColor = Color.white;
        }

        public ModConfig LoadFromIniConfig(string configPath)
        {
            ModConfig finalModConfig = new ModConfig();
            if (File.Exists(configPath))
            {
                var sourceLines = File.ReadAllLines(configPath);

                foreach (var line in sourceLines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.Contains('='))
                    {
                        // Key-value pair
                        string[] keyValue = trimmedLine.Split(new[] { '=' }, 2);
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();
                        var convertedValue = ConvertValue(value);
                        PropertyInfo propertyInfo = this.GetType().GetProperty(key,
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                        propertyInfo?.SetValue(finalModConfig, convertedValue, null);
#if DEBUG
                        Debug.Log(
                            $"Tried to set property of {propertyInfo?.Name} as {propertyInfo?.PropertyType} against {key} with value {convertedValue} of type {convertedValue.GetType()}");
#endif
                    }
                }
            }
            else
            {
                return null;
            }

            return finalModConfig;
        }

        public static ModConfig LoadConfigJson(string configPath)
        {
            return File.Exists(configPath)
                ? JsonConvert.DeserializeObject<ModConfig>(File.ReadAllText(configPath))
                : new ModConfig();
        }

        public void SaveConfigJson(string configPath)
        {
            Logger.LogDebug("SaveConfigJson(): Saving config...");
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(this, Formatting.Indented);
            Logger.LogDebug("SaveConfigJson(): Object is serialized");
            File.WriteAllText(configPath, data);
            Logger.LogDebug("SaveConfigJson(): Saved config");
        }

        public void LoadConfig(Dictionary<string, object> propertiesDictionary)
        {
            Logger.LogDebug("Loading config");
            foreach (var nameValuePair in propertiesDictionary)
            {
                Logger.LogDebug($"Loading info property {nameValuePair.Key} with value {nameValuePair.Value}");
                try
                {
                    PropertyInfo propertyInfo = this.GetType().GetProperty(nameValuePair.Key,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    Logger.LogDebug($"Does property exist? {propertyInfo != null}");
                    Logger.LogDebug($"Loading property named {nameValuePair.Key} with value {nameValuePair.Value} of type {propertyInfo?.PropertyType}");
                    propertyInfo?.SetValue(this, nameValuePair.Value,
                        null);
                    Logger.LogDebug("Post assignment debug.");
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message);
                }
            }
            Logger.LogDebug("LoadConfig(): Finished loading properties into memory!");
        }

        private object ConvertValue(string value)
        {
            if (int.TryParse(value, out int intValue))
            {
                return intValue;
            }

            if (float.TryParse(value, out float floatValue))
            {
                return floatValue;
            }

            if (bool.TryParse(value, out bool boolValue))
            {
                return boolValue;
            }

            // Color parse
            if (ColorUtility.TryParseHtmlString(value.Replace("\"", string.Empty), out Color colorParsed))
            {
                return colorParsed;
            }

            return value;
        }
    }
}