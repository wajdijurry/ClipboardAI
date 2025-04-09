using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardAI.Plugins.Features
{
    /// <summary>
    /// Plugin that provides email template expansion functionality
    /// </summary>
    public class EmailTemplateExpansionPlugin : AIFeaturePluginBase
    {
        private Dictionary<string, object> _settings;
        private Dictionary<string, string> _templates;
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "ClipboardAI.Plugins.EmailTemplateExpansion";
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "Email Template Expansion";
        
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
        public override string Description => "Expands email templates with personalized information";
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public override AIFeatureType FeatureType => AIFeatureType.EmailTemplateExpansion;
        
        public EmailTemplateExpansionPlugin()
        {
            _settings = new Dictionary<string, object>
            {
                { "DefaultSignature", "Best regards,\nYour Name" },
                { "DefaultGreeting", "Hello {recipient}," },
                { "IncludeTimestamp", true }
            };
            
            // Sample templates
            _templates = new Dictionary<string, string>
            {
                { "meeting_request", "Subject: Meeting Request: {topic}\n\n{greeting}\n\nI hope this email finds you well. I would like to schedule a meeting to discuss {topic}. Would you be available on {date} at {time}?\n\nPlease let me know if this works for you, or suggest an alternative time that would be more convenient.\n\n{signature}" },
                { "thank_you", "Subject: Thank You\n\n{greeting}\n\nThank you for {reason}. I really appreciate your {quality}.\n\n{signature}" },
                { "introduction", "Subject: Introduction: {name}\n\n{greeting}\n\nI would like to introduce myself. My name is {name} and I am a {role} at {company}. I am reaching out because {reason}.\n\nI look forward to connecting with you.\n\n{signature}" }
            };
        }
        
        /// <summary>
        /// Process text by expanding email templates
        /// </summary>
        /// <param name="text">Template identifier or template text with placeholders</param>
        /// <param name="options">Optional parameters for template expansion</param>
        /// <returns>Expanded email template</returns>
        public override async Task<string> ProcessTextAsync(string text, object options = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            
            try
            {
                Host?.LogMessage(Id, LogLevel.Information, "Expanding email template");
                
                string template = text;
                
                // Check if the input is a template identifier
                if (_templates.ContainsKey(text.Trim().ToLower()))
                {
                    template = _templates[text.Trim().ToLower()];
                    Host?.LogMessage(Id, LogLevel.Information, $"Using template: {text.Trim().ToLower()}");
                }
                
                // Extract placeholders
                var placeholders = ExtractPlaceholders(template);
                
                // Create a dictionary for replacements
                Dictionary<string, string> replacements = new Dictionary<string, string>();
                
                // Add default values
                replacements["greeting"] = GetSettingValue<string>("DefaultGreeting", "Hello {recipient},");
                replacements["signature"] = GetSettingValue<string>("DefaultSignature", "Best regards,\nYour Name");
                replacements["date"] = DateTime.Now.AddDays(3).ToString("MMMM d, yyyy");
                replacements["time"] = "10:00 AM";
                replacements["recipient"] = "there";
                replacements["topic"] = "our upcoming project";
                replacements["reason"] = "your help with the project";
                replacements["quality"] = "support and expertise";
                replacements["name"] = "John Doe";
                replacements["role"] = "Software Developer";
                replacements["company"] = "ClipboardAI";
                
                // Add timestamp if enabled
                if (GetSettingValue<bool>("IncludeTimestamp", true))
                {
                    replacements["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                
                // Override with values from options if provided
                if (options is Dictionary<string, string> optionsDict)
                {
                    foreach (var kvp in optionsDict)
                    {
                        replacements[kvp.Key] = kvp.Value;
                    }
                }
                
                // Replace placeholders
                string expanded = template;
                foreach (var placeholder in placeholders)
                {
                    if (replacements.TryGetValue(placeholder, out string value))
                    {
                        expanded = expanded.Replace($"{{{placeholder}}}", value);
                    }
                }
                
                // Add a note that this is a mock implementation
                expanded += "\n\n[This is a mock template expansion. In a real implementation, this would use more sophisticated template processing.]";
                
                return expanded;
            }
            catch (Exception ex)
            {
                Host?.LogMessage(Id, LogLevel.Error, $"Error expanding email template: {ex.Message}");
                return $"Error expanding email template: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Process image (not supported for email template expansion)
        /// </summary>
        public override async Task<string> ProcessImageAsync(BitmapSource image, object options = null)
        {
            // Email template expansion doesn't process images
            return "Error: Email template expansion does not support image processing";
        }
        
        /// <summary>
        /// Check if this feature supports a specific content type
        /// </summary>
        public override bool SupportsContentType(ContentType contentType)
        {
            return contentType == ContentType.Text;
        }
        
        /// <summary>
        /// Get the plugin settings
        /// </summary>
        public override Dictionary<string, object> GetSettings()
        {
            return _settings;
        }
        
        /// <summary>
        /// Update the plugin settings
        /// </summary>
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
        
        /// <summary>
        /// Get the UI configuration for this plugin
        /// </summary>
        public override PluginUIConfig GetUIConfig()
        {
            return new PluginUIConfig
            {
                HasSettings = true,
                SettingsUIType = null, // We don't have a real UI type yet
                IconPath = "Icons/email_template.png"
            };
        }
        
        /// <summary>
        /// Extract placeholders from a template
        /// </summary>
        private List<string> ExtractPlaceholders(string template)
        {
            List<string> placeholders = new List<string>();
            Regex regex = new Regex(@"\{([^{}]+)\}");
            MatchCollection matches = regex.Matches(template);
            
            foreach (Match match in matches)
            {
                string placeholder = match.Groups[1].Value;
                if (!placeholders.Contains(placeholder))
                {
                    placeholders.Add(placeholder);
                }
            }
            
            return placeholders;
        }
        
        /// <summary>
        /// Get a setting value with a default if not found or wrong type
        /// </summary>
        private T GetSettingValue<T>(string key, T defaultValue)
        {
            if (_settings.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            
            return defaultValue;
        }
    }
}
