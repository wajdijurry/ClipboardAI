using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardAI.Plugins.Features
{
    /// <summary>
    /// Plugin for formatting JSON content
    /// </summary>
    public class JsonFormatterPlugin : AIFeaturePluginBase
    {
        private Dictionary<string, object> _settings;

        // Plugin implementation
        public override string Id => "com.clipboardai.plugins.jsonformatter";
        public override string Name => "JSON Formatter";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "ClipboardAI Team";
        public override string Description => "Formats JSON content with proper indentation and structure";
        public override AIFeatureType FeatureType => AIFeatureType.JsonFormatter;

        public JsonFormatterPlugin()
        {
            _settings = new Dictionary<string, object>
            {
                { "IndentSize", 2 },
                { "WriteIndented", true },
                { "AllowTrailingCommas", false }
            };
        }

        public override async Task<string> ProcessTextAsync(string text, object options = null)
        {
            try
            {
                // Check if the input is valid JSON
                if (!IsJson(text))
                {
                    Host?.LogMessage(Id, LogLevel.Warning, "Input is not valid JSON");
                    return "Error: Input is not valid JSON";
                }
                
                // Parse and format JSON
                try
                {
                    // Get settings
                    int indentSize = 2;
                    bool writeIndented = true;
                    bool allowTrailingCommas = false;
                    
                    if (_settings.ContainsKey("IndentSize") && _settings["IndentSize"] is int)
                        indentSize = (int)_settings["IndentSize"];
                        
                    if (_settings.ContainsKey("WriteIndented") && _settings["WriteIndented"] is bool)
                        writeIndented = (bool)_settings["WriteIndented"];
                        
                    if (_settings.ContainsKey("AllowTrailingCommas") && _settings["AllowTrailingCommas"] is bool)
                        allowTrailingCommas = (bool)_settings["AllowTrailingCommas"];
                    
                    // Parse and format
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = writeIndented,
                        AllowTrailingCommas = allowTrailingCommas,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    
                    try
                    {
                        // Try to parse and format
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(text);
                        return JsonSerializer.Serialize(jsonElement, jsonOptions);
                    }
                    catch (Exception ex)
                    {
                        // If all parsing fails, return error
                        Host?.LogMessage(Id, LogLevel.Error, $"Error formatting JSON: {ex.Message}");
                        return $"Error formatting JSON: {ex.Message}";
                    }
                }
                catch (Exception ex)
                {
                    Host?.LogMessage(Id, LogLevel.Error, $"Error: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public override async Task<string> ProcessImageAsync(BitmapSource image, object options = null)
        {
            // JSON formatter doesn't process images
            return "Error: JSON formatter does not support image processing";
        }

        public override Dictionary<string, object> GetSettings()
        {
            return _settings;
        }

        public override void UpdateSettings(Dictionary<string, object> settings)
        {
            foreach (var setting in settings)
            {
                if (_settings.ContainsKey(setting.Key))
                {
                    _settings[setting.Key] = setting.Value;
                }
            }
        }

        public override PluginUIConfig GetUIConfig()
        {
            return new PluginUIConfig
            {
                HasSettings = true,
                SettingsUIType = null, // We don't have a real UI type yet
                IconPath = "Icons/json_formatter.png"
            };
        }
        
        private bool IsJson(string text)
        {
            text = text.Trim();
            if (!((text.StartsWith("{") && text.EndsWith("}")) || // Object
                 (text.StartsWith("[") && text.EndsWith("]"))))   // Array
            {
                return false;
            }
            
            try
            {
                // Try to parse it as JSON to validate
                JsonSerializer.Deserialize<JsonElement>(text);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
