using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClipboardAI.Common;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Type of clipboard content
    /// </summary>
    public enum ClipboardContentType
    {
        Text,
        Image,
        FilePath,
        Html,
        RichText,
        FileDrop
    }

    /// <summary>
    /// Represents a single clipboard item
    /// </summary>
    public class ClipboardItem
    {
        // Unique identifier
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Content type
        public ClipboardContentType ContentType { get; set; }
        
        // Content data
        public string TextContent { get; set; }
        public byte[] ImageData { get; set; }
        public string FilePath { get; set; }
        
        // Metadata
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateTime LastUsedAt { get; set; } = DateTime.Now;
        public int UseCount { get; set; } = 0;
        public bool IsFavorite { get; set; } = false;
        public bool IsEncrypted { get; set; } = false;
        public string SourceApplication { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        
        // Preview text property for binding in XAML
        public string Preview => GetPreviewText(100);
        
        // Preview text (for display in the UI)
        public string GetPreviewText(int maxLength = 100)
        {
            if (ContentType == ClipboardContentType.Text || ContentType == ClipboardContentType.Html || ContentType == ClipboardContentType.RichText)
            {
                if (string.IsNullOrEmpty(TextContent))
                    return "[Empty]";
                    
                if (TextContent.Length <= maxLength)
                    return TextContent;
                
                return TextContent.Substring(0, maxLength) + "...";
            }
            else if (ContentType == ClipboardContentType.Image)
            {
                return "[Image]";
            }
            else if (ContentType == ClipboardContentType.FilePath || ContentType == ClipboardContentType.FileDrop)
            {
                if (string.IsNullOrEmpty(FilePath))
                    return "[File path]";
                
                return Path.GetFileName(FilePath);
            }
            
            return "[Unknown content]";
        }
        
        // Thumbnail image for display in the UI
        public BitmapSource Thumbnail
        {
            get
            {
                if (ContentType != ClipboardContentType.Image || ImageData == null || ImageData.Length == 0)
                    return null;
                
                try
                {
                    // Create a new MemoryStream each time to avoid issues with disposed streams
                    var stream = new MemoryStream(ImageData);
                    stream.Position = 0;
                    
                    // Try to create a bitmap from the image data
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.DecodePixelWidth = 40; // Limit size for performance
                    bitmap.DecodePixelHeight = 40;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Important for cross-thread access
                    
                    // Don't dispose the stream here as it's still being used by the BitmapImage
                    // The GC will handle it
                    
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating thumbnail: {ex.Message}");
                    return null;
                }
            }
        }
    }

    /// <summary>
    /// Serializable version of ClipboardItem that stores image data as Base64 string
    /// </summary>
    public class SerializableClipboardItem
    {
        // Unique identifier
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Content type
        public ClipboardContentType ContentType { get; set; }
        
        // Content data
        public string TextContent { get; set; }
        public string ImageData { get; set; } // Base64 encoded image data
        public string FilePath { get; set; }
        
        // Metadata
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateTime LastUsedAt { get; set; } = DateTime.Now;
        public int UseCount { get; set; } = 0;
        public bool IsFavorite { get; set; } = false;
        public bool IsEncrypted { get; set; } = false;
        public string SourceApplication { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Manages clipboard history and operations
    /// </summary>
    public class ClipboardManager
    {
        private static readonly Lazy<ClipboardManager> _instance = new Lazy<ClipboardManager>(() => new ClipboardManager());
        public static ClipboardManager Instance => _instance.Value;
        
        // History of clipboard items
        private readonly List<ClipboardItem> _history = new List<ClipboardItem>();
        
        // Observable collection for UI binding
        public ObservableCollection<ClipboardItem> Items { get; private set; } = new ObservableCollection<ClipboardItem>();
        
        // Favorites
        public ObservableCollection<ClipboardItem> Favorites { get; private set; } = new ObservableCollection<ClipboardItem>();
        
        // Settings
        private AppSettings _settings;
        
        // Encryption key for secure items
        private byte[] _encryptionKey;
        
        // Maximum history size
        private const int DefaultMaxHistorySize = 100;
        
        // File path for saving history
        private readonly string _historyFilePath;
        
        private ClipboardManager()
        {
            _settings = AppSettings.Load();
            
            // Set up history file path
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClipboardAI");
                
            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);
                
            _historyFilePath = Path.Combine(appDataPath, "history.json");
            
            // Initialize encryption
            InitializeEncryption();
            
            // Load history from file
            LoadHistory();
            
            // Start cleanup timer for expiration rules
            StartCleanupTimer();
        }
        
        /// <summary>
        /// Initialize encryption for secure clipboard items
        /// </summary>
        private void InitializeEncryption()
        {
            try
            {
                string keyFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClipboardAI",
                    "encryption.key");
                    
                if (File.Exists(keyFile))
                {
                    _encryptionKey = File.ReadAllBytes(keyFile);
                }
                else
                {
                    // Generate a new encryption key
                    using (var rng = new RNGCryptoServiceProvider())
                    {
                        _encryptionKey = new byte[32]; // 256 bits
                        rng.GetBytes(_encryptionKey);
                        File.WriteAllBytes(keyFile, _encryptionKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing encryption: {ex.Message}");
                // Fall back to a default key (not secure, but prevents crashes)
                _encryptionKey = Encoding.UTF8.GetBytes("ClipboardAIDefaultEncryptionKey12345");
            }
        }
        
        /// <summary>
        /// Start timer for cleaning up expired items
        /// </summary>
        private void StartCleanupTimer()
        {
            var timer = new System.Threading.Timer(_ =>
            {
                CleanupExpiredItems();
            }, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        }
        
        /// <summary>
        /// Add a new item to the clipboard history
        /// </summary>
        public void AddItem(ClipboardItem item)
        {
            // Check if item should be added based on app whitelisting
            if (_settings.UseAppWhitelisting && 
                !string.IsNullOrEmpty(item.SourceApplication) && 
                !_settings.WhitelistedApps.Contains(item.SourceApplication))
            {
                return;
            }
            
            // Check for duplicates
            var existingItem = _history.FirstOrDefault(i => 
                i.ContentType == item.ContentType && 
                ((i.ContentType == ClipboardContentType.Text && i.TextContent == item.TextContent) ||
                 (i.ContentType == ClipboardContentType.FilePath && i.FilePath == item.FilePath)));
                
            if (existingItem != null)
            {
                // Update existing item
                existingItem.LastUsedAt = DateTime.Now;
                existingItem.UseCount++;
                
                // Move to top of the list
                _history.Remove(existingItem);
                _history.Insert(0, existingItem);
                
                // Update observable collections
                RefreshCollections();
                return;
            }
            
            // Add new item
            _history.Insert(0, item);
            
            // Trim history if needed
            int maxHistorySize = _settings.MaxHistorySize > 0 ? _settings.MaxHistorySize : DefaultMaxHistorySize;
            if (_history.Count > maxHistorySize)
            {
                // Remove oldest non-favorite items
                var itemsToRemove = _history
                    .Where(i => !i.IsFavorite)
                    .OrderBy(i => i.LastUsedAt)
                    .Take(_history.Count - maxHistorySize)
                    .ToList();
                    
                foreach (var itemToRemove in itemsToRemove)
                {
                    _history.Remove(itemToRemove);
                }
            }
            
            // Update observable collections
            RefreshCollections();
            
            // Save history
            SaveHistory();
            
            // Log activity if enabled
            if (_settings.EnableAuditLogging)
            {
                LogClipboardActivity(item);
            }
        }
        
        /// <summary>
        /// Remove an item from the clipboard history
        /// </summary>
        public void RemoveItem(string id)
        {
            var item = _history.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                _history.Remove(item);
                RefreshCollections();
                
                // Save history asynchronously to prevent UI freezing
                Task.Run(() => SaveHistory());
            }
        }
        
        /// <summary>
        /// Toggle favorite status for an item
        /// </summary>
        public void ToggleFavorite(string id)
        {
            var item = _history.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                item.IsFavorite = !item.IsFavorite;
                RefreshCollections();
                SaveHistory();
            }
        }
        
        /// <summary>
        /// Search for items in the history
        /// </summary>
        public List<ClipboardItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _history.ToList();
                
            query = query.ToLower();
            
            return _history.Where(item => 
                (item.ContentType == ClipboardContentType.Text && item.TextContent?.ToLower().Contains(query) == true) ||
                (item.ContentType == ClipboardContentType.FilePath && item.FilePath?.ToLower().Contains(query) == true) ||
                (item.Tags != null && item.Tags.Any(t => t.ToLower().Contains(query)))
            ).ToList();
        }
        
        /// <summary>
        /// Encrypt text content
        /// </summary>
        public string EncryptText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.GenerateIV();
                    
                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        // Write IV to the beginning of the stream
                        ms.Write(aes.IV, 0, aes.IV.Length);
                        
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(text);
                        }
                        
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error encrypting text: {ex.Message}");
                return text;
            }
        }
        
        /// <summary>
        /// Decrypt text content
        /// </summary>
        public string DecryptText(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;
                
            try
            {
                byte[] cipherText = Convert.FromBase64String(encryptedText);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    
                    // Get IV from the beginning of the cipherText
                    byte[] iv = new byte[aes.BlockSize / 8];
                    Array.Copy(cipherText, 0, iv, 0, iv.Length);
                    aes.IV = iv;
                    
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting text: {ex.Message}");
                return "[Encrypted Content]";
            }
        }
        
        /// <summary>
        /// Clean up expired items based on expiration rules
        /// </summary>
        private void CleanupExpiredItems()
        {
            if (_settings.ExpirationDays <= 0)
                return;
                
            var expirationDate = DateTime.Now.AddDays(-_settings.ExpirationDays);
            
            var expiredItems = _history
                .Where(i => !i.IsFavorite && i.LastUsedAt < expirationDate)
                .ToList();
                
            foreach (var item in expiredItems)
            {
                _history.Remove(item);
            }
            
            if (expiredItems.Any())
            {
                RefreshCollections();
                SaveHistory();
            }
        }
        
        /// <summary>
        /// Refresh the observable collections
        /// </summary>
        private void RefreshCollections()
        {
            // Update Items collection
            Items.Clear();
            foreach (var item in _history)
            {
                Items.Add(item);
            }
            
            // Update Favorites collection
            Favorites.Clear();
            foreach (var item in _history.Where(i => i.IsFavorite))
            {
                Favorites.Add(item);
            }
        }
        
        /// <summary>
        /// Save history to file
        /// </summary>
        private void SaveHistory()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                // Convert image data to Base64 strings for serialization
                var serializableItems = _history.Select(item => new SerializableClipboardItem
                {
                    Id = item.Id,
                    ContentType = item.ContentType,
                    TextContent = item.TextContent,
                    ImageData = item.ImageData != null ? Convert.ToBase64String(item.ImageData) : null,
                    FilePath = item.FilePath,
                    Timestamp = item.Timestamp,
                    LastUsedAt = item.LastUsedAt,
                    UseCount = item.UseCount,
                    IsFavorite = item.IsFavorite,
                    IsEncrypted = item.IsEncrypted,
                    SourceApplication = item.SourceApplication,
                    Tags = item.Tags
                }).ToList();
                
                string json = JsonSerializer.Serialize(serializableItems, options);
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving history: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load history from file
        /// </summary>
        private void LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    string json = File.ReadAllText(_historyFilePath);
                    var serializableItems = JsonSerializer.Deserialize<List<SerializableClipboardItem>>(json);
                    
                    if (serializableItems != null)
                    {
                        _history.Clear();
                        
                        foreach (var serItem in serializableItems)
                        {
                            var item = new ClipboardItem
                            {
                                Id = serItem.Id,
                                ContentType = serItem.ContentType,
                                TextContent = serItem.TextContent,
                                ImageData = !string.IsNullOrEmpty(serItem.ImageData) 
                                    ? Convert.FromBase64String(serItem.ImageData) 
                                    : null,
                                FilePath = serItem.FilePath,
                                Timestamp = serItem.Timestamp,
                                LastUsedAt = serItem.LastUsedAt,
                                UseCount = serItem.UseCount,
                                IsFavorite = serItem.IsFavorite,
                                IsEncrypted = serItem.IsEncrypted,
                                SourceApplication = serItem.SourceApplication,
                                Tags = serItem.Tags
                            };
                            
                            _history.Add(item);
                        }
                        
                        RefreshCollections();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading history: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log clipboard activity
        /// </summary>
        private void LogClipboardActivity(ClipboardItem item)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClipboardAI",
                    "Logs");
                    
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                    
                string logFile = Path.Combine(logDir, $"clipboard_{DateTime.Now:yyyy-MM-dd}.log");
                
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                 $"Type: {item.ContentType}, " +
                                 $"App: {item.SourceApplication ?? "Unknown"}, " +
                                 $"Preview: {item.GetPreviewText(50)}";
                                 
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging activity: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Export clipboard history to a file
        /// </summary>
        public bool ExportHistory(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                // Convert image data to Base64 strings for serialization
                var serializableItems = _history.Select(item => new SerializableClipboardItem
                {
                    Id = item.Id,
                    ContentType = item.ContentType,
                    TextContent = item.TextContent,
                    ImageData = item.ImageData != null ? Convert.ToBase64String(item.ImageData) : null,
                    FilePath = item.FilePath,
                    Timestamp = item.Timestamp,
                    LastUsedAt = item.LastUsedAt,
                    UseCount = item.UseCount,
                    IsFavorite = item.IsFavorite,
                    IsEncrypted = item.IsEncrypted,
                    SourceApplication = item.SourceApplication,
                    Tags = item.Tags
                }).ToList();
                
                string json = JsonSerializer.Serialize(serializableItems, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting history: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Import clipboard history from a file
        /// </summary>
        public bool ImportHistory(string filePath, bool merge = false)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;
                    
                string json = File.ReadAllText(filePath);
                var serializableItems = JsonSerializer.Deserialize<List<SerializableClipboardItem>>(json);
                
                if (serializableItems == null)
                    return false;
                
                if (merge)
                {
                    // Merge with existing history
                    foreach (var serItem in serializableItems)
                    {
                        if (!_history.Any(i => i.Id == serItem.Id))
                        {
                            var item = new ClipboardItem
                            {
                                Id = serItem.Id,
                                ContentType = serItem.ContentType,
                                TextContent = serItem.TextContent,
                                ImageData = !string.IsNullOrEmpty(serItem.ImageData) 
                                    ? Convert.FromBase64String(serItem.ImageData) 
                                    : null,
                                FilePath = serItem.FilePath,
                                Timestamp = serItem.Timestamp,
                                LastUsedAt = serItem.LastUsedAt,
                                UseCount = serItem.UseCount,
                                IsFavorite = serItem.IsFavorite,
                                IsEncrypted = serItem.IsEncrypted,
                                SourceApplication = serItem.SourceApplication,
                                Tags = serItem.Tags
                            };
                            
                            _history.Add(item);
                        }
                    }
                }
                else
                {
                    // Replace existing history
                    _history.Clear();
                    
                    foreach (var serItem in serializableItems)
                    {
                        var item = new ClipboardItem
                        {
                            Id = serItem.Id,
                            ContentType = serItem.ContentType,
                            TextContent = serItem.TextContent,
                            ImageData = !string.IsNullOrEmpty(serItem.ImageData) 
                                ? Convert.FromBase64String(serItem.ImageData) 
                                : null,
                            FilePath = serItem.FilePath,
                            Timestamp = serItem.Timestamp,
                            LastUsedAt = serItem.LastUsedAt,
                            UseCount = serItem.UseCount,
                            IsFavorite = serItem.IsFavorite,
                            IsEncrypted = serItem.IsEncrypted,
                            SourceApplication = serItem.SourceApplication,
                            Tags = serItem.Tags
                        };
                        
                        _history.Add(item);
                    }
                }
                
                RefreshCollections();
                SaveHistory();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing history: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get all clipboard items in the history
        /// </summary>
        /// <returns>List of clipboard items</returns>
        public List<ClipboardItem> GetClipboardItems()
        {
            return _history.ToList();
        }
        
        /// <summary>
        /// Set clipboard content from a clipboard item
        /// </summary>
        /// <param name="item">The clipboard item to set as current content</param>
        public void SetClipboardContent(ClipboardItem item)
        {
            try
            {
                // Update item usage statistics
                item.LastUsedAt = DateTime.Now;
                item.UseCount++;
                
                // Set content to clipboard based on type
                if (item.ContentType == ClipboardContentType.Text)
                {
                    System.Windows.Clipboard.SetText(item.TextContent);
                }
                else if (item.ContentType == ClipboardContentType.Image && item.ImageData != null)
                {
                    using (MemoryStream stream = new MemoryStream(item.ImageData))
                    {
                        stream.Position = 0;
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Important for cross-thread access
                        
                        System.Windows.Clipboard.SetImage(bitmap);
                    }
                }
                else if (item.ContentType == ClipboardContentType.FilePath || item.ContentType == ClipboardContentType.FileDrop)
                {
                    if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                    {
                        string[] files = { item.FilePath };
                        System.Windows.Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection { item.FilePath });
                    }
                }
                
                // Save history to reflect usage update
                SaveHistory();
                
                // Log activity if enabled
                if (_settings.EnableAuditLogging)
                {
                    LogClipboardActivity(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting clipboard content: {ex.Message}");
            }
        }
    }
}
