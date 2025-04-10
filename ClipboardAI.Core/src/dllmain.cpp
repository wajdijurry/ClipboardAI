#include <windows.h>
#include <string>
#include "../include/AIProcessor.h"

// Define the export macro
#ifdef CLIPBOARDAICORE_EXPORTS
#define CLIPBOARDAI_API __declspec(dllexport)
#else
#define CLIPBOARDAI_API __declspec(dllimport)
#endif

// Global instance of the AI processor
static ClipboardAI::Core::AIProcessor* g_pProcessor = nullptr;

// Helper function to convert std::string to LPWSTR
LPWSTR ConvertToLPWSTR(const std::string& str)
{
    int size = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, nullptr, 0);
    LPWSTR result = new WCHAR[size];
    MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, result, size);
    return result;
}

extern "C" {
    // Create a new AI processor instance
    CLIPBOARDAI_API void* CreateAIProcessor()
    {
        if (!g_pProcessor)
        {
            g_pProcessor = new ClipboardAI::Core::AIProcessor();
        }
        return g_pProcessor;
    }

    // Initialize the AI processor with the specified model path
    CLIPBOARDAI_API bool InitializeAIProcessor(void* processor, LPCWSTR modelPath)
    {
        if (!processor)
            return false;

        // Convert wide string to UTF-8
        int size = WideCharToMultiByte(CP_UTF8, 0, modelPath, -1, nullptr, 0, nullptr, nullptr);
        std::string modelPathStr(size, 0);
        WideCharToMultiByte(CP_UTF8, 0, modelPath, -1, &modelPathStr[0], size, nullptr, nullptr);

        ClipboardAI::Core::AIProcessor* pProcessor = static_cast<ClipboardAI::Core::AIProcessor*>(processor);
        return pProcessor->Initialize(modelPathStr);
    }

    // Process text using the AI processor
    CLIPBOARDAI_API LPWSTR ProcessText(void* processor, LPCWSTR text)
    {
        if (!processor)
            return nullptr;

        // Convert wide string to UTF-8
        int size = WideCharToMultiByte(CP_UTF8, 0, text, -1, nullptr, 0, nullptr, nullptr);
        std::string textStr(size, 0);
        WideCharToMultiByte(CP_UTF8, 0, text, -1, &textStr[0], size, nullptr, nullptr);

        ClipboardAI::Core::AIProcessor* pProcessor = static_cast<ClipboardAI::Core::AIProcessor*>(processor);
        std::string result = pProcessor->ProcessText(textStr);
        
        // Convert result back to wide string
        return ConvertToLPWSTR(result);
    }

    // Perform OCR on an image
    CLIPBOARDAI_API LPWSTR PerformOCR(void* processor, unsigned char* imageData, int width, int height, int channels)
    {
        if (!processor)
            return nullptr;

        ClipboardAI::Core::AIProcessor* pProcessor = static_cast<ClipboardAI::Core::AIProcessor*>(processor);
        std::string result = pProcessor->PerformOCR(imageData, width, height, channels);
        
        // Convert result back to wide string
        return ConvertToLPWSTR(result);
    }

    // Free a string allocated by the DLL
    CLIPBOARDAI_API void FreeString(LPWSTR str)
    {
        if (str)
        {
            delete[] str;
        }
    }

    // Destroy the AI processor
    CLIPBOARDAI_API void DestroyAIProcessor(void* processor)
    {
        if (processor && processor == g_pProcessor)
        {
            delete g_pProcessor;
            g_pProcessor = nullptr;
        }
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // Initialize the DLL
        break;
    case DLL_THREAD_ATTACH:
        break;
    case DLL_THREAD_DETACH:
        break;
    case DLL_PROCESS_DETACH:
        // Clean up resources
        if (g_pProcessor)
        {
            delete g_pProcessor;
            g_pProcessor = nullptr;
        }
        break;
    }
    return TRUE;
}
