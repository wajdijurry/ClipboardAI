using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Base implementation for AI feature plugins
    /// </summary>
    public abstract class AIFeaturePluginBase : IAIFeaturePlugin
    {
        protected IPluginHost Host { get; private set; }
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public abstract string Id { get; }
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// Gets the plugin version
        /// </summary>
        public abstract Version Version { get; }
        
        /// <summary>
        /// Gets the plugin author
        /// </summary>
        public abstract string Author { get; }
        
        /// <summary>
        /// Gets the plugin description
        /// </summary>
        public abstract string Description { get; }
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public abstract AIFeatureType FeatureType { get; }
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">The plugin host interface</param>
        /// <returns>True if initialization was successful</returns>
        public virtual bool Initialize(IPluginHost host)
        {
            Host = host;
            return true;
        }
        
        /// <summary>
        /// Process text using the plugin (synchronous version)
        /// </summary>
        /// <param name="text">Input text to process</param>
        /// <returns>Processed text result</returns>
        public virtual string ProcessText(string text)
        {
            // Default implementation calls the async version and waits for the result
            return ProcessTextAsync(text).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Asynchronously process text using the AI feature
        /// </summary>
        /// <param name="text">Input text to process</param>
        /// <param name="options">Optional parameters for processing</param>
        /// <returns>Processed text result</returns>
        public virtual Task<string> ProcessTextAsync(string text, object options = null)
        {
            // Default implementation returns the input text unchanged
            return Task.FromResult(text);
        }
        
        /// <summary>
        /// Asynchronously process an image using the AI feature
        /// </summary>
        /// <param name="image">Input image to process</param>
        /// <param name="options">Optional parameters for processing</param>
        /// <returns>Processed text result</returns>
        public virtual Task<string> ProcessImageAsync(BitmapSource image, object options = null)
        {
            // Default implementation returns an error message
            return Task.FromResult("This feature does not support image processing");
        }
        
        /// <summary>
        /// Checks if this feature can process the given content type
        /// </summary>
        /// <param name="contentType">The type of content to check</param>
        /// <returns>True if this feature can process the content type</returns>
        public virtual bool SupportsContentType(ContentType contentType)
        {
            // Default implementation only supports text
            return contentType == ContentType.Text;
        }
        
        /// <summary>
        /// Gets the menu option for this plugin to be displayed in the UI
        /// </summary>
        /// <returns>A MenuOption object containing the display information for this plugin</returns>
        public virtual MenuOption GetMenuOption()
        {
            // Default implementation based on feature type
            string icon = "🔧";
            string text = Name;
            
            switch (FeatureType)
            {
                case AIFeatureType.OCR:
                    icon = "📷";
                    text = "Text Extraction (OCR)";
                    break;

                case AIFeatureType.JsonFormatter:
                    icon = "💻";
                    text = "Format JSON";
                    break;
                case AIFeatureType.PasswordGeneration:
                    icon = "🔑";
                    text = "Generate Password";
                    break;
                case AIFeatureType.EmailTemplateExpansion:
                    icon = "📧";
                    text = "Expand Template";
                    break;
                case AIFeatureType.TableConversion:
                    icon = "📊";
                    text = "Convert Table";
                    break;
            }
            
            return new MenuOption(icon, text, FeatureType);
        }
        
        /// <summary>
        /// Gets the plugin settings
        /// </summary>
        /// <returns>Dictionary of settings</returns>
        public virtual Dictionary<string, object> GetSettings()
        {
            return new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Updates plugin settings
        /// </summary>
        /// <param name="settings">New settings values</param>
        public virtual void UpdateSettings(Dictionary<string, object> settings)
        {
            // Default implementation does nothing
        }
        
        /// <summary>
        /// Gets the plugin's UI configuration
        /// </summary>
        /// <returns>Plugin UI configuration</returns>
        public virtual PluginUIConfig GetUIConfig()
        {
            return new PluginUIConfig
            {
                HasSettings = false,
                SettingsUIType = null
            };
        }
        
        /// <summary>
        /// Shutdown the plugin
        /// </summary>
        public virtual void Shutdown()
        {
            // Default implementation does nothing
        }
        
        /// <summary>
        /// Log a message to the host application
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        protected void Log(LogLevel level, string message)
        {
            Host?.LogMessage(Id, level, message);
        }
    }
}
