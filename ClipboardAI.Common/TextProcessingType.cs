using System;

namespace ClipboardAI.Common
{
    /// <summary>
    /// Types of text processing operations supported by the AI service
    /// </summary>
    public enum TextProcessingType
    {
        Summarize,
        Paraphrase,
        FormatCode,
        JsonFormat,
        GeneratePassword,
        ExpandEmailTemplate,
        ExtractTable
    }
}
