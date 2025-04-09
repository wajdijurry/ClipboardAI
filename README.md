# ClipboardAI

A powerful clipboard enhancement tool with AI capabilities for text processing and OCR. ClipboardAI is designed to boost productivity by providing advanced clipboard management with intelligent processing features.

## Features

- **Clipboard History Management**: Store, search, and organize your clipboard history
- **OCR Image-to-Text**: Extract text from images with multilingual support (English, Arabic, French, German, Spanish)
- **Language Detection**: Automatically identify the language of your clipboard content
- **Smart Formatting**: Clean up and format text with a single click
- **JSON Formatter**: Beautify and validate JSON snippets
- **Table Conversion**: Convert between various table formats
- **Password Generation**: Create strong, secure passwords
- **Email Template Expansion**: Quickly fill in email templates
- **Keyword Extraction**: Identify key topics in your text

## Project Structure

- **ClipboardAI.Core (C++)**: Performance-critical AI processing components
  - AI processing engine with ONNX Runtime
  - OCR integration with Tesseract

- **ClipboardAI.UI (C#/WPF)**: User interface
  - Main application window
  - Settings panels
  - Clipboard monitoring

- **ClipboardAI.Common (C#)**: Shared components
  - Data models
  - Configuration

- **ClipboardAI.Plugins (C#)**: Plugin system
  - Plugin interfaces
  - Plugin manager

## Requirements

- Windows 10 or later
- .NET 6.0 Desktop Runtime
- Visual C++ Redistributable 2019 or later

## Installation

### Using the Installer

1. Download the latest installer from the Releases page
2. Run the installer and follow the on-screen instructions
3. Select the plugins and language models you want to install
4. Launch the application from the Start menu or desktop shortcut

### OCR Language Support

ClipboardAI supports OCR in multiple languages. The following language models are available:

- English (22.4 MB)
- Arabic (9.6 MB)
- French (13.6 MB)
- German (14.7 MB)
- Spanish (17.4 MB)

The installer will download the selected language models during installation.

## Building the Project

### Prerequisites

- Visual Studio 2022 or later with:
  - .NET Desktop Development workload
- Inno Setup 6 or later (for building the installer)

### Build Steps

1. Clone the repository
2. Open `ClipboardAI.sln` in Visual Studio
3. Build the solution in Release mode
4. To create the installer, run the Inno Setup script at `ClipboardAI.InnoSetup/ClipboardAI.iss`

### Development Notes

- The OCR plugin uses Tesseract 5.0 with special handling for right-to-left languages like Arabic
- Architecture-specific native libraries (x64/x86) are loaded dynamically based on the process architecture
- The plugin system allows for easy extension with new functionality

## License

[MIT License](LICENSE)
