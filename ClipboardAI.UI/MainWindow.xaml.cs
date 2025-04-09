using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Drawing; // For Icon
using System.Windows.Controls.Primitives; // For Popup
using System.Collections.Generic; // For Dictionary
using ClipboardAI.Common;
using ClipboardAI.Plugins;
using System.Windows.Input; // For AIFeatureType

namespace ClipboardAI.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AIService _aiService;
        private ClipboardManager _clipboardManager;
        private ObservableCollection<ClipboardItem> _filteredItems;
        private bool _isSearchBoxDefault = true;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private HotkeyManager _hotkeyManager;
        private AppSettings _settings;
        private ContentType _currentContentType = ContentType.Text; // Default to text
        
        // Persistent context menus
        private ContextMenu _clipboardContextMenu;
        private ContextMenu _favoritesContextMenu;
        
        // Event handler for plugin settings changed
        public static event EventHandler PluginSettingsChanged;
        
        // Static method to raise the event
        public static void OnPluginSettingsChanged()
        {
            PluginSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Subscribe to plugin settings changed event
            PluginSettingsChanged += MainWindow_PluginSettingsChanged;
            
            // Force settings to load from disk
            _settings = AppSettings.Load();
            
            // Initialize plugins and features
            InitializePlugins();
            
            // Initialize clipboard manager
            _clipboardManager = ClipboardManager.Instance;
            _filteredItems = new ObservableCollection<ClipboardItem>();
            
            // Subscribe to clipboard events
            ClipboardMonitor.Instance.ClipboardChanged += OnClipboardChanged;
            
            // Set initial status
            UpdateStatus("AI service initialized");
            
            // Set up processing type change handler
            cmbProcessingType.SelectionChanged += CmbProcessingType_SelectionChanged;
            
            // Default to showing all items
            ShowAllItems();
            
            // Set up system tray icon
            InitializeNotifyIcon();
            
            // Register for window closing to clean up resources and minimize to tray
            this.Closing += Window_Closing;
            
            // Register for window loaded event
            this.Loaded += Window_Loaded;
            
            // Initialize context menus
            InitializeContextMenus();
            
            // Force refresh of all plugins from settings
            Console.WriteLine("Refreshing all plugins from settings at startup");
            var pluginManager = PluginManager.Instance;
            foreach (var plugin in pluginManager.GetAllPlugins())
            {
                if (plugin is IRefreshablePlugin refreshablePlugin)
                {
                    refreshablePlugin.RefreshFromAppSettings();
                    Console.WriteLine($"Refreshed plugin {plugin.Name} ({plugin.Id}) from settings at startup");
                    
                    // Log the plugin's enabled state
                    bool isEnabled = _settings.IsPluginEnabled(plugin.Id);
                    Console.WriteLine($"Plugin {plugin.Name} ({plugin.Id}) enabled state: {isEnabled}");
                }
            }
        }
        
        /// <summary>
        /// Initialize plugins and features
        /// </summary>
        private void InitializePlugins()
        {
            try
            {
                // Check which plugins are actually installed
                CheckInstalledPlugins();
                
                // No need to call LoadPlugins since it's already called in the Initialize method
                
                // Initialize AI service
                _aiService = new AIService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing plugins: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Checks if plugins are installed and disables features if they're not
        /// </summary>
        private void CheckInstalledPlugins()
        {
            try
            {
                var settings = AppSettings.Instance;
                bool settingsChanged = false;
                
                // Get all available plugins from the plugin manager
                var availablePlugins = PluginManager.Instance.GetAllPlugins().ToList();
                var availablePluginIds = availablePlugins.Select(p => p.Id).ToList();
                
                Console.WriteLine($"Available plugin IDs: {string.Join(", ", availablePluginIds)}");
                
                // Get all enabled plugins from settings
                var enabledPlugins = settings.GetEnabledPlugins();
                
                Console.WriteLine($"Enabled plugin IDs: {string.Join(", ", enabledPlugins)}");
                
                // For each enabled plugin, check if it's actually available
                foreach (var pluginId in enabledPlugins.ToList())  // Create a copy to avoid modification during enumeration
                {
                    if (!availablePluginIds.Contains(pluginId))
                    {
                        // Plugin is enabled in settings but not available, disable it
                        settings.SetPluginEnabled(pluginId, false);
                        settingsChanged = true;
                        Console.WriteLine($"Plugin {pluginId} is enabled in settings but not available, disabling it");
                    }
                }
                
                // Save settings if changed
                // if (settingsChanged)
                // {
                //     settings.Save();
                //     Console.WriteLine("Settings saved due to plugin availability changes");
                // }
                
                // Refresh the plugin manager to apply changes
                PluginManager.Instance.RefreshPlugins();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking installed plugins: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize persistent context menus
        /// </summary>
        private void InitializeContextMenus()
        {
            // Initialize clipboard context menu
            _clipboardContextMenu = new ContextMenu();
            _clipboardContextMenu.StaysOpen = false;
            _clipboardContextMenu.Placement = PlacementMode.MousePoint;
            _clipboardContextMenu.PlacementTarget = this;
            _clipboardContextMenu.Opened += ContextMenu_Opened;
            
            // Initialize favorites context menu
            _favoritesContextMenu = new ContextMenu();
            _favoritesContextMenu.StaysOpen = false;
            _favoritesContextMenu.Placement = PlacementMode.MousePoint;
            _favoritesContextMenu.PlacementTarget = this;
            _favoritesContextMenu.Opened += ContextMenu_Opened;
            
            // Add mouse down event handler to the application
            Application.Current.MainWindow.PreviewMouseDown += Window_PreviewMouseDown;
        }
        
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            // When a context menu is opened, set a flag to ignore the next mouse click
            // This prevents the menu from being closed immediately by the same click that opened it
            _ignoreNextClick = true;
        }
        
        private bool _ignoreNextClick = false;
        
        private void Window_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_ignoreNextClick)
            {
                _ignoreNextClick = false;
                return;
            }
            
            // Close any open context menus
            _clipboardContextMenu.IsOpen = false;
            _favoritesContextMenu.IsOpen = false;
        }

        private void ShowClipboardContextMenu()
        {
            // Clear existing items
            _clipboardContextMenu.Items.Clear();
            
            // Create the content for the context menu
            MenuItem header = new MenuItem();
            header.Header = "Recent Items";
            header.IsEnabled = false;
            header.FontWeight = FontWeights.Bold;
            _clipboardContextMenu.Items.Add(header);
            
            // Add separator
            Separator separator = new Separator();
            _clipboardContextMenu.Items.Add(separator);
            
            // Add recent items (up to 10)
            var recentItems = _clipboardManager.GetClipboardItems().Take(10);
            foreach (var item in recentItems)
            {
                MenuItem itemButton = new MenuItem();
                itemButton.Header = TruncateText(item.Preview, 50);
                itemButton.Tag = item;
                
                // Add click handler
                itemButton.Click += (s, e) => 
                {
                    if (s is MenuItem menuItem && menuItem.Tag is ClipboardItem clickedItem)
                    {
                        // Set the selected item to the clipboard
                        _clipboardManager.SetClipboardContent(clickedItem);
                        
                        // Simulate paste operation
                        SimulatePaste();
                    }
                };
                
                _clipboardContextMenu.Items.Add(itemButton);
            }
            
            // Show the context menu
            _clipboardContextMenu.IsOpen = true;
        }
        
        private void ShowFavoritesContextMenu()
        {
            // Clear existing items
            _favoritesContextMenu.Items.Clear();
            
            // Create the content for the context menu
            MenuItem header = new MenuItem();
            header.Header = "Favorite Items";
            header.IsEnabled = false;
            header.FontWeight = FontWeights.Bold;
            _favoritesContextMenu.Items.Add(header);
            
            // Add separator
            Separator separator = new Separator();
            _favoritesContextMenu.Items.Add(separator);
            
            // Add favorite items
            var favoriteItems = _clipboardManager.GetClipboardItems().Where(i => i.IsFavorite);
            
            if (!favoriteItems.Any())
            {
                MenuItem noItems = new MenuItem();
                noItems.Header = "No favorite items";
                noItems.IsEnabled = false;
                noItems.Foreground = System.Windows.Media.Brushes.Gray;
                _favoritesContextMenu.Items.Add(noItems);
            }
            else
            {
                foreach (var item in favoriteItems)
                {
                    MenuItem itemButton = new MenuItem();
                    itemButton.Header = TruncateText(item.Preview, 50);
                    itemButton.Tag = item;
                    
                    // Add click handler
                    itemButton.Click += (s, e) => 
                    {
                        if (s is MenuItem menuItem && menuItem.Tag is ClipboardItem clickedItem)
                        {
                            // Set the selected item to the clipboard
                            _clipboardManager.SetClipboardContent(clickedItem);
                            
                            // Simulate paste operation
                            SimulatePaste();
                        }
                    };
                    
                    _favoritesContextMenu.Items.Add(itemButton);
                }
            }
            
            // Show the context menu
            _favoritesContextMenu.IsOpen = true;
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                Text = "ClipboardAI",
                Visible = true
            };
            
            // Create context menu for the tray icon
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            
            var showItem = new System.Windows.Forms.ToolStripMenuItem("Show");
            showItem.Click += (s, e) => ShowFromTray();
            contextMenu.Items.Add(showItem);
            
            var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => 
            {
                ShowFromTray();
                btnSettings_Click(null, null);
            };
            contextMenu.Items.Add(settingsItem);
            
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Double-click to show the application
            _notifyIcon.DoubleClick += (s, e) => ShowFromTray();
        }
        
        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        
        private void ExitApplication()
        {
            // Clean up notify icon
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            
            // Stop clipboard monitoring
            ClipboardMonitor.Instance.Stop();
            
            // Exit application
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Handles mouse wheel events on the filter buttons ScrollViewer to enable horizontal scrolling
        /// </summary>
        private void FilterButtons_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the UI
            UpdateHistoryCount();
            
            // Disable content panels initially
            DisableContentPanels();
            
            // Set up initial processing options
            CmbProcessingType_SelectionChanged(cmbProcessingType, null);
            
            // Initialize hotkey manager (moved from constructor to avoid handle issues)
            InitializeHotkeyManager();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save application settings before closing to ensure all settings are persisted
            AppSettings.Instance.Save();
            
            // Unregister all hotkeys when closing
            _hotkeyManager?.UnregisterAllHotkeys();
            
            // Clean up notify icon
            _notifyIcon?.Dispose();
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Only minimize to tray if the setting is enabled
            if (_settings.MinimizeToTray)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                // Clean up resources when actually closing
                _hotkeyManager?.UnregisterAllHotkeys();
                _notifyIcon?.Dispose();
                ClipboardMonitor.Instance.Stop();
            }
        }
        
        private void InitializeHotkeyManager()
        {
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Initialize(this);
            
            // Only register hotkeys if enabled in settings
            if (_settings.EnableHotkeys)
            {
                RegisterHotkeys();
            }
        }
        
        private void RegisterHotkeys()
        {
            // Unregister any existing hotkeys first
            _hotkeyManager.UnregisterAllHotkeys();
            
            // Get hotkey strings from settings
            string clipboardMenuHotkey = GetHotkeyString("ClipboardMenu", "Ctrl+Alt+V");
            string favoritesMenuHotkey = GetHotkeyString("FavoritesMenu", "Ctrl+Alt+F");
            string quickPasteHotkey = GetHotkeyString("QuickPaste", "Ctrl+Alt+Q");
            
            // Register hotkeys with actions
            _hotkeyManager.RegisterHotkey("ClipboardMenu", clipboardMenuHotkey, ShowClipboardContextMenu);
            _hotkeyManager.RegisterHotkey("FavoritesMenu", favoritesMenuHotkey, ShowFavoritesContextMenu);
            _hotkeyManager.RegisterHotkey("QuickPaste", quickPasteHotkey, QuickPasteLastItem);
        }
        
        private string GetHotkeyString(string hotkeyName, string defaultValue)
        {
            if (_settings.CustomHotkeys.TryGetValue(hotkeyName, out string value))
                return value;
            return defaultValue;
        }
        
        private void QuickPasteLastItem()
        {
            try
            {
                // Get the most recent clipboard item
                var lastItem = _clipboardManager.GetClipboardItems().FirstOrDefault();
                
                if (lastItem != null)
                {
                    // Set the item to the clipboard
                    _clipboardManager.SetClipboardContent(lastItem);
                    
                    // Add a small delay to ensure the clipboard content is set
                    System.Threading.Thread.Sleep(100);
                    
                    // Simulate paste operation
                    SimulatePaste();
                    
                    // Update status
                    UpdateStatus($"Quick-pasted: {TruncateText(lastItem.Preview, 30)}");
                }
                else
                {
                    UpdateStatus("No items in clipboard history to paste");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in QuickPasteLastItem: {ex.Message}");
                UpdateStatus("Error performing quick paste");
            }
        }
        
        private void ClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is ClipboardItem item)
            {
                // Set the selected item to the clipboard
                _clipboardManager.SetClipboardContent(item);
                
                // Simulate paste operation
                SimulatePaste();
            }
        }
        
        private void SimulatePaste()
        {
            // Simulate Ctrl+V keystroke
            System.Windows.Forms.SendKeys.SendWait("^v");
        }
        
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            if (text.Length <= maxLength)
                return text;
                
            return text.Substring(0, maxLength) + "...";
        }

        private void OnClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
            // Process clipboard changes on the UI thread
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Don't update the view panel if an item is selected
                    bool hasSelectedItem = lvClipboardHistory.SelectedItem != null;
                    
                    // Always add to clipboard history and process content
                    if (e.ContainsText)
                    {
                        // Add to clipboard history
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Text,
                            TextContent = e.Text,
                            SourceApplication = e.SourceApplication
                        };
                        _clipboardManager.AddItem(item);
                        
                        // Update history count
                        UpdateHistoryCount();
                        
                        // Only update the view if no item is selected
                        if (!hasSelectedItem)
                        {
                            txtOriginal.Text = e.Text;
                            imgOriginal.Visibility = Visibility.Collapsed;
                            txtOriginal.Visibility = Visibility.Visible;
                            UpdateStatus("Text copied to clipboard");
                            
                            // Update source application
                            txtClipboardSource.Text = $"Source: {e.SourceApplication ?? "Unknown"}";
                            
                            // Show appropriate processing options for text
                            ShowAllProcessingOptions();
                        }
                    }
                    else if (e.ContainsImage)
                    {
                        // Add to clipboard history
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.Image,
                            ImageData = e.ImageData,
                            SourceApplication = e.SourceApplication
                        };
                        _clipboardManager.AddItem(item);
                        
                        // Update history count
                        UpdateHistoryCount();
                        
                        // Only update the view if no item is selected
                        if (!hasSelectedItem)
                        {
                            // Display the image in the original content area
                            try
                            {
                                using (MemoryStream stream = new MemoryStream(e.ImageData))
                                {
                                    // Reset stream position to beginning
                                    stream.Position = 0;
                                    
                                    // Create a bitmap image from the stream
                                    BitmapImage bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.StreamSource = stream;
                                    bitmap.EndInit();
                                    bitmap.Freeze(); // Important for cross-thread access
                                    
                                    imgOriginal.Source = bitmap;
                                    imgOriginal.Visibility = Visibility.Visible;
                                    txtOriginal.Visibility = Visibility.Collapsed;
                                    
                                    // Update status with image dimensions
                                    UpdateStatus($"Image: {bitmap.PixelWidth}x{bitmap.PixelHeight} pixels");
                                }
                                
                                // For images, only show image processing options
                                ShowImageProcessingOptions();
                                
                                // Update source application
                                txtClipboardSource.Text = $"Source: {e.SourceApplication ?? "Unknown"}";
                            }
                            catch (Exception ex)
                            {
                                txtOriginal.Text = "[Error loading image: " + ex.Message + "]";
                                imgOriginal.Visibility = Visibility.Collapsed;
                                txtOriginal.Visibility = Visibility.Visible;
                                UpdateStatus($"Error loading image: {ex.Message}");
                            }
                        }
                    }
                    else if (e.ContainsFileDrop && e.FileDropList != null && e.FileDropList.Count > 0)
                    {
                        // Add to clipboard history
                        var item = new ClipboardItem
                        {
                            ContentType = ClipboardContentType.FileDrop,
                            TextContent = string.Join(Environment.NewLine, e.FileDropList),
                            FilePath = e.FileDropList.Count == 1 ? e.FileDropList[0] : null,
                            SourceApplication = e.SourceApplication
                        };
                        _clipboardManager.AddItem(item);
                        
                        // Update history count
                        UpdateHistoryCount();
                        
                        // Only update the view if no item is selected
                        if (!hasSelectedItem)
                        {
                            txtOriginal.Text = string.Join(Environment.NewLine, e.FileDropList);
                            imgOriginal.Visibility = Visibility.Collapsed;
                            txtOriginal.Visibility = Visibility.Visible;
                            UpdateStatus($"Files copied to clipboard: {e.FileDropList.Count} file(s)");
                            
                            // Update source application
                            txtClipboardSource.Text = $"Source: {e.SourceApplication ?? "Unknown"}";
                            
                            // Show limited processing options for files
                            ShowLimitedProcessingOptions();
                        }
                    }
                    
                    // Refresh the filtered items if showing all
                    if (lvClipboardHistory.ItemsSource == _filteredItems && _filteredItems.Count == _clipboardManager.Items.Count)
                    {
                        ShowAllItems();
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error processing clipboard content: {ex.Message}");
                }
            });
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtProcessed.Text);
                UpdateStatus("Processed text copied to clipboard");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error copying to clipboard: {ex.Message}");
            }
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    txtOriginal.Text = Clipboard.GetText();
                    UpdateStatus("Text pasted from clipboard");
                }
                else if (Clipboard.ContainsImage())
                {
                    txtOriginal.Text = "[Image data in clipboard]";
                    UpdateStatus("Image pasted from clipboard");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error pasting from clipboard: {ex.Message}");
            }
        }

        private async void Process(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Processing text...");
                var options = new Dictionary<string, string>();
                string result = await _aiService.ProcessTextAsync(txtOriginal.Text, TextProcessingType.JsonFormat, options);
                txtProcessed.Text = result;
                UpdateStatus("Text processed successfully");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error processing text: {ex.Message}");
            }
        }

        private async void ImageRecognition(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if image recognition feature is enabled
                if (!_settings.IsPluginEnabled("OCR"))
                {
                    MessageBox.Show(
                        "The image recognition feature is not enabled. Please enable it in the settings to use this feature.",
                        "Feature Not Enabled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    UpdateStatus("Image recognition feature is not enabled");
                    return;
                }
                
                // Check if we have an image displayed in the UI
                if (imgOriginal.Visibility == Visibility.Visible && imgOriginal.Source != null)
                {
                    BitmapSource image = imgOriginal.Source as BitmapSource;
                    if (image != null)
                    {
                        UpdateStatus("Performing image recognition...");
                        
                        // Don't pass any options - let the plugin use its saved preferred language
                        string result = await _aiService.ProcessImageAsync(image, AIFeatureType.OCR);
                        txtProcessed.Text = result;
                        UpdateStatus("Image recognition complete");
                    }
                }
                else
                {
                    MessageBox.Show("Please load an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatus("No image to process");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing image recognition: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Error: {ex.Message}");
            }
        }
        

        
        private async void btnFormatCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No code to format");
                    return;
                }
                
                // Check if the input is valid JSON
                bool isJson = IsJsonContent(txtOriginal.Text);
                if (isJson)
                {
                    // If it's JSON, use the JsonFormatterPlugin through FormatJson
                    FormatJson(sender, e);
                    Console.WriteLine("Detected JSON content, using JsonFormatterPlugin");
                    return;
                }
                
                // For other code types, use the JSON formatting service
                UpdateStatus("Formatting JSON...");
                var options = new Dictionary<string, string>();
                string result = await _aiService.ProcessTextAsync(txtOriginal.Text, TextProcessingType.JsonFormat, options);
                txtProcessed.Text = result;
                UpdateStatus("JSON formatting complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in btnFormatCode_Click: {ex}");
                UpdateStatus($"Error formatting JSON: {ex.Message}");
            }
        }
        
        private async void FormatJson(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No JSON to format");
                    return;
                }
                
                UpdateStatus("Formatting JSON...");
                Console.WriteLine($"Attempting to format JSON text of length: {txtOriginal.Text.Length}");
                
                // Add language hint to options
                var options = new Dictionary<string, string>
                {
                    { "language", "json" }
                };
                
                // First try using the JsonFormat processing type
                string result = await _aiService.ProcessTextAsync(txtOriginal.Text, TextProcessingType.JsonFormat, options);
                
                // If that fails, try creating and using the JsonFormatterPlugin directly
                if (result.StartsWith("No plugin available") || result.StartsWith("Error"))
                {
                    Console.WriteLine("JSON formatting failed through AIService, trying direct approach");
                    try
                    {
                        // Get the JSON formatter plugin from the plugin manager
                        var jsonFormatter = ClipboardAI.Plugins.PluginManager.Instance.GetPlugin<ClipboardAI.Plugins.IAIFeaturePlugin>(ClipboardAI.Plugins.AIFeatureType.JsonFormatter);
                        
                        if (jsonFormatter != null)
                        {
                            // Process the text using the plugin
                            result = await jsonFormatter.ProcessTextAsync(txtOriginal.Text, options);
                        }
                        else
                        {
                            // Fallback if plugin not found
                            result = "JSON formatter plugin not available.";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error using direct JsonFormatterPlugin: {ex.Message}");
                        // If direct approach fails, fall back to code formatting
                        result = await _aiService.ProcessTextAsync(txtOriginal.Text, TextProcessingType.JsonFormat, options);
                    }
                }
                
                txtProcessed.Text = result;
                UpdateStatus("JSON formatting complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in FormatJson: {ex}");
                UpdateStatus($"Error formatting JSON: {ex.Message}");
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            bool? result = settingsWindow.ShowDialog();
            
            if (result == true)
            {
                // Reload settings
                _settings = AppSettings.Load();
                
                // Update hotkeys based on new settings
                UpdateHotkeysFromSettings();
            }
        }
        
        /// <summary>
        /// Updates hotkeys based on current settings
        /// </summary>
        private void UpdateHotkeysFromSettings()
        {
            // Only update if hotkeys are enabled
            if (_settings.EnableHotkeys)
            {
                // Unregister existing hotkeys first
                _hotkeyManager.UnregisterAllHotkeys();
                
                // Register hotkeys with updated settings
                RegisterHotkeys();
                
                UpdateStatus("Hotkeys updated");
            }
            else
            {
                // Unregister all hotkeys if disabled
                _hotkeyManager.UnregisterAllHotkeys();
                UpdateStatus("Hotkeys disabled");
            }
        }
        
        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            // Ask for confirmation before clearing all items
            var result = MessageBox.Show(
                "Are you sure you want to clear all clipboard history items?\nThis action cannot be undone.", 
                "Clear All Items", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                // Clear all items from the history
                foreach (var item in _clipboardManager.Items.ToList())
                {
                    _clipboardManager.RemoveItem(item.Id);
                }
                
                // Refresh the list view
                _filteredItems.Clear();
                
                // Update status
                UpdateStatus("All clipboard history items cleared");
            }
        }
        
        private void btnApplyProcessing_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected item from the dropdown
            if (cmbProcessingType.SelectedItem == null)
            {
                UpdateStatus("No processing option selected");
                return;
            }
            
            var selectedItem = cmbProcessingType.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                UpdateStatus("Invalid processing option selected");
                return;
            }
            
            // Get the TextBlock content from the StackPanel in the ComboBoxItem
            string processingType = string.Empty;
            if (selectedItem.Content is StackPanel stackPanel)
            {
                // Find the TextBlock that contains the text (second child)
                if (stackPanel.Children.Count >= 2 && stackPanel.Children[1] is TextBlock textBlock)
                {
                    processingType = textBlock.Text;
                }
            }
            else if (selectedItem.Content is string contentString)
            {
                processingType = contentString;
            }
            
            if (string.IsNullOrEmpty(processingType))
            {
                UpdateStatus("Error: Could not determine processing type");
                return;
            }
            
            // Provide immediate visual feedback
            btnApplyProcessing.IsEnabled = false;
            UpdateStatus($"Applying {processingType} processing...");
            
            try
            {
                // Check if we're dealing with an image and OCR is selected
                if (processingType == "Text Extraction (Image Recognition)" || processingType == "Text Extraction (OCR)")
                {
                    if (_currentContentType == ContentType.Image && imgOriginal.Visibility == Visibility.Visible && imgOriginal.Source != null)
                    {
                        // Check if OCR is enabled
                        if (!_settings.IsPluginEnabled("OCR"))
                        {
                            MessageBox.Show(
                                "The OCR feature is not enabled. Please enable it in the settings to use this feature.",
                                "Feature Not Enabled",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            UpdateStatus("OCR feature is not enabled");
                            return;
                        }
                        
                        ImageRecognition(sender, e);
                    }
                    else
                    {
                        UpdateStatus("No image available for OCR processing");
                        MessageBox.Show(
                            "Please select an image first before using OCR.",
                            "No Image",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    return;
                }
                else if (processingType == "OCR Not Enabled")
                {
                    MessageBox.Show(
                        "The OCR feature is not enabled. Please enable it in the settings to use this feature.",
                        "Feature Not Enabled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    UpdateStatus("OCR feature is not enabled");
                    return;
                }
                
                // Handle text processing options
                switch (processingType)
                {
                    // Summarization case removed
                    case "Format JSON":
                        FormatJson(sender, e);
                        break;

                    case "Smart Formatting":
                        ProcessSmartFormatting(sender, e);
                        break;

                    case "Detect Language":
                        ProcessLanguageDetection(sender, e);
                        break;

                    case "Extract Keywords":
                        ProcessKeywordExtraction(sender, e);
                        break;

                    case "Check Grammar":
                        ProcessGrammarCheck(sender, e);
                        break;

                    case "Generate Password":
                        GeneratePassword();
                        break;
                    case "Expand Template":
                        ExpandTemplate();
                        break;
                    case "Convert Table":
                        ConvertTable();
                        break;
                    default:
                        Process(sender, e);
                        break;
                }
            }
            finally
            {
                // Re-enable the button after processing
                btnApplyProcessing.IsEnabled = true;
            }
        }
        
        private async void GeneratePassword()
        {
            try
            {
                UpdateStatus("Generating secure password...");
                var options = new Dictionary<string, string>();
                string result = await _aiService.ProcessTextAsync("", TextProcessingType.GeneratePassword, options);
                txtProcessed.Text = result;
                UpdateStatus("Password generation complete");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error generating password: {ex.Message}");
            }
        }
        
        private async void ProcessSmartFormatting(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No text to format");
                    return;
                }
                
                UpdateStatus("Applying smart formatting...");
                Console.WriteLine($"Attempting to apply smart formatting to text of length: {txtOriginal.Text.Length}");
                
                // Get the Smart Formatting plugin
                var smartFormatter = PluginManager.Instance.GetPlugin<IAIFeaturePlugin>(AIFeatureType.SmartFormatting);
                
                if (smartFormatter != null)
                {
                    // Process the text using the plugin
                    string result = await smartFormatter.ProcessTextAsync(txtOriginal.Text, new Dictionary<string, string>());
                    txtProcessed.Text = result;
                    UpdateStatus("Smart formatting complete");
                }
                else
                {
                    txtProcessed.Text = "Smart Formatting plugin not available.";
                    UpdateStatus("Smart Formatting plugin not available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ProcessSmartFormatting: {ex}");
                UpdateStatus($"Error applying smart formatting: {ex.Message}");
            }
        }
        
        private async void ProcessLanguageDetection(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No text to analyze");
                    return;
                }
                
                UpdateStatus("Detecting language...");
                Console.WriteLine($"Attempting to detect language of text with length: {txtOriginal.Text.Length}");
                
                // Get the Language Detection plugin
                var languageDetector = PluginManager.Instance.GetPlugin<IAIFeaturePlugin>(AIFeatureType.LanguageDetection);
                
                if (languageDetector != null)
                {
                    // Process the text using the plugin
                    string result = await languageDetector.ProcessTextAsync(txtOriginal.Text, new Dictionary<string, string>());
                    txtProcessed.Text = result;
                    UpdateStatus("Language detection complete");
                }
                else
                {
                    txtProcessed.Text = "Language Detection plugin not available.";
                    UpdateStatus("Language Detection plugin not available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ProcessLanguageDetection: {ex}");
                UpdateStatus($"Error detecting language: {ex.Message}");
            }
        }
        
        private async void ProcessKeywordExtraction(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No text to analyze");
                    return;
                }
                
                UpdateStatus("Extracting keywords...");
                Console.WriteLine($"Attempting to extract keywords from text with length: {txtOriginal.Text.Length}");
                
                // Get the Keyword Extraction plugin
                var keywordExtractor = PluginManager.Instance.GetPlugin<IAIFeaturePlugin>(AIFeatureType.KeywordExtraction);
                
                if (keywordExtractor != null)
                {
                    // Process the text using the plugin
                    string result = await keywordExtractor.ProcessTextAsync(txtOriginal.Text, new Dictionary<string, string>());
                    txtProcessed.Text = result;
                    UpdateStatus("Keyword extraction complete");
                }
                else
                {
                    txtProcessed.Text = "Keyword Extraction plugin not available.";
                    UpdateStatus("Keyword Extraction plugin not available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ProcessKeywordExtraction: {ex}");
                UpdateStatus($"Error extracting keywords: {ex.Message}");
            }
        }
        
        private async void ProcessGrammarCheck(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No text to check");
                    return;
                }
                
                UpdateStatus("Checking grammar...");
                Console.WriteLine($"Attempting to check grammar of text with length: {txtOriginal.Text.Length}");
                
                // Get the Grammar Checker plugin
                var grammarChecker = PluginManager.Instance.GetPlugin<IAIFeaturePlugin>(AIFeatureType.GrammarChecker);
                
                if (grammarChecker != null)
                {
                    // Process the text using the plugin
                    string result = await grammarChecker.ProcessTextAsync(txtOriginal.Text, new Dictionary<string, string>());
                    txtProcessed.Text = result;
                    UpdateStatus("Grammar check complete");
                }
                else
                {
                    txtProcessed.Text = "Grammar Checker plugin not available.";
                    UpdateStatus("Grammar Checker plugin not available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ProcessGrammarCheck: {ex}");
                UpdateStatus($"Error checking grammar: {ex.Message}");
            }
        }
        
        private async void ExpandTemplate()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No template to expand");
                    return;
                }
                
                UpdateStatus("Expanding email template...");
                var options = new Dictionary<string, string>();
                string result = await _aiService.ProcessTextAsync(txtOriginal.Text, TextProcessingType.ExpandEmailTemplate, options);
                txtProcessed.Text = result;
                UpdateStatus("Template expansion complete");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error expanding template: {ex.Message}");
            }
        }
        
        private async void ConvertTable()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOriginal.Text))
                {
                    UpdateStatus("No table data to convert");
                    return;
                }
                
                UpdateStatus("Converting table data...");
                var options = new Dictionary<string, string>();
                string result = await _aiService.ProcessTextAsync(txtOriginal.Text, TextProcessingType.ConvertTable, options);
                txtProcessed.Text = result;
                UpdateStatus("Table conversion complete");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error converting table: {ex.Message}");
            }
        }
        
        private void CmbProcessingType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;
            
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;
            
            string processingType = selectedItem.Content.ToString();
            
            // Hide all option controls
            cmbLanguage.Visibility = Visibility.Collapsed;
            cmbCodeLanguage.Visibility = Visibility.Collapsed;
            cmbTone.Visibility = Visibility.Collapsed;
            cmbTableFormat.Visibility = Visibility.Collapsed;
            
            // Show relevant options based on processing type
            switch (processingType)
            {
                case "Format JSON":
                    cmbCodeLanguage.Visibility = Visibility.Visible;
                    break;

                case "Convert Table":
                    cmbTableFormat.Visibility = Visibility.Visible;
                    break;
            }
        }
        
        private void UpdateStatus(string message)
        {
            // Update the status bar with the given message
            txtStatus.Text = message;
        }
        
        private void UpdateHistoryCount()
        {
            txtHistoryCount.Text = $"History: {_clipboardManager.Items.Count} items";
        }
        
        private void DisableContentPanels()
        {
            // Disable and clear content panels when no item is selected
            txtOriginal.Text = string.Empty;
            txtOriginal.IsEnabled = false;
            imgOriginal.Source = null;
            imgOriginal.Visibility = Visibility.Collapsed;
            
            txtProcessed.Text = string.Empty;
            txtProcessed.IsEnabled = false;
            
            btnApplyProcessing.IsEnabled = false;
            cmbProcessingType.IsEnabled = false;
            cmbLanguage.IsEnabled = false;
            cmbCodeLanguage.IsEnabled = false;
            cmbTone.IsEnabled = false;
            cmbTableFormat.IsEnabled = false;
        }
        
        private void EnableContentPanels()
        {
            // Enable content panels when an item is selected
            txtOriginal.IsEnabled = true;
            txtProcessed.IsEnabled = true;
            
            btnApplyProcessing.IsEnabled = true;
            cmbProcessingType.IsEnabled = true;
            cmbLanguage.IsEnabled = true;
            cmbCodeLanguage.IsEnabled = true;
            cmbTone.IsEnabled = true;
            cmbTableFormat.IsEnabled = true;
        }
        
        #region Clipboard History UI Event Handlers
        
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't search if the text is the placeholder
            if (_isSearchBoxDefault || txtSearch.Text == "Search history...")
                return;
                
            // Filter items based on search text
            string searchText = txtSearch.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // If search is cleared, show all items
                ShowAllItems();
                return;
            }
            
            _filteredItems.Clear();
            foreach (var item in _clipboardManager.Items)
            {
                bool matchFound = false;
                
                // Search in text content
                if (item.TextContent != null && item.TextContent.ToLower().Contains(searchText))
                {
                    matchFound = true;
                }
                
                // Search in file paths
                if (item.FilePath != null && item.FilePath.ToLower().Contains(searchText))
                {
                    matchFound = true;
                }
                
                if (matchFound)
                {
                    _filteredItems.Add(item);
                }
            }
            
            lvClipboardHistory.ItemsSource = _filteredItems;
            
            // Update status with search results count
            UpdateStatus($"Found {_filteredItems.Count} items matching '{searchText}'");
        }
        
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_isSearchBoxDefault)
            {
                txtSearch.Text = "";
                txtSearch.Foreground = System.Windows.Media.Brushes.Black;
                _isSearchBoxDefault = false;
            }
        }
        
        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search history...";
                txtSearch.Foreground = System.Windows.Media.Brushes.Gray;
                _isSearchBoxDefault = true;
            }
        }
        
        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "Search history...";
            txtSearch.Foreground = System.Windows.Media.Brushes.Gray;
            _isSearchBoxDefault = true;
            ShowAllItems();
        }
        
        private void btnShowAll_Click(object sender, RoutedEventArgs e)
        {
            ShowAllItems();
            SetActiveTab(btnShowAll);
        }
        
        private void btnShowFavorites_Click(object sender, RoutedEventArgs e)
        {
            _filteredItems.Clear();
            foreach (var item in _clipboardManager.Items.Where(i => i.IsFavorite))
            {
                _filteredItems.Add(item);
            }
            lvClipboardHistory.ItemsSource = _filteredItems;
            SetActiveTab(btnShowFavorites);
            
            // Update the status to show the count of favorite items
            UpdateStatus($"Showing {_filteredItems.Count} favorite items");
        }
        
        private void btnShowText_Click(object sender, RoutedEventArgs e)
        {
            _filteredItems.Clear();
            foreach (var item in _clipboardManager.Items.Where(i => i.ContentType == ClipboardContentType.Text || 
                                                                   i.ContentType == ClipboardContentType.Html || 
                                                                   i.ContentType == ClipboardContentType.RichText))
            {
                _filteredItems.Add(item);
            }
            lvClipboardHistory.ItemsSource = _filteredItems;
            SetActiveTab(btnShowText);
            
            // Update the status to show the count of text items
            UpdateStatus($"Showing {_filteredItems.Count} text items");
        }
        
        private void btnShowImages_Click(object sender, RoutedEventArgs e)
        {
            _filteredItems.Clear();
            foreach (var item in _clipboardManager.Items.Where(i => i.ContentType == ClipboardContentType.Image))
            {
                _filteredItems.Add(item);
            }
            lvClipboardHistory.ItemsSource = _filteredItems;
            SetActiveTab(btnShowImages);
            
            // Update the status to show the count of image items
            UpdateStatus($"Showing {_filteredItems.Count} image items");
        }
        
        private void btnShowFiles_Click(object sender, RoutedEventArgs e)
        {
            _filteredItems.Clear();
            foreach (var item in _clipboardManager.Items.Where(i => i.ContentType == ClipboardContentType.FileDrop || 
                                                                   i.ContentType == ClipboardContentType.FilePath))
            {
                _filteredItems.Add(item);
            }
            lvClipboardHistory.ItemsSource = _filteredItems;
            SetActiveTab(btnShowFiles);
            
            // Update the status to show the count of file items
            UpdateStatus($"Showing {_filteredItems.Count} file items");
        }
        
        private void lvClipboardHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lvClipboardHistory.SelectedItem as ClipboardItem;
            if (selectedItem == null) 
            {
                DisableContentPanels();
                return;
            }
            
            // Enable content panels
            EnableContentPanels();
            
            // Clear the processed content area when switching items
            txtProcessed.Text = string.Empty;
            
            // Update the original content area based on the selected item
            if (selectedItem.ContentType == ClipboardContentType.Text || 
                selectedItem.ContentType == ClipboardContentType.Html || 
                selectedItem.ContentType == ClipboardContentType.RichText)
            {
                txtOriginal.Text = selectedItem.TextContent;
                imgOriginal.Visibility = Visibility.Collapsed;
                txtOriginal.Visibility = Visibility.Visible;
                
                // Set current content type to Text
                _currentContentType = ContentType.Text;
                
                // Rebuild processing options for text
                RebuildProcessingOptions();
                
                // Enable the Apply button for text items
                btnApplyProcessing.IsEnabled = true;
                
                // Update context menu to show all AI options
                UpdateContextMenuForTextItem();
            }
            else if (selectedItem.ContentType == ClipboardContentType.Image)
            {
                // Display the image in the original content area
                try
                {
                    using (MemoryStream stream = new MemoryStream(selectedItem.ImageData))
                    {
                        // Reset stream position to beginning
                        stream.Position = 0;
                        
                        // Create a bitmap image from the stream
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Important for cross-thread access
                        
                        imgOriginal.Source = bitmap;
                        imgOriginal.Visibility = Visibility.Visible;
                        txtOriginal.Visibility = Visibility.Collapsed;
                        
                        // Update status with image dimensions
                        UpdateStatus($"Image: {bitmap.PixelWidth}x{bitmap.PixelHeight} pixels");
                    }
                    
                    // Set current content type to Image
                    _currentContentType = ContentType.Image;
                    
                    // Rebuild processing options for images
                    RebuildProcessingOptions();
                    
                    // Enable the Apply button for image items
                    btnApplyProcessing.IsEnabled = true;
                    
                    // Update context menu for image items
                    UpdateContextMenuForImageItem();
                }
                catch (Exception ex)
                {
                    txtOriginal.Text = "[Error loading image: " + ex.Message + "]";
                    imgOriginal.Visibility = Visibility.Collapsed;
                    txtOriginal.Visibility = Visibility.Visible;
                    UpdateStatus($"Error loading image: {ex.Message}");
                }
            }
            else if (selectedItem.ContentType == ClipboardContentType.FileDrop || 
                     selectedItem.ContentType == ClipboardContentType.FilePath)
            {
                txtOriginal.Text = selectedItem.FilePath;
                imgOriginal.Visibility = Visibility.Collapsed;
                txtOriginal.Visibility = Visibility.Visible;
                
                // Set current content type to File
                _currentContentType = ContentType.File;
                
                // Show limited options for files
                RebuildProcessingOptions();
                
                // Disable the Apply button for file items
                btnApplyProcessing.IsEnabled = false;
                
                // Update context menu to hide AI options for file items
                UpdateContextMenuForFileItem();
            }
        }
        
        private void ShowAllProcessingOptions()
        {
            // Clear existing items
            cmbProcessingType.Items.Clear();
            
            // Get all available plugins from the plugin manager
            var plugins = GetAvailablePlugins();
            
            // Add menu options for each available plugin
            foreach (var plugin in plugins)
            {
                try
                {
                    // Get the menu option from the plugin
                    var menuOption = plugin.GetMenuOption();
                    if (menuOption != null)
                    {
                        AddProcessingOption(menuOption.Icon, menuOption.Text);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting menu option from plugin {plugin.Name}: {ex.Message}");
                }
            }
            
            // If no plugins are available, show a message
            if (cmbProcessingType.Items.Count == 0)
            {
                AddProcessingOption("ℹ️", "No Plugins Available");
            }
        }
        
        private void ShowImageProcessingOptions()
        {
            // Only show image processing options for images
            cmbProcessingType.Items.Clear();
            
            // Try to find the image processing plugin
            var imageProcessingPlugin = GetPluginByFeatureType(AIFeatureType.OCR);
            
            if (imageProcessingPlugin != null && (imageProcessingPlugin as IFeaturePlugin)?.IsEnabled == true)
            {
                try
                {
                    // Get the menu option from the plugin
                    var menuOption = imageProcessingPlugin.GetMenuOption();
                    if (menuOption != null)
                    {
                        AddProcessingOption(menuOption.Icon, menuOption.Text);
                        Console.WriteLine($"Added OCR option: {menuOption.Text}");
                    }
                    else
                    {
                        // Fallback if plugin doesn't provide a menu option
                        AddProcessingOption("📷", "Text Extraction (Image Recognition)");
                        Console.WriteLine("Added fallback OCR option");
                    }
                }
                catch
                {
                    // Fallback if there's an error getting the menu option
                    AddProcessingOption("📷", "Text Extraction (Image Recognition)");
                    Console.WriteLine($"Error getting OCR menu option");
                }
            }
            else
            {
                // OCR is not enabled
                AddProcessingOption("ℹ️", "OCR Not Enabled");
                Console.WriteLine("OCR is not enabled, added disabled option");
                
                // Update status to inform the user
                UpdateStatus("OCR feature is not enabled. Enable it in Settings to use this feature.");
            }
        }
        
        private void ShowLimitedProcessingOptions()
        {
            // Clear existing items
            cmbProcessingType.Items.Clear();
            
            // Get plugins that support text processing for files
            // Removed Summarization plugin reference
            var jsonPlugin = GetPluginByFeatureType(AIFeatureType.JsonFormatter);
            
            // Add menu options for available plugins
            // Text extraction option removed (was using Summarization plugin)
            
            if (jsonPlugin != null && (jsonPlugin as IFeaturePlugin)?.IsEnabled == true)
            {
                AddProcessingOption("📝", "Format as List");
            }
            
            // If no plugins are available, show a message
            if (cmbProcessingType.Items.Count == 0)
            {
                AddProcessingOption("ℹ️", "No Plugins Available");
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
                    // Force the plugin manager to refresh its state from settings
                    PluginManager.Instance.RefreshPlugins();
                    
                    // Get all plugins after refresh
                    var allPlugins = PluginManager.Instance.GetPlugins<IAIFeaturePlugin>();
                    
                    // Get the app settings
                    var appSettings = AppSettings.Instance;
                    
                    // Filter out duplicate plugins by feature type and only include enabled plugins
                    var uniquePlugins = allPlugins
                        .Where(p => {
                            // Check if the plugin is a feature plugin
                            if (p is IFeaturePlugin featurePlugin)
                            {
                                // Check both the plugin's internal state and the settings
                                bool internalEnabled = featurePlugin.IsEnabled;
                                bool settingsEnabled = appSettings.IsPluginEnabled(featurePlugin.FeatureId);
                                
                                Console.WriteLine($"Plugin {p.Name} ({featurePlugin.FeatureId}): Internal enabled={internalEnabled}, Settings enabled={settingsEnabled}");
                                
                                // If there's a mismatch, update the plugin's state
                                if (internalEnabled != settingsEnabled)
                                {
                                    Console.WriteLine($"Mismatch detected for {p.Name} - updating plugin state to match settings: {settingsEnabled}");
                                    featurePlugin.SetEnabled(settingsEnabled);
                                }
                                
                                // Use the settings value for consistency
                                return settingsEnabled;
                            }
                            return false;
                        })
                        .GroupBy(p => p.FeatureType)
                        .Select(g => g.First())
                        .ToList();
                    
                    plugins.AddRange(uniquePlugins);
                    
                    // Log the enabled plugins for debugging
                    Console.WriteLine($"Found {plugins.Count} enabled plugins:");
                    foreach (var plugin in plugins)
                    {
                        Console.WriteLine($"  - {plugin.Name} ({plugin.FeatureType})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available plugins: {ex.Message}");
            }
            
            return plugins;
        }
        
        /// <summary>
        /// Gets a plugin by its feature type
        /// </summary>
        private IAIFeaturePlugin GetPluginByFeatureType(AIFeatureType featureType)
        {
            try
            {
                // Try to get the plugin from the plugin manager
                if (PluginManager.Instance != null)
                {
                    return PluginManager.Instance.GetPlugin<IAIFeaturePlugin>(featureType);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting plugin for feature type {featureType}: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets the plugin manager instance for use by other classes
        /// </summary>
        /// <returns>Plugin manager instance</returns>
        public PluginManager GetPluginManager()
        {
            return PluginManager.Instance;
        }
        
        /// <summary>
        /// Refreshes the dropdown menu based on the current content type
        /// </summary>
        public void RefreshProcessingOptions()
        {
            Console.WriteLine("REFRESHING PROCESSING OPTIONS");
            Console.WriteLine($"Current content type: {_currentContentType}");
            
            // Clear the dropdown menu
            cmbProcessingType.Items.Clear();
            
            // Check the current content type and rebuild the menu
            if (_currentContentType == ContentType.Image)
            {
                ShowImageProcessingOptions();
            }
            else if (_currentContentType == ContentType.Text)
            {
                ShowAllProcessingOptions();
            }
            else if (_currentContentType == ContentType.File)
            {
                ShowLimitedProcessingOptions();
            }
            
            // Select the first item if available
            if (cmbProcessingType.Items.Count > 0)
            {
                cmbProcessingType.SelectedIndex = 0;
            }
        }
        
        private void AddProcessingOption(string emoji, string text)
        {
            var item = new ComboBoxItem();
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new TextBlock { Text = emoji, FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji"), FontSize = 14, Margin = new Thickness(0, 0, 5, 0) });
            stackPanel.Children.Add(new TextBlock { Text = text });
            item.Content = stackPanel;
            cmbProcessingType.Items.Add(item);
        }
        
        private void lvClipboardHistory_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = lvClipboardHistory.SelectedItem as ClipboardItem;
            if (selectedItem == null) return;
            
            if (selectedItem.ContentType == ClipboardContentType.Text)
            {
                Clipboard.SetText(selectedItem.TextContent);
                UpdateStatus("Text copied to clipboard");
            }
            else if (selectedItem.ContentType == ClipboardContentType.FilePath)
            {
                Clipboard.SetText(selectedItem.FilePath);
                UpdateStatus("File path copied to clipboard");
            }
        }
        
        private void btnToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            string itemId = null;
            
            // Handle both Button and TextBlock senders
            if (sender is Button button)
            {
                itemId = button.Tag as string;
            }
            else if (sender is TextBlock textBlock)
            {
                itemId = textBlock.Tag as string;
            }
            
            if (string.IsNullOrEmpty(itemId)) return;
            
            // Get the item before toggling to check its current state
            var item = _clipboardManager.Items.FirstOrDefault(i => i.Id == itemId);
            bool wasFavorite = item?.IsFavorite ?? false;
            
            // Toggle favorite in the clipboard manager
            _clipboardManager.ToggleFavorite(itemId);
            
            // If we're in the Favorites view and the item was unfavorited, remove it from the filtered list
            bool isInFavoritesView = lvClipboardHistory.ItemsSource == _filteredItems && 
                                    btnShowFavorites.Background != btnShowAll.Background; // Check if favorites tab is active
                                    
            if (isInFavoritesView && wasFavorite)
            {
                var itemToRemove = _filteredItems.FirstOrDefault(i => i.Id == itemId);
                if (itemToRemove != null)
                {
                    _filteredItems.Remove(itemToRemove);
                    // Update the status to show the updated count of favorite items
                    UpdateStatus($"Showing {_filteredItems.Count} favorite items");
                }
            }
            // For other filtered views, just refresh the display
            else if (lvClipboardHistory.ItemsSource != _clipboardManager.Items)
            {
                // Force a refresh of the ListView to update the star icon
                var currentSource = lvClipboardHistory.ItemsSource;
                lvClipboardHistory.ItemsSource = null;
                lvClipboardHistory.ItemsSource = currentSource;
            }
        }
        
        
        private void menuToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = lvClipboardHistory.SelectedItem as ClipboardItem;
            if (selectedItem == null) return;
            
            // Store the current favorite state before toggling
            bool wasFavorite = selectedItem.IsFavorite;
            
            // Toggle favorite in the clipboard manager
            _clipboardManager.ToggleFavorite(selectedItem.Id);
            
            // If we're in the Favorites view and the item was unfavorited, remove it from the filtered list
            bool isInFavoritesView = lvClipboardHistory.ItemsSource == _filteredItems && 
                                    btnShowFavorites.Background != btnShowAll.Background; // Check if favorites tab is active
                                    
            if (isInFavoritesView && wasFavorite)
            {
                _filteredItems.Remove(selectedItem);
                // Update the status to show the updated count of favorite items
                UpdateStatus($"Showing {_filteredItems.Count} favorite items");
            }
            // For other filtered views, just refresh the display
            else if (lvClipboardHistory.ItemsSource != _clipboardManager.Items)
            {
                // Force a refresh of the ListView to update the star icon
                var currentSource = lvClipboardHistory.ItemsSource;
                lvClipboardHistory.ItemsSource = null;
                lvClipboardHistory.ItemsSource = currentSource;
            }
        }
        
        private void menuRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = lvClipboardHistory.SelectedItem as ClipboardItem;
            if (selectedItem == null) return;
            
            // Store the ID before removal for UI updates
            string itemId = selectedItem.Id;
            
            // Update the UI immediately to improve responsiveness
            if (lvClipboardHistory.ItemsSource != _clipboardManager.Items)
            {
                // If we're using a filtered view, remove from the filtered collection first
                var filteredCollection = lvClipboardHistory.ItemsSource as ObservableCollection<ClipboardItem>;
                if (filteredCollection != null)
                {
                    var itemToRemove = filteredCollection.FirstOrDefault(i => i.Id == itemId);
                    if (itemToRemove != null)
                    {
                        filteredCollection.Remove(itemToRemove);
                    }
                }
            }
            
            // Remove the item from the clipboard manager (which now saves asynchronously)
            _clipboardManager.RemoveItem(itemId);
            
            // Update the history count
            UpdateHistoryCount();
        }
        
        // Summarization feature has been completely removed
        
        private void menuJsonFormat_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = lvClipboardHistory.SelectedItem as ClipboardItem;
            if (selectedItem == null) return;
            
            if (selectedItem.ContentType == ClipboardContentType.Text)
            {
                txtOriginal.Text = selectedItem.TextContent;
                // Use FormatJson for JSON formatting instead of btnFormatCode_Click
                FormatJson(sender, e);
            }
        }
        
        /// <summary>
        /// Handle keyboard events for the ListView
        /// </summary>
        private void lvClipboardHistory_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Handle Delete key press to remove the selected item
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                var selectedItem = lvClipboardHistory.SelectedItem as ClipboardItem;
                if (selectedItem != null)
                {
                    // Call the existing remove method to ensure consistent behavior
                    menuRemove_Click(sender, null);
                    
                    // Mark the event as handled to prevent further processing
                    e.Handled = true;
                }
            }
        }
        
        /// <summary>
        /// Prevent context menu from showing for file items
        /// </summary>
        private void lvClipboardHistory_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Get the item at the mouse position
            var hitTestResult = VisualTreeHelper.HitTest(lvClipboardHistory, e.GetPosition(lvClipboardHistory));
            if (hitTestResult == null) return;
            
            // Find the list view item
            DependencyObject obj = hitTestResult.VisualHit;
            while (obj != null && !(obj is ListViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            
            if (obj is ListViewItem item)
            {
                // Get the clipboard item
                var clipboardItem = item.Content as ClipboardItem;
                if (clipboardItem != null)
                {
                    // If it's a file item, prevent the context menu from showing
                    if (clipboardItem.ContentType == ClipboardContentType.FileDrop || 
                        clipboardItem.ContentType == ClipboardContentType.FilePath)
                    {
                        e.Handled = true;
                    }
                }
            }
        }
        
        private void ShowAllItems()
        {
            lvClipboardHistory.ItemsSource = _clipboardManager.Items;
            
            // Update the status to show the count of all items
            UpdateStatus($"Showing all {_clipboardManager.Items.Count} items");
        }
        
        private void FilterItems(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ShowAllItems();
                return;
            }
            
            var searchResults = _clipboardManager.Search(searchText);
            _filteredItems.Clear();
            foreach (var item in searchResults)
            {
                _filteredItems.Add(item);
            }
            lvClipboardHistory.ItemsSource = _filteredItems;
        }
        
        private void SetActiveTab(System.Windows.Controls.Button activeButton)
        {
            // Reset all tab buttons to default style
            System.Windows.Media.Color defaultColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F0F0F0");
            System.Windows.Media.SolidColorBrush defaultBrush = new System.Windows.Media.SolidColorBrush(defaultColor);
            
            btnShowAll.Background = defaultBrush;
            btnShowFavorites.Background = defaultBrush;
            btnShowText.Background = defaultBrush;
            btnShowImages.Background = defaultBrush;
            btnShowFiles.Background = defaultBrush;
            
            // Set active tab button to highlighted style
            System.Windows.Media.Color activeColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0");
            System.Windows.Media.SolidColorBrush activeBrush = new System.Windows.Media.SolidColorBrush(activeColor);
            activeButton.Background = activeBrush;
        }
        
        #endregion

        private bool IsJsonContent(string text)
        {
            text = text.Trim();
            return (text.StartsWith("{") && text.EndsWith("}")) || // Object
                   (text.StartsWith("[") && text.EndsWith("]"));   // Array
        }
        
        private void UpdateContextMenuForTextItem()
        {
            // Get the context menu from the ListView
            if (lvClipboardHistory.ContextMenu is ContextMenu contextMenu)
            {
                // Show all AI processing options for text items
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        string header = menuItem.Header.ToString();
                        
                        // Show all menu items
                        menuItem.Visibility = Visibility.Visible;
                    }
                }
            }
        }
        
        private void UpdateContextMenuForImageItem()
        {
            // Get the context menu from the ListView
            if (lvClipboardHistory.ContextMenu is ContextMenu contextMenu)
            {
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        string header = menuItem.Header.ToString();
                        
                        // Hide all AI processing options except OCR
                        if (header == "Summarize" || header == "Format JSON")
                        {
                            menuItem.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }
        
        private void UpdateContextMenuForFileItem()
        {
            // Get the context menu from the ListView
            if (lvClipboardHistory.ContextMenu is ContextMenu contextMenu)
            {
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        string header = menuItem.Header.ToString();
                        
                        // Hide AI processing options for file items
                        if (header == "Summarize" || header == "Format JSON")
                        {
                            menuItem.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Completely rebuilds the processing options dropdown menu
        /// </summary>
        public void RebuildProcessingOptions()
        {
            Console.WriteLine("REBUILDING PROCESSING OPTIONS");
            Console.WriteLine($"Current content type: {_currentContentType}");
            
            // Clear the dropdown menu
            cmbProcessingType.Items.Clear();
            
            // Force the plugin manager to refresh its state
            if (PluginManager.Instance != null)
            {
                PluginManager.Instance.RefreshPlugins();
                
                // Log all available plugins and their enabled state
                var allPlugins = PluginManager.Instance.GetPlugins<IAIFeaturePlugin>();
                Console.WriteLine($"Available plugins: {allPlugins.Count}");
                foreach (var plugin in allPlugins)
                {
                    var featurePlugin = plugin as IFeaturePlugin;
                    bool isEnabled = featurePlugin?.IsEnabled ?? false;
                    Console.WriteLine($"  - {plugin.Name} ({plugin.FeatureType}): {(isEnabled ? "Enabled" : "Disabled")}");
                }
            }
            
            // Rebuild the dropdown menu based on the current content type
            if (_currentContentType == ContentType.Image)
            {
                Console.WriteLine("Building image processing options");
                BuildImageProcessingOptions();
            }
            else if (_currentContentType == ContentType.Text)
            {
                Console.WriteLine("Building all processing options");
                BuildAllProcessingOptions();
            }
            else if (_currentContentType == ContentType.File)
            {
                Console.WriteLine("Building limited processing options");
                BuildLimitedProcessingOptions();
            }
            
            // Select the first item if available
            if (cmbProcessingType.Items.Count > 0)
            {
                cmbProcessingType.SelectedIndex = 0;
                Console.WriteLine("Selected first processing option");
            }
            
            // Force the UI to update
            cmbProcessingType.UpdateLayout();
            Console.WriteLine($"Processing options rebuilt, {cmbProcessingType.Items.Count} items in dropdown");
        }
        
        /// <summary>
        /// Builds the image processing options dropdown menu
        /// </summary>
        private void BuildImageProcessingOptions()
        {
            // Try to find the image processing plugin
            var imageProcessingPlugin = GetPluginByFeatureType(AIFeatureType.OCR);
            
            if (imageProcessingPlugin != null && (imageProcessingPlugin as IFeaturePlugin)?.IsEnabled == true)
            {
                try
                {
                    // Get the menu option from the plugin
                    var menuOption = imageProcessingPlugin.GetMenuOption();
                    if (menuOption != null)
                    {
                        AddProcessingOption(menuOption.Icon, menuOption.Text);
                        Console.WriteLine($"Added OCR option: {menuOption.Text}");
                    }
                    else
                    {
                        // Fallback if plugin doesn't provide a menu option
                        AddProcessingOption("📷", "Text Extraction (Image Recognition)");
                        Console.WriteLine("Added fallback OCR option");
                    }
                }
                catch (Exception ex)
                {
                    // Fallback if there's an error getting the menu option
                    AddProcessingOption("📷", "Text Extraction (Image Recognition)");
                    Console.WriteLine($"Error getting OCR menu option: {ex.Message}");
                }
            }
            else
            {
                // OCR is not enabled
                AddProcessingOption("ℹ️", "OCR Not Enabled");
                Console.WriteLine("OCR is not enabled, added disabled option");
                
                // Update status to inform the user
                UpdateStatus("OCR feature is not enabled. Enable it in Settings to use this feature.");
            }
        }
        
        /// <summary>
        /// Builds the all processing options dropdown menu
        /// </summary>
        private void BuildAllProcessingOptions()
        {
            // Get all available plugins
            var plugins = GetAvailablePlugins();
            
            // Add options for each plugin
            foreach (var plugin in plugins)
            {
                try
                {
                    // Skip OCR plugin for non-image content
                    if (plugin.FeatureType == AIFeatureType.OCR)
                    {
                        continue;
                    }
                    
                    // Get the menu option from the plugin
                    var menuOption = plugin.GetMenuOption();
                    if (menuOption != null)
                    {
                        AddProcessingOption(menuOption.Icon, menuOption.Text);
                        Console.WriteLine($"Added option: {menuOption.Text}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding menu option for {plugin.Name}: {ex.Message}");
                }
            }
            
            // Add built-in options
            // We no longer need to add a Format JSON option here as it's already provided by the JsonFormatterPlugin
            // The JsonFormatterPlugin will be available if JSON content is detected
            
            // If no plugins are available, show a message
            if (cmbProcessingType.Items.Count == 0)
            {
                AddProcessingOption("ℹ️", "No Plugins Available");
            }
        }
        
        /// <summary>
        /// Builds the limited processing options dropdown menu
        /// </summary>
        private void BuildLimitedProcessingOptions()
        {
            // Get plugins that support text processing for files
            // Removed Summarization plugin reference
            var jsonPlugin = GetPluginByFeatureType(AIFeatureType.JsonFormatter);
            
            // Add menu options for available plugins
            // Text extraction option removed (was using Summarization plugin)
            
            if (jsonPlugin != null && (jsonPlugin as IFeaturePlugin)?.IsEnabled == true)
            {
                AddProcessingOption("📝", "Format as List");
            }
            
            // If no plugins are available, show a message
            if (cmbProcessingType.Items.Count == 0)
            {
                AddProcessingOption("ℹ️", "No Plugins Available");
            }
        }
        
        /// <summary>
        /// Handler for plugin settings changed event
        /// </summary>
        private void MainWindow_PluginSettingsChanged(object sender, EventArgs e)
        {
            // Make sure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => MainWindow_PluginSettingsChanged(sender, e)));
                return;
            }

            Console.WriteLine("PLUGIN SETTINGS CHANGED - REBUILDING PROCESSING OPTIONS");
            
            // Force settings to reload from disk
            _settings = AppSettings.Load();
            Console.WriteLine("Reloaded settings from disk");
            
            // Log all enabled plugins from settings
            var enabledPlugins = _settings.GetEnabledPlugins();
            Console.WriteLine($"Enabled plugins in settings: {string.Join(", ", enabledPlugins)}");
            
            // Force the plugin manager to refresh
            if (PluginManager.Instance != null)
            {
                PluginManager.Instance.RefreshPlugins();
                Console.WriteLine("Refreshed plugin manager");
                
                // Log all plugins and their enabled state
                var allPlugins = PluginManager.Instance.GetPlugins<IAIFeaturePlugin>();
                Console.WriteLine($"All plugins after refresh: {allPlugins.Count()}");
                foreach (var plugin in allPlugins)
                {
                    if (plugin is IFeaturePlugin featurePlugin)
                    {
                        Console.WriteLine($"  - {plugin.Name} ({featurePlugin.FeatureId}): IsEnabled={featurePlugin.IsEnabled}");
                    }
                }
            }
            
            // Completely rebuild the processing options
            RebuildProcessingOptions();
            
            // Update the UI to reflect the changes
            UpdateStatus("Plugin settings updated");
        }
    }

    public class SimplePluginHost : IPluginHost
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
        
        public void LogMessage(string pluginId, LogLevel level, string message)
        {
            Console.WriteLine($"[{level}] {pluginId}: {message}");
        }
        
        public string GetPluginDataPath(string pluginId)
        {
            // Return a temporary directory for plugin data
            string path = Path.Combine(Path.GetTempPath(), "ClipboardAI", "Plugins", pluginId);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        
        public Dictionary<string, object> GetPluginSettings(string pluginId)
        {
            // Return empty settings for simplicity
            return new Dictionary<string, object>();
        }
        
        public void SavePluginSettings(string pluginId, Dictionary<string, object> settings)
        {
            // Do nothing in this simple implementation
            Console.WriteLine($"Saving settings for plugin {pluginId}");
        }
        
        public object GetApplicationSettings()
        {
            // Return null for simplicity
            return null;
        }
        
        public void ShowNotification(string title, string message, PluginNotificationType type)
        {
            // Log the notification to console
            Console.WriteLine($"[Notification] {type}: {title} - {message}");
            
            // Show a message box for important notifications
            if (type == PluginNotificationType.Error || type == PluginNotificationType.Warning)
            {
                MessageBoxImage icon = type == PluginNotificationType.Error ? 
                    MessageBoxImage.Error : MessageBoxImage.Warning;
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(message, title, MessageBoxButton.OK, icon);
                });
            }
        }
    }
}

public enum ContentType
{
    Text,
    Image,
    File
}
