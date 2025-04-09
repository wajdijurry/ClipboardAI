using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;
using System.Reflection;

namespace ClipboardAI.Common
{
    /// <summary>
    /// Application settings class that handles loading/saving configuration
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardAI",
            "settings.json");
            
        private static AppSettings _instance;
        
        /// <summary>
        /// Gets the singleton instance of the application settings
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        // General settings
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public string DefaultLanguage { get; set; } = "en";

        // Plugin settings
        private Dictionary<string, bool> _enabledPlugins = new Dictionary<string, bool>();
        private Dictionary<string, Dictionary<string, object>> _pluginSettings = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Gets or sets the enabled plugins dictionary
        /// </summary>
        public Dictionary<string, bool> EnabledPlugins
        {
            get { return _enabledPlugins; }
            set { _enabledPlugins = value; }
        }
        
        /// <summary>
        /// Gets or sets the plugin settings dictionary
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> PluginSettings
        {
            get { return _pluginSettings; }
            set { _pluginSettings = value; }
        }

        // AI model settings
        public string ModelDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
        
        // Advanced settings
        public int ProcessingThreads { get; set; } = 2;
        public int MemoryLimitMB { get; set; } = 1024;
        public bool EnableDebugLogging { get; set; } = false;
        public bool UseCpuOnly { get; set; } = false;
        
        // Clipboard history settings
        public int MaxHistorySize { get; set; } = 100;
        public int ExpirationDays { get; set; } = 30;
        public bool EnableFuzzySearch { get; set; } = true;
        public bool EnableMultiClipboard { get; set; } = true;
        
        // Security settings
        public bool EncryptSensitiveData { get; set; } = false;
        public bool AutoClearSensitiveData { get; set; } = false;
        public bool UseAppWhitelisting { get; set; } = false;
        public List<string> WhitelistedApps { get; set; } = new List<string>();
        public bool EnableAuditLogging { get; set; } = false;
        
        // Team features
        public bool EnableSharedClipboard { get; set; } = false;
        public string SharedClipboardAddress { get; set; } = "localhost";
        public int SharedClipboardPort { get; set; } = 8765;
        public string SharedClipboardPassword { get; set; } = "";
        
        // UI settings
        public string Theme { get; set; } = "Light";
        public bool EnableSoundFeedback { get; set; } = false;
        public bool ShowMiniPreview { get; set; } = true;
        public bool EnableHotkeys { get; set; } = true;
        public Dictionary<string, string> CustomHotkeys { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Normalizes a plugin ID to a consistent format
        /// </summary>
        /// <param name="pluginId">The plugin ID to normalize</param>
        /// <returns>The normalized plugin ID</returns>
        private string NormalizePluginId(string pluginId)
        {
            // Handle null or empty IDs
            if (string.IsNullOrEmpty(pluginId))
            {
                return string.Empty;
            }
            
            // Convert to lowercase for case-insensitive comparison
            string normalizedId = pluginId;
            
            // If the ID is fully qualified (contains dots), extract the relevant part
            if (normalizedId.Contains("."))
            {
                // If it starts with ClipboardAI.Plugins., extract the last part
                if (normalizedId.StartsWith("ClipboardAI.Plugins.", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedId = normalizedId.Substring("ClipboardAI.Plugins.".Length);
                }
                // If it starts with ClipboardAI.Plugin., extract the last part (singular form)
                else if (normalizedId.StartsWith("ClipboardAI.Plugin.", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedId = normalizedId.Substring("ClipboardAI.Plugin.".Length);
                }
                // If it starts with com.clipboardai.plugins., extract the last part
                else if (normalizedId.StartsWith("com.clipboardai.plugins.", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedId = normalizedId.Substring("com.clipboardai.plugins.".Length);
                }
            }
            
            // Convert to PascalCase (first letter uppercase, rest unchanged)
            if (normalizedId.Length > 0)
            {
                normalizedId = char.ToUpperInvariant(normalizedId[0]) + 
                              (normalizedId.Length > 1 ? normalizedId.Substring(1) : string.Empty);
            }
            
            return normalizedId;
        }

        /// <summary>
        /// Cleans up duplicate plugin entries in the EnabledPlugins dictionary
        /// </summary>
        private void CleanupDuplicatePluginEntries()
        {
            if (EnabledPlugins == null)
            {
                EnabledPlugins = new Dictionary<string, bool>();
                return;
            }

            // Create a new dictionary to store normalized entries
            var normalizedEntries = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            
            // Track which keys we've processed to avoid duplicates
            var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Log the cleanup process
            Console.WriteLine("Cleaning up duplicate plugin entries...");
            Console.WriteLine($"Before cleanup: {EnabledPlugins.Count} entries");
            
            foreach (var entry in EnabledPlugins)
            {
                string normalizedKey = NormalizePluginId(entry.Key);
                
                // Skip if we've already processed this key (case-insensitive)
                if (processedKeys.Contains(normalizedKey))
                {
                    Console.WriteLine($"Skipping duplicate entry: {entry.Key} (normalized to {normalizedKey})");
                    continue;
                }
                
                // Add the normalized key to the new dictionary
                normalizedEntries[normalizedKey] = entry.Value;
                processedKeys.Add(normalizedKey);
                
                Console.WriteLine($"Processed plugin entry: {entry.Key} (normalized to {normalizedKey}), enabled = {entry.Value}");
            }
            
            // Replace the original dictionary with the normalized one
            _enabledPlugins = normalizedEntries;
            
            Console.WriteLine($"After cleanup: {_enabledPlugins.Count} entries");
            foreach (var entry in _enabledPlugins)
            {
                Console.WriteLine($"  Plugin: {entry.Key}, Enabled: {entry.Value}");
            }
            
            // Also clean up plugin-specific settings with the same normalization
            if (_pluginSettings != null)
            {
                var normalizedSettings = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
                var processedSettingsKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                Console.WriteLine($"Cleaning up plugin settings. Before cleanup: {_pluginSettings.Count} entries");
                
                foreach (var entry in _pluginSettings)
                {
                    string normalizedKey = NormalizePluginId(entry.Key);
                    
                    // Skip if we've already processed this key (case-insensitive)
                    if (processedSettingsKeys.Contains(normalizedKey))
                    {
                        Console.WriteLine($"Skipping duplicate settings entry: {entry.Key} (normalized to {normalizedKey})");
                        continue;
                    }
                    
                    // Add the normalized key to our processed set
                    processedSettingsKeys.Add(normalizedKey);
                    
                    // Add the entry with the normalized key to our new dictionary
                    normalizedSettings[normalizedKey] = entry.Value;
                    
                    Console.WriteLine($"Added normalized settings entry: {normalizedKey}");
                }
                
                // Replace the old settings dictionary with the normalized one
                _pluginSettings = normalizedSettings;
                
                Console.WriteLine($"After plugin settings cleanup: {_pluginSettings.Count} entries");
            }
        }

        /// <summary>
        /// Checks if a plugin is enabled
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>True if the plugin is enabled, false otherwise</returns>
        public bool IsPluginEnabled(string pluginId)
        {
            // Normalize the plugin ID
            string normalizedId = NormalizePluginId(pluginId);
            
            // Log the plugin ID normalization for debugging
            Console.WriteLine($"IsPluginEnabled: Original ID '{pluginId}' normalized to '{normalizedId}'");
            
            // Check if the plugin is enabled
            if (_enabledPlugins.TryGetValue(normalizedId, out var enabled))
            {
                Console.WriteLine($"IsPluginEnabled: Found plugin '{normalizedId}' in _enabledPlugins, enabled = {enabled}");
                return enabled;
            }
            
            Console.WriteLine($"IsPluginEnabled: Plugin '{normalizedId}' not found in _enabledPlugins, returning false");
            return false;
        }

        /// <summary>
        /// Sets whether a plugin is enabled
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <param name="enabled">Whether the plugin should be enabled</param>
        public void SetPluginEnabled(string pluginId, bool enabled)
        {
            // Normalize the plugin ID
            string normalizedId = NormalizePluginId(pluginId);
            
            // Log the plugin ID normalization for debugging
            Console.WriteLine($"SetPluginEnabled: Original ID '{pluginId}' normalized to '{normalizedId}', setting to {enabled}");
            
            // Set the enabled state
            _enabledPlugins[normalizedId] = enabled;
            
            // Force a save to disk to ensure the setting persists
            Save();
            
            // Verify the setting was saved
            if (_enabledPlugins.TryGetValue(normalizedId, out var savedEnabled))
            {
                Console.WriteLine($"SetPluginEnabled: Verified plugin '{normalizedId}' enabled state: {savedEnabled}");
            }
            else
            {
                Console.WriteLine($"SetPluginEnabled: Failed to verify plugin '{normalizedId}' enabled state");
            }
        }

        /// <summary>
        /// Gets a plugin setting, using normalized IDs
        /// </summary>
        public T GetPluginSetting<T>(string pluginId, string settingName, T defaultValue)
        {
            // Normalize the plugin ID
            string normalizedId = NormalizePluginId(pluginId);
            
            // Log the plugin ID normalization for debugging
            Console.WriteLine($"GetPluginSetting: Original ID '{pluginId}' normalized to '{normalizedId}'");
            
            // Check if the plugin has any settings
            if (_pluginSettings.TryGetValue(normalizedId, out var settings))
            {
                // Check if the specific setting exists
                if (settings.TryGetValue(settingName, out var value))
                {
                    try
                    {
                        // Convert the value to the requested type
                        if (value is JsonElement jsonElement)
                        {
                            // Handle JsonElement conversion
                            return ConvertJsonElementToType<T>(jsonElement);
                        }
                        else if (value is T typedValue)
                        {
                            return typedValue;
                        }
                        else
                        {
                            // Try to convert the value to the requested type
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting setting {settingName} for plugin {normalizedId}: {ex.Message}");
                    }
                }
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Sets a plugin setting, using normalized IDs
        /// </summary>
        public void SetPluginSetting<T>(string pluginId, string settingName, T value)
        {
            string normalizedId = NormalizePluginId(pluginId);
            
            if (!_pluginSettings.ContainsKey(normalizedId))
            {
                _pluginSettings[normalizedId] = new Dictionary<string, object>();
            }
            _pluginSettings[normalizedId][settingName] = value;
        }

        /// <summary>
        /// Gets all settings for a plugin, using normalized IDs
        /// </summary>
        public Dictionary<string, object> GetAllPluginSettings(string pluginId)
        {
            string normalizedId = NormalizePluginId(pluginId);
            
            // First try with the normalized ID
            if (_pluginSettings.TryGetValue(normalizedId, out var settings))
            {
                return new Dictionary<string, object>(settings);
            }
            
            // Then try with the original ID
            if (_pluginSettings.TryGetValue(pluginId, out settings))
            {
                return new Dictionary<string, object>(settings);
            }
            
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets a list of all enabled plugins
        /// </summary>
        public List<string> GetEnabledPlugins()
        {
            return _enabledPlugins
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Converts a JsonElement to the specified type
        /// </summary>
        private T ConvertJsonElementToType<T>(JsonElement jsonElement)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)jsonElement.GetString();
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)jsonElement.GetInt32();
                }
                else if (typeof(T) == typeof(bool))
                {
                    return (T)(object)jsonElement.GetBoolean();
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)jsonElement.GetDouble();
                }
                else
                {
                    // For complex types, use JSON deserialization
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting JsonElement to type {typeof(T).Name}: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Save settings to the configuration file
        /// </summary>
        public void Save()
        {
            try
            {
                // Clean up duplicate plugin entries before saving
                CleanupDuplicatePluginEntries();
                
                string configPath = GetConfigFilePath();
                string configDir = Path.GetDirectoryName(configPath);
                
                Console.WriteLine($"Saving settings to: {configPath}");
                
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(configPath, json);
                Console.WriteLine($"Settings saved to disk: {configPath}");
                
                // Log all plugin settings for debugging
                foreach (var pluginId in _pluginSettings.Keys)
                {
                    var settings = _pluginSettings[pluginId];
                    Console.WriteLine($"Plugin {pluginId} settings: {string.Join(", ", settings.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                }
                
                // Notify plugins of settings changes
                try
                {
                    // Use reflection to avoid direct dependency on Plugins assembly
                    var pluginManagerType = Type.GetType("ClipboardAI.Plugins.PluginManager, ClipboardAI.Plugins");
                    if (pluginManagerType != null)
                    {
                        var instanceProperty = pluginManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (instanceProperty != null)
                        {
                            var pluginManager = instanceProperty.GetValue(null);
                            if (pluginManager != null)
                            {
                                var refreshMethod = pluginManagerType.GetMethod("RefreshPlugins");
                                if (refreshMethod != null)
                                {
                                    refreshMethod.Invoke(pluginManager, null);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error notifying plugins of settings changes: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load settings from the configuration file
        /// </summary>
        /// <returns>Loaded settings or default settings if file not found</returns>
        public static AppSettings Load()
        {
            try
            {
                string configPath = GetConfigFilePath();
                
                Console.WriteLine($"Loading settings from: {configPath}");
                Console.WriteLine($"File exists: {File.Exists(configPath)}");
                
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    
                    // Initialize dictionaries if they're null
                    if (settings._enabledPlugins == null)
                    {
                        settings._enabledPlugins = new Dictionary<string, bool>();
                    }
                    
                    if (settings._pluginSettings == null)
                    {
                        settings._pluginSettings = new Dictionary<string, Dictionary<string, object>>();
                    }
                    
                    // Debug: Dump the contents of the EnabledPlugins dictionary
                    Console.WriteLine("Loaded EnabledPlugins from settings file:");
                    foreach (var plugin in settings._enabledPlugins)
                    {
                        Console.WriteLine($"  Plugin: {plugin.Key}, Enabled: {plugin.Value}");
                    }
                    
                    // Clean up duplicate plugin entries
                    settings.CleanupDuplicatePluginEntries();
                    
                    // Debug: Dump the contents of the EnabledPlugins dictionary after cleanup
                    Console.WriteLine("EnabledPlugins after cleanup:");
                    foreach (var plugin in settings._enabledPlugins)
                    {
                        Console.WriteLine($"  Plugin: {plugin.Key}, Enabled: {plugin.Value}");
                    }
                    
                    return settings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            return new AppSettings();
        }
        
        /// <summary>
        /// Gets the path to the configuration file
        /// </summary>
        private static string GetConfigFilePath()
        {
            return SettingsFilePath;
        }
    }
}
