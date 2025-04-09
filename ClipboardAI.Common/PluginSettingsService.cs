using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipboardAI.Common
{
    /// <summary>
    /// Service for managing plugin settings
    /// </summary>
    public class PluginSettingsService
    {
        private static readonly string SettingsBaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardAI",
            "Plugins",
            "Settings");

        /// <summary>
        /// Load settings for a plugin
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>Dictionary of plugin settings</returns>
        public static Dictionary<string, object> LoadSettings(string pluginId)
        {
            try
            {
                // Create the settings file path
                string settingsFile = Path.Combine(SettingsBaseDirectory, $"{pluginId}.json");
                
                // Check if the settings file exists
                if (File.Exists(settingsFile))
                {
                    // Read the settings from the file
                    string json = File.ReadAllText(settingsFile);
                    
                    // Deserialize the settings
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        json, 
                        new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            Converters = { new ObjectJsonConverter() }
                        });
                    
                    Console.WriteLine($"Loaded settings for plugin {pluginId} from {settingsFile}");
                    return settings ?? new Dictionary<string, object>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading plugin settings for {pluginId}: {ex.Message}");
            }
            
            // Return an empty dictionary if no settings are found or an error occurs
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Save settings for a plugin
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <param name="settings">Dictionary of plugin settings</param>
        public static void SaveSettings(string pluginId, Dictionary<string, object> settings)
        {
            try
            {
                // Create the directory if it doesn't exist
                if (!Directory.Exists(SettingsBaseDirectory))
                {
                    Directory.CreateDirectory(SettingsBaseDirectory);
                }
                
                // Create the settings file path
                string settingsFile = Path.Combine(SettingsBaseDirectory, $"{pluginId}.json");
                
                // Serialize the settings to JSON
                string json = JsonSerializer.Serialize(
                    settings, 
                    new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                
                // Write the settings to the file
                File.WriteAllText(settingsFile, json);
                
                Console.WriteLine($"Saved settings for plugin {pluginId} to {settingsFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving plugin settings for {pluginId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the plugin data directory
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>Path to the plugin data directory</returns>
        public static string GetPluginDataPath(string pluginId)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClipboardAI",
                "Plugins",
                "Data",
                pluginId);
                
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            return path;
        }
    }

    /// <summary>
    /// JSON converter for handling Dictionary<string, object> properly
    /// </summary>
    public class ObjectJsonConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long longValue))
                        return longValue;
                    return reader.GetDouble();
                case JsonTokenType.String:
                    if (reader.TryGetDateTime(out DateTime datetime))
                        return datetime;
                    return reader.GetString();
                case JsonTokenType.StartObject:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        return doc.RootElement.Clone();
                    }
                default:
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
        }
    }
}
