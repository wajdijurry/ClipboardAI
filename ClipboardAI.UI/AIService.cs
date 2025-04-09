using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using ClipboardAI.Common;
using ClipboardAI.Plugins;

namespace ClipboardAI.UI
{
    public class AIService
    {
        private bool _isInitialized = false;
        private IntPtr _processorHandle;
        private readonly FeatureManager _featureManager;
        private readonly Dictionary<AIFeatureType, IAIFeaturePlugin> _featurePlugins;
        private readonly ClipboardAI.Plugins.FeatureRegistry _featureRegistry;
        
        public AIService()
        {
            _featureManager = FeatureManager.Instance;
            _featurePlugins = new Dictionary<AIFeatureType, IAIFeaturePlugin>();
            _featureRegistry = ClipboardAI.Plugins.FeatureRegistry.Instance;
            Initialize();
        }

        private void LogMessage(string source, LogLevel level, string message)
        {
            // Log to console for now, can be expanded to use a proper logging system
            Console.WriteLine($"[{level}] {source}: {message}");
        }

        public void Initialize()
        {
            try
            {
                if (_isInitialized)
                    return;
                
                // Initialize native processor if not using mock implementation
                try
                {
                    // Initialize the native AI processor
                    _processorHandle = NativeMethods.InitializeProcessor();
                    if (_processorHandle == IntPtr.Zero)
                    {
                        Console.WriteLine("Warning: Native AI processor initialization returned a null handle. Using plugin-only mode.");
                    }
                }
                catch (DllNotFoundException ex)
                {
                    Console.WriteLine($"Warning: Native AI processor not available: {ex.Message}. Using plugin-only mode.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error initializing native AI processor: {ex.Message}. Using plugin-only mode.");
                }
                
                // Load plugins regardless of native processor status
                InitializePlugins();
                
                _isInitialized = true;
                Console.WriteLine("AI service initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing AI service: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Initialize AI feature plugins
        /// </summary>
        private void InitializePlugins()
        {
            try
            {
                // Initialize the plugin manager
                string pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                Console.WriteLine($"Looking for plugins in: {pluginsDirectory}");
                
                // Check if plugins directory exists
                if (!Directory.Exists(pluginsDirectory))
                {
                    Console.WriteLine($"Creating plugins directory: {pluginsDirectory}");
                    Directory.CreateDirectory(pluginsDirectory);
                }
                
                // List all files in the plugins directory
                if (Directory.Exists(pluginsDirectory))
                {
                    Console.WriteLine("Files in plugins directory:");
                    foreach (var file in Directory.GetFiles(pluginsDirectory))
                    {
                        Console.WriteLine($"- {Path.GetFileName(file)}");
                    }
                }
                
                // Check if this is first run and download required models
                CheckFirstRunAndDownloadModels();
                
                // Initialize the plugin manager - this will load all plugins
                PluginManager.Instance.Initialize(pluginsDirectory);
                
                // Get all available AI feature plugins
                var plugins = PluginManager.Instance.GetPlugins<IAIFeaturePlugin>();
                
                // Debug: Log all available plugins
                Console.WriteLine($"Found {plugins.Count()} AI feature plugins:");
                foreach (var p in plugins)
                {
                    Console.WriteLine($"- {p.Name} ({p.Id}): {p.FeatureType}");
                    
                    // Register the plugin with the feature registry
                    ClipboardAI.Plugins.FeatureRegistry.Instance.RegisterFeatureProvider(p.FeatureType.ToString(), p);
                }
                
                // Load plugin settings from AppSettings
                var appSettings = AppSettings.Instance;
                var enabledPlugins = appSettings.GetEnabledPlugins();
                
                // Register plugins based on their enabled status in settings
                foreach (var plugin in plugins)
                {
                    string pluginId = plugin.FeatureType.ToString();
                    
                    // Check if the plugin is enabled in settings
                    bool isEnabled = appSettings.IsPluginEnabled(pluginId);
                    
                    if (isEnabled)
                    {
                        RegisterPlugin(plugin);
                        Console.WriteLine($"Registered plugin {plugin.Name} ({plugin.Id}) for feature {plugin.FeatureType}");
                    }
                    else
                    {
                        Console.WriteLine($"Plugin {plugin.Name} ({plugin.Id}) for feature {plugin.FeatureType} is disabled");
                    }
                }
                
                // If no plugins were loaded, log a message
                if (_featurePlugins.Count == 0)
                {
                    Console.WriteLine("No plugins loaded.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing plugins: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if this is the first run of the application and download required models
        /// </summary>
        private void CheckFirstRunAndDownloadModels()
        {
            try
            {
                // Check if this is the first run
                var settings = AppSettings.Instance;
                
                // Check if OCR models need to be downloaded
                if (settings.IsPluginEnabled("OCR") && 
                    settings.GetPluginSetting<List<string>>("OCR", "InstalledLanguages", new List<string>()).Count == 0)
                {
                    Console.WriteLine("First run detected. Downloading OCR models...");
                    
                    // Download OCR models
                    var modelDownloader = new ModelDownloader();
                    Task.Run(async () => 
                    {
                        await modelDownloader.DownloadOcrModelAsync("eng", new Progress<int>(p => Console.WriteLine($"OCR model download progress: {p}%")));
                        
                        // Add the language to the installed languages list
                        var installedLanguages = settings.GetPluginSetting<List<string>>("OCR", "InstalledLanguages", new List<string>());
                        installedLanguages.Add("eng");
                        settings.SetPluginSetting("OCR", "InstalledLanguages", installedLanguages);
                        settings.Save();
                    }).Wait();
                }
                
                // Summarization plugin has been removed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking first run and downloading models: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Register an AI feature plugin
        /// </summary>
        /// <param name="plugin">Plugin to register</param>
        private void RegisterPlugin(IAIFeaturePlugin plugin)
        {
            if (plugin == null)
                return;
                
            // Check if we already have a plugin for this feature type
            if (_featurePlugins.ContainsKey(plugin.FeatureType))
            {
                // Replace the existing plugin
                _featurePlugins[plugin.FeatureType] = plugin;
                Console.WriteLine($"Replaced plugin for {plugin.FeatureType}: {plugin.Name} ({plugin.Id})");
            }
            else
            {
                // Add the plugin
                _featurePlugins.Add(plugin.FeatureType, plugin);
                Console.WriteLine($"Registered plugin for {plugin.FeatureType}: {plugin.Name} ({plugin.Id})");
            }
        }
        
        public async Task<string> ProcessTextAsync(string text, TextProcessingType processingType, Dictionary<string, string> options = null)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("AI Service is not initialized");
            }
            
            // Ensure options dictionary exists
            options = options ?? new Dictionary<string, string>();
            
            // Map TextProcessingType to AIFeatureType
            AIFeatureType featureType;
            switch (processingType)
            {
                case TextProcessingType.JsonFormat:
                    featureType = AIFeatureType.JsonFormatter;
                    break;
                case TextProcessingType.GeneratePassword:
                    featureType = AIFeatureType.PasswordGeneration;
                    break;
                case TextProcessingType.ExpandEmailTemplate:
                    featureType = AIFeatureType.EmailTemplateExpansion;
                    break;
                case TextProcessingType.ConvertTable:
                    featureType = AIFeatureType.TableConversion;
                    break;
                default:
                    return $"Unsupported processing type: {processingType}";
            }
            
            // Try to find a plugin that can handle this feature type
            if (PluginManager.Instance != null)
            {
                try
                {
                    var plugin = PluginManager.Instance.GetPlugin<IAIFeaturePlugin>(featureType);
                    if (plugin != null)
                    {
                        try
                        {
                            // Convert options dictionary to the format expected by the plugin
                            Dictionary<string, object> pluginOptions = options.ToDictionary(
                                kvp => kvp.Key, 
                                kvp => (object)kvp.Value
                            );
                            
                            return await plugin.ProcessTextAsync(text, pluginOptions);
                        }
                        catch (Exception ex)
                        {
                            LogMessage("AIService", LogLevel.Error, $"Error processing text with {featureType} plugin: {ex.Message}");
                            return $"Error processing text with {featureType} plugin: {ex.Message}";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No plugin found for feature type {featureType}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error trying to find plugin for {featureType}: {ex.Message}");
                    LogMessage("AIService", LogLevel.Error, $"Error trying to find plugin for {featureType}: {ex.Message}");
                }
                
                return $"No plugin available for {featureType}. Please ensure the feature is enabled and the plugin is installed.";
            }
            
            return $"No plugin available for {featureType}. Please ensure the feature is enabled and the plugin is installed.";
        }
        
        public async Task<string> ProcessImageAsync(BitmapSource image, AIFeatureType featureType, Dictionary<string, object> options = null)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("AI Service is not initialized");
            }
            
            // Determine the feature based on the feature type
            ClipboardAI.Common.Feature feature;
            switch (featureType)
            {
                case AIFeatureType.OCR:
                    feature = ClipboardAI.Common.Feature.OCR;
                    break;
                default:
                    feature = ClipboardAI.Common.Feature.TextProcessing;
                    break;
            }
            
            if (!_featureManager.IsEnabled(feature))
            {
                return $"{featureType} feature is not enabled. You can enable it by reinstalling the application and selecting the {featureType} feature.";
            }
            
            // First try to get the plugin from our internal dictionary
            IAIFeaturePlugin plugin = null;
            if (!_featurePlugins.TryGetValue(featureType, out plugin))
            {
                // If not found in our dictionary, try to get it from the FeatureRegistry
                var featureId = featureType.ToString();
                var providers = _featureRegistry.GetFeatureProviders(featureId);
                plugin = providers.FirstOrDefault() as IAIFeaturePlugin;
                
                // If found in registry, add it to our dictionary for future use
                if (plugin != null)
                {
                    Console.WriteLine($"Found plugin {plugin.Id} in registry for feature {featureType}, adding to internal dictionary");
                    RegisterPlugin(plugin);
                }
            }
            
            if (plugin != null)
            {
                try
                {
                    // For OCR, let the plugin use its own saved preferred language
                    // For other feature types, pass the options as provided
                    if (featureType == AIFeatureType.OCR)
                    {
                        return await plugin.ProcessImageAsync(image, null);
                    }
                    else
                    {
                        return await plugin.ProcessImageAsync(image, options);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("AIService", LogLevel.Error, $"Error processing image with plugin {plugin.Id}: {ex.Message}");
                    return $"Error processing image: {ex.Message}";
                }
            }
            
            return $"No plugin available for {featureType}. Please ensure the feature is enabled and the plugin is installed.";
        }
        
        /// <summary>
        /// Plugin host implementation for AI service
        /// </summary>
        private class AIServicePluginHost : IPluginHost
        {
            public void LogMessage(string pluginId, LogLevel level, string message)
            {
                Console.WriteLine($"[{level}] {pluginId}: {message}");
            }
            
            public Dictionary<string, object> GetPluginSettings(string pluginId)
            {
                // Use the new PluginSettingsService to load settings
                return ClipboardAI.Common.PluginSettingsService.LoadSettings(pluginId);
            }
            
            public void SavePluginSettings(string pluginId, Dictionary<string, object> settings)
            {
                // Use the new PluginSettingsService to save settings
                ClipboardAI.Common.PluginSettingsService.SaveSettings(pluginId, settings);
            }
            
            public string GetPluginDataPath(string pluginId)
            {
                // Use the new PluginSettingsService to get the plugin data path
                return ClipboardAI.Common.PluginSettingsService.GetPluginDataPath(pluginId);
            }
            
            public object GetApplicationSettings()
            {
                // Return application settings
                return ClipboardAI.Common.AppSettings.Instance;
            }
            
            public void ShowNotification(string title, string message, PluginNotificationType type)
            {
                // Log the notification for now
                string typeStr = type.ToString();
                Console.WriteLine($"[Notification] {typeStr}: {title} - {message}");
                
                // In a real implementation, this would show a notification in the UI
                // For example, using a toast notification or a popup
                // This could be implemented by dispatching to the UI thread and showing a notification
                
                // Example:
                // Application.Current.Dispatcher.Invoke(() =>
                // {
                //     var notification = new NotificationWindow(title, message, type);
                //     notification.Show();
                // });
            }
        }
        
        // Native methods for interacting with the C++ DLL
        private static class NativeMethods
        {
            private const string DllName = "ClipboardAI.Core.dll";

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr InitializeProcessor();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr ProcessText(IntPtr processor, [MarshalAs(UnmanagedType.LPWStr)] string text);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreeString(IntPtr stringPtr);
        }
    }
    
    public enum TextProcessingType
    {
        JsonFormat,
        GeneratePassword,
        ExpandEmailTemplate,
        ConvertTable
    }
}
