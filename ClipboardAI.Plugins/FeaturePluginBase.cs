using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Base class for plugins that provide a specific feature
    /// </summary>
    public abstract class FeaturePluginBase : AIFeaturePluginBase, IFeaturePlugin, IRefreshablePlugin, IPluginWithSettings
    {
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        public abstract string FeatureId { get; }
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        public abstract string FeatureName { get; }
        
        /// <summary>
        /// Gets whether the feature is enabled
        /// </summary>
        public virtual bool IsEnabled => GetIsEnabled();
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">Plugin host</param>
        /// <returns>True if initialization was successful, false otherwise</returns>
        public override bool Initialize(IPluginHost host)
        {
            if (!base.Initialize(host))
                return false;
                
            // Register this plugin with the feature registry
            FeatureRegistry.Instance.RegisterFeatureProvider(FeatureId, this);
            
            return true;
        }
        
        /// <summary>
        /// Gets whether the feature is enabled
        /// </summary>
        /// <returns>True if the feature is enabled, false otherwise</returns>
        protected virtual bool GetIsEnabled()
        {
            // By default, check if the plugin is enabled in app settings
            var settings = GetAppSettings();
            if (settings != null)
            {
                // Use FeatureId to check if it's enabled for consistency
                bool isEnabled = settings.IsPluginEnabled(FeatureId);
                Console.WriteLine($"Plugin {Name} ({FeatureId}): GetIsEnabled() returning {isEnabled}");
                return isEnabled;
            }
            
            return false;
        }
        
        /// <summary>
        /// Enables or disables the feature
        /// </summary>
        /// <param name="enabled">Whether the feature should be enabled</param>
        public virtual void SetEnabled(bool enabled)
        {
            // Update app settings
            var settings = GetAppSettings();
            if (settings != null)
            {
                // Use FeatureId when saving the enabled state for consistency
                Console.WriteLine($"Plugin {Name} ({FeatureId}): SetEnabled({enabled})");
                settings.SetPluginEnabled(FeatureId, enabled);
                settings.Save();
            }
        }
        
        /// <summary>
        /// Gets the application settings
        /// </summary>
        /// <returns>Application settings or null if not available</returns>
        protected Common.AppSettings GetAppSettings()
        {
            return Host?.GetApplicationSettings() as Common.AppSettings;
        }
        
        /// <summary>
        /// Gets a plugin-specific setting from the application settings
        /// </summary>
        /// <typeparam name="T">Type of the setting</typeparam>
        /// <param name="settingName">Name of the setting</param>
        /// <param name="defaultValue">Default value if the setting is not found</param>
        /// <returns>Setting value or default value if not found</returns>
        protected T GetSetting<T>(string settingName, T defaultValue)
        {
            var settings = GetAppSettings();
            if (settings != null)
            {
                return settings.GetPluginSetting<T>(FeatureId, settingName, defaultValue);
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Sets a plugin-specific setting in the application settings
        /// </summary>
        /// <typeparam name="T">Type of the setting</typeparam>
        /// <param name="settingName">Name of the setting</param>
        /// <param name="value">Setting value</param>
        /// <param name="saveImmediately">Whether to save the settings immediately</param>
        protected void SetSetting<T>(string settingName, T value, bool saveImmediately = true)
        {
            var settings = GetAppSettings();
            if (settings != null)
            {
                settings.SetPluginSetting(FeatureId, settingName, value);
                
                if (saveImmediately)
                {
                    settings.Save();
                }
            }
        }
        
        /// <summary>
        /// Refreshes the plugin state from application settings
        /// </summary>
        public virtual void RefreshFromAppSettings()
        {
            // By default, just log that the plugin was refreshed
            Console.WriteLine($"Refreshing plugin {Name} ({FeatureId}) from application settings");
            
            // No need to notify property changed as the base class doesn't implement INotifyPropertyChanged
            // The plugin manager will handle refreshing the UI
        }
        
        /// <summary>
        /// Creates a UI control containing the plugin's settings
        /// </summary>
        /// <returns>A WPF control containing the plugin's settings UI</returns>
        public virtual FrameworkElement CreateSettingsControl()
        {
            // Create a panel to hold the settings
            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(10);
            
            // Add the enabled checkbox
            CheckBox enabledCheckBox = new CheckBox();
            enabledCheckBox.Content = $"Enable {Name}";
            enabledCheckBox.IsChecked = IsEnabled;
            enabledCheckBox.Margin = new Thickness(0, 5, 0, 10);
            enabledCheckBox.Tag = "EnabledCheckBox";
            stackPanel.Children.Add(enabledCheckBox);
            
            // Add plugin-specific settings by overriding this method in derived classes
            
            return stackPanel;
        }
        
        /// <summary>
        /// Saves the settings from the provided control
        /// </summary>
        /// <param name="control">The settings control previously created by CreateSettingsControl</param>
        /// <returns>True if settings were saved successfully</returns>
        public virtual bool SaveSettings(FrameworkElement control)
        {
            try
            {
                if (control is StackPanel panel)
                {
                    // Process the enabled checkbox
                    foreach (var child in panel.Children)
                    {
                        if (child is CheckBox checkBox && checkBox.Tag?.ToString() == "EnabledCheckBox")
                        {
                            bool isEnabled = checkBox.IsChecked ?? false;
                            SetEnabled(isEnabled);
                            break;
                        }
                    }
                    
                    // Save plugin-specific settings by overriding this method in derived classes
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings for plugin {Name}: {ex.Message}");
                return false;
            }
        }
    }
}
