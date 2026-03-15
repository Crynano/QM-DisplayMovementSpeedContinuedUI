using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public class ModConfig
    {
        public enum UiMode
        {
            Off,
            AlwaysOn,
            OnlyWhenFocused
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public UiMode UIMode { get; set; } = UiMode.OnlyWhenFocused;

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

        [JsonIgnore]
        public Color HealthChunkDividerColor
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
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(configPath, data);
        }

        public void LoadConfig(Dictionary<string, object> propertiesDictionary)
        {
            foreach (var nameValuePair in propertiesDictionary)
            {
                try
                {
                    PropertyInfo propertyInfo = this.GetType()
                        .GetProperty(nameValuePair.Key,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                    if (propertyInfo.PropertyType.IsEnum)
                    {
                        //Convert from string to enum
                        object enumValue = Enum.Parse(propertyInfo.PropertyType, nameValuePair.Value.ToString());
                        propertyInfo?.SetValue(this, enumValue, null);
                    }
                    else
                    {
                        propertyInfo?.SetValue(this, nameValuePair.Value, null);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message);
                }
            }
        }

        private static object ConvertValue(string value)
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