using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ClipboardAI.Plugin.LanguageDetection
{
    public partial class LanguageDetectionPlugin
    {
        /// <summary>
        /// Detect language using the Hugging Face ONNX model
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Dictionary of language codes and confidence scores</returns>
        private Dictionary<string, double> DetectLanguageWithHuggingFace(string text)
        {
            var result = new Dictionary<string, double>();
            
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    result["en"] = 1.0; // Default to English for empty text
                    return result;
                }
                
                if (!_languageModelLoaded || _languageModelSession == null)
                {
                    // Fall back to rule-based detection
                    return DetectLanguageWithScores(text);
                }
                
                // For XLM-RoBERTa, we need to use character-level processing since we don't have the actual tokenizer
                // This approach will work better than basic word splitting for languages like Spanish
                
                // Maximum sequence length for the model
                const int maxSeqLength = 128;
                
                // Create input tensors
                var inputIds = new DenseTensor<long>(new[] { 1, maxSeqLength });
                var attentionMask = new DenseTensor<long>(new[] { 1, maxSeqLength });
                
                // Normalize text: lowercase and trim
                text = text.ToLowerInvariant().Trim();
                
                // Special token IDs (approximate values used by XLM-RoBERTa)
                const long clsTokenId = 0;  // <s> token
                const long sepTokenId = 2;  // </s> token
                const long padTokenId = 1;  // <pad> token
                
                // Set CLS token at the beginning
                inputIds[0, 0] = clsTokenId;
                attentionMask[0, 0] = 1;
                
                // Character-level processing with n-gram approach
                // This helps capture subword information similar to how XLM-RoBERTa's tokenizer works
                int position = 1;
                var chars = text.ToCharArray();
                
                // Process text character by character with context
                for (int i = 0; i < chars.Length && position < maxSeqLength - 1; i++)
                {
                    // Generate a hash code based on character and its context
                    // This simulates subword tokenization by creating unique IDs for character sequences
                    string charContext = GetCharacterContext(text, i, 5);
                    long tokenId = Math.Abs(charContext.GetHashCode()) % 32000 + 10; // Avoid special token IDs
                    
                    inputIds[0, position] = tokenId;
                    attentionMask[0, position] = 1;
                    position++;
                    
                    // Skip ahead for efficiency - we don't need every single character
                    // This simulates how the tokenizer would group characters
                    if (i % 2 == 0 && char.IsLetterOrDigit(chars[i]))
                    {
                        i++;
                    }
                }
                
                // Add SEP token
                inputIds[0, position] = sepTokenId;
                attentionMask[0, position] = 1;
                position++;
                
                // Pad the rest of the sequence
                for (int i = position; i < maxSeqLength; i++)
                {
                    inputIds[0, i] = padTokenId;
                    attentionMask[0, i] = 0;
                }
                
                // Helper method to get character context (simulates subword tokenization)
                string GetCharacterContext(string input, int index, int contextSize)
                {
                    int start = Math.Max(0, index - contextSize);
                    int end = Math.Min(input.Length - 1, index + contextSize);
                    return input.Substring(start, end - start + 1);
                }
                
                // Run inference
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                    NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask)
                };
                
                try
                {
                    // Run inference
                    using (var outputs = _languageModelSession.Run(inputs))
                    {
                        // Get the output tensor (logits)
                        var outputTensor = outputs.First(x => x.Name == "logits").AsTensor<float>();
                        var logits = new float[outputTensor.Dimensions[1]];
                        
                        for (int i = 0; i < outputTensor.Dimensions[1]; i++)
                        {
                            logits[i] = outputTensor[0, i];
                        }
                        
                        // Apply linguistic feature analysis to improve detection
                        var linguisticScores = AnalyzeLinguisticFeatures(text);
                        
                        // Convert logits to probabilities using softmax
                        var probabilities = Softmax(logits);
                        
                        // Create a hybrid scoring system combining model predictions with linguistic features
                        var hybridScores = new Dictionary<string, double>();
                        
                        // Add all languages to result, even with low confidence
                        for (int i = 0; i < probabilities.Length; i++)
                        {
                            if (_languageLabels.TryGetValue(i, out string languageCode))
                            {
                                // Convert ISO 639-1 codes if needed
                                string normalizedCode = NormalizeLanguageCode(languageCode);
                                
                                // Get model confidence
                                double modelConfidence = probabilities[i];
                                
                                // Get linguistic feature confidence (default to 0 if not present)
                                double linguisticConfidence = linguisticScores.TryGetValue(normalizedCode, out double score) ? score : 0.0;
                                
                                // Combine scores - weighted average favoring linguistic features for certain languages
                                double hybridConfidence;
                                if (normalizedCode == "es" || normalizedCode == "de" || normalizedCode == "pt" || normalizedCode == "fr" || normalizedCode == "it")
                                {
                                    // For European languages, give more weight to linguistic features
                                    hybridConfidence = (modelConfidence * 0.3) + (linguisticConfidence * 0.7);
                                }
                                else if (normalizedCode == "hi" || normalizedCode == "ur")
                                {
                                    // For Hindi/Urdu, reduce confidence unless linguistic features confirm
                                    hybridConfidence = (modelConfidence * 0.2) + (linguisticConfidence * 0.8);
                                }
                                else
                                {
                                    // For other languages, balanced approach
                                    hybridConfidence = (modelConfidence * 0.6) + (linguisticConfidence * 0.4);
                                }
                                
                                hybridScores[normalizedCode] = hybridConfidence;
                            }
                        }
                        
                        // Normalize the hybrid scores
                        double sum = hybridScores.Values.Sum();
                        if (sum > 0)
                        {
                            foreach (var lang in hybridScores.Keys.ToList())
                            {
                                hybridScores[lang] /= sum;
                            }
                        }
                        
                        // Transfer to result
                        foreach (var pair in hybridScores)
                        {
                            result[pair.Key] = pair.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during Hugging Face model inference: {ex.Message}");
                    // Fall back to rule-based detection
                    return DetectLanguageWithScores(text);
                }
                
                // If no results, fall back to rule-based detection
                if (result.Count == 0)
                {
                    return DetectLanguageWithScores(text);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DetectLanguageWithHuggingFace: {ex.Message}");
                result["en"] = 1.0; // Default to English on error
                return result;
            }
        }
    }
}
