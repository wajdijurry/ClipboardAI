using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClipboardAI.Plugins;
using ClipboardAI.Common;

namespace ClipboardAI.Plugin.EmailExpansion
{
    /// <summary>
    /// Plugin that provides email template expansion functionality
    /// </summary>
    public class EmailTemplateExpansionPlugin : FeaturePluginBase, IRefreshablePlugin
    {
        private Dictionary<string, object> _settings;
        private Dictionary<string, string> _templates;
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "EmailTemplateExpansion";
        
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
        
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        public override string FeatureId => "EmailTemplateExpansion";
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        public override string FeatureName => "Email Template Expansion";
        
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
        
        /// <summary>
        /// Refreshes plugin settings from application settings
        /// </summary>
        public override void RefreshFromAppSettings()
        {
            var settings = GetAppSettings();
            if (settings != null)
            {
                // Get email template expansion settings
                bool includeSignature = settings.GetPluginSetting<bool>("EmailTemplateExpansion", "IncludeSignature", true);
                string defaultSignature = settings.GetPluginSetting<string>("EmailTemplateExpansion", "DefaultSignature", "Best regards,\n[Your Name]");
                bool autoFillRecipientName = settings.GetPluginSetting<bool>("EmailTemplateExpansion", "AutoFillRecipientName", true);
                
                // Update internal settings
                _settings["IncludeSignature"] = includeSignature;
                _settings["DefaultSignature"] = defaultSignature;
                _settings["AutoFillRecipientName"] = autoFillRecipientName;
                
                // Load templates from settings
                LoadTemplatesFromSettings(settings);
            }
        }
        
        /// <summary>
        /// Loads email templates from application settings
        /// </summary>
        private void LoadTemplatesFromSettings(AppSettings settings)
        {
            if (settings == null)
                return;
                
            // Clear existing templates
            _templates.Clear();
            
            // Get templates from settings
            var templatesDict = settings.GetPluginSetting<Dictionary<string, string>>("EmailTemplateExpansion", "Templates", null);
            if (templatesDict != null)
            {
                foreach (var template in templatesDict)
                {
                    _templates[template.Key] = template.Value;
                }
            }
            else
            {
                // Add default templates if none exist
                _templates["thank_you"] = "Dear [Recipient],\n\nThank you for your email. I appreciate your [Reason].\n\n[Signature]";
                _templates["meeting_request"] = "Dear [Recipient],\n\nI would like to schedule a meeting to discuss [Topic]. Are you available on [Date] at [Time]?\n\n[Signature]";
                _templates["follow_up"] = "Dear [Recipient],\n\nI'm following up on our conversation about [Topic]. [Message]\n\n[Signature]";
                
                // Save default templates to settings
                settings.SetPluginSetting("EmailTemplateExpansion", "Templates", new Dictionary<string, string>(_templates));
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
            
            // Add signature settings
            var signatureLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Email Signature:",
                Margin = new System.Windows.Thickness(0, 10, 0, 5)
            };
            panel.Children.Add(signatureLabel);
            
            // Add checkbox for including signature
            var includeSignatureCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Include Signature in Templates",
                IsChecked = _settings.ContainsKey("IncludeSignature") ? 
                    (bool)_settings["IncludeSignature"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "IncludeSignature"
            };
            panel.Children.Add(includeSignatureCheckBox);
            
            // Add textbox for default signature
            var signatureTextBox = new System.Windows.Controls.TextBox
            {
                Text = _settings.ContainsKey("DefaultSignature") ? 
                    _settings["DefaultSignature"].ToString() : "Best regards,\n[Your Name]",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                AcceptsReturn = true,
                Height = 80,
                Margin = new System.Windows.Thickness(0, 5, 0, 10),
                Tag = "DefaultSignature"
            };
            panel.Children.Add(signatureTextBox);
            
            // Add checkbox for auto-filling recipient name
            var autoFillCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Auto-fill Recipient Name When Possible",
                IsChecked = _settings.ContainsKey("AutoFillRecipientName") ? 
                    (bool)_settings["AutoFillRecipientName"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "AutoFillRecipientName"
            };
            panel.Children.Add(autoFillCheckBox);
            
            // Add templates section
            var templatesLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Email Templates:",
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 15, 0, 5)
            };
            panel.Children.Add(templatesLabel);
            
            // Add description for templates
            var templatesDescription = new System.Windows.Controls.TextBlock
            {
                Text = "Templates can be managed through the application. Use [Placeholder] syntax for variables that will be replaced when expanding templates.",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(0, 0, 0, 10),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
            panel.Children.Add(templatesDescription);
            
            // Add template examples
            var examplesLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Available Templates:",
                Margin = new System.Windows.Thickness(0, 5, 0, 5)
            };
            panel.Children.Add(examplesLabel);
            
            // Create a list of available templates
            var templatesListBox = new System.Windows.Controls.ListBox
            {
                Height = 100,
                Margin = new System.Windows.Thickness(0, 5, 0, 10),
                Tag = "TemplatesList"
            };
            
            // Add templates to the list
            foreach (var template in _templates)
            {
                templatesListBox.Items.Add(template.Key);
            }
            
            panel.Children.Add(templatesListBox);
            
            return panel;
        }
        
        /// <summary>
        /// Saves the settings from the provided control
        /// </summary>
        /// <param name="control">The settings control previously created by CreateSettingsControl</param>
        /// <returns>True if settings were saved successfully</returns>
        public override bool SaveSettings(System.Windows.FrameworkElement control)
        {
            // First save the base settings (enabled state)
            if (!base.SaveSettings(control))
                return false;
            
            try
            {
                if (control is System.Windows.Controls.StackPanel panel)
                {
                    var settings = GetAppSettings();
                    if (settings == null)
                        return false;
                    
                    // Process each control in the panel
                    foreach (var child in panel.Children)
                    {
                        // Handle include signature checkbox
                        if (child is System.Windows.Controls.CheckBox includeSignatureCheckBox && 
                            includeSignatureCheckBox.Tag?.ToString() == "IncludeSignature")
                        {
                            bool includeSignature = includeSignatureCheckBox.IsChecked ?? true;
                            _settings["IncludeSignature"] = includeSignature;
                            settings.SetPluginSetting("EmailTemplateExpansion", "IncludeSignature", includeSignature);
                        }
                        // Handle default signature textbox
                        else if (child is System.Windows.Controls.TextBox signatureTextBox && 
                                signatureTextBox.Tag?.ToString() == "DefaultSignature")
                        {
                            string defaultSignature = signatureTextBox.Text;
                            _settings["DefaultSignature"] = defaultSignature;
                            settings.SetPluginSetting("EmailTemplateExpansion", "DefaultSignature", defaultSignature);
                        }
                        // Handle auto-fill recipient name checkbox
                        else if (child is System.Windows.Controls.CheckBox autoFillCheckBox && 
                                autoFillCheckBox.Tag?.ToString() == "AutoFillRecipientName")
                        {
                            bool autoFillRecipientName = autoFillCheckBox.IsChecked ?? true;
                            _settings["AutoFillRecipientName"] = autoFillRecipientName;
                            settings.SetPluginSetting("EmailTemplateExpansion", "AutoFillRecipientName", autoFillRecipientName);
                        }
                    }
                    
                    // Save templates to settings
                    settings.SetPluginSetting("EmailTemplateExpansion", "Templates", new Dictionary<string, string>(_templates));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving email template expansion settings: {ex.Message}");
                return false;
            }
        }
    }
}
