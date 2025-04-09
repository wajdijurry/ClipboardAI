using System;
using System.Collections.Generic;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Interface for plugins that provide a specific feature
    /// </summary>
    public interface IFeaturePlugin : IPlugin
    {
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        string FeatureId { get; }
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        string FeatureName { get; }
        
        /// <summary>
        /// Gets whether the feature is enabled
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Enables or disables the feature
        /// </summary>
        /// <param name="enabled">Whether the feature should be enabled</param>
        void SetEnabled(bool enabled);
    }
}
