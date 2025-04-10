#pragma once

#include <string>
#include <memory>
#include <map>

namespace ClipboardAI {
namespace Core {

/**
 * @brief Main AI processing engine for clipboard content
 * 
 * This class handles the core AI functionality including:
 * - Text recognition (OCR)
 * - Text/code processing
 * - Summarization
 * - Paraphrasing
 * - Code formatting
 * - Table conversion
 */
class AIProcessor {
public:
    AIProcessor();
    ~AIProcessor();

    /**
     * @brief Initialize the AI processor with the specified models
     * @param modelPath Path to the directory containing AI models
     * @return True if initialization was successful
     */
    bool Initialize(const std::string& modelPath);

    /**
     * @brief Process text using the loaded AI models
     * @param text Input text to process
     * @return Processed text result
     */
    std::string ProcessText(const std::string& text);

    /**
     * @brief Perform OCR on an image
     * @param imageData Raw image data
     * @param width Image width
     * @param height Image height
     * @param channels Number of color channels
     * @return Extracted text from the image
     */
    std::string PerformOCR(const unsigned char* imageData, int width, int height, int channels);

    /**
     * @brief Summarize long text
     * @param text Text to summarize
     * @param maxLength Maximum length of the summary
     * @return Summarized text
     */
    std::string SummarizeText(const std::string& text, int maxLength = 200);
    
    /**
     * @brief Paraphrase text in different tone
     * @param text Text to paraphrase
     * @param tone Tone to use (formal, casual, professional, etc.)
     * @return Paraphrased text
     */
    std::string ParaphraseText(const std::string& text, const std::string& tone);
    
    /**
     * @brief Format code according to language conventions
     * @param code Code to format
     * @param language Programming language
     * @return Formatted code
     */
    std::string FormatCode(const std::string& code, const std::string& language);
    
    /**
     * @brief Generate a strong password
     * @param length Desired password length
     * @param includeSpecial Include special characters
     * @param includeNumbers Include numbers
     * @return Generated password
     */
    std::string GeneratePassword(int length = 16, bool includeSpecial = true, bool includeNumbers = true);
    
    /**
     * @brief Expand email template with placeholders
     * @param templateText Email template with placeholders
     * @param replacements Map of placeholder keys to replacement values
     * @return Expanded email text
     */
    std::string ExpandEmailTemplate(const std::string& templateText, const std::map<std::string, std::string>& replacements);
    
    /**
     * @brief Convert tabular text to different formats
     * @param text Text containing tabular data
     * @param format Target format (html, excel, markdown)
     * @return Converted table
     */
    std::string ConvertTable(const std::string& text, const std::string& format);

private:
    // Private implementation details
    class Impl;
    std::unique_ptr<Impl> m_pImpl;
};

} // namespace Core
} // namespace ClipboardAI
