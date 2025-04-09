using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClipboardAI.Common;
using ClipboardAI.Plugins;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ClipboardAI.Plugin.KeywordExtraction
{
    /// <summary>
    /// Plugin that provides keyword extraction functionality using ONNX models
    /// </summary>
    public class KeywordExtractionPlugin : FeaturePluginBase
    {
        private int _maxKeywords = 5;
        private bool _isInitialized = false;
        private bool _isModelInitialized = false;
        private readonly Progress<int> _progressReporter;
        private readonly Dictionary<string, float> _wordImportance = new Dictionary<string, float>();
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "KeywordExtraction";
        
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        public override string FeatureId => "KeywordExtraction";
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "Keyword Extraction";
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        public override string FeatureName => "Keyword Extraction";
        
        /// <summary>
        /// Gets the menu option for this plugin to be displayed in the UI
        /// </summary>
        /// <returns>A MenuOption object containing the display information for this plugin</returns>
        public override MenuOption GetMenuOption()
        {
            return new MenuOption("🔍", "Extract Keywords", FeatureType);
        }
        
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
        public override string Description => "Extract important keywords from text using AI models";
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public override AIFeatureType FeatureType => AIFeatureType.KeywordExtraction;

        /// <summary>
        /// Constructor
        /// </summary>
        public KeywordExtractionPlugin()
        {
            _progressReporter = new Progress<int>(progress => {
                Console.WriteLine($"Keyword Extraction model initialization: {progress}%");
            });
        }
        
        /// <summary>
        /// Initialize the plugin
        /// </summary>
        /// <param name="host">The plugin host interface</param>
        /// <returns>True if initialization was successful</returns>
        public override bool Initialize(IPluginHost host)
        {
            if (_isInitialized)
                return true;
                
            if (!base.Initialize(host))
                return false;
                
            try
            {
                // Load settings
                var settings = Host.GetPluginSettings(Id);
                if (settings != null && settings.ContainsKey("MaxKeywords") && int.TryParse(settings["MaxKeywords"].ToString(), out int maxKeywords))
                {
                    _maxKeywords = maxKeywords;
                }
                
                // Initialize model in background
                Task.Run(async () => {
                    await InitializeModelAsync();
                });
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing KeywordExtractionPlugin: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Initialize the ONNX model
        /// </summary>
        private async Task<bool> InitializeModelAsync()
        {
            try
            {
                // Initialize the model service
                bool success = await MultilingualModelService.Instance.InitializeAsync("multilingual-e5-small", _progressReporter);
                
                if (success)
                {
                    Console.WriteLine("Keyword extraction model initialized successfully");
                    _isModelInitialized = true;
                }
                else
                {
                    Console.WriteLine("Failed to initialize keyword extraction model");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing keyword extraction model: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Process text using the plugin
        /// </summary>
        /// <param name="text">Text to process</param>
        /// <param name="options">Optional processing options</param>
        /// <returns>Processed text</returns>
        public override async Task<string> ProcessTextAsync(string text, object options = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
                
            try
            {
                // Wait for model initialization if needed
                if (!_isModelInitialized)
                {
                    bool success = await InitializeModelAsync();
                    if (!success)
                    {
                        // Fall back to rule-based approach if model initialization fails
                        return ExtractKeywordsRuleBased(text);
                    }
                }
                
                // Use the model for keyword extraction
                return await ExtractKeywordsWithModelAsync(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in KeywordExtractionPlugin.ProcessTextAsync: {ex.Message}");
                // Fall back to rule-based approach if model fails
                return ExtractKeywordsRuleBased(text);
            }
        }

        /// <summary>
        /// Extract keywords using the ONNX model
        /// </summary>
        private async Task<string> ExtractKeywordsWithModelAsync(string text)
        {
            try
            {
                // Normalize and tokenize text
                var normalizedText = text.ToLowerInvariant();
                
                // Split into sentences (simplified approach)
                var sentences = normalizedText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Split into words
                var allWords = normalizedText.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '\'', '\"', '-', '_', '+', '=', '*', '&', '^', '%', '$', '#', '@', '~', '`' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Filter out stop words and short words
                var filteredWords = allWords
                    .Where(w => w.Length >= 3 && !IsStopWord(w))
                    .Distinct()
                    .ToList();
                
                // If we have too few words, use rule-based approach
                if (filteredWords.Count < 5)
                {
                    return ExtractKeywordsRuleBased(text);
                }
                
                // Create embedding for the full text
                var textEmbedding = await CreateEmbeddingAsync(normalizedText);
                
                // Calculate importance scores for each word
                _wordImportance.Clear();
                foreach (var word in filteredWords)
                {
                    // For each word, calculate its importance by comparing embeddings
                    // of sentences with and without the word
                    float importanceScore = 0;
                    
                    foreach (var sentence in sentences)
                    {
                        if (sentence.Contains(word))
                        {
                            // Create a version of the sentence without this word
                            var sentenceWithoutWord = sentence.Replace(word, "");
                            
                            // Skip if sentence becomes too short
                            if (sentenceWithoutWord.Length < 10)
                                continue;
                                
                            // Get embeddings
                            var originalEmbedding = await CreateEmbeddingAsync(sentence);
                            var modifiedEmbedding = await CreateEmbeddingAsync(sentenceWithoutWord);
                            
                            // Calculate cosine distance between embeddings
                            float distance = CalculateCosineSimilarity(originalEmbedding, modifiedEmbedding);
                            
                            // Higher distance means the word is more important
                            importanceScore += (1 - distance);
                        }
                    }
                    
                    // Store importance score
                    _wordImportance[word] = importanceScore;
                }
                
                // Sort by importance score
                var keywords = _wordImportance
                    .OrderByDescending(kv => kv.Value)
                    .Take(_maxKeywords)
                    .Select(kv => kv.Key)
                    .ToList();
                
                // If we couldn't extract keywords with the model, fall back to rule-based
                if (keywords.Count == 0)
                {
                    return ExtractKeywordsRuleBased(text);
                }
                
                // Format the result
                return string.Join(", ", keywords);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting keywords with model: {ex.Message}");
                return ExtractKeywordsRuleBased(text);
            }
        }

        /// <summary>
        /// Create an embedding for the given text using the ONNX model
        /// </summary>
        private async Task<float[]> CreateEmbeddingAsync(string text)
        {
            try
            {
                // Prepare input for the model
                var inputTensor = new DenseTensor<float>(new[] { 1, text.Length });
                
                // Convert text to tensor (simplified tokenization)
                for (int i = 0; i < text.Length; i++)
                {
                    inputTensor[0, i] = text[i];
                }
                
                // Create input dictionary
                var inputs = new Dictionary<string, Tensor<float>>
                {
                    { "input_ids", inputTensor }
                };
                
                // Run inference
                var outputs = MultilingualModelService.Instance.RunInference(inputs);
                
                // Extract embedding from output
                if (outputs.TryGetValue("last_hidden_state", out var embeddingTensor))
                {
                    // Average pooling to get a single vector
                    var embedding = new float[embeddingTensor.Dimensions[2]];
                    for (int i = 0; i < embeddingTensor.Dimensions[1]; i++)
                    {
                        for (int j = 0; j < embeddingTensor.Dimensions[2]; j++)
                        {
                            embedding[j] += embeddingTensor[0, i, j];
                        }
                    }
                    
                    // Normalize
                    for (int i = 0; i < embedding.Length; i++)
                    {
                        embedding[i] /= embeddingTensor.Dimensions[1];
                    }
                    
                    return embedding;
                }
                
                throw new Exception("Failed to get embedding from model output");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating embedding: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Calculate cosine similarity between two vectors
        /// </summary>
        private float CalculateCosineSimilarity(float[] a, float[] b)
        {
            float dotProduct = 0;
            float normA = 0;
            float normB = 0;
            
            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            
            return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }
        
        /// <summary>
        /// Fall back to rule-based keyword extraction
        /// </summary>
        private string ExtractKeywordsRuleBased(string text)
        {
            // Normalize text
            var normalizedText = text.ToLowerInvariant();
            
            // Split into words
            var words = normalizedText.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '\'', '\"', '-', '_', '+', '=', '*', '&', '^', '%', '$', '#', '@', '~', '`' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Count word frequency
            var wordFrequency = new Dictionary<string, int>();
            foreach (var word in words)
            {
                if (word.Length < 3) // Skip short words
                    continue;
                    
                if (IsStopWord(word)) // Skip stop words
                    continue;
                    
                if (wordFrequency.ContainsKey(word))
                    wordFrequency[word]++;
                else
                    wordFrequency[word] = 1;
            }
            
            // Sort by frequency
            var sortedWords = wordFrequency.OrderByDescending(kv => kv.Value).Take(_maxKeywords).Select(kv => kv.Key).ToList();
            
            // Format the result
            return string.Join(", ", sortedWords);
        }
        
        /// <summary>
        /// Check if a word is a common stop word
        /// </summary>
        private bool IsStopWord(string word)
        {
            // Common English stop words
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "a", "an", "the", "and", "or", "but", "if", "then", "else", "when",
                "at", "from", "by", "on", "off", "for", "in", "out", "over", "under",
                "again", "further", "then", "once", "here", "there", "when", "where", "why",
                "how", "all", "any", "both", "each", "few", "more", "most", "other", "some",
                "such", "no", "nor", "not", "only", "own", "same", "so", "than", "too",
                "very", "can", "will", "just", "should", "now"
            };
            
            return stopWords.Contains(word);
        }
        
        /// <summary>
        /// Creates a UI control containing the plugin's settings
        /// </summary>
        /// <returns>A WPF control containing the plugin's settings UI</returns>
        public override FrameworkElement CreateSettingsControl()
        {
            // Get the base settings panel with the enabled checkbox
            var panel = (StackPanel)base.CreateSettingsControl();
            
            // Add title
            var titleTextBlock = new TextBlock();
            titleTextBlock.Text = "Keyword Extraction Settings";
            titleTextBlock.FontWeight = System.Windows.FontWeights.Bold;
            titleTextBlock.Margin = new System.Windows.Thickness(0, 0, 0, 10);
            panel.Children.Insert(0, titleTextBlock); // Insert at the beginning
            
            // Add model status
            var modelStatusLabel = new TextBlock();
            modelStatusLabel.Text = $"AI Model: {(_isModelInitialized ? "Loaded" : "Loading...")}";
            modelStatusLabel.Margin = new System.Windows.Thickness(0, 0, 0, 10);
            panel.Children.Add(modelStatusLabel);
            
            // Add max keywords slider
            var maxKeywordsLabel = new TextBlock();
            maxKeywordsLabel.Text = $"Maximum Keywords: {_maxKeywords}";
            maxKeywordsLabel.Margin = new System.Windows.Thickness(0, 0, 0, 5);
            panel.Children.Add(maxKeywordsLabel);
            
            var maxKeywordsSlider = new Slider();
            maxKeywordsSlider.Minimum = 1;
            maxKeywordsSlider.Maximum = 20;
            maxKeywordsSlider.Value = _maxKeywords;
            maxKeywordsSlider.IsSnapToTickEnabled = true;
            maxKeywordsSlider.TickFrequency = 1;
            maxKeywordsSlider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
            maxKeywordsSlider.Tag = "MaxKeywordsSlider";
            maxKeywordsSlider.ValueChanged += (sender, e) => {
                _maxKeywords = (int)e.NewValue;
                maxKeywordsLabel.Text = $"Maximum Keywords: {_maxKeywords}";
            };
            panel.Children.Add(maxKeywordsSlider);
            
            // Add description
            var descriptionTextBlock = new TextBlock();
            descriptionTextBlock.Text = "This plugin uses an AI model to extract the most important keywords from text. If the model is not available, it will fall back to a rule-based approach.";
            descriptionTextBlock.TextWrapping = System.Windows.TextWrapping.Wrap;
            descriptionTextBlock.Margin = new System.Windows.Thickness(0, 10, 0, 0);
            panel.Children.Add(descriptionTextBlock);
            
            return panel;
        }
        
        /// <summary>
        /// Saves the settings from the provided control
        /// </summary>
        /// <param name="control">The settings control previously created by CreateSettingsControl</param>
        /// <returns>True if settings were saved successfully</returns>
        public override bool SaveSettings(FrameworkElement control)
        {
            // First save the base settings (including the enabled state)
            if (!base.SaveSettings(control))
                return false;
                
            try
            {
                if (control is StackPanel panel)
                {
                    // Process plugin-specific controls
                    foreach (var child in panel.Children)
                    {
                        if (child is Slider slider && slider.Tag?.ToString() == "MaxKeywordsSlider")
                        {
                            _maxKeywords = (int)slider.Value;
                        }
                    }
                    
                    // Save plugin-specific settings
                    SetSetting("MaxKeywords", _maxKeywords);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings for plugin {Name}: {ex.Message}");
                return false;
            }
        }
    }
}
