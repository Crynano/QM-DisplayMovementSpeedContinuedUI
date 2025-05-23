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
        public bool EnabledHealthBar { get; set; } = true;
        public bool EnabledAttackType { get; set; } = true;
        public bool EnabledActionPoints { get; set; } = true;
        public bool HealthBarBlink { get; set; } = true;
        public bool EnableRemainingHealthPreviewBar { get; set; } = true;
        public Color CurrentHealthColor { get; set; }
        public Color RemainingHealthColor { get; set; }
        public Color BackgroundHealthColor { get; set; }
        public bool HealthChunkEnabled { get; set; } = true;
        public int HealthChunkValue { get; set; } = 20;
        public Color HealthChunkDividerColor { get; set; }

        public void LoadConfig(string configPath)
        {
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
                        PropertyInfo propertyInfo = this.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                        propertyInfo?.SetValue(this, convertedValue, null);
#if DEBUG
                        Debug.Log($"Tried to set property of {propertyInfo?.Name} as {propertyInfo?.PropertyType} against {key} with value {convertedValue} of type {convertedValue.GetType()}");
#endif
                    }
                }
            }
        }

        public void LoadConfig(Dictionary<string, object> propertiesDictionary)
        {
            foreach (var nameValuePair in propertiesDictionary)
            {
                PropertyInfo propertyInfo = this.GetType().GetProperty(nameValuePair.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
#if DEBUG
                if (propertyInfo == null) { Debug.LogWarning($"[MORE STACK SIZE] Property {nameValuePair.Key} not found"); continue; }
#endif
                propertyInfo?.SetValue(this, Convert.ChangeType(nameValuePair.Value, propertyInfo.PropertyType), null);
#if DEBUG
                Debug.Log($"Tried to set property of {propertyInfo?.Name} as {propertyInfo?.PropertyType} against {nameValuePair.Key} with value {nameValuePair.Value} of type {propertyInfo?.PropertyType}");
#endif
            }
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