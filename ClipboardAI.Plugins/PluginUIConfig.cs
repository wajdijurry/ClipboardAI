using System;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Configuration for plugin UI integration
    /// </summary>
    public class PluginUIConfig
    {
        /// <summary>
        /// Gets or sets whether the plugin has settings that can be configured
        /// </summary>
        public bool HasSettings { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the settings UI control
        /// </summary>
        public Type? SettingsUIType { get; set; }
        
        /// <summary>
        /// Gets or sets whether the plugin has a custom UI
        /// </summary>
        public bool HasCustomUI { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the custom UI control
        /// </summary>
        public Type? CustomUIType { get; set; }
        
        /// <summary>
        /// Gets or sets the icon for the plugin
        /// </summary>
        public string? IconPath { get; set; }
    }
}
