using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClipboardAI.Plugins;
using Tesseract;

namespace ClipboardAI.Plugin.OCR
{
    /// <summary>
    /// Plugin that provides OCR functionality using Tesseract
    /// </summary>
    public class OcrPlugin : FeaturePluginBase, IPluginWithSettings, OnnxOcrProcessor.ILogger
    {
        private const string TessdataPath = "Plugins\\OCR\\Models";
        private string _preferredLanguage = "eng";
        private string _preferredEngine = "tesseract";
        private readonly Dictionary<string, TesseractEngine> _engines = new Dictionary<string, TesseractEngine>();
        private string _tessdataDirectory;
        private string _onnxModelsDirectory;
        private readonly List<string> _installedLanguages = new List<string>();
        private OnnxOcrProcessor? _onnxProcessor;
        
        /// <summary>
        /// Plugin ID
        /// </summary>
        public override string Id => "OCR";
        
        /// <summary>
        /// Feature ID
        /// </summary>
        public override string FeatureId => Id;
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "OCR Plugin";
        
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
        /// Gets the display name of the feature
        /// </summary>
        public override string FeatureName => "Optical Character Recognition";
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">The plugin host interface</param>
        /// <returns>True if initialization was successful</returns>
        public override bool Initialize(IPluginHost host)
        {
            bool result = base.Initialize(host);
            if (!result) return false;
            
            // Register this plugin as a provider for the feature
            FeatureRegistry.Instance.RegisterFeatureProvider(FeatureId, this);
            
            try
            {
                // Immediately load settings from disk
                RefreshFromAppSettings();
                
                // Force enable the plugin if it's not already enabled
                var settings = GetAppSettings();
                if (settings != null)
                {
                    bool isEnabled = settings.IsPluginEnabled(Id);
                    Console.WriteLine($"OCR Plugin: Initialize - current enabled state: {isEnabled}");
                    
                    if (!isEnabled)
                    {
                        Console.WriteLine($"OCR Plugin: Initialize - forcing enabled state to True");
                        settings.SetPluginEnabled(Id, true);
                    }
                }
                
                // Determine the correct architecture directory
                string architecture = Environment.Is64BitProcess ? "x64" : "x86";
                this.Log(LogLevel.Information, $"Running as {architecture} process");
                
                // Define the single model directory path
                string appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
                string pluginsDir = Path.Combine(appBaseDir, "Plugins");
                string ocrPluginDir = Path.Combine(pluginsDir, "OCR");
                string ocrModelsDir = Path.Combine(ocrPluginDir, "Models");
                
                // Set the ONNX models directory
                _onnxModelsDirectory = ocrModelsDir;
                
                // Create a list with just the one preferred location
                List<string> potentialTessdataDirs = new List<string>();
                potentialTessdataDirs.Add(ocrModelsDir);
                
                // Log all potential directories we're checking
                this.Log(LogLevel.Information, $"Checking {potentialTessdataDirs.Count} potential tessdata directories:");
                foreach (var dir in potentialTessdataDirs)
                {
                    this.Log(LogLevel.Information, $"- {dir} (Exists: {Directory.Exists(dir)})");
                    
                    // If directory exists, check for language files
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir, "*.traineddata");
                        this.Log(LogLevel.Information, $"  Found {files.Length} language files: {string.Join(", ", files.Select(Path.GetFileName))}");
                    }
                }
                
                // Find the first existing tessdata directory with language files
                string tessdataDir = null;
                foreach (var dir in potentialTessdataDirs)
                {
                    if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.traineddata").Length > 0)
                    {
                        tessdataDir = dir;
                        this.Log(LogLevel.Information, $"Found existing tessdata directory with language files: {tessdataDir}");
                        break;
                    }
                    else if (Directory.Exists(dir))
                    {
                        this.Log(LogLevel.Information, $"Directory exists but contains no language files: {dir}");
                    }
                }
                
                // If no tessdata directory with language files exists, show an error message
                if (tessdataDir == null)
                {
                    // Use the plugin-specific model directory as the default location
                    tessdataDir = Path.Combine(appBaseDir, TessdataPath);
                    
                    // Ensure the directory exists
                    if (!Directory.Exists(tessdataDir))
                    {
                        Directory.CreateDirectory(tessdataDir);
                    }
                    
                    this.Log(LogLevel.Error, $"No OCR language files found. Models should be downloaded during installation.");
                    this.Log(LogLevel.Error, $"Please reinstall the application or contact support.");
                }
                
                // Store the tessdata directory for later use
                _tessdataDirectory = tessdataDir;
                
                // Log the tessdata directory for debugging
                this.Log(LogLevel.Information, $"Using tessdata directory: {_tessdataDirectory}");
                
                // Load available language files
                LoadAvailableLanguages();
                
                // Initialize ONNX processor
                InitializeOnnxProcessor();
                
                // Try to get the global app settings for OCR language preference
                try
                {
                    var appSettings = GetAppSettings();
                    if (appSettings != null)
                    {
                        string preferredLanguage = appSettings.GetPluginSetting<string>("OCR", "PreferredLanguage", "eng");
                        if (!string.IsNullOrEmpty(preferredLanguage))
                        {
                            _preferredLanguage = preferredLanguage;
                            this.Log(LogLevel.Information, $"Using application setting for OCR language: {_preferredLanguage}");
                        }
                        
                        string preferredEngine = appSettings.GetPluginSetting<string>("OCR", "PreferredEngine", "tesseract");
                        if (!string.IsNullOrEmpty(preferredEngine))
                        {
                            _preferredEngine = preferredEngine;
                            this.Log(LogLevel.Information, $"Using application setting for OCR engine: {_preferredEngine}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Log(LogLevel.Warning, $"Could not retrieve application settings for OCR: {ex.Message}");
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
                throw new ArgumentNullException(nameof(image));
            }
            
            // Always use the preferred language from settings
            string language = _preferredLanguage;
            
            // Log the language being used
            this.Log(LogLevel.Information, $"OCR requested with language: {language} using engine: {_preferredEngine}");
            
            try
            {
                // ONNX engine option is disabled until appropriate models are found
                // if (_preferredEngine == "onnx")
                // {
                //     if (_onnxProcessor == null)
                //     {
                //         this.Log(LogLevel.Error, "ONNX processor is not initialized");
                //         this.Log(LogLevel.Information, "Falling back to Tesseract OCR");
                //     }
                //     else
                //     {
                //         try
                //         {
                //             this.Log(LogLevel.Information, "Processing with ONNX Vision");
                //             return await _onnxProcessor.ProcessImageAsync(image);
                //         }
                //         catch (Exception ex)
                //         {
                //             this.Log(LogLevel.Error, $"Error in ONNX processing: {ex.Message}");
                //             this.Log(LogLevel.Information, "Falling back to Tesseract OCR");
                //         }
                //     }
                // }
                
                // Use Tesseract as default or fallback
                // First try using the safer external Tesseract approach
                return await Task.Run(() => ProcessImageWithExternalTesseract(image, language));
            }
            catch (AccessViolationException ex)
            {
                this.Log(LogLevel.Error, $"Access violation in Tesseract: {ex.Message}");
                this.Log(LogLevel.Information, "Falling back to command-line Tesseract");
                
                // If we get an access violation, try the command-line approach as a last resort
                return await Task.Run(() => ProcessImageWithCommandLineTesseract(image, language));
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error in OCR processing: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Process an image using a safer external Tesseract approach to avoid access violations
        /// </summary>
        private string ProcessImageWithExternalTesseract(BitmapSource image, string language)
        {
            try
            {
                // Create a unique temporary directory for this operation
                string tempDir = Path.Combine(Path.GetTempPath(), $"ocr_temp_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);
                
                try
                {
                    // Save the image to a temporary file
                    string imageFile = Path.Combine(tempDir, "image.tiff");
                    SaveImageToFile(image, imageFile);
                    
                    // Create a temporary tessdata directory
                    string tempTessdata = Path.Combine(tempDir, "tessdata");
                    Directory.CreateDirectory(tempTessdata);
                    
                    // Copy the language file to the temporary tessdata directory
                    string sourceLanguageFile = Path.Combine(_tessdataDirectory, $"{language}.traineddata");
                    string destLanguageFile = Path.Combine(tempTessdata, $"{language}.traineddata");
                    File.Copy(sourceLanguageFile, destLanguageFile);
                    
                    // Determine the architecture
                    string architecture = Environment.Is64BitProcess ? "x64" : "x86";
                    
                    // Get the plugin directory
                    string pluginDir = Path.GetDirectoryName(GetType().Assembly.Location);
                    if (pluginDir == null)
                    {
                        throw new InvalidOperationException("Could not determine plugin directory");
                    }
                    
                    // Set the architecture-specific directory for Tesseract DLLs
                    string archDir = Path.Combine(pluginDir, "..", architecture);
                    
                    // Copy the Tesseract DLLs to the temporary directory
                    if (Directory.Exists(archDir))
                    {
                        foreach (var dll in Directory.GetFiles(archDir, "*.dll"))
                        {
                            string destDll = Path.Combine(tempDir, Path.GetFileName(dll));
                            File.Copy(dll, destDll);
                        }
                    }
                    
                    // Create a new Tesseract engine in the isolated environment
                    using (var engine = new TesseractEngine(tempTessdata, language, EngineMode.Default))
                    {
                        // Load the image using Pix
                        using (var pix = Pix.LoadFromFile(imageFile))
                        {
                            if (pix == null)
                            {
                                return "Error: Failed to load image for OCR processing";
                            }
                            
                            // Process the image
                            using (var page = engine.Process(pix))
                            {
                                // Get the recognized text
                                string text = page.GetText();
                                
                                // Log OCR confidence
                                float confidence = page.GetMeanConfidence();
                                this.Log(LogLevel.Information, $"OCR completed with {confidence * 100:F1}% confidence");
                                
                                return text;
                            }
                        }
                    }
                }
                finally
                {
                    // Clean up the temporary directory
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        this.Log(LogLevel.Warning, $"Failed to clean up temporary directory: {ex.Message}");
                    }
                }
            }
            catch (AccessViolationException ex)
            {
                this.Log(LogLevel.Error, $"Access violation in Tesseract: {ex.Message}");
                
                // Try one last approach - use a command-line process to run Tesseract
                return ProcessImageWithCommandLineTesseract(image, language);
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error processing image: {ex.Message}");
                return $"Error performing OCR: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Process an image using command-line Tesseract as a last resort
        /// </summary>
        private string ProcessImageWithCommandLineTesseract(BitmapSource image, string language)
        {
            try
            {
                // Create a unique temporary directory for this operation
                string tempDir = Path.Combine(Path.GetTempPath(), $"ocr_cmd_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);
                
                try
                {
                    // Save the image to a temporary file
                    string imageFile = Path.Combine(tempDir, "image.tiff");
                    SaveImageToFile(image, imageFile);
                    
                    // Determine the architecture
                    string architecture = Environment.Is64BitProcess ? "x64" : "x86";
                    
                    // Get the plugin directory
                    string pluginDir = Path.GetDirectoryName(GetType().Assembly.Location);
                    if (pluginDir == null)
                    {
                        throw new InvalidOperationException("Could not determine plugin directory");
                    }
                    
                    // Set the architecture-specific directory for Tesseract DLLs
                    string archDir = Path.Combine(pluginDir, "..", architecture);
                    
                    // Output file for Tesseract
                    string outputBase = Path.Combine(tempDir, "output");
                    string outputFile = outputBase + ".txt";
                    
                    // Create a process to run Tesseract
                    using (var process = new System.Diagnostics.Process())
                    {
                        process.StartInfo.FileName = Path.Combine(archDir, "tesseract.exe");
                        process.StartInfo.Arguments = $"\"{imageFile}\" \"{outputBase}\" -l {language}";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.CreateNoWindow = true;
                        
                        // Set the TESSDATA_PREFIX environment variable
                        process.StartInfo.EnvironmentVariables["TESSDATA_PREFIX"] = _tessdataDirectory;
                        
                        // Start the process
                        process.Start();
                        
                        // Read the output
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        
                        // Wait for the process to exit
                        process.WaitForExit();
                        
                        // Check if the process was successful
                        if (process.ExitCode != 0)
                        {
                            this.Log(LogLevel.Error, $"Tesseract command-line process failed with exit code {process.ExitCode}: {error}");
                            return $"Error performing OCR: Tesseract command-line process failed with exit code {process.ExitCode}";
                        }
                        
                        // Read the output file
                        if (File.Exists(outputFile))
                        {
                            string text = File.ReadAllText(outputFile);
                            this.Log(LogLevel.Information, $"OCR completed using command-line Tesseract");
                            return text;
                        }
                        else
                        {
                            this.Log(LogLevel.Error, $"Tesseract output file not found: {outputFile}");
                            return "Error performing OCR: Tesseract output file not found";
                        }
                    }
                }
                finally
                {
                    // Clean up the temporary directory
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        this.Log(LogLevel.Warning, $"Failed to clean up temporary directory: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error processing image with command-line Tesseract: {ex.Message}");
                return $"Error performing OCR: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Save a BitmapSource to a file
        /// </summary>
        private void SaveImageToFile(BitmapSource image, string filePath)
        {
            // Ensure the source is in a format Tesseract can handle
            BitmapSource processableSource = image;
            
            // If the source format is not compatible, convert it to a compatible format
            if (image.Format != System.Windows.Media.PixelFormats.Bgr24 && 
                image.Format != System.Windows.Media.PixelFormats.Bgra32 &&
                image.Format != System.Windows.Media.PixelFormats.Rgb24)
            {
                processableSource = new System.Windows.Media.Imaging.FormatConvertedBitmap(
                    image, System.Windows.Media.PixelFormats.Bgr24, null, 0);
            }
            
            // Save the image to the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new System.Windows.Media.Imaging.TiffBitmapEncoder();
                encoder.Compression = System.Windows.Media.Imaging.TiffCompressOption.None;
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(processableSource));
                encoder.Save(fileStream);
            }
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
                { "PreferredEngine", "tesseract" }, // Force Tesseract as the only option for now
                { "InstalledLanguages", _installedLanguages }
            };
        }
        
        /// <summary>
        /// Updates plugin settings
        /// </summary>
        /// <param name="settings">New settings values</param>
        public override void UpdateSettings(Dictionary<string, object> settings)
        {
            if (settings.ContainsKey("PreferredLanguage") && settings["PreferredLanguage"] is string language)
            {
                _preferredLanguage = language;
            }
            
            // Force Tesseract as the engine regardless of settings
            _preferredEngine = "tesseract";
            
            // ONNX initialization disabled until appropriate models are found
            // if (settings.ContainsKey("PreferredEngine") && settings["PreferredEngine"] is string engine)
            // {
            //     bool engineChanged = _preferredEngine != engine;
            //     _preferredEngine = engine;
            //     
            //     // If engine changed to ONNX and ONNX processor is not initialized, initialize it
            //     if (engineChanged && _preferredEngine == "onnx" && _onnxProcessor == null)
            //     {
            //         InitializeOnnxProcessor();
            //     }
            // }
            
            // Save settings
            Host?.SavePluginSettings(Id, settings);
        }
        
        /// <summary>
        /// Refreshes plugin settings from application settings
        /// </summary>
        public override void RefreshFromAppSettings()
        {
            var settings = GetAppSettings();
            if (settings != null)
            {
                // Get the preferred language from settings
                string preferredLanguage = settings.GetPluginSetting<string>(Id, "PreferredLanguage", "eng");
                if (!string.IsNullOrEmpty(preferredLanguage))
                {
                    _preferredLanguage = preferredLanguage;
                    this.Log(LogLevel.Information, $"Refreshed OCR language preference: {_preferredLanguage}");
                }
                else
                {
                    this.Log(LogLevel.Warning, "Failed to get preferred language from settings, using default: eng");
                }
                
                // Force Tesseract as the engine regardless of settings
                _preferredEngine = "tesseract";
                this.Log(LogLevel.Information, $"Using Tesseract OCR engine (ONNX Vision disabled until appropriate models are found)");
                
                // ONNX initialization disabled until appropriate models are found
                // string preferredEngine = settings.GetPluginSetting<string>(Id, "PreferredEngine", "tesseract");
                // if (!string.IsNullOrEmpty(preferredEngine))
                // {
                //     bool engineChanged = _preferredEngine != preferredEngine;
                //     _preferredEngine = preferredEngine;
                //     this.Log(LogLevel.Information, $"Refreshed OCR engine preference: {_preferredEngine}");
                //     
                //     // If engine changed to ONNX and ONNX processor is not initialized, initialize it
                //     if (engineChanged && _preferredEngine == "onnx" && _onnxProcessor == null)
                //     {
                //         InitializeOnnxProcessor();
                //     }
                // }
                
                // Check if the preferred language is installed
                if (!_installedLanguages.Contains(_preferredLanguage))
                {
                    this.Log(LogLevel.Warning, $"Preferred OCR language '{_preferredLanguage}' is not installed");
                }
                
                // Log all plugin settings for debugging
                var allSettings = settings.GetAllPluginSettings(Id);
                this.Log(LogLevel.Information, $"All OCR plugin settings: {string.Join(", ", allSettings.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            }
            else
            {
                this.Log(LogLevel.Error, "Failed to get application settings");
            }
        }
        
        /// <summary>
        /// Initialize the ONNX OCR processor
        /// </summary>
        private void InitializeOnnxProcessor()
        {
            try
            {
                // Check if ONNX models exist
                string detectionModelPath = Path.Combine(_onnxModelsDirectory, "EasyOCRDetector.onnx");
                string recognitionModelPath = Path.Combine(_onnxModelsDirectory, "EasyOCRRecognizer.onnx");
                
                if (!File.Exists(detectionModelPath))
                {
                    this.Log(LogLevel.Error, $"ONNX detection model not found at: {detectionModelPath}");
                    return;
                }
                
                if (!File.Exists(recognitionModelPath))
                {
                    this.Log(LogLevel.Error, $"ONNX recognition model not found at: {recognitionModelPath}");
                    return;
                }
                
                this.Log(LogLevel.Information, "Initializing ONNX OCR processor...");
                
                // Create and initialize the ONNX processor
                _onnxProcessor = new OnnxOcrProcessor(detectionModelPath, recognitionModelPath, this);
                bool success = _onnxProcessor.Initialize();
                
                if (success)
                {
                    this.Log(LogLevel.Information, "ONNX OCR processor initialized successfully");
                }
                else
                {
                    this.Log(LogLevel.Error, "Failed to initialize ONNX OCR processor");
                    _onnxProcessor = null;
                }
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error initializing ONNX OCR processor: {ex.Message}");
                _onnxProcessor = null;
            }
        }
        
        /// <summary>
        /// Implements the OnnxOcrProcessor.ILogger interface
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The log level</param>
        public void Log(string message, ClipboardAI.Plugins.LogLevel level)
        {
            this.Log(level, message);
        }
        
        /// <summary>
        /// Load available OCR language files from the tessdata directory
        /// </summary>
        private void LoadAvailableLanguages()
        {
            _installedLanguages.Clear();
            
            if (Directory.Exists(_tessdataDirectory))
            {
                foreach (string file in Directory.GetFiles(_tessdataDirectory, "*.traineddata"))
                {
                    string language = Path.GetFileNameWithoutExtension(file);
                    
                    // Check file size to ensure it's not corrupted
                    var fileInfo = new FileInfo(file);
                    this.Log(LogLevel.Information, $"Language file {language}: {fileInfo.Length} bytes");
                    
                    if (fileInfo.Length < 1000) // Suspiciously small file
                    {
                        this.Log(LogLevel.Warning, $"Language file {language} appears to be too small ({fileInfo.Length} bytes), it may be corrupted");
                        
                        // Try to find a valid copy of this language file elsewhere
                        bool repaired = TryRepairLanguageFile(language, file);
                        if (!repaired)
                        {
                            this.Log(LogLevel.Warning, $"Could not repair corrupted language file: {language}");
                            continue; // Skip this language
                        }
                    }
                    
                    // Add the language to the list of installed languages
                    _installedLanguages.Add(language);
                }
            }
            
            if (_installedLanguages.Count == 0)
            {
                this.Log(LogLevel.Warning, $"No OCR language files found in tessdata directory: {_tessdataDirectory}");
                this.Log(LogLevel.Warning, "Please download language files from https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata");
                this.Log(LogLevel.Warning, $"and place them in the {_tessdataDirectory} directory");
                
                // Add a fallback language to prevent crashes
                _installedLanguages.Add("eng");
                
                // Try to create the directory and download the language file
                try
                {
                    if (!Directory.Exists(_tessdataDirectory))
                    {
                        Directory.CreateDirectory(_tessdataDirectory);
                    }
                    
                    // Check if we can find language files elsewhere in the project
                    string projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
                    if (projectRoot != null)
                    {
                        var languageFiles = Directory.GetFiles(projectRoot, "*.traineddata", SearchOption.AllDirectories);
                        if (languageFiles.Length > 0)
                        {
                            foreach (var file in languageFiles)
                            {
                                string destFile = Path.Combine(_tessdataDirectory, Path.GetFileName(file));
                                File.Copy(file, destFile, true);
                                this.Log(LogLevel.Information, $"Copied language file from {file} to {destFile}");
                            }
                            
                            // Reload the languages after copying
                            _installedLanguages.Clear();
                            foreach (string file in Directory.GetFiles(_tessdataDirectory, "*.traineddata"))
                            {
                                string language = Path.GetFileNameWithoutExtension(file);
                                
                                // Verify file size again
                                var fileInfo = new FileInfo(file);
                                if (fileInfo.Length >= 1000)
                                {
                                    _installedLanguages.Add(language);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Log(LogLevel.Error, $"Error creating tessdata directory or copying language files: {ex.Message}");
                }
            }
            else
            {
                this.Log(LogLevel.Information, $"Found {_installedLanguages.Count} OCR language files: {string.Join(", ", _installedLanguages)}");
            }
        }
        
        private bool TryRepairLanguageFile(string language, string corruptedFilePath)
        {
            try
            {
                this.Log(LogLevel.Information, $"Attempting to repair corrupted language file: {language}");
                
                // Search for valid copies of this language file in other tessdata directories
                List<string> potentialSources = new List<string>();
                
                // 1. Check in the base tessdata directory (not architecture-specific)
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                if (Directory.Exists(baseDir))
                {
                    string baseFile = Path.Combine(baseDir, $"{language}.traineddata");
                    if (File.Exists(baseFile))
                    {
                        var fileInfo = new FileInfo(baseFile);
                        if (fileInfo.Length >= 1000)
                        {
                            potentialSources.Add(baseFile);
                        }
                    }
                }
                
                // 2. Check in the plugin directory
                string pluginDir = Path.GetDirectoryName(GetType().Assembly.Location);
                if (pluginDir != null)
                {
                    string pluginFile = Path.Combine(pluginDir, "tessdata", $"{language}.traineddata");
                    if (File.Exists(pluginFile))
                    {
                        var fileInfo = new FileInfo(pluginFile);
                        if (fileInfo.Length >= 1000)
                        {
                            potentialSources.Add(pluginFile);
                        }
                    }
                }
                
                // 3. Search the entire project directory
                string projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
                if (projectRoot != null)
                {
                    var languageFiles = Directory.GetFiles(projectRoot, $"{language}.traineddata", SearchOption.AllDirectories);
                    foreach (var file in languageFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length >= 1000)
                        {
                            potentialSources.Add(file);
                        }
                    }
                }
                
                // If we found any valid copies, use the first one
                if (potentialSources.Count > 0)
                {
                    string sourceFile = potentialSources[0];
                    this.Log(LogLevel.Information, $"Found valid copy of {language}.traineddata at {sourceFile}");
                    
                    // Copy the valid file over the corrupted one
                    File.Copy(sourceFile, corruptedFilePath, true);
                    
                    // Verify the copy was successful
                    var newFileInfo = new FileInfo(corruptedFilePath);
                    if (newFileInfo.Length >= 1000)
                    {
                        this.Log(LogLevel.Information, $"Successfully repaired {language}.traineddata");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error attempting to repair language file {language}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get a Tesseract engine for the specified language
        /// </summary>
        private TesseractEngine GetTesseractEngine(string language)
        {
            string cacheKey = language.ToLowerInvariant();
            
            if (_engines.TryGetValue(cacheKey, out var engine))
            {
                return engine;
            }
            
            try
            {
                // Determine the architecture
                string architecture = Environment.Is64BitProcess ? "x64" : "x86";
                
                // Get the plugin directory
                string pluginDir = Path.GetDirectoryName(GetType().Assembly.Location);
                if (pluginDir == null)
                {
                    throw new InvalidOperationException("Could not determine plugin directory");
                }
                
                // Set the architecture-specific directory for Tesseract DLLs
                string archDir = Path.Combine(pluginDir, "..", architecture);
                
                // If the architecture directory exists, set the environment path to include it
                if (Directory.Exists(archDir))
                {
                    // Set the PATH environment variable to include the architecture-specific directory
                    string path = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!path.Contains(archDir))
                    {
                        Environment.SetEnvironmentVariable("PATH", archDir + Path.PathSeparator + path);
                    }
                    
                    this.Log(LogLevel.Information, $"Added {architecture} directory to PATH: {archDir}");
                }
                
                // Create a new Tesseract engine with the specified language
                engine = new TesseractEngine(_tessdataDirectory, language, EngineMode.Default);
                _engines[cacheKey] = engine;
                
                return engine;
            }
            catch (Exception ex)
            {
                this.Log(LogLevel.Error, $"Error creating Tesseract engine for language {language}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Creates a UI control containing the plugin's settings
        /// </summary>
        /// <returns>A WPF control containing the plugin's settings UI</returns>
        public override System.Windows.FrameworkElement CreateSettingsControl()
        {
            // Get the base settings panel with the enabled checkbox
            var panel = (System.Windows.Controls.StackPanel)base.CreateSettingsControl();
            
            // Add OCR engine selection
            var engineLabel = new System.Windows.Controls.TextBlock
            {
                Text = "OCR Engine:",
                Margin = new System.Windows.Thickness(0, 10, 0, 5)
            };
            panel.Children.Add(engineLabel);
            
            var engineComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 200,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Tag = "EngineComboBox"
            };
            
            // Add engine options - ONNX Vision option removed until appropriate models are found
            engineComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Tesseract (Default)", Tag = "tesseract" });
            // engineComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "ONNX Vision", Tag = "onnx" });
            
            // Set the selected engine
            var settings = GetAppSettings();
            string preferredEngine = settings?.GetPluginSetting<string>(Id, "PreferredEngine", "tesseract") ?? "tesseract";
            
            foreach (System.Windows.Controls.ComboBoxItem item in engineComboBox.Items)
            {
                if (item.Tag.ToString() == preferredEngine)
                {
                    engineComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // If no engine is selected, default to Tesseract
            if (engineComboBox.SelectedItem == null)
            {
                engineComboBox.SelectedIndex = 0;
            }
            
            panel.Children.Add(engineComboBox);
            
            // Add OCR language selection
            var languageLabel = new System.Windows.Controls.TextBlock
            {
                Text = "OCR Language:",
                Margin = new System.Windows.Thickness(0, 10, 0, 5)
            };
            panel.Children.Add(languageLabel);
            
            var languageComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 200,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Tag = "LanguageComboBox"
            };
            
            // Add language options
            languageComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "English (en)", Tag = "eng" });
            languageComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Arabic (ar)", Tag = "ara" });
            languageComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "French (fr)", Tag = "fra" });
            languageComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "German (de)", Tag = "deu" });
            languageComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Spanish (es)", Tag = "spa" });
            
            // Set the selected language
            string preferredLanguage = settings?.GetPluginSetting<string>(Id, "PreferredLanguage", "eng") ?? "eng";
            
            // Log the language being loaded for debugging
            Console.WriteLine($"Loading OCR language preference from settings: {preferredLanguage}");
            
            // Log all available language options
            Console.WriteLine("Available language options:");
            foreach (System.Windows.Controls.ComboBoxItem item in languageComboBox.Items)
            {
                Console.WriteLine($"  - {item.Content} (Tag: {item.Tag})");
            }
            
            // Try to find the matching language item
            System.Windows.Controls.ComboBoxItem selectedItem = null;
            foreach (System.Windows.Controls.ComboBoxItem item in languageComboBox.Items)
            {
                if (item.Tag.ToString() == preferredLanguage)
                {
                    selectedItem = item;
                    Console.WriteLine($"Found matching language item: {item.Content} (Tag: {item.Tag})");
                    break;
                }
            }
            
            // Set the selected item
            if (selectedItem != null)
            {
                languageComboBox.SelectedItem = selectedItem;
                Console.WriteLine($"Selected language item: {selectedItem.Content} (Tag: {selectedItem.Tag})");
            }
            else
            {
                // If no language is selected, default to English
                languageComboBox.SelectedIndex = 0;
                Console.WriteLine($"No matching language found for '{preferredLanguage}', defaulting to first item: {((System.Windows.Controls.ComboBoxItem)languageComboBox.Items[0]).Content}");
            }
            
            panel.Children.Add(languageComboBox);
            
            return panel;
        }
        
        /// <summary>
        /// Saves the plugin settings from the provided control
        /// </summary>
        /// <param name="control">The control containing the settings</param>
        /// <returns>True if the settings were saved successfully, false otherwise</returns>
        public override bool SaveSettings(System.Windows.FrameworkElement control)
        {
            try
            {
                Console.WriteLine($"OCR Plugin: SaveSettings called");
                
                if (control is not System.Windows.Controls.StackPanel panel)
                {
                    Console.WriteLine($"OCR Plugin: SaveSettings - control is not a StackPanel");
                    return false;
                }
                
                // Get the app settings
                var settings = GetAppSettings();
                if (settings == null)
                {
                    Console.WriteLine($"OCR Plugin: SaveSettings - settings is null");
                    return false;
                }
                
                // First, handle the enabled state from the checkbox
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.CheckBox checkBox && 
                        checkBox.Tag?.ToString() == "EnabledCheckBox")
                    {
                        bool isEnabled = checkBox.IsChecked ?? false;
                        Console.WriteLine($"OCR Plugin: SaveSettings - setting enabled state to {isEnabled}");
                        settings.SetPluginEnabled(Id, isEnabled);
                        break;
                    }
                }
                
                // Find the language combobox
                System.Windows.Controls.ComboBox languageComboBox = null;
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.ComboBox comboBox && 
                        comboBox.Tag?.ToString() == "LanguageComboBox")
                    {
                        languageComboBox = comboBox;
                        break;
                    }
                }
                
                if (languageComboBox != null && languageComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedLanguage)
                {
                    string language = selectedLanguage.Tag.ToString();
                    Console.WriteLine($"OCR Plugin: SaveSettings - setting preferred language to {language}");
                    settings.SetPluginSetting(Id, "PreferredLanguage", language);
                    _preferredLanguage = language;
                }
                
                // Find the engine combobox
                System.Windows.Controls.ComboBox engineComboBox = null;
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.ComboBox comboBox && 
                        comboBox.Tag?.ToString() == "EngineComboBox")
                    {
                        engineComboBox = comboBox;
                        break;
                    }
                }
                
                if (engineComboBox != null && engineComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedEngine)
                {
                    string engine = selectedEngine.Tag.ToString();
                    Console.WriteLine($"OCR Plugin: SaveSettings - setting preferred engine to {engine}");
                    settings.SetPluginSetting(Id, "PreferredEngine", engine);
                    _preferredEngine = engine;
                }
                
                // Save the settings
                settings.Save();
                Console.WriteLine($"OCR Plugin: SaveSettings - settings saved successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR Plugin: SaveSettings - error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets whether the feature is enabled
        /// </summary>
        /// <returns>True if the feature is enabled, false otherwise</returns>
        protected override bool GetIsEnabled()
        {
            // By default, check if the plugin is enabled in app settings
            var settings = GetAppSettings();
            if (settings != null)
            {
                bool isEnabled = settings.IsPluginEnabled(Id);
                Console.WriteLine($"OCR Plugin: GetIsEnabled() called, returning {isEnabled} (Id: {Id})");
                return isEnabled;
            }
            
            Console.WriteLine("OCR Plugin: GetIsEnabled() called, but settings is null, returning false");
            return false;
        }
        
        /// <summary>
        /// Shutdown the plugin
        /// </summary>
        public override void Shutdown()
        {
            // Dispose all Tesseract engines
            foreach (var engine in _engines.Values)
            {
                engine.Dispose();
            }
            
            _engines.Clear();
            
            base.Shutdown();
        }
    }
    
    /// <summary>
    /// Helper class to convert BitmapSource to Pix format for Tesseract
    /// </summary>
    internal static class PixConverter
    {
        /// <summary>
        /// Convert a BitmapSource to a Pix object for Tesseract processing
        /// </summary>
        public static Pix ToPix(BitmapSource source)
        {
            try
            {
                // Ensure the source is in a format Tesseract can handle
                BitmapSource processableSource = source;
                
                // If the source format is not compatible, convert it to a compatible format
                if (source.Format != System.Windows.Media.PixelFormats.Bgr24 && 
                    source.Format != System.Windows.Media.PixelFormats.Bgra32 &&
                    source.Format != System.Windows.Media.PixelFormats.Rgb24)
                {
                    processableSource = new System.Windows.Media.Imaging.FormatConvertedBitmap(
                        source, System.Windows.Media.PixelFormats.Bgr24, null, 0);
                }
                
                // Create a temporary file to save the image
                string tempFile = Path.Combine(Path.GetTempPath(), $"ocr_temp_{Guid.NewGuid()}.tiff");
                
                // Use TIFF format for better compatibility with Tesseract
                using (var fileStream = new FileStream(tempFile, FileMode.Create))
                {
                    var encoder = new System.Windows.Media.Imaging.TiffBitmapEncoder();
                    encoder.Compression = System.Windows.Media.Imaging.TiffCompressOption.None;
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(processableSource));
                    encoder.Save(fileStream);
                }
                
                // Load the Pix from the file
                var pix = Pix.LoadFromFile(tempFile);
                
                // Clean up the temporary file
                try { File.Delete(tempFile); } catch { /* Ignore cleanup errors */ }
                
                return pix;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting image: {ex.Message}");
                return null;
            }
        }
    }
}
