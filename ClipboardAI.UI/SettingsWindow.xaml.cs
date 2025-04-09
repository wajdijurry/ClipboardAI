using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClipboardAI.Common;
using ClipboardAI.Plugins;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            
            LoadSettings();
            CreatePluginTabs();
        }

        private void LoadSettings()
        {
            try
            {
                _settings = AppSettings.Load();
                
                // Write debug info to a log file
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "clipboardai_settings_debug.log");
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] Loading settings:");
                    writer.WriteLine($"  ProcessingThreads: {_settings.ProcessingThreads}");
                    writer.WriteLine($"  MemoryLimitMB: {_settings.MemoryLimitMB}");
                    writer.WriteLine($"  EnableDebugLogging: {_settings.EnableDebugLogging}");
                    writer.WriteLine($"  UseCpuOnly: {_settings.UseCpuOnly}");
                }
                
                // General settings
                chkStartWithWindows.IsChecked = _settings.StartWithWindows;
                chkMinimizeToTray.IsChecked = _settings.MinimizeToTray;
                
                // Clipboard history settings
                sldExpirationDays.Value = _settings.ExpirationDays;
                lblExpirationDaysValue.Text = _settings.ExpirationDays.ToString();
                
                // Advanced settings
                sldThreads.Value = _settings.ProcessingThreads;
                sldMemoryLimit.Value = _settings.MemoryLimitMB;
                // Debug logging option removed
                chkUseCPUOnly.IsChecked = _settings.UseCpuOnly;
                
                // Initialize the label values
                lblThreadsValue.Text = _settings.ProcessingThreads.ToString();
                lblMemoryLimitValue.Text = $"{_settings.MemoryLimitMB} MB";
                
                // Write UI control values to log
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine("UI controls set to:");
                    writer.WriteLine($"  sldThreads.Value: {sldThreads.Value}");
                    writer.WriteLine($"  sldMemoryLimit.Value: {sldMemoryLimit.Value}");
                    writer.WriteLine($"  chkUseCPUOnly.IsChecked: {chkUseCPUOnly.IsChecked}");
                }
                
                // Hotkey settings
                // Load hotkeys
                if (_settings.CustomHotkeys.TryGetValue("ClipboardMenu", out string clipboardMenuHotkey))
                {
                    txtHotkey.Text = clipboardMenuHotkey;
                }
                
                if (_settings.CustomHotkeys.TryGetValue("FavoritesMenu", out string favoritesMenuHotkey))
                {
                    txtCopyHotkey.Text = favoritesMenuHotkey;
                }
                
                if (_settings.CustomHotkeys.TryGetValue("QuickPaste", out string quickPasteHotkey))
                {
                    txtPasteHotkey.Text = quickPasteHotkey;
                }
                
                // Update download button visibility
                UpdateDownloadButtonVisibility();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = AppSettings.Instance;
                
                // General settings
                settings.StartWithWindows = chkStartWithWindows.IsChecked ?? false;
                settings.MinimizeToTray = chkMinimizeToTray.IsChecked ?? false;
                
                // Clipboard history settings
                settings.ExpirationDays = (int)sldExpirationDays.Value;
                
                // Feature flags
                bool ocrWasEnabled = settings.IsPluginEnabled("ClipboardAI.Plugins.OCR");
                
                // Advanced settings
                settings.ProcessingThreads = (int)sldThreads.Value;
                settings.MemoryLimitMB = (int)sldMemoryLimit.Value;
                // Debug logging option removed - set to false by default
                settings.EnableDebugLogging = false;
                settings.UseCpuOnly = chkUseCPUOnly.IsChecked ?? false;
                
                // Save hotkeys
                if (!string.IsNullOrEmpty(txtHotkey.Text))
                {
                    settings.CustomHotkeys["ClipboardMenu"] = txtHotkey.Text;
                }
                
                if (!string.IsNullOrEmpty(txtCopyHotkey.Text))
                {
                    settings.CustomHotkeys["FavoritesMenu"] = txtCopyHotkey.Text;
                }
                
                if (!string.IsNullOrEmpty(txtPasteHotkey.Text))
                {
                    settings.CustomHotkeys["QuickPaste"] = txtPasteHotkey.Text;
                }
                
                // Save plugin-specific settings
                SavePluginSettings();
                
                // Notify plugins about settings changes
                NotifyPluginsOfSettingsChange();
                
                // Update Windows startup registry
                UpdateStartupRegistry(settings.StartWithWindows);
                
                // Close the settings window
                // DialogResult = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Notifies plugins about settings changes, particularly for OCR language preference
        /// </summary>
        private void NotifyPluginsOfSettingsChange()
        {
            try
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    // Get the plugin manager
                    var pluginManager = mainWindow.GetPluginManager();
                    if (pluginManager != null)
                    {
                        // Notify all plugins that implement IRefreshablePlugin
                        foreach (var plugin in pluginManager.GetAllPlugins())
                        {
                            if (plugin is IRefreshablePlugin refreshablePlugin)
                            {
                                refreshablePlugin.RefreshFromAppSettings();
                                Console.WriteLine($"Updated plugin {plugin.Name} with new settings");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying plugins of settings change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Finds a TextBlock with the specified text content that is a sibling of the reference element
        /// </summary>
        /// <param name="reference">The reference UI element</param>
        /// <param name="text">The text content to search for</param>
        /// <returns>The TextBlock if found, null otherwise</returns>
        private TextBlock FindSiblingTextBlock(FrameworkElement reference, string text)
        {
            if (reference == null || reference.Parent == null)
                return null;
                
            // Get the parent panel
            if (reference.Parent is System.Windows.Controls.Panel panel)
            {
                // Search through all children of the panel
                foreach (var child in panel.Children)
                {
                    if (child is TextBlock textBlock && textBlock.Text == text)
                    {
                        return textBlock;
                    }
                }
            }
            
            // If the parent is not a panel or the TextBlock wasn't found, try the parent's parent
            if (reference.Parent is FrameworkElement parentElement)
            {
                return FindSiblingTextBlock(parentElement, text);
            }
            
            return null;
        }

        private void UpdateStartupRegistry(bool addToStartup)
        {
            try
            {
                // Get the path to the executable
                string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                
                // Open the registry key for Windows startup
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (addToStartup)
                        {
                            // Add the application to startup
                            key.SetValue("ClipboardAI", executablePath);
                        }
                        else
                        {
                            // Remove the application from startup
                            if (key.GetValue("ClipboardAI") != null)
                            {
                                key.DeleteValue("ClipboardAI", false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error updating startup registry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDownloadButtonVisibility()
        {
            // No download buttons needed in the current implementation
        }

        /// <summary>
        /// Handles the click event of the reset hotkeys button
        /// </summary>
        private void btnResetHotkeys_Click(object sender, RoutedEventArgs e)
        {
            // Reset hotkeys to default values
            txtHotkey.Text = "Alt+C";
            txtCopyHotkey.Text = "Alt+V";
            txtPasteHotkey.Text = "Alt+X";
        }

        private void sldThreads_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lblThreadsValue != null)
            {
                lblThreadsValue.Text = ((int)e.NewValue).ToString();
            }
        }

        private void sldMemoryLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lblMemoryLimitValue != null)
            {
                // Round to nearest 256 MB increment
                int value = (int)e.NewValue;
                lblMemoryLimitValue.Text = $"{value} MB";
            }
        }
        
        private void sldExpirationDays_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lblExpirationDaysValue != null)
            {
                int value = (int)e.NewValue;
                lblExpirationDaysValue.Text = value.ToString();
            }
        }
        
        /// <summary>
        /// Handles mouse wheel events on the tabs ScrollViewer to enable horizontal scrolling
        /// </summary>
        private void TabsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Determine the scroll amount based on the mouse wheel delta
                double scrollAmount = e.Delta > 0 ? -30 : 30;
                
                // Scroll horizontally instead of vertically
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + scrollAmount);
                
                // Mark the event as handled to prevent it from being passed to parent containers
                e.Handled = true;
            }
        }
        
        private void txtHotkey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                // Ignore modifier keys when pressed alone
                if (e.Key == System.Windows.Input.Key.LeftCtrl || e.Key == System.Windows.Input.Key.RightCtrl ||
                    e.Key == System.Windows.Input.Key.LeftAlt || e.Key == System.Windows.Input.Key.RightAlt ||
                    e.Key == System.Windows.Input.Key.LeftShift || e.Key == System.Windows.Input.Key.RightShift ||
                    e.Key == System.Windows.Input.Key.System)
                {
                    return;
                }
                
                // Build hotkey string
                string hotkey = "";
                
                if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                    hotkey += "Ctrl+";
                
                if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                    hotkey += "Alt+";
                
                if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                    hotkey += "Shift+";
                
                if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
                    hotkey += "Win+";
                
                // Add the key
                hotkey += e.Key.ToString();
                
                // Update the textbox
                textBox.Text = hotkey;
            }
        }
        
        /// <summary>
        /// Creates tabs for each plugin with their specific settings
        /// </summary>
        private void CreatePluginTabs()
        {
            try
            {
                // Get the main TabControl
                var tabControl = this.FindName("tabControl") as System.Windows.Controls.TabControl;
                if (tabControl == null)
                {
                    Console.WriteLine("TabControl not found in the XAML");
                    return;
                }
                
                // Get all available plugins
                var plugins = GetAvailablePlugins();
                if (plugins == null || plugins.Count == 0)
                {
                    Console.WriteLine("No plugins found");
                    return;
                }
                
                Console.WriteLine($"Found {plugins.Count} plugins with settings");
                
                // Group plugins by category for ordering purposes
                var pluginsByCategory = new Dictionary<string, List<IAIFeaturePlugin>>();
                
                // Define categories
                pluginsByCategory["Text Processing"] = new List<IAIFeaturePlugin>();
                pluginsByCategory["Data Conversion"] = new List<IAIFeaturePlugin>();
                pluginsByCategory["Utilities"] = new List<IAIFeaturePlugin>();
                
                // Categorize plugins
                foreach (var plugin in plugins)
                {
                    if (plugin is IPluginWithSettings)
                    {
                        // Categorize by feature type
                        switch (plugin.FeatureType)
                        {
                            case AIFeatureType.TableConversion:
                            case AIFeatureType.JsonFormatter:

                                pluginsByCategory["Data Conversion"].Add(plugin);
                                break;
                            case AIFeatureType.EmailTemplateExpansion:
                            case AIFeatureType.PasswordGeneration:
                            case AIFeatureType.OCR:
                                pluginsByCategory["Utilities"].Add(plugin);
                                break;
                            default:
                                // If not categorized, add to Utilities
                                pluginsByCategory["Utilities"].Add(plugin);
                                break;
                        }
                    }
                }
                
                // Add all plugin tabs directly to the main TabControl, ordered by category
                foreach (var category in pluginsByCategory.Keys)
                {
                    var categoryPlugins = pluginsByCategory[category];
                    if (categoryPlugins.Count == 0)
                        continue;
                    
                    // Add each plugin as a tab
                    foreach (var plugin in categoryPlugins)
                    {
                        try
                        {
                            // Skip if plugin doesn't implement IPluginWithSettings
                            if (!(plugin is IPluginWithSettings pluginWithSettings))
                                continue;
                                
                            // Create a new tab item for the plugin
                            var tabItem = new System.Windows.Controls.TabItem
                            {
                                Header = plugin.Name,
                                Tag = plugin.Id
                            };
                            
                            // Get the settings control from the plugin
                            var settingsControl = pluginWithSettings.CreateSettingsControl();
                            if (settingsControl != null)
                            {
                                // Create a ScrollViewer to ensure content is scrollable if needed
                                var scrollViewer = new System.Windows.Controls.ScrollViewer
                                {
                                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled
                                };
                                
                                // Add the settings control to the ScrollViewer
                                scrollViewer.Content = settingsControl;
                                
                                // Add the ScrollViewer to the TabItem
                                tabItem.Content = scrollViewer;
                                
                                // Add the TabItem to the TabControl
                                tabControl.Items.Add(tabItem);
                                
                                Console.WriteLine($"Added tab for plugin: {plugin.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating tab for plugin {plugin.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating plugin tabs: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves settings from the dynamically created plugin tabs
        /// </summary>
        private void SavePluginSettings()
        {
            try
            {
                // Get the TabControl
                var tabControl = this.FindName("tabControl") as System.Windows.Controls.TabControl;
                if (tabControl == null)
                {
                    Console.WriteLine("TabControl not found when saving plugin settings");
                    return;
                }
                
                Console.WriteLine($"Saving settings for {tabControl.Items.Count} tabs");
                
                // Loop through each tab item
                foreach (System.Windows.Controls.TabItem tabItem in tabControl.Items)
                {
                    try
                    {
                        // Skip non-plugin tabs (General, AI Models, Hotkeys, Advanced)
                        if (tabItem.Tag == null || !(tabItem.Tag is string pluginId))
                        {
                            Console.WriteLine($"Skipping tab '{tabItem.Header}' as it's not a plugin tab");
                            continue;
                        }
                        
                        Console.WriteLine($"Processing tab for plugin ID: {pluginId}");
                        
                        // Get the plugin
                        var plugin = GetPluginById(pluginId);
                        if (plugin == null)
                        {
                            Console.WriteLine($"Plugin with ID '{pluginId}' not found");
                            continue;
                        }
                        
                        if (!(plugin is IPluginWithSettings pluginWithSettings))
                        {
                            Console.WriteLine($"Plugin '{plugin.Name}' does not implement IPluginWithSettings");
                            continue;
                        }
                        
                        Console.WriteLine($"Found plugin: {plugin.Name} ({plugin.Id})");
                        
                        // Get the settings control from the tab
                        if (tabItem.Content is System.Windows.Controls.ScrollViewer scrollViewer)
                        {
                            var settingsControl = scrollViewer.Content as System.Windows.FrameworkElement;
                            if (settingsControl != null)
                            {
                                Console.WriteLine($"Found settings control of type {settingsControl.GetType().Name} for plugin {plugin.Name}");
                                
                                // Let the plugin save its settings
                                bool success = pluginWithSettings.SaveSettings(settingsControl);
                                Console.WriteLine($"Plugin {plugin.Name} settings saved: {success}");
                                
                                if (success)
                                {
                                    // Find the enabled checkbox and explicitly set the plugin's enabled state
                                    if (settingsControl is System.Windows.Controls.StackPanel panel)
                                    {
                                        foreach (var child in panel.Children)
                                        {
                                            if (child is System.Windows.Controls.CheckBox checkBox && 
                                                checkBox.Tag?.ToString() == "EnabledCheckBox")
                                            {
                                                bool isEnabled = checkBox.IsChecked ?? false;
                                                string pluginIdentifier = plugin.Id;
                                                string featureIdentifier = "";
                                                
                                                // Get the FeatureId if available
                                                if (plugin is IFeaturePlugin featurePlugin)
                                                {
                                                    featureIdentifier = featurePlugin.FeatureId;
                                                    Console.WriteLine($"Plugin {plugin.Name} has FeatureId: {featureIdentifier}");
                                                }
                                                
                                                // Use FeatureId if available, otherwise fall back to Id
                                                string idToUse = !string.IsNullOrEmpty(featureIdentifier) ? featureIdentifier : pluginIdentifier;
                                                Console.WriteLine($"Setting plugin {plugin.Name} enabled state to {isEnabled} using ID: {idToUse}");
                                                AppSettings.Instance.SetPluginEnabled(idToUse, isEnabled);
                                                Console.WriteLine($"Plugin {plugin.Name} enabled state: {isEnabled}");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving settings for tab {tabItem.Header}: {ex.Message}");
                    }
                }
                
                // Refresh the processing options in the main window to reflect the new settings
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.Dispatcher.Invoke(() => {
                        mainWindow.RefreshProcessingOptions();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving plugin settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets a plugin by its ID
        /// </summary>
        private IAIFeaturePlugin GetPluginById(string pluginId)
        {
            try
            {
                // Get all available plugins
                var plugins = GetAvailablePlugins();
                
                // Find the plugin with the matching ID
                return plugins.FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting plugin by ID: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all available plugins from the plugin manager
        /// </summary>
        private List<IAIFeaturePlugin> GetAvailablePlugins()
        {
            var plugins = new List<IAIFeaturePlugin>();
            
            try
            {
                // Get all registered plugins from the plugin manager
                if (PluginManager.Instance != null)
                {
                    var allPlugins = PluginManager.Instance.GetPlugins<IAIFeaturePlugin>();
                    
                    // Include all plugins that implement IPluginWithSettings
                    foreach (var plugin in allPlugins)
                    {
                        if (plugin is IPluginWithSettings)
                        {
                            plugins.Add(plugin);
                        }
                    }
                    
                    // Log the plugins found
                    Console.WriteLine($"Found {plugins.Count} plugins with settings");
                    foreach (var plugin in plugins)
                    {
                        Console.WriteLine($"Plugin with settings: {plugin.Name} ({plugin.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available plugins: {ex.Message}");
            }
            
            return plugins;
        }
        
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Save all settings to memory
            SaveSettings();
            
            // Save plugin-specific settings
            SavePluginSettings();
            
            // Save all settings to file
            Console.WriteLine("Saving all settings to disk...");
            AppSettings.Instance.Save();
            Console.WriteLine("Settings saved to disk successfully");
            
            // Refresh plugin states in the plugin manager
            if (ClipboardAI.Plugins.PluginManager.Instance != null)
            {
                Console.WriteLine("Refreshing plugin states from settings...");
                ClipboardAI.Plugins.PluginManager.Instance.RefreshPlugins();
            }
            
            // Trigger the plugin settings changed event
            Console.WriteLine("Triggering plugin settings changed event...");
            MainWindow.OnPluginSettingsChanged();
            
            // Only set DialogResult if the window was shown as a dialog
            if (this.Owner != null)
            {
                this.DialogResult = true;
            }
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Only set DialogResult if the window was shown as a dialog
            if (this.Owner != null)
            {
                this.DialogResult = false;
            }
            this.Close();
        }
    }
}
