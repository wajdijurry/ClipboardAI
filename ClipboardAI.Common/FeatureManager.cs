using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace ClipboardAI.Common
{
    /// <summary>
    /// Manages feature flags for the application, loading them from the registry
    /// </summary>
    public class FeatureManager
    {
        private const string RegistryKeyPath = @"SOFTWARE\ClipboardAI\Features";
        private static FeatureManager _instance;
        private readonly AppSettings _settings;

        // Mapping from Feature enum to plugin IDs
        private readonly Dictionary<Feature, string> _featureToPluginMap = new Dictionary<Feature, string>
        {
            { Feature.OCR, "OCR" },
            { Feature.Summarization, "Summarization" },
            { Feature.Paraphrasing, "Paraphrasing" },
            { Feature.CodeFormatting, "CodeFormatting" },
            { Feature.PasswordGeneration, "PasswordGeneration" },
            { Feature.EmailTemplateExpansion, "EmailTemplateExpansion" },
            { Feature.TableConversion, "TableConversion" }
        };

        public static FeatureManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FeatureManager();
                }
                return _instance;
            }
        }

        private FeatureManager()
        {
            _settings = AppSettings.Instance;
            LoadFeaturesFromRegistry();
        }

        /// <summary>
        /// Loads feature flags from the registry and updates the application settings
        /// </summary>
        public void LoadFeaturesFromRegistry()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        bool settingsChanged = false;
                        
                        // Load feature flags from registry for each plugin
                        foreach (var featureMapping in _featureToPluginMap)
                        {
                            string pluginId = featureMapping.Value;
                            string registryName = $"Enable{featureMapping.Key}";
                            
                            // Get current setting
                            bool currentSetting = _settings.IsPluginEnabled(pluginId);
                            
                            // Get registry value
                            bool registryValue = GetRegistryValue(key, registryName, currentSetting);
                            
                            // Update setting if different
                            if (currentSetting != registryValue)
                            {
                                _settings.SetPluginEnabled(pluginId, registryValue);
                                settingsChanged = true;
                            }
                        }

                        // Save updated settings if any changes were made
                        if (settingsChanged)
                        {
                            _settings.Save();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading features from registry: {ex.Message}");
                // Fall back to default settings
            }
        }

        /// <summary>
        /// Gets a boolean value from the registry
        /// </summary>
        private bool GetRegistryValue(RegistryKey key, string valueName, bool defaultValue)
        {
            object value = key.GetValue(valueName);
            if (value != null && value is int intValue)
            {
                return intValue != 0;
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks if a feature is enabled
        /// </summary>
        /// <param name="feature">The feature to check</param>
        /// <returns>True if the feature is enabled, false otherwise</returns>
        public bool IsEnabled(Feature feature)
        {
            // Text processing is a core feature that's always enabled
            if (feature == Feature.TextProcessing)
            {
                return true;
            }
            
            // Check if the feature has a plugin mapping
            if (_featureToPluginMap.TryGetValue(feature, out string pluginId))
            {
                // Check if the plugin is enabled in settings
                return _settings.IsPluginEnabled(pluginId);
            }
            
            // Unknown feature
            return false;
        }
        
        /// <summary>
        /// Enables or disables a feature
        /// </summary>
        /// <param name="feature">The feature to enable or disable</param>
        /// <param name="enabled">Whether the feature should be enabled</param>
        public void SetFeatureEnabled(Feature feature, bool enabled)
        {
            // Ignore core features
            if (feature == Feature.TextProcessing)
            {
                return;
            }
            
            // Check if the feature has a plugin mapping
            if (_featureToPluginMap.TryGetValue(feature, out string pluginId))
            {
                // Update the plugin setting
                _settings.SetPluginEnabled(pluginId, enabled);
                _settings.Save();
            }
        }
    }
}
