using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Interface for plugins that provide their own settings UI
    /// </summary>
    public interface IPluginWithSettings : IPlugin
    {
        /// <summary>
        /// Creates a UI control containing the plugin's settings
        /// </summary>
        /// <returns>A WPF control containing the plugin's settings UI</returns>
        FrameworkElement CreateSettingsControl();
        
        /// <summary>
        /// Saves the settings from the provided control
        /// </summary>
        /// <param name="control">The settings control previously created by CreateSettingsControl</param>
        /// <returns>True if settings were saved successfully</returns>
        bool SaveSettings(FrameworkElement control);
    }
}
