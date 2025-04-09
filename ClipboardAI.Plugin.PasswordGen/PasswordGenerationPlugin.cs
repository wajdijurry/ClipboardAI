using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClipboardAI.Plugins;
using ClipboardAI.Common;

namespace ClipboardAI.Plugin.PasswordGen
{
    /// <summary>
    /// Plugin that provides password generation functionality
    /// </summary>
    public class PasswordGenerationPlugin : FeaturePluginBase, IRefreshablePlugin
    {
        private Dictionary<string, object> _settings;
        private int _defaultLength;
        private bool _includeSpecialChars;
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "PasswordGeneration";
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "Password Generator";
        
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
        public override string Description => "Generates secure random passwords";
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public override AIFeatureType FeatureType => AIFeatureType.PasswordGeneration;
        
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        public override string FeatureId => "PasswordGeneration";
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        public override string FeatureName => "Password Generator";
        
        public PasswordGenerationPlugin()
        {
            _settings = new Dictionary<string, object>
            {
                { "DefaultLength", 16 },
                { "IncludeUppercase", true },
                { "IncludeLowercase", true },
                { "IncludeNumbers", true },
                { "IncludeSpecialChars", true },
                { "ExcludeSimilar", false }
            };
        }
        
        /// <summary>
        /// Process text by generating a password
        /// </summary>
        /// <param name="text">Input text (ignored for password generation)</param>
        /// <param name="options">Optional parameters for password generation</param>
        /// <returns>Generated password</returns>
        public override async Task<string> ProcessTextAsync(string text, object options = null)
        {
            try
            {
                Host?.LogMessage(Id, LogLevel.Information, "Generating password");
                
                // Get settings
                int length = GetSettingValue<int>("DefaultLength", 16);
                bool useUppercase = GetSettingValue<bool>("IncludeUppercase", true);
                bool useLowercase = GetSettingValue<bool>("IncludeLowercase", true);
                bool useNumbers = GetSettingValue<bool>("IncludeNumbers", true);
                bool useSpecial = GetSettingValue<bool>("IncludeSpecialChars", true);
                bool excludeSimilar = GetSettingValue<bool>("ExcludeSimilar", false);
                
                // Generate password
                string password = GeneratePassword(length, useUppercase, useLowercase, 
                    useNumbers, useSpecial, excludeSimilar);
                
                return password;
            }
            catch (Exception ex)
            {
                Host?.LogMessage(Id, LogLevel.Error, $"Error generating password: {ex.Message}");
                return $"Error generating password: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Process image (not supported for password generation)
        /// </summary>
        public override async Task<string> ProcessImageAsync(BitmapSource image, object options = null)
        {
            // Password generation doesn't process images
            return "Error: Password generation does not support image processing";
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
            
            // Save settings to application settings
            var appSettings = GetAppSettings();
            if (appSettings != null)
            {
                if (_settings.TryGetValue("DefaultLength", out object lengthValue) && lengthValue is int length)
                {
                    appSettings.SetPluginSetting("PasswordGeneration", "DefaultLength", length);
                }
                
                if (_settings.TryGetValue("IncludeSpecialChars", out object specialValue) && specialValue is bool special)
                {
                    appSettings.SetPluginSetting("PasswordGeneration", "IncludeSpecialChars", special);
                }
                
                if (_settings.TryGetValue("IncludeUppercase", out object upperValue) && upperValue is bool upper)
                {
                    appSettings.SetPluginSetting("PasswordGeneration", "IncludeUppercase", upper);
                }
                
                if (_settings.TryGetValue("IncludeLowercase", out object lowerValue) && lowerValue is bool lower)
                {
                    appSettings.SetPluginSetting("PasswordGeneration", "IncludeLowercase", lower);
                }
                
                if (_settings.TryGetValue("IncludeNumbers", out object numbersValue) && numbersValue is bool numbers)
                {
                    appSettings.SetPluginSetting("PasswordGeneration", "IncludeNumbers", numbers);
                }
            }
        }
        
        /// <summary>
        /// Generate a random password based on the specified criteria
        /// </summary>
        private string GeneratePassword(int length, bool useUppercase, bool useLowercase, 
            bool useNumbers, bool useSpecial, bool excludeSimilar)
        {
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";
            const string specialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
            const string similarChars = "Il1O0";
            
            StringBuilder charSet = new StringBuilder();
            
            if (useUppercase)
                charSet.Append(uppercaseChars);
                
            if (useLowercase)
                charSet.Append(lowercaseChars);
                
            if (useNumbers)
                charSet.Append(numberChars);
                
            if (useSpecial)
                charSet.Append(specialChars);
                
            string availableChars = charSet.ToString();
            
            if (excludeSimilar)
            {
                foreach (char c in similarChars)
                {
                    availableChars = availableChars.Replace(c.ToString(), "");
                }
            }
            
            if (string.IsNullOrEmpty(availableChars))
            {
                throw new ArgumentException("No character set selected for password generation");
            }
            
            StringBuilder password = new StringBuilder();
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);
                
                for (int i = 0; i < length; i++)
                {
                    int index = randomBytes[i] % availableChars.Length;
                    password.Append(availableChars[index]);
                }
            }
            
            return password.ToString();
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
                // Get password generation settings
                _defaultLength = settings.GetPluginSetting<int>("PasswordGeneration", "DefaultLength", 16);
                _includeSpecialChars = settings.GetPluginSetting<bool>("PasswordGeneration", "IncludeSpecialChars", true);
                
                // Update local settings dictionary
                _settings["DefaultLength"] = _defaultLength;
                _settings["IncludeSpecialChars"] = _includeSpecialChars;
                
                // Get other settings
                bool includeUppercase = settings.GetPluginSetting<bool>("PasswordGeneration", "IncludeUppercase", true);
                bool includeLowercase = settings.GetPluginSetting<bool>("PasswordGeneration", "IncludeLowercase", true);
                bool includeNumbers = settings.GetPluginSetting<bool>("PasswordGeneration", "IncludeNumbers", true);
                
                // Update local settings dictionary
                _settings["IncludeUppercase"] = includeUppercase;
                _settings["IncludeLowercase"] = includeLowercase;
                _settings["IncludeNumbers"] = includeNumbers;
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
            
            // Add password length setting
            var lengthLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Default Password Length:",
                Margin = new System.Windows.Thickness(0, 10, 0, 5)
            };
            panel.Children.Add(lengthLabel);
            
            // Create a slider for password length
            var lengthSlider = new System.Windows.Controls.Slider
            {
                Minimum = 8,
                Maximum = 32,
                Value = _settings.ContainsKey("DefaultLength") ? 
                    (int)_settings["DefaultLength"] : 16,
                TickFrequency = 4,
                IsSnapToTickEnabled = true,
                TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
                Width = 200,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new System.Windows.Thickness(0, 0, 0, 5),
                Tag = "DefaultLength"
            };
            panel.Children.Add(lengthSlider);
            
            // Add a label to show the current slider value
            var lengthValueLabel = new System.Windows.Controls.TextBlock
            {
                Text = lengthSlider.Value.ToString(),
                Margin = new System.Windows.Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(lengthValueLabel);
            
            // Update the label when the slider value changes
            lengthSlider.ValueChanged += (sender, e) => 
            {
                lengthValueLabel.Text = ((int)lengthSlider.Value).ToString();
            };
            
            // Add checkbox for including special characters
            var specialCharsCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Include Special Characters",
                IsChecked = _settings.ContainsKey("IncludeSpecialChars") ? 
                    (bool)_settings["IncludeSpecialChars"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "IncludeSpecialChars"
            };
            panel.Children.Add(specialCharsCheckBox);
            
            // Add checkbox for including numbers
            var numbersCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Include Numbers",
                IsChecked = _settings.ContainsKey("IncludeNumbers") ? 
                    (bool)_settings["IncludeNumbers"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "IncludeNumbers"
            };
            panel.Children.Add(numbersCheckBox);
            
            // Add checkbox for including uppercase letters
            var uppercaseCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Include Uppercase Letters",
                IsChecked = _settings.ContainsKey("IncludeUppercase") ? 
                    (bool)_settings["IncludeUppercase"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "IncludeUppercase"
            };
            panel.Children.Add(uppercaseCheckBox);
            
            // Add checkbox for including lowercase letters
            var lowercaseCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Include Lowercase Letters",
                IsChecked = _settings.ContainsKey("IncludeLowercase") ? 
                    (bool)_settings["IncludeLowercase"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "IncludeLowercase"
            };
            panel.Children.Add(lowercaseCheckBox);
            
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
                        // Handle password length slider
                        if (child is System.Windows.Controls.Slider lengthSlider && 
                            lengthSlider.Tag?.ToString() == "DefaultLength")
                        {
                            int defaultLength = (int)lengthSlider.Value;
                            _settings["DefaultLength"] = defaultLength;
                            settings.SetPluginSetting("PasswordGeneration", "DefaultLength", defaultLength);
                        }
                        // Handle include special characters checkbox
                        else if (child is System.Windows.Controls.CheckBox specialCharsCheckBox && 
                                specialCharsCheckBox.Tag?.ToString() == "IncludeSpecialChars")
                        {
                            bool includeSpecialChars = specialCharsCheckBox.IsChecked ?? true;
                            _settings["IncludeSpecialChars"] = includeSpecialChars;
                            settings.SetPluginSetting("PasswordGeneration", "IncludeSpecialChars", includeSpecialChars);
                        }
                        // Handle include numbers checkbox
                        else if (child is System.Windows.Controls.CheckBox numbersCheckBox && 
                                numbersCheckBox.Tag?.ToString() == "IncludeNumbers")
                        {
                            bool includeNumbers = numbersCheckBox.IsChecked ?? true;
                            _settings["IncludeNumbers"] = includeNumbers;
                            settings.SetPluginSetting("PasswordGeneration", "IncludeNumbers", includeNumbers);
                        }
                        // Handle include uppercase checkbox
                        else if (child is System.Windows.Controls.CheckBox uppercaseCheckBox && 
                                uppercaseCheckBox.Tag?.ToString() == "IncludeUppercase")
                        {
                            bool includeUppercase = uppercaseCheckBox.IsChecked ?? true;
                            _settings["IncludeUppercase"] = includeUppercase;
                            settings.SetPluginSetting("PasswordGeneration", "IncludeUppercase", includeUppercase);
                        }
                        // Handle include lowercase checkbox
                        else if (child is System.Windows.Controls.CheckBox lowercaseCheckBox && 
                                lowercaseCheckBox.Tag?.ToString() == "IncludeLowercase")
                        {
                            bool includeLowercase = lowercaseCheckBox.IsChecked ?? true;
                            _settings["IncludeLowercase"] = includeLowercase;
                            settings.SetPluginSetting("PasswordGeneration", "IncludeLowercase", includeLowercase);
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving password generation settings: {ex.Message}");
                return false;
            }
        }
    }
}
