using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ClipboardAI.Common
{
    /// <summary>
    /// Service for managing and using ONNX models for multilingual AI tasks
    /// </summary>
    public class MultilingualModelService
    {
        private static readonly Lazy<MultilingualModelService> _instance = new Lazy<MultilingualModelService>(() => new MultilingualModelService());
        
        public static MultilingualModelService Instance => _instance.Value;
        
        private InferenceSession _session;
        private bool _isInitialized;
        private bool _isInitializing;
        private string _modelPath;
        private readonly object _lockObject = new object();
        
        // Model information
        public static readonly Dictionary<string, ModelInfo> Models = new Dictionary<string, ModelInfo>
        {
            { "multilingual-e5-small", new ModelInfo 
                { 
                    Name = "Multilingual E5 Small", 
                    Size = 118, // Size in MB
                    Description = "Lightweight multilingual embedding model for text analysis",
                    Url = "https://huggingface.co/Xenova/multilingual-e5-small/resolve/main/onnx/model_quantized.onnx?download=true"
                }
            }
        };
        
        private MultilingualModelService()
        {
            // Private constructor for singleton
        }
        
        /// <summary>
        /// Initialize the model service
        /// </summary>
        /// <param name="modelName">Name of the model to initialize</param>
        /// <param name="progressCallback">Optional callback to report initialization progress</param>
        /// <returns>True if initialization was successful</returns>
        public async Task<bool> InitializeAsync(string modelName = "multilingual-e5-small", IProgress<int> progressCallback = null)
        {
            if (_isInitialized)
                return true;
                
            if (_isInitializing)
                return false;
                
            _isInitializing = true;
            
            try
            {
                // Report initial progress
                progressCallback?.Report(0);
                
                // Determine model path using plugin-specific directories
                string pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                
                // Try to find the appropriate plugin directory based on the calling assembly
                string callingAssembly = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
                string pluginDir;
                
                if (callingAssembly.Contains("GrammarChecker"))
                {
                    pluginDir = Path.Combine(pluginsDir, "GrammarChecker");
                    Console.WriteLine("Loading model for Grammar Checker plugin");
                }
                else if (callingAssembly.Contains("LanguageDetection"))
                {
                    pluginDir = Path.Combine(pluginsDir, "LanguageDetection");
                    Console.WriteLine("Loading model for Language Detection plugin");
                }
                else
                {
                    // Fallback to common directory if the calling assembly is not recognized
                    pluginDir = Path.Combine(pluginsDir, "Common");
                    Console.WriteLine("Loading model for unknown plugin, using Common directory");
                }
                
                // Set model path in the plugin-specific Models directory
                string modelsDir = Path.Combine(pluginDir, "Models");
                _modelPath = Path.Combine(modelsDir, $"{modelName}.onnx");
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(modelsDir))
                {
                    Directory.CreateDirectory(modelsDir);
                }
                
                Console.WriteLine($"Looking for model at: {_modelPath}");
                
                // Check if model exists
                if (!File.Exists(_modelPath))
                {
                    // Model doesn't exist and should have been downloaded by the installer
                    Console.WriteLine($"Model {modelName} not found at {_modelPath}");
                    Console.WriteLine("Models should be downloaded during installation. Please reinstall the application.");
                    _isInitializing = false;
                    return false;
                }
                
                // Report progress
                progressCallback?.Report(50);
                
                // Load model in a background task
                await Task.Run(() => {
                    try
                    {
                        var sessionOptions = new SessionOptions();
                        sessionOptions.EnableMemoryPattern = true;
                        sessionOptions.EnableCpuMemArena = true;
                        
                        // Create inference session
                        _session = new InferenceSession(_modelPath, sessionOptions);
                        
                        // Report progress
                        progressCallback?.Report(90);
                        
                        _isInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading model: {ex.Message}");
                        _isInitialized = false;
                    }
                });
                
                // Report completion
                progressCallback?.Report(100);
                
                return _isInitialized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing model service: {ex.Message}");
                return false;
            }
            finally
            {
                _isInitializing = false;
            }
        }
        
        // Model downloading has been removed as models are now downloaded by the installer
        
        /// <summary>
        /// Run inference on the model
        /// </summary>
        /// <param name="inputs">Dictionary of input name to tensor</param>
        /// <returns>Dictionary of output name to tensor</returns>
        public Dictionary<string, Tensor<float>> RunInference(Dictionary<string, Tensor<float>> inputs)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Model service is not initialized");
                
            lock (_lockObject)
            {
                try
                {
                    // Convert inputs to OnnxValue
                    var onnxInputs = new List<NamedOnnxValue>();
                    foreach (var input in inputs)
                    {
                        onnxInputs.Add(NamedOnnxValue.CreateFromTensor(input.Key, input.Value));
                    }
                    
                    // Run inference
                    using (var results = _session.Run(onnxInputs))
                    {
                        // Convert outputs to dictionary
                        var outputs = new Dictionary<string, Tensor<float>>();
                        foreach (var result in results)
                        {
                            outputs[result.Name] = result.AsTensor<float>();
                        }
                        
                        return outputs;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running inference: {ex.Message}");
                    throw;
                }
            }
        }
    }
    
    /// <summary>
    /// Information about a model
    /// </summary>
    public class ModelInfo
    {
        /// <summary>
        /// Display name of the model
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Size of the model in MB
        /// </summary>
        public int Size { get; set; }
        
        /// <summary>
        /// Description of the model
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// URL to download the model
        /// </summary>
        public string Url { get; set; }
    }
}
