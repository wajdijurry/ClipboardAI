using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Tesseract;

namespace ClipboardAI.Plugins.Features
{
    /// <summary>
    /// Plugin that provides OCR functionality using Tesseract
    /// </summary>
    public class OcrPlugin : AIFeaturePluginBase
    {
        private const string TessdataPath = "tessdata";
        private string _preferredLanguage = "eng";
        private List<string> _installedLanguages = new List<string>();
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "ClipboardAI.Plugins.OCR";
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "OCR (Image to Text)";
        
        /// <summary>
        /// Gets the plugin version
        /// </summary>
        public override Version Version => new Version(1, 0, 0);
        
        /// <summary>
        /// Gets the plugin author
        /// </summary>
        public override string Author => "ClipboardAI";
        
        /// <summary>
        /// Gets the plugin description
        /// </summary>
        public override string Description => "Extract text from images using OCR technology";
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public override AIFeatureType FeatureType => AIFeatureType.OCR;
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">The plugin host interface</param>
        /// <returns>True if initialization was successful</returns>
        public override bool Initialize(IPluginHost host)
        {
            bool result = base.Initialize(host);
            if (!result) return false;
            
            try
            {
                // Check for tessdata directory
                string tessdataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TessdataPath);
                if (!Directory.Exists(tessdataDir))
                {
                    Directory.CreateDirectory(tessdataDir);
                    this.Log(LogLevel.Warning, $"Created tessdata directory at {tessdataDir}");
                }
                
                // Load available language files
                LoadAvailableLanguages();
                
                // Load settings
                var settings = host.GetPluginSettings(Id);
                if (settings != null && settings.ContainsKey("PreferredLanguage"))
                {
                    _preferredLanguage = settings["PreferredLanguage"] as string ?? "eng";
                }
                
                return true;
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error initializing OCR plugin: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Asynchronously process an image using OCR
        /// </summary>
        /// <param name="image">Input image to process</param>
        /// <param name="options">Optional parameters for processing</param>
        /// <returns>Extracted text from the image</returns>
        public override async Task<string> ProcessImageAsync(BitmapSource image, object options = null)
        {
            if (image == null)
            {
                return "Error: No image provided for OCR";
            }
            
            // Freeze the image to make it thread-safe
            BitmapSource threadSafeImage = image;
            if (!threadSafeImage.IsFrozen && threadSafeImage.CanFreeze)
            {
                threadSafeImage = threadSafeImage.Clone();
                threadSafeImage.Freeze();
            }
            
            return await Task.Run(() =>
            {
                try
                {
                    if (_installedLanguages.Count == 0)
                    {
                        return "No OCR language files found. Please install at least one language.";
                    }
                    
                    // Use all available languages, with preferred language as primary if available
                    string languageString;
                    if (_installedLanguages.Contains(_preferredLanguage))
                    {
                        // Put preferred language first
                        var languages = new List<string>(_installedLanguages);
                        languages.Remove(_preferredLanguage);
                        languages.Insert(0, _preferredLanguage);
                        languageString = string.Join("+", languages);
                    }
                    else
                    {
                        languageString = string.Join("+", _installedLanguages);
                    }
                    
                    string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TessdataPath);
                    using (var engine = new TesseractEngine(tessdataPath, languageString, EngineMode.Default))
                    {
                        using (var img = PixConverter.ToPix(threadSafeImage))
                        {
                            using (var page = engine.Process(img))
                            {
                                return page.GetText();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Log(LogLevel.Error, $"OCR error: {ex.Message}");
                    return $"Error performing OCR: {ex.Message}";
                }
            });
        }
        
        /// <summary>
        /// Checks if this feature can process the given content type
        /// </summary>
        /// <param name="contentType">The type of content to check</param>
        /// <returns>True if this feature can process the content type</returns>
        public override bool SupportsContentType(ContentType contentType)
        {
            return contentType == ContentType.Image;
        }
        
        /// <summary>
        /// Gets the plugin settings
        /// </summary>
        /// <returns>Dictionary of settings</returns>
        public override Dictionary<string, object> GetSettings()
        {
            return new Dictionary<string, object>
            {
                { "PreferredLanguage", _preferredLanguage },
                { "InstalledLanguages", _installedLanguages }
            };
        }
        
        /// <summary>
        /// Updates plugin settings
        /// </summary>
        /// <param name="settings">New settings values</param>
        public override void UpdateSettings(Dictionary<string, object> settings)
        {
            if (settings.ContainsKey("PreferredLanguage"))
            {
                _preferredLanguage = settings["PreferredLanguage"] as string ?? "eng";
            }
        }
        
        /// <summary>
        /// Load available language files from the tessdata directory
        /// </summary>
        private void LoadAvailableLanguages()
        {
            try
            {
                string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TessdataPath);
                if (Directory.Exists(tessdataPath))
                {
                    string[] languageFiles = Directory.GetFiles(tessdataPath, "*.traineddata");
                    _installedLanguages = languageFiles
                        .Select(file => Path.GetFileNameWithoutExtension(file))
                        .ToList();
                    
                    this.Log(LogLevel.Information, $"Found {_installedLanguages.Count} OCR language files: {string.Join(", ", _installedLanguages)}");
                }
                else
                {
                    this.Log(LogLevel.Warning, $"Tessdata directory not found at {tessdataPath}");
                }
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error loading OCR language files: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Helper class to convert BitmapSource to Pix format for Tesseract
    /// </summary>
    public static class PixConverter
    {
        /// <summary>
        /// Convert a BitmapSource to a Pix object for Tesseract processing
        /// </summary>
        public static Pix ToPix(BitmapSource source)
        {
            // Convert BitmapSource to a format Tesseract can use
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            var stride = width * ((source.Format.BitsPerPixel + 7) / 8);
            var pixelData = new byte[stride * height];
            source.CopyPixels(pixelData, stride, 0);
            
            // Convert to PNG format for Tesseract
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                
                // Load the Pix from the memory stream
                return Pix.LoadFromMemory(memoryStream.ToArray());
            }
        }
    }
}
