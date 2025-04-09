using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using Newtonsoft.Json;

namespace ClipboardAI.Common
{
    /// <summary>
    /// Handles downloading and managing ML models
    /// </summary>
    public class ModelDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private string _modelDirectory = "Models";

        /// <summary>
        /// Constructor that allows specifying a custom model directory
        /// </summary>
        /// <param name="modelDirectory">Directory to store models</param>
        public ModelDownloader(string modelDirectory = null)
        {
            if (!string.IsNullOrEmpty(modelDirectory))
            {
                _modelDirectory = modelDirectory;
            }
        }

        /// <summary>
        /// Download an OCR model for a specific language
        /// </summary>
        /// <param name="language">Language code</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>True if download was successful</returns>
        public async Task<bool> DownloadOcrModelAsync(string language, IProgress<int> progress = null)
        {
            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(_modelDirectory))
                {
                    Directory.CreateDirectory(_modelDirectory);
                    Console.WriteLine($"Created model directory: {_modelDirectory}");
                }
                
                // Create OCR model subdirectory
                string ocrDirectory = Path.Combine(_modelDirectory, "OCR");
                if (!Directory.Exists(ocrDirectory))
                {
                    Directory.CreateDirectory(ocrDirectory);
                    Console.WriteLine($"Created OCR model directory: {ocrDirectory}");
                }
                
                // Get file paths
                string modelFilePath = Path.Combine(ocrDirectory, $"{language}.traineddata");
                
                // Check if file already exists
                if (File.Exists(modelFilePath))
                {
                    Console.WriteLine($"OCR model for {language} already exists");
                    progress?.Report(100);
                    return true;
                }
                
                // Download OCR model from GitHub
                string modelUrl = $"https://github.com/tesseract-ocr/tessdata/raw/main/{language}.traineddata";
                Console.WriteLine($"Downloading OCR model for {language} from {modelUrl}...");
                
                try
                {
                    using (var response = await _httpClient.GetAsync(modelUrl))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fileStream = new FileStream(modelFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }
                    Console.WriteLine($"Downloaded OCR model to: {modelFilePath}");
                    progress?.Report(100);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading OCR model: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading OCR model: {ex.Message}");
                return false;
            }
        }
        
        // Summarization plugin has been removed
        
        /// <summary>
        /// Check if the paraphrasing model for the specified language is installed
        /// </summary>
        /// <param name="language">Language to check</param>
        /// <returns>True if model is installed, false otherwise</returns>
        public bool IsParaphrasingModelInstalled(string language)
        {
            try
            {
                // Get model directory
                string modelDirectory = GetParaphrasingModelDirectory(language);
                
                // Define model file paths based on actual filenames
                string encoderPath = Path.Combine(modelDirectory, "encoder_model.onnx");
                string decoderPath = Path.Combine(modelDirectory, "decoder_model.onnx");
                
                // The vocab file name depends on the language
                string vocabPath;
                if (language.Equals("English", StringComparison.OrdinalIgnoreCase))
                {
                    vocabPath = Path.Combine(modelDirectory, "spiece.model");
                }
                else
                {
                    vocabPath = Path.Combine(modelDirectory, "sentencepiece.model");
                }
                
                string configPath = Path.Combine(modelDirectory, "config.json");
                
                // Debug logging - only log at Debug level to reduce noise
                Console.WriteLine($"Checking paraphrasing model for language: {language}");
                Console.WriteLine($"Model directory: {modelDirectory}");
                Console.WriteLine($"Encoder model file exists: {File.Exists(encoderPath)}");
                Console.WriteLine($"Decoder model file exists: {File.Exists(decoderPath)}");
                Console.WriteLine($"Vocab file exists: {File.Exists(vocabPath)}");
                Console.WriteLine($"Config file exists: {File.Exists(configPath)}");
                
                // Check if all required files exist
                bool result = File.Exists(encoderPath) && File.Exists(decoderPath) && File.Exists(vocabPath);
                Console.WriteLine($"IsParaphrasingModelInstalled result: {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if paraphrasing model is installed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get the directory for paraphrasing models for the specified language
        /// </summary>
        /// <param name="language">Language to get directory for</param>
        /// <returns>Path to the model directory</returns>
        public string GetParaphrasingModelDirectory(string language)
        {
            try
            {
                // Get the base directory of the application
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Application base directory: {appDir}");
                
                // Check for models in the Plugins/Paraphrasing structure (primary location)
                string pluginsPath = Path.Combine(appDir, "Plugins", "Paraphrasing", "Models", language);
                
                // Only log at Debug level
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine($"Plugins directory path: {pluginsPath}");
                    Console.WriteLine($"Plugins directory exists: {Directory.Exists(pluginsPath)}");
                }
                
                if (Directory.Exists(pluginsPath))
                {
                    return pluginsPath;
                }
                
                // Check for models in the standard model directory (fallback)
                string modelDirectory = Path.Combine(_modelDirectory, "Paraphrasing", language);
                
                // Only log at Debug level
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine($"Standard model directory path: {modelDirectory}");
                    Console.WriteLine($"Standard directory exists: {Directory.Exists(modelDirectory)}");
                }
                
                if (Directory.Exists(modelDirectory))
                {
                    return modelDirectory;
                }
                
                // Check for models in Program Files (x86) location (installer location)
                string programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string programFilesPath = Path.Combine(programFilesDir, "ClipboardAI", "Plugins", "paraphrasing", "Models", language);
                
                // Only log at Debug level
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine($"Program Files directory path: {programFilesPath}");
                    Console.WriteLine($"Program Files directory exists: {Directory.Exists(programFilesPath)}");
                }
                
                if (Directory.Exists(programFilesPath))
                {
                    return programFilesPath;
                }
                
                // If no directory exists, return the plugins path as the preferred location
                return pluginsPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting paraphrasing model directory: {ex.Message}");
                
                // In case of error, return a default path
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(appDir, "Plugins", "Paraphrasing", "Models", language);
            }
        }
        
        private async Task<bool> DownloadFileAsync(string url, string filePath, IProgress<int> progress = null)
        {
            try
            {
                using (var response = await _httpClient.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sets the directory where models will be downloaded
        /// </summary>
        /// <param name="modelDirectory">The directory path</param>
        public void SetModelDirectory(string modelDirectory)
        {
            if (!string.IsNullOrEmpty(modelDirectory))
            {
                _modelDirectory = modelDirectory;
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(_modelDirectory))
                {
                    Directory.CreateDirectory(_modelDirectory);
                }
            }
        }
    }
}
