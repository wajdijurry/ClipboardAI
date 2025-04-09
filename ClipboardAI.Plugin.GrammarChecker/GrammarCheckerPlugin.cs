using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClipboardAI.Common;
using ClipboardAI.Plugins;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ClipboardAI.Plugin.GrammarChecker
{
    /// <summary>
    /// Plugin that provides grammar checking functionality using ONNX models
    /// </summary>
    public class GrammarCheckerPlugin : FeaturePluginBase
    {
        private bool _isInitialized = false;
        private bool _isModelInitialized = false;
        private bool _autoCorrect = true;
        private bool _showSuggestions = true;
        private readonly Progress<int> _progressReporter;
        private readonly Dictionary<string, List<GrammarError>> _commonErrors = new Dictionary<string, List<GrammarError>>();
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "GrammarChecker";
        
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        public override string FeatureId => "GrammarChecker";
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "Grammar Checker";
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        public override string FeatureName => "Grammar Checker";
        
        /// <summary>
        /// Gets the menu option for this plugin to be displayed in the UI
        /// </summary>
        /// <returns>A MenuOption object containing the display information for this plugin</returns>
        public override MenuOption GetMenuOption()
        {
            return new MenuOption("📒", "Check Grammar", FeatureType);
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
        public override string Description => "Check and correct grammar in text using AI models";
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public override AIFeatureType FeatureType => AIFeatureType.GrammarChecker;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public GrammarCheckerPlugin()
        {
            _progressReporter = new Progress<int>(progress => {
                Console.WriteLine($"Grammar Checker model initialization: {progress}%");
            });
            
            // Initialize common grammar errors for fallback
            InitializeCommonErrors();
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
                if (settings != null)
                {
                    if (settings.ContainsKey("AutoCorrect") && bool.TryParse(settings["AutoCorrect"].ToString(), out bool autoCorrect))
                    {
                        _autoCorrect = autoCorrect;
                    }
                    
                    if (settings.ContainsKey("ShowSuggestions") && bool.TryParse(settings["ShowSuggestions"].ToString(), out bool showSuggestions))
                    {
                        _showSuggestions = showSuggestions;
                    }
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
                Console.WriteLine($"Error initializing GrammarCheckerPlugin: {ex.Message}");
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
                    Console.WriteLine("Grammar checker model initialized successfully");
                    _isModelInitialized = true;
                }
                else
                {
                    Console.WriteLine("Failed to initialize grammar checker model");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing grammar checker model: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Initialize common grammar errors for fallback
        /// </summary>
        private void InitializeCommonErrors()
        {
            // English grammar errors
            var englishErrors = new List<GrammarError>
            {
                new GrammarError(" i ", " I ", "Capitalize 'I'"),
                new GrammarError(" i'm ", " I'm ", "Capitalize 'I'"),
                new GrammarError(" dont ", " don't ", "Missing apostrophe"),
                new GrammarError(" cant ", " can't ", "Missing apostrophe"),
                new GrammarError(" wont ", " won't ", "Missing apostrophe"),
                new GrammarError(" didnt ", " didn't ", "Missing apostrophe"),
                new GrammarError(" its a ", " it's a ", "'its' should be 'it's' (it is)"),
                new GrammarError(" thats ", " that's ", "Missing apostrophe"),
                new GrammarError(" theyre ", " they're ", "Missing apostrophe"),
                new GrammarError(" youre ", " you're ", "Missing apostrophe"),
                new GrammarError(" youll ", " you'll ", "Missing apostrophe"),
                new GrammarError(" youd ", " you'd ", "Missing apostrophe"),
                new GrammarError(" couldnt ", " couldn't ", "Missing apostrophe"),
                new GrammarError(" shouldnt ", " shouldn't ", "Missing apostrophe"),
                new GrammarError(" wouldnt ", " wouldn't ", "Missing apostrophe"),
                new GrammarError(" isnt ", " isn't ", "Missing apostrophe"),
                new GrammarError(" arent ", " aren't ", "Missing apostrophe"),
                new GrammarError(" wasnt ", " wasn't ", "Missing apostrophe"),
                new GrammarError(" werent ", " weren't ", "Missing apostrophe"),
                new GrammarError(" alot ", " a lot ", "'alot' should be 'a lot'"),
                new GrammarError(" alright ", " all right ", "'alright' should be 'all right'"),
                new GrammarError(" cant wait ", " can't wait ", "Missing apostrophe"),
                new GrammarError(" could of ", " could have ", "'could of' should be 'could have'"),
                new GrammarError(" should of ", " should have ", "'should of' should be 'should have'"),
                new GrammarError(" would of ", " would have ", "'would of' should be 'would have'"),
                new GrammarError(" your welcome ", " you're welcome ", "'your' should be 'you're'"),
                new GrammarError(" your right ", " you're right ", "'your' should be 'you're'"),
                new GrammarError(" your wrong ", " you're wrong ", "'your' should be 'you're'"),
                new GrammarError(" their going ", " they're going ", "'their' should be 'they're'"),
                new GrammarError(" their coming ", " they're coming ", "'their' should be 'they're'"),
                new GrammarError(" there going ", " they're going ", "'there' should be 'they're'"),
                new GrammarError(" there coming ", " they're coming ", "'there' should be 'they're'"),
                new GrammarError(" whos ", " who's ", "Missing apostrophe"),
                new GrammarError(" hows ", " how's ", "Missing apostrophe"),
                new GrammarError(" whats ", " what's ", "Missing apostrophe"),
                new GrammarError(" wheres ", " where's ", "Missing apostrophe"),
                new GrammarError(" whens ", " when's ", "Missing apostrophe"),
                new GrammarError(" theres ", " there's ", "Missing apostrophe"),
                new GrammarError(" heres ", " here's ", "Missing apostrophe")
            };
            _commonErrors["en"] = englishErrors;
            
            // Spanish grammar errors
            var spanishErrors = new List<GrammarError>
            {
                new GrammarError(" asi ", " así ", "Missing accent"),
                new GrammarError(" tu ", " tú ", "Missing accent when used as 'you'"),
                new GrammarError(" el ", " él ", "Missing accent when used as 'he'"),
                new GrammarError(" mas ", " más ", "Missing accent when used as 'more'"),
                new GrammarError(" que ", " qué ", "Missing accent when used as question word")
            };
            _commonErrors["es"] = spanishErrors;
            
            // French grammar errors
            var frenchErrors = new List<GrammarError>
            {
                new GrammarError(" a ", " à ", "Missing accent when used as 'to'"),
                new GrammarError(" ca ", " ça ", "Missing cedilla"),
                new GrammarError(" ou ", " où ", "Missing accent when used as 'where'")
            };
            _commonErrors["fr"] = frenchErrors;
            
            // German grammar errors
            var germanErrors = new List<GrammarError>
            {
                new GrammarError(" uber ", " über ", "Missing umlaut"),
                new GrammarError(" fur ", " für ", "Missing umlaut"),
                new GrammarError(" konnen ", " können ", "Missing umlaut"),
                new GrammarError(" mochte ", " möchte ", "Missing umlaut")
            };
            _commonErrors["de"] = germanErrors;
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
                        return ProcessTextRuleBased(text);
                    }
                }
                
                // Use the model for grammar checking
                return await ProcessTextWithModelAsync(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GrammarCheckerPlugin.ProcessTextAsync: {ex.Message}");
                // Fall back to rule-based approach if model fails
                return ProcessTextRuleBased(text);
            }
        }
        
        /// <summary>
        /// Process text using the ONNX model
        /// </summary>
        private async Task<string> ProcessTextWithModelAsync(string text)
        {
            try
            {
                // Detect language
                var language = await DetectLanguageWithModelAsync(text);
                
                // Split text into sentences for better processing
                var sentences = SplitIntoSentences(text);
                var correctedSentences = new List<string>();
                var suggestions = new List<GrammarSuggestion>();
                
                // Process each sentence
                foreach (var sentence in sentences)
                {
                    if (string.IsNullOrWhiteSpace(sentence))
                    {
                        correctedSentences.Add(sentence);
                        continue;
                    }
                    
                    // Create embedding for the sentence
                    var sentenceEmbedding = await CreateEmbeddingAsync(sentence);
                    
                    // Check for grammar errors using the model
                    var sentenceSuggestions = await CheckGrammarWithModelAsync(sentence, sentenceEmbedding, language);
                    
                    // Apply corrections
                    var correctedSentence = sentence;
                    foreach (var suggestion in sentenceSuggestions)
                    {
                        // Add to overall suggestions
                        suggestions.Add(suggestion);
                        
                        // Apply correction if auto-correct is enabled
                        if (_autoCorrect)
                        {
                            correctedSentence = correctedSentence.Replace(suggestion.OriginalText, suggestion.SuggestedText);
                        }
                    }
                    
                    correctedSentences.Add(correctedSentence);
                }
                
                // Combine sentences back into text
                var correctedText = string.Join(" ", correctedSentences);
                
                // If auto-correct is enabled, return the corrected text
                if (_autoCorrect)
                {
                    return correctedText;
                }
                
                // If suggestions are enabled, return the original text with suggestions
                if (_showSuggestions && suggestions.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(text);
                    sb.AppendLine();
                    sb.AppendLine("--- Grammar Suggestions ---");
                    
                    foreach (var suggestion in suggestions)
                    {
                        sb.AppendLine($"- {suggestion.Explanation}: '{suggestion.OriginalText}' → '{suggestion.SuggestedText}'.");
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("Corrected text:");
                    sb.AppendLine(correctedText);
                    
                    return sb.ToString();
                }
                
                // If neither auto-correct nor suggestions are enabled, return the original text
                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing text with model: {ex.Message}");
                return ProcessTextRuleBased(text);
            }
        }
        
        /// <summary>
        /// Check grammar using the ONNX model
        /// </summary>
        private async Task<List<GrammarSuggestion>> CheckGrammarWithModelAsync(string sentence, float[] sentenceEmbedding, string language)
        {
            var suggestions = new List<GrammarSuggestion>();
            
            try
            {
                // For each potential error pattern in the detected language
                if (_commonErrors.TryGetValue(language, out var languageErrors))
                {
                    foreach (var error in languageErrors)
                    {
                        if (sentence.Contains(error.ErrorPattern))
                        {
                            // Create a corrected version of the sentence
                            var correctedSentence = sentence.Replace(error.ErrorPattern, error.Correction);
                            
                            // Get embedding for corrected sentence
                            var correctedEmbedding = await CreateEmbeddingAsync(correctedSentence);
                            
                            // Calculate similarity between original and corrected embeddings
                            float similarity = CalculateCosineSimilarity(sentenceEmbedding, correctedEmbedding);
                            
                            // If similarity is high (meaning the semantic meaning is preserved)
                            // but not identical (meaning there was a meaningful change)
                            if (similarity > 0.95f && similarity < 0.999f)
                            {
                                suggestions.Add(new GrammarSuggestion
                                {
                                    OriginalText = error.ErrorPattern,
                                    SuggestedText = error.Correction,
                                    Explanation = error.Explanation,
                                    Confidence = (1.0f - similarity) * 10 // Scale confidence
                                });
                            }
                        }
                    }
                }
                
                return suggestions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking grammar with model: {ex.Message}");
                return new List<GrammarSuggestion>();
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
        /// Split text into sentences
        /// </summary>
        private List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();
            var sentenceRegex = new Regex(@"(\S.+?[.!?])(?=\s+|$)|\s*\n+\s*", RegexOptions.Multiline);
            
            var matches = sentenceRegex.Matches(text);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    sentences.Add(match.Value.Trim());
                }
            }
            else
            {
                // If no sentences were found, treat the entire text as one sentence
                sentences.Add(text.Trim());
            }
            
            return sentences;
        }
        
        /// <summary>
        /// Process text using rule-based approach (fallback)
        /// </summary>
        private string ProcessTextRuleBased(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
                
            try
            {
                // Detect language
                var language = DetectLanguageRuleBased(text);
                
                // Apply basic grammar rules
                var correctedText = text;
                
                // Apply language-specific rules
                if (_commonErrors.TryGetValue(language, out var languageErrors))
                {
                    foreach (var error in languageErrors)
                    {
                        correctedText = correctedText.Replace(error.ErrorPattern, error.Correction);
                    }
                }
                
                // If auto-correct is enabled, return the corrected text
                if (_autoCorrect)
                {
                    return correctedText;
                }
                
                // If suggestions are enabled, return the original text with suggestions
                if (_showSuggestions)
                {
                    // If no changes were made, return the original text
                    if (correctedText == text)
                    {
                        return text;
                    }
                    
                    // Return the original text with suggestions
                    return $"{text}\n\n--- Grammar Suggestions ---\n{correctedText}";
                }
                
                // If neither auto-correct nor suggestions are enabled, return the original text
                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in rule-based processing: {ex.Message}");
                return text;
            }
        }
        
        /// <summary>
        /// Detect the language of the input text using the ONNX model
        /// </summary>
        private async Task<string> DetectLanguageWithModelAsync(string text)
        {
            try
            {
                if (!_isModelInitialized)
                {
                    return DetectLanguageRuleBased(text);
                }
                
                // Create embedding for the text
                var textEmbedding = await CreateEmbeddingAsync(text);
                
                // Compare with language reference embeddings
                // This is a simplified approach - in a real implementation,
                // we would have reference embeddings for each language
                
                // For now, fall back to rule-based detection
                return DetectLanguageRuleBased(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting language with model: {ex.Message}");
                return DetectLanguageRuleBased(text);
            }
        }
        
        /// <summary>
        /// Detect the language of the input text using rule-based approach
        /// </summary>
        private string DetectLanguageRuleBased(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "en";
                
            try
            {
                // Simple language detection based on character frequency
                var normalizedText = text.ToLowerInvariant();
                
                // Check for specific language markers
                if (normalizedText.Contains("ñ") || normalizedText.Contains("¿") || normalizedText.Contains("¡"))
                    return "es";
                    
                if (normalizedText.Contains("ç") || normalizedText.Contains("œ") || normalizedText.Contains("à") || normalizedText.Contains("é"))
                    return "fr";
                    
                if (normalizedText.Contains("ß") || normalizedText.Contains("ü") || normalizedText.Contains("ö") || normalizedText.Contains("ä"))
                    return "de";
                    
                if (normalizedText.Contains("ا") || normalizedText.Contains("ل") || normalizedText.Contains("م"))
                    return "ar";
                    
                // Default to English
                return "en";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting language: {ex.Message}");
                return "en";
            }
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
            titleTextBlock.Text = "Grammar Checker Settings";
            titleTextBlock.FontWeight = System.Windows.FontWeights.Bold;
            titleTextBlock.Margin = new System.Windows.Thickness(0, 0, 0, 10);
            panel.Children.Insert(0, titleTextBlock); // Insert at the beginning
            
            // Add auto-correct checkbox
            var autoCorrectCheckBox = new CheckBox();
            autoCorrectCheckBox.Content = "Auto-correct grammar issues";
            autoCorrectCheckBox.IsChecked = _autoCorrect;
            autoCorrectCheckBox.Margin = new System.Windows.Thickness(0, 0, 0, 5);
            autoCorrectCheckBox.Tag = "AutoCorrectCheckBox";
            autoCorrectCheckBox.Checked += (sender, e) => {
                _autoCorrect = true;
                // If auto-correct is enabled, disable suggestions
                foreach (var child in panel.Children)
                {
                    if (child is CheckBox cb && cb.Tag?.ToString() == "ShowSuggestionsCheckBox")
                    {
                        cb.IsChecked = false;
                        _showSuggestions = false;
                        break;
                    }
                }
            };
            autoCorrectCheckBox.Unchecked += (sender, e) => {
                _autoCorrect = false;
            };
            panel.Children.Add(autoCorrectCheckBox);
            
            // Add show suggestions checkbox
            var suggestionsCheckBox = new CheckBox();
            suggestionsCheckBox.Content = "Show grammar suggestions";
            suggestionsCheckBox.IsChecked = _showSuggestions;
            suggestionsCheckBox.Margin = new System.Windows.Thickness(0, 0, 0, 5);
            suggestionsCheckBox.Tag = "ShowSuggestionsCheckBox";
            suggestionsCheckBox.Checked += (sender, e) => {
                _showSuggestions = true;
                // If suggestions are enabled, disable auto-correct
                foreach (var child in panel.Children)
                {
                    if (child is CheckBox cb && cb.Tag?.ToString() == "AutoCorrectCheckBox")
                    {
                        cb.IsChecked = false;
                        _autoCorrect = false;
                        break;
                    }
                }
            };
            suggestionsCheckBox.Unchecked += (sender, e) => {
                _showSuggestions = false;
            };
            panel.Children.Add(suggestionsCheckBox);
            
            // Add model status
            var modelStatusLabel = new TextBlock();
            modelStatusLabel.Text = $"AI Model: {(_isModelInitialized ? "Loaded" : "Loading...")}";
            modelStatusLabel.Margin = new System.Windows.Thickness(0, 10, 0, 0);
            panel.Children.Add(modelStatusLabel);
            
            // Add description
            var descriptionTextBlock = new TextBlock();
            descriptionTextBlock.Text = "This plugin uses an AI model to check and correct grammar in text. If the model is not available, it will fall back to a rule-based approach.";
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
                    // Process plugin-specific checkboxes
                    foreach (var child in panel.Children)
                    {
                        if (child is CheckBox checkBox)
                        {
                            string tag = checkBox.Tag?.ToString() ?? "";
                            bool isChecked = checkBox.IsChecked ?? false;
                            
                            if (tag == "AutoCorrectCheckBox")
                                _autoCorrect = isChecked;
                            else if (tag == "ShowSuggestionsCheckBox")
                                _showSuggestions = isChecked;
                        }
                    }
                    
                    // Save plugin-specific settings
                    SetSetting("AutoCorrect", _autoCorrect);
                    SetSetting("ShowSuggestions", _showSuggestions);
                    
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
    
    /// <summary>
    /// Represents a grammar error pattern and its correction
    /// </summary>
    public class GrammarError
    {
        /// <summary>
        /// The error pattern to look for
        /// </summary>
        public string ErrorPattern { get; set; }
        
        /// <summary>
        /// The correction to apply
        /// </summary>
        public string Correction { get; set; }
        
        /// <summary>
        /// Explanation of the error
        /// </summary>
        public string Explanation { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public GrammarError(string errorPattern, string correction, string explanation)
        {
            ErrorPattern = errorPattern;
            Correction = correction;
            Explanation = explanation;
        }
    }
    
    /// <summary>
    /// Represents a grammar correction suggestion
    /// </summary>
    public class GrammarSuggestion
    {
        /// <summary>
        /// The original text with the error
        /// </summary>
        public string OriginalText { get; set; }
        
        /// <summary>
        /// The suggested correction
        /// </summary>
        public string SuggestedText { get; set; }
        
        /// <summary>
        /// Explanation of the error
        /// </summary>
        public string Explanation { get; set; }
        
        /// <summary>
        /// Confidence score for the suggestion (0-1)
        /// </summary>
        public float Confidence { get; set; }
    }
}
