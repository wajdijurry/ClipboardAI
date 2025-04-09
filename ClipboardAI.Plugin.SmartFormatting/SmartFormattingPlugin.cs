using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClipboardAI.Plugins;

namespace ClipboardAI.Plugin.SmartFormatting
{
    /// <summary>
    /// Plugin that provides smart formatting detection and conversion
    /// </summary>
    public class SmartFormattingPlugin : FeaturePluginBase
    {
        private bool _isInitialized = false;
        private bool _preserveFormatting = true;
        private bool _detectCodeLanguage = true;
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "SmartFormatting";
        
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        public override string FeatureId => "SmartFormatting";
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "Smart Formatting";
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        public override string FeatureName => "Smart Formatting";
        
        /// <summary>
        /// Gets the plugin version
        /// </summary>
        public override Version Version => new Version(1, 0, 0);
        
        /// <summary>
        /// Gets the plugin author
        /// </summary>
        public override string Author => "ClipboardAI";
        
        /// <summary>
        /// Gets the plugin description
        /// </summary>
        public override string Description => "Detect and preserve text formatting";
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public override AIFeatureType FeatureType => AIFeatureType.SmartFormatting;
        
        /// <summary>
        /// Gets the menu option for this plugin to be displayed in the UI
        /// </summary>
        /// <returns>A MenuOption object containing the display information for this plugin</returns>
        public override MenuOption GetMenuOption()
        {
            return new MenuOption("✏️", "Smart Formatting", FeatureType);
        }
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">The plugin host interface</param>
        /// <returns>True if initialization was successful</returns>
        public override bool Initialize(IPluginHost host)
        {
            if (_isInitialized)
                return true;
                
            if (!base.Initialize(host))
                return false;
                
            try
            {
                // Load settings
                var settings = Host.GetPluginSettings(Id);
                if (settings != null)
                {
                    if (settings.ContainsKey("PreserveFormatting") && bool.TryParse(settings["PreserveFormatting"].ToString(), out bool preserveFormatting))
                    {
                        _preserveFormatting = preserveFormatting;
                    }
                    
                    if (settings.ContainsKey("DetectCodeLanguage") && bool.TryParse(settings["DetectCodeLanguage"].ToString(), out bool detectCodeLanguage))
                    {
                        _detectCodeLanguage = detectCodeLanguage;
                    }
                }
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing SmartFormattingPlugin: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Process text using the plugin
        /// </summary>
        /// <param name="text">Text to process</param>
        /// <param name="options">Optional processing options</param>
        /// <returns>Processed text</returns>
        public override async Task<string> ProcessTextAsync(string text, object options = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
                
            try
            {
                // Detect formatting
                var formattingInfo = DetectFormatting(text);
                
                // If preserve formatting is disabled, return the original text
                if (!_preserveFormatting)
                {
                    return text;
                }
                
                // Build formatted output
                var result = new System.Text.StringBuilder();
                
                // Add formatting information header
                result.AppendLine("--- Formatting Information ---");
                
                // Add language information
                if (formattingInfo.ContainsKey("language"))
                {
                    result.AppendLine($"Language: {formattingInfo["language"]}");
                }
                
                // Add code information
                if (formattingInfo.ContainsKey("isCode") && (bool)formattingInfo["isCode"])
                {
                    result.AppendLine("Content Type: Code");
                    
                    // Add programming language if detected and enabled
                    if (_detectCodeLanguage && formattingInfo.ContainsKey("programmingLanguage"))
                    {
                        result.AppendLine($"Programming Language: {formattingInfo["programmingLanguage"]}");
                    }
                }
                else if (formattingInfo.ContainsKey("isJson") && (bool)formattingInfo["isJson"])
                {
                    result.AppendLine("Content Type: JSON");
                }
                else if (formattingInfo.ContainsKey("isXml") && (bool)formattingInfo["isXml"])
                {
                    result.AppendLine("Content Type: XML/HTML");
                }
                else if (formattingInfo.ContainsKey("isMarkdown") && (bool)formattingInfo["isMarkdown"])
                {
                    result.AppendLine("Content Type: Markdown");
                }
                else if (formattingInfo.ContainsKey("containsList") && (bool)formattingInfo["containsList"])
                {
                    result.AppendLine("Content Type: List");
                }
                else if (formattingInfo.ContainsKey("containsTable") && (bool)formattingInfo["containsTable"])
                {
                    result.AppendLine("Content Type: Table");
                }
                else
                {
                    result.AppendLine("Content Type: Plain Text");
                }
                
                // Add separator
                result.AppendLine();
                result.AppendLine("--- Original Content ---");
                result.AppendLine();
                
                // Add original text
                result.Append(text);
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SmartFormattingPlugin.ProcessTextAsync: {ex.Message}");
                return $"Error detecting formatting: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Detect formatting in the input text
        /// </summary>
        /// <param name="text">Text to detect formatting for</param>
        /// <returns>Dictionary with detected formatting information</returns>
        private Dictionary<string, object> DetectFormatting(string text)
        {
            var result = new Dictionary<string, object>();
            
            if (string.IsNullOrWhiteSpace(text))
                return result;
                
            try
            {
                // Detect language
                var language = DetectLanguage(text);
                result["language"] = language;
                
                // Detect if text is code
                bool isCode = DetectIfCode(text);
                result["isCode"] = isCode;
                
                if (isCode)
                {
                    // Detect programming language
                    var programmingLanguage = DetectProgrammingLanguage(text);
                    result["programmingLanguage"] = programmingLanguage;
                }
                
                // Detect if text contains a list
                bool containsList = text.Contains("\n- ") || text.Contains("\n* ") || 
                                   Enumerable.Range(1, 9).Any(i => text.Contains($"\n{i}. "));
                result["containsList"] = containsList;
                
                // Detect if text contains a table
                bool containsTable = text.Contains("\n|") && text.Contains("|\n");
                result["containsTable"] = containsTable;
                
                // Detect if text is JSON
                bool isJson = text.Trim().StartsWith("{") && text.Trim().EndsWith("}");
                result["isJson"] = isJson;
                
                // Detect if text is XML/HTML
                bool isXml = text.Contains("<") && text.Contains(">") && 
                            (text.Contains("</") || text.Contains("/>"));
                result["isXml"] = isXml;
                
                // Detect if text is Markdown
                bool isMarkdown = text.Contains("##") || text.Contains("**") || 
                                 text.Contains("__") || text.Contains("```");
                result["isMarkdown"] = isMarkdown;
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting formatting: {ex.Message}");
                return result;
            }
        }
        
        /// <summary>
        /// Detect the language of the input text
        /// </summary>
        private string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "en";
                
            try
            {
                // Simple language detection based on character frequency
                // In a real implementation, this would use an ONNX model
                
                // For now, implement a simple n-gram based detection
                var normalizedText = text.ToLowerInvariant();
                
                // Check for specific language markers
                if (normalizedText.Contains("ñ") || normalizedText.Contains("¿") || normalizedText.Contains("¡"))
                    return "es";
                    
                if (normalizedText.Contains("ç") || normalizedText.Contains("œ") || normalizedText.Contains("à") || normalizedText.Contains("é"))
                    return "fr";
                    
                if (normalizedText.Contains("ß") || normalizedText.Contains("ü") || normalizedText.Contains("ö") || normalizedText.Contains("ä"))
                    return "de";
                    
                if (normalizedText.Contains("ا") || normalizedText.Contains("ل") || normalizedText.Contains("م"))
                    return "ar";
                    
                // Default to English
                return "en";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting language: {ex.Message}");
                return "en";
            }
        }
        
        /// <summary>
        /// Detect if the text is code
        /// </summary>
        private bool DetectIfCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            // Check for common code indicators
            bool hasCodeIndicators = text.Contains("{") && text.Contains("}") &&
                                    (text.Contains("(") && text.Contains(")")) &&
                                    (text.Contains(";") || text.Contains("=>") || 
                                     text.Contains("function ") || text.Contains("def ") ||
                                     text.Contains("class ") || text.Contains("import ") ||
                                     text.Contains("using ") || text.Contains("var ") ||
                                     text.Contains("const ") || text.Contains("let "));
                                     
            // Check for indentation patterns
            bool hasIndentation = text.Contains("\n  ") || text.Contains("\n\t");
            
            return hasCodeIndicators || hasIndentation;
        }
        
        /// <summary>
        /// Detect the programming language of code
        /// </summary>
        private string DetectProgrammingLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "unknown";
                
            // Check for language-specific indicators
            if (text.Contains("using System;") || text.Contains("namespace ") || 
                text.Contains("public class ") || text.Contains("private void "))
                return "csharp";
                
            if (text.Contains("import java.") || (text.Contains("public class ") && 
                text.Contains("public static void main")))
                return "java";
                
            if (text.Contains("import React") || text.Contains("const [") || 
                text.Contains("useState") || text.Contains("export default"))
                return "javascript";
                
            if (text.Contains("def ") && text.Contains(":") && 
                (text.Contains("import ") || text.Contains("from ")))
                return "python";
                
            if (text.Contains("#include <") || text.Contains("int main(") || 
                text.Contains("std::"))
                return "cpp";
                
            if (text.Contains("<?php") || text.Contains("function ") && text.Contains("$"))
                return "php";
                
            if (text.Contains("<html") || text.Contains("<!DOCTYPE html>"))
                return "html";
                
            if (text.Contains("SELECT ") && text.Contains(" FROM ") && 
                (text.Contains(" WHERE ") || text.Contains(" JOIN ")))
                return "sql";
                
            return "unknown";
        }
        
        /// <summary>
        /// Creates a UI control containing the plugin's settings
        /// </summary>
        /// <returns>A WPF control containing the plugin's settings UI</returns>
        public override FrameworkElement CreateSettingsControl()
        {
            // Get the base settings panel with the enabled checkbox
            var panel = (StackPanel)base.CreateSettingsControl();
            
            // Add title
            var titleTextBlock = new TextBlock();
            titleTextBlock.Text = "Smart Formatting Settings";
            titleTextBlock.FontWeight = System.Windows.FontWeights.Bold;
            titleTextBlock.Margin = new System.Windows.Thickness(0, 0, 0, 10);
            panel.Children.Insert(0, titleTextBlock); // Insert at the beginning
            
            // Add preserve formatting checkbox
            var preserveFormattingCheckBox = new CheckBox();
            preserveFormattingCheckBox.Content = "Preserve and display formatting information";
            preserveFormattingCheckBox.IsChecked = _preserveFormatting;
            preserveFormattingCheckBox.Margin = new System.Windows.Thickness(0, 0, 0, 5);
            preserveFormattingCheckBox.Tag = "PreserveFormattingCheckBox";
            preserveFormattingCheckBox.Checked += (sender, e) => {
                _preserveFormatting = true;
            };
            preserveFormattingCheckBox.Unchecked += (sender, e) => {
                _preserveFormatting = false;
            };
            panel.Children.Add(preserveFormattingCheckBox);
            
            // Add detect code language checkbox
            var detectCodeLanguageCheckBox = new CheckBox();
            detectCodeLanguageCheckBox.Content = "Detect programming language for code";
            detectCodeLanguageCheckBox.IsChecked = _detectCodeLanguage;
            detectCodeLanguageCheckBox.Margin = new System.Windows.Thickness(0, 0, 0, 5);
            detectCodeLanguageCheckBox.Tag = "DetectCodeLanguageCheckBox";
            detectCodeLanguageCheckBox.Checked += (sender, e) => {
                _detectCodeLanguage = true;
            };
            detectCodeLanguageCheckBox.Unchecked += (sender, e) => {
                _detectCodeLanguage = false;
            };
            panel.Children.Add(detectCodeLanguageCheckBox);
            
            return panel;
        }
        
        /// <summary>
        /// Saves the settings from the provided control
        /// </summary>
        /// <param name="control">The settings control previously created by CreateSettingsControl</param>
        /// <returns>True if settings were saved successfully</returns>
        public override bool SaveSettings(FrameworkElement control)
        {
            // First save the base settings (including the enabled state)
            if (!base.SaveSettings(control))
                return false;
                
            try
            {
                if (control is StackPanel panel)
                {
                    // Process plugin-specific checkboxes
                    foreach (var child in panel.Children)
                    {
                        if (child is CheckBox checkBox)
                        {
                            string tag = checkBox.Tag?.ToString() ?? "";
                            bool isChecked = checkBox.IsChecked ?? false;
                            
                            if (tag == "PreserveFormattingCheckBox")
                                _preserveFormatting = isChecked;
                            else if (tag == "DetectCodeLanguageCheckBox")
                                _detectCodeLanguage = isChecked;
                        }
                    }
                    
                    // Save plugin-specific settings
                    SetSetting("PreserveFormatting", _preserveFormatting);
                    SetSetting("DetectCodeLanguage", _detectCodeLanguage);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings for plugin {Name}: {ex.Message}");
                return false;
            }
        }
    }
}
