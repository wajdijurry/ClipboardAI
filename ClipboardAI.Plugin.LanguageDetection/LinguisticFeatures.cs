using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClipboardAI.Plugin.LanguageDetection
{
    public partial class LanguageDetectionPlugin
    {
        /// <summary>
        /// Analyzes linguistic features in text to improve language detection
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Dictionary of language codes and confidence scores based on linguistic features</returns>
        private Dictionary<string, double> AnalyzeLinguisticFeatures(string text)
        {
            var result = new Dictionary<string, double>();
            
            if (string.IsNullOrWhiteSpace(text))
                return result;
                
            // Normalize text
            text = text.ToLowerInvariant().Trim();
            
            // Calculate language scores based on character and word patterns
            
            // Spanish features
            double spanishScore = CalculateSpanishScore(text);
            if (spanishScore > 0)
                result["es"] = spanishScore;
                
            // German features
            double germanScore = CalculateGermanScore(text);
            if (germanScore > 0)
                result["de"] = germanScore;
                
            // French features
            double frenchScore = CalculateFrenchScore(text);
            if (frenchScore > 0)
                result["fr"] = frenchScore;
                
            // Italian features
            double italianScore = CalculateItalianScore(text);
            if (italianScore > 0)
                result["it"] = italianScore;
                
            // Portuguese features
            double portugueseScore = CalculatePortugueseScore(text);
            if (portugueseScore > 0)
                result["pt"] = portugueseScore;
                
            // English features
            double englishScore = CalculateEnglishScore(text);
            if (englishScore > 0)
                result["en"] = englishScore;
                
            // Hindi features
            double hindiScore = CalculateHindiScore(text);
            if (hindiScore > 0)
                result["hi"] = hindiScore;
                
            // Urdu features
            double urduScore = CalculateUrduScore(text);
            if (urduScore > 0)
                result["ur"] = urduScore;
                
            // Normalize scores
            double sum = result.Values.Sum();
            if (sum > 0)
            {
                foreach (var lang in result.Keys.ToList())
                {
                    result[lang] /= sum;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Calculate Spanish language score based on linguistic features
        /// </summary>
        private double CalculateSpanishScore(string text)
        {
            double score = 0;
            
            // Check for Spanish-specific characters and patterns
            if (text.Contains('ñ'))
                score += 0.4;
                
            if (text.Contains('¿') || text.Contains('¡'))
                score += 0.3;
                
            // Spanish common word patterns
            string[] spanishPatterns = { " el ", " la ", " los ", " las ", " un ", " una ", " y ", " o ", " de ", " del ", " al ", " que ", " en ", " con ", " por ", " para ", " como ", " donde ", " cuando ", " porque ", " si ", " no ", " muy ", " más ", " menos ", " mucho ", " poco " };
            foreach (var pattern in spanishPatterns)
            {
                if (text.Contains(pattern))
                    score += 0.05;
            }
            
            // Spanish verb endings
            string[] verbEndings = { "ar ", "er ", "ir ", "ando ", "endo ", "ado ", "ido ", "aba ", "ía ", "ó " };
            foreach (var ending in verbEndings)
            {
                if (Regex.IsMatch(text, $"\\w+{ending}"))
                    score += 0.1;
            }
            
            // Cap at 1.0
            return Math.Min(1.0, score);
        }
        
        /// <summary>
        /// Calculate German language score based on linguistic features
        /// </summary>
        private double CalculateGermanScore(string text)
        {
            double score = 0;
            
            // Check for German-specific characters
            if (text.Contains('ä') || text.Contains('ö') || text.Contains('ü') || text.Contains('ß'))
                score += 0.4;
                
            // German common word patterns
            string[] germanPatterns = { " der ", " die ", " das ", " ein ", " eine ", " und ", " oder ", " aber ", " wenn ", " weil ", " für ", " mit ", " zu ", " von ", " auf ", " ist ", " sind ", " war ", " nicht ", " kein ", " keine " };
            foreach (var pattern in germanPatterns)
            {
                if (text.Contains(pattern))
                    score += 0.05;
            }
            
            // German compound words (long words are common in German)
            var words = text.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (word.Length > 10)
                    score += 0.1;
            }
            
            // Cap at 1.0
            return Math.Min(1.0, score);
        }
        
        /// <summary>
        /// Calculate French language score based on linguistic features
        /// </summary>
        private double CalculateFrenchScore(string text)
        {
            double score = 0;
            
            // Check for French-specific characters and patterns
            if (text.Contains('é') || text.Contains('è') || text.Contains('ê') || text.Contains('ç'))
                score += 0.3;
                
            if (text.Contains(" l'") || text.Contains(" d'") || text.Contains(" c'") || text.Contains(" j'"))
                score += 0.3;
                
            // French common word patterns
            string[] frenchPatterns = { " le ", " la ", " les ", " un ", " une ", " des ", " et ", " ou ", " de ", " du ", " au ", " qui ", " que ", " quoi ", " où ", " quand ", " pourquoi ", " comment ", " si ", " ne ", " pas ", " plus ", " moins ", " très ", " beaucoup " };
            foreach (var pattern in frenchPatterns)
            {
                if (text.Contains(pattern))
                    score += 0.05;
            }
            
            // Cap at 1.0
            return Math.Min(1.0, score);
        }
        
        /// <summary>
        /// Calculate Italian language score based on linguistic features
        /// </summary>
        private double CalculateItalianScore(string text)
        {
            double score = 0;
            
            // Check for Italian-specific patterns
            if (text.Contains(" il ") || text.Contains(" lo ") || text.Contains(" la ") || text.Contains(" i ") || text.Contains(" gli ") || text.Contains(" le "))
                score += 0.3;
                
            // Italian common word endings
            string[] italianEndings = { "zione", "mento", "ità", "etto", "ello", "ino", "ista", "are", "ere", "ire" };
            foreach (var ending in italianEndings)
            {
                if (Regex.IsMatch(text, $"\\w+{ending}\\b"))
                    score += 0.1;
            }
            
            // Italian common word patterns
            string[] italianPatterns = { " e ", " o ", " ma ", " se ", " perché ", " come ", " quando ", " dove ", " chi ", " che ", " cosa ", " di ", " da ", " in ", " con ", " su ", " per ", " tra ", " fra " };
            foreach (var pattern in italianPatterns)
            {
                if (text.Contains(pattern))
                    score += 0.05;
            }
            
            // Cap at 1.0
            return Math.Min(1.0, score);
        }
        
        /// <summary>
        /// Calculate Portuguese language score based on linguistic features
        /// </summary>
        private double CalculatePortugueseScore(string text)
        {
            double score = 0;
            
            // Check for Portuguese-specific characters
            if (text.Contains('ã') || text.Contains('õ') || text.Contains('ç'))
                score += 0.4;
                
            // Portuguese common word patterns
            string[] portuguesePatterns = { " o ", " a ", " os ", " as ", " um ", " uma ", " e ", " ou ", " de ", " do ", " da ", " no ", " na ", " em ", " com ", " por ", " para ", " como ", " onde ", " quando ", " porque ", " se ", " não ", " muito ", " mais ", " menos " };
            foreach (var pattern in portuguesePatterns)
            {
                if (text.Contains(pattern))
                    score += 0.05;
            }
            
            // Portuguese verb endings
            string[] verbEndings = { "ar ", "er ", "ir ", "ando ", "endo ", "indo ", "ado ", "ido ", "ava ", "ia " };
            foreach (var ending in verbEndings)
            {
                if (Regex.IsMatch(text, $"\\w+{ending}"))
                    score += 0.1;
            }
            
            // Cap at 1.0
            return Math.Min(1.0, score);
        }
        
        /// <summary>
        /// Calculate English language score based on linguistic features
        /// </summary>
        private double CalculateEnglishScore(string text)
        {
            double score = 0;
            
            // English common word patterns
            string[] englishPatterns = { " the ", " a ", " an ", " and ", " or ", " but ", " if ", " because ", " when ", " where ", " how ", " what ", " who ", " why ", " to ", " of ", " in ", " on ", " at ", " by ", " with ", " for ", " from ", " is ", " are ", " was ", " were ", " not ", " no " };
            foreach (var pattern in englishPatterns)
            {
                if (text.Contains(pattern))
                    score += 0.05;
            }
            
            // English verb endings
            string[] verbEndings = { "ing ", "ed ", "ly ", "s ", "'s ", "'t " };
            foreach (var ending in verbEndings)
            {
                if (Regex.IsMatch(text, $"\\w+{ending}"))
                    score += 0.1;
            }
            
            // Cap at 1.0
            return Math.Min(1.0, score);
        }
        
        /// <summary>
        /// Calculate Hindi language score based on linguistic features
        /// </summary>
        private double CalculateHindiScore(string text)
        {
            // For Hindi, we'll check if the text contains Devanagari script characters
            // Since we're dealing with Spanish text being misidentified as Hindi,
            // we want to ensure we don't falsely identify Latin script as Hindi
            
            // Check if text contains any Devanagari script (used for Hindi)
            // Devanagari Unicode range: U+0900 to U+097F
            bool containsDevanagari = text.Any(c => c >= '\u0900' && c <= '\u097F');
            
            return containsDevanagari ? 1.0 : 0.0;
        }
        
        /// <summary>
        /// Calculate Urdu language score based on linguistic features
        /// </summary>
        private double CalculateUrduScore(string text)
        {
            // For Urdu, we'll check if the text contains Arabic script characters
            // Since we're dealing with Spanish text being misidentified as Urdu,
            // we want to ensure we don't falsely identify Latin script as Urdu
            
            // Check if text contains any Arabic script (used for Urdu)
            // Arabic Unicode range: U+0600 to U+06FF
            bool containsArabic = text.Any(c => c >= '\u0600' && c <= '\u06FF');
            
            return containsArabic ? 1.0 : 0.0;
        }
    }
}
