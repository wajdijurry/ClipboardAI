namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Enum representing the different types of AI features available as plugins
    /// </summary>
    public enum AIFeatureType
    {
        /// <summary>
        /// Optical Character Recognition (OCR) for extracting text from images
        /// </summary>
        OCR,

        /// <summary>
        /// JSON formatting and validation
        /// </summary>
        JsonFormatter,
        
        /// <summary>
        /// Password generation
        /// </summary>
        PasswordGeneration,
        
        /// <summary>
        /// Email template expansion
        /// </summary>
        EmailTemplateExpansion,
        
        /// <summary>
        /// Table extraction from text
        /// </summary>
        TableConversion,
        
        /// <summary>
        /// Keyword extraction from text
        /// </summary>
        KeywordExtraction,
        
        /// <summary>
        /// Grammar checking and correction
        /// </summary>
        GrammarChecker,
        
        /// <summary>
        /// Smart formatting detection and conversion
        /// </summary>
        SmartFormatting,
        
        /// <summary>
        /// Language detection for multilingual text
        /// </summary>
        LanguageDetection,
        
        /// <summary>
        /// Other AI features not categorized above
        /// </summary>
        Other
    }
}
