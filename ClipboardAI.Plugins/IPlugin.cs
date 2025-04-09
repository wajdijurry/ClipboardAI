using System;
using System.Collections.Generic;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Interface that all ClipboardAI plugins must implement
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the plugin version
        /// </summary>
        Version Version { get; }
        
        /// <summary>
        /// Gets the plugin author
        /// </summary>
        string Author { get; }
        
        /// <summary>
        /// Gets a description of the plugin
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">The plugin host interface</param>
        /// <returns>True if initialization was successful</returns>
        bool Initialize(IPluginHost host);
        
        /// <summary>
        /// Process text using the plugin
        /// </summary>
        /// <param name="text">Input text to process</param>
        /// <returns>Processed text result</returns>
        string ProcessText(string text);
        
        /// <summary>
        /// Gets the plugin settings
        /// </summary>
        /// <returns>Dictionary of settings</returns>
        Dictionary<string, object> GetSettings();
        
        /// <summary>
        /// Updates the plugin settings
        /// </summary>
        /// <param name="settings">Dictionary of settings</param>
        void UpdateSettings(Dictionary<string, object> settings);
        
        /// <summary>
        /// Shutdown the plugin
        /// </summary>
        void Shutdown();
    }
    
    /// <summary>
    /// Interface for the plugin host that provides services to plugins
    /// </summary>
    public interface IPluginHost
    {
        /// <summary>
        /// Log a message from the plugin
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        void LogMessage(string pluginId, LogLevel level, string message);
        
        /// <summary>
        /// Get the plugin data directory
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>Path to the plugin data directory</returns>
        string GetPluginDataPath(string pluginId);
        
        /// <summary>
        /// Get plugin settings
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>Dictionary of plugin settings</returns>
        Dictionary<string, object> GetPluginSettings(string pluginId);
        
        /// <summary>
        /// Save plugin settings
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <param name="settings">Dictionary of plugin settings</param>
        void SavePluginSettings(string pluginId, Dictionary<string, object> settings);
        
        /// <summary>
        /// Get the application settings
        /// </summary>
        /// <returns>Application settings</returns>
        object GetApplicationSettings();
        
        /// <summary>
        /// Show a notification to the user
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="type">Notification type</param>
        void ShowNotification(string title, string message, PluginNotificationType type);
    }
    
    /// <summary>
    /// Log levels for plugin messages
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical,
        Fatal
    }
    
    /// <summary>
    /// Types of plugin notifications
    /// </summary>
    public enum PluginNotificationType
    {
        Information,
        Success,
        Warning,
        Error
    }
}
