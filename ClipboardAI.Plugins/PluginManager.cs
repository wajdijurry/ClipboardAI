using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Manages the loading and execution of plugins
    /// </summary>
    public class PluginManager : IPluginHost
    {
        private static readonly Lazy<PluginManager> _instance = new Lazy<PluginManager>(() => new PluginManager());
        
        public static PluginManager Instance => _instance.Value;
        
        private readonly Dictionary<string, IPlugin> _plugins = new Dictionary<string, IPlugin>();
        private readonly Dictionary<string, Assembly> _pluginAssemblies = new Dictionary<string, Assembly>();
        
        private string _pluginsDirectory;
        private bool _isInitialized;
        
        private PluginManager()
        {
            // Private constructor for singleton
        }
        
        /// <summary>
        /// Initialize the plugin manager
        /// </summary>
        /// <param name="pluginsDirectory">Directory containing plugins</param>
        public void Initialize(string pluginsDirectory)
        {
            if (_isInitialized)
                return;
                
            _pluginsDirectory = pluginsDirectory;
            
            // Ensure plugins directory exists
            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
            }
            
            // Load plugins
            LoadPlugins();
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Load plugins from the plugins directory
        /// </summary>
        private void LoadPlugins()
        {
            try
            {
                Console.WriteLine($"Loading plugins from: {_pluginsDirectory}");
                
                // Check if plugins directory exists
                if (!Directory.Exists(_pluginsDirectory))
                {
                    Console.WriteLine($"Plugins directory does not exist: {_pluginsDirectory}");
                    return;
                }
                
                // Get all DLL files in the plugins directory
                string[] dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
                Console.WriteLine($"Found {dllFiles.Length} DLL files in plugins directory");
                
                foreach (string dllFile in dllFiles)
                {
                    try
                    {
                        Console.WriteLine($"Attempting to load assembly: {Path.GetFileName(dllFile)}");
                        
                        // Load the assembly
                        Assembly assembly = Assembly.LoadFrom(dllFile);
                        Console.WriteLine($"Successfully loaded assembly: {assembly.FullName}");
                        
                        // Find types that implement IPlugin
                        var pluginTypes = assembly.GetTypes()
                            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                            .ToList();
                            
                        Console.WriteLine($"Found {pluginTypes.Count} plugin types in assembly {Path.GetFileName(dllFile)}");
                        
                        foreach (var pluginType in pluginTypes)
                        {
                            try
                            {
                                Console.WriteLine($"Creating instance of plugin type: {pluginType.FullName}");
                                
                                // Create an instance of the plugin
                                var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                                
                                // Initialize the plugin
                                if (plugin.Initialize(this))
                                {
                                    // Add the plugin to the collection
                                    _plugins.Add(plugin.Id, plugin);
                                    _pluginAssemblies.Add(plugin.Id, assembly);
                                    
                                    LogMessage(plugin.Id, LogLevel.Information, $"Loaded plugin: {plugin.Name} ({plugin.Id}) v{plugin.Version}");
                                    Console.WriteLine($"Successfully initialized plugin: {plugin.Name} ({plugin.Id})");
                                }
                                else
                                {
                                    LogMessage(plugin.Id, LogLevel.Error, $"Failed to initialize plugin: {plugin.Name} ({plugin.Id})");
                                    Console.WriteLine($"Failed to initialize plugin: {plugin.Name} ({plugin.Id})");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage(pluginType.FullName, LogLevel.Error, $"Error creating plugin instance: {ex.Message}");
                                Console.WriteLine($"Error creating plugin instance of type {pluginType.FullName}: {ex.Message}");
                                Console.WriteLine($"Exception details: {ex}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage(dllFile, LogLevel.Error, $"Error loading assembly: {ex.Message}");
                        Console.WriteLine($"Error loading assembly {Path.GetFileName(dllFile)}: {ex.Message}");
                        Console.WriteLine($"Exception details: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("PluginManager", LogLevel.Error, $"Error loading plugins: {ex.Message}");
                Console.WriteLine($"Error loading plugins: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
            }
        }
        
        /// <summary>
        /// Get all loaded plugins
        /// </summary>
        /// <returns>Collection of loaded plugins</returns>
        public IEnumerable<IPlugin> GetPlugins()
        {
            return _plugins.Values;
        }
        
        /// <summary>
        /// Get all loaded plugins (alias for GetPlugins for clarity)
        /// </summary>
        /// <returns>Collection of all loaded plugins</returns>
        public IEnumerable<IPlugin> GetAllPlugins()
        {
            return GetPlugins();
        }
        
        /// <summary>
        /// Get a plugin by ID
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>Plugin instance or null if not found</returns>
        public IPlugin GetPlugin(string pluginId)
        {
            if (_plugins.TryGetValue(pluginId, out var plugin))
            {
                return plugin;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all plugins of a specific type
        /// </summary>
        /// <typeparam name="T">The plugin interface type</typeparam>
        /// <returns>A list of plugins that implement the specified interface</returns>
        public List<T> GetPlugins<T>() where T : IPlugin
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Plugin manager is not initialized");
            }
            
            return _plugins.Values
                .Where(p => p is T)
                .Cast<T>()
                .ToList();
        }
        
        /// <summary>
        /// Get a plugin by feature type
        /// </summary>
        /// <typeparam name="T">Type of plugin to get</typeparam>
        /// <param name="featureType">The feature type to look for</param>
        /// <returns>The plugin if found, null otherwise</returns>
        public T GetPlugin<T>(AIFeatureType featureType) where T : class, IPlugin
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Plugin manager is not initialized");
                
            return _plugins.Values
                .Where(p => p is T && p is IAIFeaturePlugin fp && fp.FeatureType == featureType)
                .Select(p => p as T)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Process text using all enabled plugins
        /// </summary>
        /// <param name="text">Input text to process</param>
        /// <returns>Processed text result</returns>
        public string ProcessText(string text)
        {
            string result = text;
            
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    result = plugin.ProcessText(result);
                }
                catch (Exception ex)
                {
                    LogMessage(plugin.Id, LogLevel.Error, $"Error processing text with plugin {plugin.Id}: {ex.Message}");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Shutdown all plugins
        /// </summary>
        public void Shutdown()
        {
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    plugin.Shutdown();
                }
                catch (Exception ex)
                {
                    LogMessage(plugin.Id, LogLevel.Error, $"Error shutting down plugin {plugin.Id}: {ex.Message}");
                }
            }
            
            _plugins.Clear();
            _pluginAssemblies.Clear();
            _isInitialized = false;
        }
        
        /// <summary>
        /// Refreshes all plugins that implement IRefreshablePlugin
        /// </summary>
        public void RefreshPlugins()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Plugin manager is not initialized");
                
            // Get the application settings
            var appSettings = GetApplicationSettings() as ClipboardAI.Common.AppSettings;
            
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    // Log plugin state if it's a feature plugin
                    if (plugin is IFeaturePlugin featurePlugin && appSettings != null)
                    {
                        bool isEnabled = appSettings.IsPluginEnabled(featurePlugin.FeatureId);
                        Console.WriteLine($"Plugin {plugin.Name} ({featurePlugin.FeatureId}) is {(isEnabled ? "enabled" : "disabled")}");
                        
                        // Ensure the plugin's internal state matches the settings
                        if (featurePlugin.IsEnabled != isEnabled)
                        {
                            Console.WriteLine($"Updating plugin {plugin.Name} enabled state to match settings: {isEnabled}");
                            featurePlugin.SetEnabled(isEnabled);
                        }
                    }
                    
                    // Refresh plugin if it implements IRefreshablePlugin
                    if (plugin is IRefreshablePlugin refreshablePlugin)
                    {
                        refreshablePlugin.RefreshFromAppSettings();
                        LogMessage(plugin.Id, LogLevel.Information, $"Refreshed plugin settings: {plugin.Name}");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(plugin.Id, LogLevel.Error, $"Error refreshing plugin settings: {ex.Message}");
                }
            }
        }
        
        #region IPluginHost Implementation
        
        /// <summary>
        /// Log a message from the plugin
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        public void LogMessage(string pluginId, LogLevel level, string message)
        {
            // In a real implementation, this would log to a file or other logging system
            Console.WriteLine($"[{level}] {pluginId}: {message}");
        }
        
        /// <summary>
        /// Get the plugin data directory
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>Path to the plugin data directory</returns>
        public string GetPluginDataPath(string pluginId)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClipboardAI",
                "Plugins",
                pluginId);
                
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            return path;
        }
        
        /// <summary>
        /// Get plugin settings
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <returns>Dictionary of plugin settings</returns>
        public Dictionary<string, object> GetPluginSettings(string pluginId)
        {
            // In a real implementation, this would load settings from a configuration file
            // For now, return an empty dictionary
            return new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Save plugin settings
        /// </summary>
        /// <param name="pluginId">ID of the plugin</param>
        /// <param name="settings">Dictionary of plugin settings</param>
        public void SavePluginSettings(string pluginId, Dictionary<string, object> settings)
        {
            // In a real implementation, this would save settings to a configuration file
            // For now, do nothing
        }
        
        /// <summary>
        /// Get the application settings
        /// </summary>
        /// <returns>Application settings</returns>
        public object GetApplicationSettings()
        {
            // Return the application settings instance
            return ClipboardAI.Common.AppSettings.Instance;
        }
        
        /// <summary>
        /// Show a notification to the user
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="type">Notification type</param>
        public void ShowNotification(string title, string message, PluginNotificationType type)
        {
            // In a real implementation, this would show a notification in the UI
            // For now, just log it to the console
            Console.WriteLine($"[{type}] {title}: {message}");
        }
        
        #endregion
    }
}
