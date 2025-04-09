using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Interface for AI feature plugins that provide specific AI processing capabilities
    /// </summary>
    public interface IAIFeaturePlugin : IPlugin
    {
        /// <summary>
        /// Gets the feature type provided by this plugin
        /// </summary>
        AIFeatureType FeatureType { get; }
        
        /// <summary>
        /// Asynchronously process text using the AI feature
        /// </summary>
        /// <param name="text">Input text to process</param>
        /// <param name="options">Optional parameters for processing</param>
        /// <returns>Processed text result</returns>
        Task<string> ProcessTextAsync(string text, object options = null);
        
        /// <summary>
        /// Asynchronously process an image using the AI feature (for OCR, etc.)
        /// </summary>
        /// <param name="image">Input image to process</param>
        /// <param name="options">Optional parameters for processing</param>
        /// <returns>Processed text result</returns>
        Task<string> ProcessImageAsync(BitmapSource image, object options = null);
        
        /// <summary>
        /// Check if this feature supports a specific content type
        /// </summary>
        /// <param name="contentType">The type of content to check</param>
        /// <returns>True if this feature can process the content type</returns>
        bool SupportsContentType(ContentType contentType);
        
        /// <summary>
        /// Gets the menu option for this plugin to be displayed in the UI
        /// </summary>
        /// <returns>A MenuOption object containing the display information for this plugin</returns>
        MenuOption GetMenuOption();
    }
    
    /// <summary>
    /// Represents a menu option for a plugin in the UI
    /// </summary>
    public class MenuOption
    {
        /// <summary>
        /// The emoji icon to display next to the menu option
        /// </summary>
        public string Icon { get; set; }
        
        /// <summary>
        /// The text to display for the menu option
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// The feature type this menu option represents
        /// </summary>
        public AIFeatureType FeatureType { get; set; }
        
        /// <summary>
        /// Creates a new menu option with the specified properties
        /// </summary>
        public MenuOption(string icon, string text, AIFeatureType featureType)
        {
            Icon = icon;
            Text = text;
            FeatureType = featureType;
        }
    }
    
    /// <summary>
    /// Enum representing different content types that can be processed
    /// </summary>
    public enum ContentType
    {
        Text,
        Image,
        Table,
        Code
    }
}
