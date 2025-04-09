using System;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Interface for plugins that can refresh their state from application settings
    /// </summary>
    public interface IRefreshablePlugin
    {
        /// <summary>
        /// Refreshes the plugin state from application settings
        /// </summary>
        void RefreshFromAppSettings();
    }
}
