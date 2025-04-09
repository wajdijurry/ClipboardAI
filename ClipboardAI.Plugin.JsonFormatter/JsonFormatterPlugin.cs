using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;
using ClipboardAI.Common;
using ClipboardAI.Plugins;

namespace ClipboardAI.Plugin.JsonFormatter
{
    /// <summary>
    /// Plugin for formatting JSON content
    /// </summary>
    public class JsonFormatterPlugin : FeaturePluginBase, IRefreshablePlugin
    {
        private Dictionary<string, object> _settings;

        // Plugin implementation
        public override string Id => "JsonFormatter";
        public override string Name => "JSON Formatter";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "ClipboardAI Team";
        public override string Description => "Formats JSON content with proper indentation and structure";
        public override AIFeatureType FeatureType => AIFeatureType.JsonFormatter;
        
        // Feature implementation
        public override string FeatureId => "JsonFormatter";
        public override string FeatureName => "JSON Formatter";

        public JsonFormatterPlugin()
        {
            _settings = new Dictionary<string, object>
            {
                { "IndentSize", 2 },
                { "WriteIndented", true },
                { "AllowTrailingCommas", false }
            };
        }
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">The plugin host interface</param>
        /// <returns>True if initialization was successful</returns>
        public override bool Initialize(IPluginHost host)
        {
            bool result = base.Initialize(host);
            if (!result) return false;
            
            try
            {
                // Immediately load settings from disk
                RefreshFromAppSettings();
                
                // Force enable the plugin if it's not already enabled
                var settings = GetAppSettings();
                if (settings != null)
                {
                    bool isEnabled = settings.IsPluginEnabled(FeatureId);
                    Console.WriteLine($"JSON Formatter Plugin: Initialize - current enabled state: {isEnabled}");
                    
                    if (!isEnabled)
                    {
                        Console.WriteLine($"JSON Formatter Plugin: Initialize - forcing enabled state to True");
                        settings.SetPluginEnabled(FeatureId, true);
                        settings.Save();
                    }
                }
                
                // Log that initialization was successful
                Console.WriteLine("JSON Formatter Plugin: Initialization successful");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing JSON Formatter plugin: {ex.Message}");
                return false;
            }
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
                        indentSize = Convert.ToInt32(_settings["IndentSize"]);
                        
                    if (_settings.ContainsKey("WriteIndented") && _settings["WriteIndented"] is bool)
                        writeIndented = Convert.ToBoolean(_settings["WriteIndented"]);
                    else if (_settings.ContainsKey("WriteIndented"))
                        writeIndented = Convert.ToBoolean(_settings["WriteIndented"]);
                        
                    if (_settings.ContainsKey("AllowTrailingCommas"))
                        allowTrailingCommas = Convert.ToBoolean(_settings["AllowTrailingCommas"]);
                    
                    // Parse and format
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = writeIndented,
                        AllowTrailingCommas = allowTrailingCommas,
                        ReadCommentHandling = JsonCommentHandling.Skip
                        // Removed PropertyNamingPolicy to preserve original property names
                    };
                    
                    try
                    {
                        // Try to parse and format
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(text);
                        string formattedJson = JsonSerializer.Serialize(jsonElement, jsonOptions);
                        
                        // Apply custom indentation if needed
                        if (writeIndented && indentSize != 2) // Default indentation is 2 spaces
                        {
                            // Replace the default indentation with the custom indentation
                            string defaultIndent = "  "; // 2 spaces
                            string customIndent = new string(' ', indentSize);
                            formattedJson = formattedJson.Replace(defaultIndent, customIndent);
                        }
                        
                        return formattedJson;
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

        /// <summary>
        /// Refreshes plugin settings from application settings
        /// </summary>
        public override void RefreshFromAppSettings()
        {
            var settings = GetAppSettings();
            if (settings != null)
            {
                // Get JSON formatter settings
                int indentSize = settings.GetPluginSetting<int>(Id, "IndentSize", 2);
                bool writeIndented = settings.GetPluginSetting<bool>(Id, "WriteIndented", true);
                bool allowTrailingCommas = settings.GetPluginSetting<bool>(Id, "AllowTrailingCommas", false);
                
                // Update internal settings
                _settings["IndentSize"] = indentSize;
                _settings["WriteIndented"] = writeIndented;
                _settings["AllowTrailingCommas"] = allowTrailingCommas;
            }
        }
        
        /// <summary>
        /// Creates a UI control containing the plugin's settings
        /// </summary>
        /// <returns>A WPF control containing the plugin's settings UI</returns>
        public override System.Windows.FrameworkElement CreateSettingsControl()
        {
            // Get the base settings panel with the enabled checkbox
            var panel = (System.Windows.Controls.StackPanel)base.CreateSettingsControl();
            
            // Add indentation size setting
            var indentLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Indentation Size:",
                Margin = new System.Windows.Thickness(0, 10, 0, 5)
            };
            panel.Children.Add(indentLabel);
            
            // Create a combo box for indentation size
            var indentComboBox = new System.Windows.Controls.ComboBox
            {
                Margin = new System.Windows.Thickness(0, 0, 0, 10),
                Tag = "IndentSize"
            };
            
            // Add indentation size options
            indentComboBox.Items.Add("2");
            indentComboBox.Items.Add("4");
            indentComboBox.Items.Add("8");
            
            // Set selected indentation size
            int currentIndentSize = _settings.ContainsKey("IndentSize") ? 
                Convert.ToInt32(_settings["IndentSize"]) : 2;
            indentComboBox.SelectedItem = currentIndentSize.ToString();
            
            panel.Children.Add(indentComboBox);
            
            // Add checkbox for write indentation
            var writeIndentCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Write Indentation",
                IsChecked = _settings.ContainsKey("WriteIndented") ? 
                    Convert.ToBoolean(_settings["WriteIndented"]) : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "WriteIndented"
            };
            panel.Children.Add(writeIndentCheckBox);
            
            // Add checkbox for allowing trailing commas
            var trailingCommasCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Allow Trailing Commas",
                IsChecked = _settings.ContainsKey("AllowTrailingCommas") ? 
                    Convert.ToBoolean(_settings["AllowTrailingCommas"]) : false,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "AllowTrailingCommas"
            };
            panel.Children.Add(trailingCommasCheckBox);
            
            return panel;
        }
        
        /// <summary>
        /// Saves the settings from the provided control
        /// </summary>
        /// <param name="control">The settings control previously created by CreateSettingsControl</param>
        /// <returns>True if settings were saved successfully</returns>
        public override bool SaveSettings(System.Windows.FrameworkElement control)
        {
            try
            {
                var panel = control as System.Windows.Controls.StackPanel;
                if (panel == null) return false;
                
                var settings = GetAppSettings();
                if (settings == null) return false;
                
                // Find the indent size combobox
                System.Windows.Controls.ComboBox indentComboBox = null;
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.ComboBox comboBox && 
                        comboBox.Tag?.ToString() == "IndentSize")
                    {
                        indentComboBox = comboBox;
                        break;
                    }
                }
                
                if (indentComboBox != null && indentComboBox.SelectedItem != null)
                {
                    int indentSize = int.Parse(indentComboBox.SelectedItem.ToString());
                    settings.SetPluginSetting(Id, "IndentSize", indentSize);
                    _settings["IndentSize"] = indentSize;
                }
                
                // Find the write indented checkbox
                System.Windows.Controls.CheckBox writeIndentedCheckBox = null;
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.CheckBox checkBox && 
                        checkBox.Tag?.ToString() == "WriteIndented")
                    {
                        writeIndentedCheckBox = checkBox;
                        break;
                    }
                }
                
                if (writeIndentedCheckBox != null)
                {
                    bool writeIndented = writeIndentedCheckBox.IsChecked ?? true;
                    settings.SetPluginSetting(Id, "WriteIndented", writeIndented);
                    _settings["WriteIndented"] = writeIndented;
                }
                
                // Find the allow trailing commas checkbox
                System.Windows.Controls.CheckBox trailingCommasCheckBox = null;
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.CheckBox checkBox && 
                        checkBox.Tag?.ToString() == "AllowTrailingCommas")
                    {
                        trailingCommasCheckBox = checkBox;
                        break;
                    }
                }
                
                if (trailingCommasCheckBox != null)
                {
                    bool allowTrailingCommas = trailingCommasCheckBox.IsChecked ?? false;
                    settings.SetPluginSetting(Id, "AllowTrailingCommas", allowTrailingCommas);
                    _settings["AllowTrailingCommas"] = allowTrailingCommas;
                }
                
                // Save the settings immediately to disk
                settings.Save();
                Console.WriteLine($"JSON Formatter Plugin: SaveSettings - settings saved successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving JSON formatter settings: {ex.Message}");
                return false;
            }
        }
    }
}
