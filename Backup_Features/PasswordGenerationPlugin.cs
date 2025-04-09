using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardAI.Plugins.Features
{
    /// <summary>
    /// Plugin that provides password generation functionality
    /// </summary>
    public class PasswordGenerationPlugin : AIFeaturePluginBase
    {
        private Dictionary<string, object> _settings;
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "ClipboardAI.Plugins.PasswordGeneration";
        
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
        
        public PasswordGenerationPlugin()
        {
            _settings = new Dictionary<string, object>
            {
                { "Length", 16 },
                { "IncludeUppercase", true },
                { "IncludeLowercase", true },
                { "IncludeNumbers", true },
                { "IncludeSpecialChars", true },
                { "ExcludeSimilarChars", true }
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
                int length = GetSettingValue<int>("Length", 16);
                bool includeUppercase = GetSettingValue<bool>("IncludeUppercase", true);
                bool includeLowercase = GetSettingValue<bool>("IncludeLowercase", true);
                bool includeNumbers = GetSettingValue<bool>("IncludeNumbers", true);
                bool includeSpecialChars = GetSettingValue<bool>("IncludeSpecialChars", true);
                bool excludeSimilarChars = GetSettingValue<bool>("ExcludeSimilarChars", true);
                
                // Generate password
                string password = GeneratePassword(length, includeUppercase, includeLowercase, 
                    includeNumbers, includeSpecialChars, excludeSimilarChars);
                
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
                IconPath = "Icons/password_generation.png"
            };
        }
        
        /// <summary>
        /// Generate a random password based on the specified criteria
        /// </summary>
        private string GeneratePassword(int length, bool includeUppercase, bool includeLowercase, 
            bool includeNumbers, bool includeSpecialChars, bool excludeSimilarChars)
        {
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";
            const string specialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
            const string similarChars = "Il1O0";
            
            StringBuilder charSet = new StringBuilder();
            
            if (includeUppercase)
                charSet.Append(uppercaseChars);
                
            if (includeLowercase)
                charSet.Append(lowercaseChars);
                
            if (includeNumbers)
                charSet.Append(numberChars);
                
            if (includeSpecialChars)
                charSet.Append(specialChars);
                
            string availableChars = charSet.ToString();
            
            if (excludeSimilarChars)
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
    }
}
