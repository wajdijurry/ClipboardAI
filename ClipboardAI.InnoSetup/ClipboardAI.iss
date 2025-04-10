#define MyAppName "ClipboardAI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ClipboardAI Software"
#define MyAppPublisherEmail "support@clipboardai.com"
#define MyAppURL "https://clipboardai.com/"
#define MyAppCopyright "Copyright © 2025 ClipboardAI Software"
#define MyAppExeName "ClipboardAI.UI.exe"
#define DotNetURL "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/6.0.36/windowsdesktop-runtime-6.0.36-win-x64.exe"
#define DotNetName "windowsdesktop-runtime-6.0.25-win-x64.exe"
#define OutputName "ClipboardAI_Setup"

; ONNX Model URLs
#define OnnxModelURL "https://drive.google.com/uc?export=download&id=1oU0osEKvbMylU08uspZFauIszLqYg2d3"
#define OnnxModelName "multilingual-e5-small.onnx"

; OCR Model URLs - ONNX models removed

; Language Detection Model URLs
; E5 Multilingual Model URL
#define LangDetectionE5ModelURL "https://drive.usercontent.google.com/download?id=1oU0osEKvbMylU08uspZFauIszLqYg2d3&export=download&authuser=0&download=true&confirm=t&uuid=a115d8e7-8686-4a61-a9b2-f81af5ba7934&at=APcmpoy_ZgTkIv3SqMiHORILkWFf%3A1744202046030"

; Hugging Face XLM-RoBERTa Model URLs
#define LangDetectionModelURL "https://drive.usercontent.google.com/download?id=1u9CKS2SdaqlX83UhULZiidEnqV4Br9ow&export=download&confirm=t&uuid=14df9f87-dcc3-489f-9226-d16d3fce1b94"
#define LangDetectionTokenizerURL "https://drive.google.com/uc?export=download&id=1-N-gab1Bl5xK72pmxbn9uFZJtwmncF-R"
#define LangDetectionConfigURL "https://drive.google.com/uc?export=download&id=1lxsI9mNEtfMhor6T-aEfr_-bx09pAPXj"
#define LangDetectionTokenizerConfigURL "https://drive.google.com/uc?export=download&id=1ylWr6vTcIBKvaYvQeV0zSVpjTDiW-tRJ"
#define LangDetectionSpecialTokensURL "https://drive.google.com/uc?export=download&id=1XQkDxZkAMw9oEz-M7MAWlAjSQdUIpgN7"
#define LangDetectionSentencePieceURL "https://drive.google.com/uc?export=download&id=1aiaTo6Mbx_1THEmdiuMSOMn9xh7cNUxc"

[Setup]
AppId={{CLIPBOARDAI-APP-GUID}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
LicenseFile=LICENSE.txt
AppUpdatesURL={#MyAppURL}
AppContact={#MyAppPublisherEmail}
AppCopyright={#MyAppCopyright}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=ClipboardAI - Advanced Clipboard Manager with AI Features
VersionInfoCopyright={#MyAppCopyright}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\Output
OutputBaseFilename={#OutputName}
SetupIconFile=..\ClipboardAI.UI\Resources\app_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
AlwaysShowComponentsList=yes
; WizardImageFile=logo.bmp
WizardSmallImageFile=logo.bmp

; Enable logging for troubleshooting
SetupLogging=yes

; Add watermark to all pages
ShowLanguageDialog=no

[Files]
; Main application files - Using framework-dependent build
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Common.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugins.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.UI.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.UI.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.UI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.UI.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\Microsoft.ML.OnnxRuntime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\onnxruntime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\onnxruntime.lib"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\onnxruntime_providers_shared.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\onnxruntime_providers_shared.lib"; DestDir: "{app}"; Flags: ignoreversion

; Plugin DLLs
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.OCR.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr

; OCR Plugin Dependencies - x64 architecture
Source: "..\ClipboardAI.Plugin.OCR\bin\Release\net6.0-windows\x64\leptonica-1.82.0.dll"; DestDir: "{app}\x64"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr
Source: "..\ClipboardAI.Plugin.OCR\bin\Release\net6.0-windows\x64\tesseract50.dll"; DestDir: "{app}\x64"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr

; OCR Plugin Dependencies - x86 architecture
Source: "..\ClipboardAI.Plugin.OCR\bin\Release\net6.0-windows\x86\leptonica-1.82.0.dll"; DestDir: "{app}\x86"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr
Source: "..\ClipboardAI.Plugin.OCR\bin\Release\net6.0-windows\x86\tesseract50.dll"; DestDir: "{app}\x86"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr

; Install Tesseract.dll in both the root directory and the OCR plugin directory
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\Tesseract.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\Tesseract.dll"; DestDir: "{app}\plugins\ocr"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr

; Table Conversion Plugin
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.TableConversion.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\tableconversion

; Password Generation Plugin
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.PasswordGen.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\passwordgen

; Email Template Expansion Plugin
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.EmailExpansion.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\emailtemplateexpansion

; Keyword Extraction Plugin
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.KeywordExtraction.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\keywordextraction

; Grammar Checker Plugin - Disabled

; Smart Formatting Plugin
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.SmartFormatting.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\smartformatting

; JSON Formatter Plugin
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.JsonFormatter.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\jsonformatter

; Language Detection Plugin
Source: "..\ClipboardAI.UI\bin\Release\net6.0-windows\win-x64\publish\ClipboardAI.Plugin.LanguageDetection.dll"; DestDir: "{app}\plugins"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\languagedetection

; Create plugin-specific model directories
Source: "README.txt"; DestDir: "{app}\Plugins\OCR\Models"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr
; Grammar Checker Models directory - Disabled
Source: "README.txt"; DestDir: "{app}\Plugins\LanguageDetection\Models"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\languagedetection


; OCR Plugin - Copy only necessary files, excluding x64 and x86 directories
Source: "..\ClipboardAI.Plugin.OCR\bin\Release\net6.0-windows\*.dll"; DestDir: "{app}\plugins\ocr"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr
Source: "..\ClipboardAI.Plugin.OCR\bin\Release\net6.0-windows\*.json"; DestDir: "{app}\plugins\ocr"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr
Source: "..\ClipboardAI.Plugin.OCR\bin\Release\net6.0-windows\*.pdb"; DestDir: "{app}\plugins\ocr"; Flags: ignoreversion skipifsourcedoesntexist; Components: plugins\ocr

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Components]
Name: "main"; Description: "Main Application"; Types: full compact custom; Flags: fixed; ExtraDiskSpaceRequired: 15400000
Name: "plugins"; Description: "Plugins"; Types: full compact custom; Flags: fixed; ExtraDiskSpaceRequired: 0
Name: "plugins\ocr"; Description: "Text Extraction (OCR) (12.2 MB)"; Types: full custom; ExtraDiskSpaceRequired: 42000
Name: "plugins\ocr\engines"; Description: "OCR Engine"; Types: full custom; ExtraDiskSpaceRequired: 6200000
Name: "plugins\ocr\engines\tesseract"; Description: "Tesseract"; Types: full custom; ExtraDiskSpaceRequired: 6200000
Name: "plugins\ocr\english"; Description: "English Language"; Types: full custom; ExtraDiskSpaceRequired: 23460000
Name: "plugins\ocr\arabic"; Description: "Arabic Language"; Types: full custom; ExtraDiskSpaceRequired: 10030000
Name: "plugins\ocr\french"; Description: "French Language"; Types: full custom; ExtraDiskSpaceRequired: 14210000
Name: "plugins\ocr\german"; Description: "German Language"; Types: full custom; ExtraDiskSpaceRequired: 15440000
Name: "plugins\ocr\spanish"; Description: "Spanish Language"; Types: full custom; ExtraDiskSpaceRequired: 18260000

Name: "plugins\tableconversion"; Description: "Table Conversion (19 KB)"; Types: full custom; ExtraDiskSpaceRequired: 19000

Name: "plugins\jsonformatter"; Description: "JSON Formatter (15 KB)"; Types: full compact custom; ExtraDiskSpaceRequired: 15000
Name: "plugins\passwordgen"; Description: "Password Generator (15 KB)"; Types: full compact custom; ExtraDiskSpaceRequired: 15000
Name: "plugins\emailtemplateexpansion"; Description: "Email Expansion (19 KB)"; Types: full custom; ExtraDiskSpaceRequired: 19000
Name: "plugins\keywordextraction"; Description: "Keyword Extraction (23 KB)"; Types: full custom; ExtraDiskSpaceRequired: 23000
; Grammar Checker plugin disabled
Name: "plugins\smartformatting"; Description: "Smart Formatting (16 KB)"; Types: full custom; ExtraDiskSpaceRequired: 16000

Name: "plugins\languagedetection"; Description: "Language Detection (56 KB)"; Types: full custom; ExtraDiskSpaceRequired: 56000
Name: "plugins\languagedetection\e5"; Description: "E5 Multilingual Model (Balanced) (115 MB)"; Types: full custom; ExtraDiskSpaceRequired: 118308185
Name: "plugins\languagedetection\huggingface"; Description: "Hugging Face XLM-RoBERTa Model (Best Accuracy) (287 MB)"; Types: full custom; ExtraDiskSpaceRequired: 301092655

[Types]
Name: "full"; Description: "Full installation"
Name: "compact"; Description: "Compact installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Messages]
; Remove the text from the bevel line
BeveledLabel=

[Run]
Filename: "{tmp}\{#DotNetName}"; Parameters: "/install /passive /norestart"; StatusMsg: "Installing .NET 6.0 Desktop Runtime..."; Check: DotNetInstallApproved; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DownloadPage: TDownloadWizardPage;
  NeedDotNet: Boolean;
  DotNetInstallConfirmed: Boolean;
  ParaphrasingDir: String;
  ParaphrasingLanguageFiles: array of String;
  ParaphrasingLanguageURLs: array of String;
  ParaphrasingLanguageCount: Integer;
  TessdataDir: String;
  ModelsDir: String;
  PluginsDir: String;
  OcrPluginDir: String;
  OcrModelsDir: String;
  GrammarCheckerDir: String;
  GrammarModelsDir: String;
  LanguageDetectionDir: String;
  LanguageModelsDir: String;
  CommonDir: String;
  CommonModelsDir: String;
  OnnxModelDownloaded: Boolean;
  I: Integer;
  
  // Plugin IDs for settings
  PluginIds: array of String;

// Check if .NET 6.0 Desktop Runtime is needed
function DotNetNeeded(): Boolean;
var
  Version: String;
begin
  // Check for .NET 6.0 Desktop Runtime in registry
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\NET Core\Setup\InstalledVersions\x64\Microsoft.WindowsDesktop.App', 'Version', Version) then
  begin
    // Check if version starts with 6.0
    if Pos('6.0', Version) = 1 then
    begin
      Result := False;
      Exit;
    end;
  end;
  
  // Check x86 version
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\NET Core\Setup\InstalledVersions\x86\Microsoft.WindowsDesktop.App', 'Version', Version) then
  begin
    // Check if version starts with 6.0
    if Pos('6.0', Version) = 1 then
    begin
      Result := False;
      Exit;
    end;
  end;
  
  Result := True;
end;

// Function to check if .NET installation was approved by user
function DotNetInstallApproved(): Boolean;
begin
  Result := DotNetInstallConfirmed;
end;

// Function to prompt user about .NET installation
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Just check if .NET is needed, but don't prompt yet
  NeedDotNet := DotNetNeeded();
end;

// Initialize plugin IDs
procedure InitializePluginIds();
begin
  // Define plugin IDs
  SetArrayLength(PluginIds, 9);
  
  PluginIds[0] := 'OCR';
  PluginIds[1] := 'JsonFormatter';
  PluginIds[2] := 'PasswordGeneration';
  PluginIds[3] := 'EmailTemplateExpansion';
  PluginIds[4] := 'TableConversion';
  PluginIds[5] := 'KeywordExtraction';
  PluginIds[6] := 'GrammarChecker';
  PluginIds[7] := 'SmartFormatting';
  PluginIds[8] := 'LanguageDetection';
end;

// Convert boolean to string for JSON
function BoolToJSON(Value: Boolean): String;
begin
  if Value then
    Result := 'true'
  else
    Result := 'false';
end;

// Create settings.json file based on user preferences
procedure CreateSettingsJsonFile();
var
  SettingsFilePath: String;
  SettingsContent: String;
  SettingsFile: TStringList;
  i: Integer;
begin
  SettingsFilePath := ExpandConstant('{app}\settings.json');
  SettingsFile := TStringList.Create;
  try
    // Build JSON content
    SettingsContent := '{' + #13#10;
    SettingsContent := SettingsContent + '  "StartWithWindows": false,' + #13#10;
    SettingsContent := SettingsContent + '  "MinimizeToTray": true,' + #13#10;
    SettingsContent := SettingsContent + '  "DefaultLanguage": "en",' + #13#10;
    
    // Enabled plugins section - enable plugins based on component selection
    SettingsContent := SettingsContent + '  "EnabledPlugins": {' + #13#10;
    for i := 0 to Length(PluginIds) - 1 do
    begin
      // Check if the corresponding component is selected
      case i of
        0: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\ocr'));
        1: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\jsonformatter'));
        2: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\passwordgen'));
        3: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\emailtemplateexpansion'));
        4: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\tableconversion'));
        5: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\keywordextraction'));
        6: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\grammarcheck'));
        7: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\smartformatting'));
        8: SettingsContent := SettingsContent + '    "' + PluginIds[i] + '": ' + BoolToJSON(WizardIsComponentSelected('plugins\languagedetection'));
      end;
      
      if i < Length(PluginIds) - 1 then
        SettingsContent := SettingsContent + ',';
      SettingsContent := SettingsContent + #13#10;
    end;
    SettingsContent := SettingsContent + '  },' + #13#10;
    
    // Plugin settings section
    SettingsContent := SettingsContent + '  "PluginSettings": {' + #13#10;
    SettingsContent := SettingsContent + '    "JsonFormatter": {' + #13#10;
    SettingsContent := SettingsContent + '      "IndentSize": 4,' + #13#10;
    SettingsContent := SettingsContent + '      "WriteIndented": true,' + #13#10;
    SettingsContent := SettingsContent + '      "AllowTrailingCommas": false' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "OCR": {' + #13#10;
    SettingsContent := SettingsContent + '      "PreferredLanguage": "eng",' + #13#10;
    SettingsContent := SettingsContent + '      "PreferredEngine": "tesseract"' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "EmailTemplateExpansion": {' + #13#10;
    SettingsContent := SettingsContent + '      "Templates": {' + #13#10;
    SettingsContent := SettingsContent + '        "thank_you": "Dear [Recipient],\n\nThank you for your email. I appreciate your [Reason].\n\n[Signature]",' + #13#10;
    SettingsContent := SettingsContent + '        "meeting_request": "Dear [Recipient],\n\nI would like to schedule a meeting to discuss [Topic]. Are you available on [Date] at [Time]?\n\n[Signature]",' + #13#10;
    SettingsContent := SettingsContent + '        "follow_up": "Dear [Recipient],\n\nI am following up on our conversation about [Topic]. [Message]\n\n[Signature]"' + #13#10;
    SettingsContent := SettingsContent + '      },' + #13#10;
    SettingsContent := SettingsContent + '      "IncludeSignature": true,' + #13#10;
    SettingsContent := SettingsContent + '      "DefaultSignature": "Best regards,\n[Your Name]",' + #13#10;
    SettingsContent := SettingsContent + '      "AutoFillRecipientName": true' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "TableConversion": {' + #13#10;
    SettingsContent := SettingsContent + '      "DefaultOutputFormat": "Markdown",' + #13#10;
    SettingsContent := SettingsContent + '      "PreserveHeaderFormatting": true,' + #13#10;
    SettingsContent := SettingsContent + '      "AutoDetectDelimiter": true' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "PasswordGeneration": {' + #13#10;
    SettingsContent := SettingsContent + '      "DefaultLength": 16,' + #13#10;
    SettingsContent := SettingsContent + '      "IncludeSpecialChars": true,' + #13#10;
    SettingsContent := SettingsContent + '      "IncludeNumbers": true,' + #13#10;
    SettingsContent := SettingsContent + '      "IncludeUppercase": true,' + #13#10;
    SettingsContent := SettingsContent + '      "IncludeLowercase": true' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "KeywordExtraction": {' + #13#10;
    SettingsContent := SettingsContent + '      "MaxKeywords": 10,' + #13#10;
    SettingsContent := SettingsContent + '      "MinimumScore": 0.3,' + #13#10;
    SettingsContent := SettingsContent + '      "IncludeScores": true,' + #13#10;
    SettingsContent := SettingsContent + '      "SortByScore": true' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "GrammarChecker": {' + #13#10;
    SettingsContent := SettingsContent + '      "CheckSpelling": true,' + #13#10;
    SettingsContent := SettingsContent + '      "CheckGrammar": true,' + #13#10;
    SettingsContent := SettingsContent + '      "CheckPunctuation": true,' + #13#10;
    SettingsContent := SettingsContent + '      "SuggestImprovements": true,' + #13#10;
    SettingsContent := SettingsContent + '      "HighlightErrors": true' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "SmartFormatting": {' + #13#10;
    SettingsContent := SettingsContent + '      "PreserveFormatting": true,' + #13#10;
    SettingsContent := SettingsContent + '      "DetectCodeLanguage": true' + #13#10;
    SettingsContent := SettingsContent + '    },' + #13#10;
    SettingsContent := SettingsContent + '    "LanguageDetection": {' + #13#10;
    SettingsContent := SettingsContent + '      "ShowConfidence": true,' + #13#10;
    SettingsContent := SettingsContent + '      "ShowAllLanguages": true' + #13#10;
    SettingsContent := SettingsContent + '    }' + #13#10;
    SettingsContent := SettingsContent + '  },' + #13#10;
    
    // Advanced settings
    SettingsContent := SettingsContent + '  "ModelDirectory": "' + ExpandConstant('{app}\Models') + '",' + #13#10;
    SettingsContent := SettingsContent + '  "ProcessingThreads": 8,' + #13#10;
    SettingsContent := SettingsContent + '  "MemoryLimitMB": 1024,' + #13#10;
    SettingsContent := SettingsContent + '  "EnableDebugLogging": false,' + #13#10;
    SettingsContent := SettingsContent + '  "UseCpuOnly": true,' + #13#10;
    SettingsContent := SettingsContent + '  "MaxHistorySize": 100,' + #13#10;
    SettingsContent := SettingsContent + '  "ExpirationDays": 30,' + #13#10;
    SettingsContent := SettingsContent + '  "EnableFuzzySearch": true,' + #13#10;
    SettingsContent := SettingsContent + '  "EnableMultiClipboard": true,' + #13#10;
    SettingsContent := SettingsContent + '  "EncryptSensitiveData": false,' + #13#10;
    SettingsContent := SettingsContent + '  "AutoClearSensitiveData": false,' + #13#10;
    SettingsContent := SettingsContent + '  "UseAppWhitelisting": false,' + #13#10;
    SettingsContent := SettingsContent + '  "WhitelistedApps": [],' + #13#10;
    SettingsContent := SettingsContent + '  "EnableAuditLogging": false,' + #13#10;
    SettingsContent := SettingsContent + '  "Theme": "Light",' + #13#10;
    SettingsContent := SettingsContent + '  "EnableSoundFeedback": false,' + #13#10;
    SettingsContent := SettingsContent + '  "ShowMiniPreview": true,' + #13#10;
    SettingsContent := SettingsContent + '  "EnableHotkeys": true,' + #13#10;
    SettingsContent := SettingsContent + '  "CustomHotkeys": {' + #13#10;
    SettingsContent := SettingsContent + '    "ClipboardMenu": "Ctrl+Alt+V",' + #13#10;
    SettingsContent := SettingsContent + '    "FavoritesMenu": "Ctrl+Alt+F",' + #13#10;
    SettingsContent := SettingsContent + '    "QuickPaste": "Ctrl+Alt+Q"' + #13#10;
    SettingsContent := SettingsContent + '  }' + #13#10;
    SettingsContent := SettingsContent + '}' + #13#10;
    
    // Write to file
    SettingsFile.Text := SettingsContent;
    SettingsFile.SaveToFile(SettingsFilePath);
  finally
    SettingsFile.Free;
  end;
end;

procedure InitializeWizard;
var
  DeveloperLabel: TNewStaticText;
begin
  // Check if .NET 6.0 Desktop Runtime is needed
  NeedDotNet := DotNetNeeded();
  DotNetInstallConfirmed := False;
  
  // Create the download page
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
  
  // Initialize plugin IDs
  InitializePluginIds();
  
  // Add custom developer label below the bevel line
  DeveloperLabel := TNewStaticText.Create(WizardForm);
  DeveloperLabel.Parent := WizardForm;
  DeveloperLabel.Caption := 'Developed by: Wajdi Jurry';
  DeveloperLabel.Font.Style := [fsBold];
  DeveloperLabel.Font.Size := 8;
  DeveloperLabel.Font.Color := $AFAFAF; // Light gray color
  
  // Position it below the bevel line
  DeveloperLabel.Top := WizardForm.Bevel.Top + WizardForm.Bevel.Height + 107;
  DeveloperLabel.Left := 15;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
  DownloadPage: TDownloadWizardPage;
  LanguageFiles: array of String;
  LanguageURLs: array of String;
  LanguageCount: Integer;
begin
  Result := True;
  
  // If we're at the ready page and .NET needs to be installed, prompt the user
  if (CurPageID = wpReady) and NeedDotNet then
  begin
    if MsgBox('.NET 6.0 Desktop Runtime is required to run ClipboardAI but is not installed on your system. Do you want to install it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DotNetInstallConfirmed := True;
      
      // Download and install .NET
      DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), 'Downloading .NET 6.0 Desktop Runtime...', nil);
      DownloadPage.Clear;
      DownloadPage.Add('{#DotNetURL}', '{#DotNetName}', '');
      
      DownloadPage.Show;
      try
        try
          DownloadPage.Download;
          Result := True;
        except
          if DownloadPage.AbortedByUser then
          begin
            Log('Download aborted by user.');
            Result := False;
            MsgBox('Download of .NET 6.0 Desktop Runtime was aborted. Installation will be cancelled.', mbInformation, MB_OK);
          end
          else
          begin
            SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
            Result := False;
          end;
        end;
      finally
        DownloadPage.Hide;
      end;
    end
    else
    begin
      // User declined to install .NET Runtime
      MsgBox('ClipboardAI requires .NET 6.0 Desktop Runtime to run. Installation will be aborted.', mbInformation, MB_OK);
      Result := False; // This will abort the installation
    end;
  end;
end;

// Download OCR language files based on selected components
procedure CurStepChanged(CurStep: TSetupStep);
var
  DownloadPage: TDownloadWizardPage;
  DownloadNeeded: Boolean;
  TessdataDir, TessdataPluginDir: String;
  I: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Create settings.json file based on user preferences
    CreateSettingsJsonFile();
    
    // Download AI models if any plugin that uses models is selected
    if WizardIsComponentSelected('plugins\languagedetection') or WizardIsComponentSelected('plugins\ocr') then
    begin
      // Create plugin-specific model directories
      PluginsDir := ExpandConstant('{app}\Plugins');
      if not DirExists(PluginsDir) then
        CreateDir(PluginsDir);
        
      // Create plugin model directories for selected plugins
      // OCR plugin
      if WizardIsComponentSelected('plugins\ocr') then
      begin
        ForceDirectories(ExpandConstant('{app}\Plugins\OCR\Models'));
        OcrModelsDir := ExpandConstant('{app}\Plugins\OCR\Models');
      end;
      
      // GrammarChecker plugin - Disabled
      
      // LanguageDetection plugin
      if WizardIsComponentSelected('plugins\languagedetection') then
      begin
        ForceDirectories(ExpandConstant('{app}\Plugins\LanguageDetection\Models'));
        LanguageModelsDir := ExpandConstant('{app}\Plugins\LanguageDetection\Models');
      end;
      
      // Create Common models directory for shared models
      // Create plugin-specific model directories as needed
      
      // Create download page for models
      DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), 'Downloading AI models...', nil);
      DownloadPage.Clear;
      
      // Add models to download based on selected components
      
      // OCR language files will be downloaded in the dedicated OCR section below
      
      // 2. Download ONNX models for plugins
      
      // Download the multilingual model for Grammar Checker if selected
      if WizardIsComponentSelected('plugins\grammarcheck') then
      begin
        DownloadPage.Add('{#OnnxModelURL}', 'multilingual-e5-small.onnx', '');
        Log('Added multilingual model for Grammar Checker to download queue');
      end;
      
      // Download language detection models if selected
      if WizardIsComponentSelected('plugins\languagedetection') then
      begin
        // E5 model
        if WizardIsComponentSelected('plugins\languagedetection\e5') then
        begin
          DownloadPage.Add('{#LangDetectionE5ModelURL}', 'multilingual-e5-small.onnx', '');
          Log('Added E5 multilingual model to download queue');
        end;
        
        // Hugging Face model
        if WizardIsComponentSelected('plugins\languagedetection\huggingface') then
        begin
          DownloadPage.Add('{#LangDetectionModelURL}', 'model_quantized.onnx', '');
          DownloadPage.Add('{#LangDetectionTokenizerURL}', 'tokenizer.json', '');
          DownloadPage.Add('{#LangDetectionConfigURL}', 'config.json', '');
          DownloadPage.Add('{#LangDetectionTokenizerConfigURL}', 'tokenizer_config.json', '');
          DownloadPage.Add('{#LangDetectionSpecialTokensURL}', 'special_tokens_map.json', '');
          DownloadPage.Add('{#LangDetectionSentencePieceURL}', 'sentencepiece.bpe.model', '');
          Log('Added Hugging Face language detection model files to download queue');
        end;
      end;
      
      // Start the download
      begin
        DownloadPage.Show;
        try
          try
            // Download the files
            DownloadPage.Download;
            
            // Copy files to their respective directories
            // OCR language files
            if WizardIsComponentSelected('plugins\ocr') then
            begin
              // Copy OCR language files based on selected components
              if FileExists(ExpandConstant('{tmp}\eng.traineddata')) then
                CopyFile(ExpandConstant('{tmp}\eng.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\eng.traineddata'), False);
                
              // Only copy language files if the specific language component is selected
              if WizardIsComponentSelected('plugins\ocr\arabic') and FileExists(ExpandConstant('{tmp}\ara.traineddata')) then
                CopyFile(ExpandConstant('{tmp}\ara.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\ara.traineddata'), False);
                
              if WizardIsComponentSelected('plugins\ocr\french') and FileExists(ExpandConstant('{tmp}\fra.traineddata')) then
                CopyFile(ExpandConstant('{tmp}\fra.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\fra.traineddata'), False);
                
              if WizardIsComponentSelected('plugins\ocr\german') and FileExists(ExpandConstant('{tmp}\deu.traineddata')) then
                CopyFile(ExpandConstant('{tmp}\deu.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\deu.traineddata'), False);
                
              if WizardIsComponentSelected('plugins\ocr\spanish') and FileExists(ExpandConstant('{tmp}\spa.traineddata')) then
                CopyFile(ExpandConstant('{tmp}\spa.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\spa.traineddata'), False);
            end;
            
            // Copy the E5 multilingual model to plugin-specific directories
            if FileExists(ExpandConstant('{tmp}\multilingual-e5-small.onnx')) then
            begin
              // Copy to Grammar Checker Models directory if selected
              if WizardIsComponentSelected('plugins\grammarcheck') then
              begin
                CopyFile(ExpandConstant('{tmp}\multilingual-e5-small.onnx'), ExpandConstant('{app}\Plugins\GrammarChecker\Models\multilingual-e5-small.onnx'), False);
                Log('Copied E5 model to Grammar Checker');
              end;
              
              // Copy to Language Detection Models directory if selected
              if WizardIsComponentSelected('plugins\languagedetection\e5') then
              begin
                CopyFile(ExpandConstant('{tmp}\multilingual-e5-small.onnx'), ExpandConstant('{app}\Plugins\LanguageDetection\Models\multilingual-e5-small.onnx'), False);
                Log('Copied E5 model to Language Detection');
              end;
            end;
            
            // Copy Hugging Face language detection model files if selected
            if WizardIsComponentSelected('plugins\languagedetection\huggingface') then
            begin
              // Copy model_quantized.onnx
              if FileExists(ExpandConstant('{tmp}\model_quantized.onnx')) then
              begin
                CopyFile(ExpandConstant('{tmp}\model_quantized.onnx'), 
                         ExpandConstant('{app}\Plugins\LanguageDetection\Models\model_quantized.onnx'), 
                         False);
                Log('Copied Hugging Face model_quantized.onnx');
              end;
              
              // Copy tokenizer.json
              if FileExists(ExpandConstant('{tmp}\tokenizer.json')) then
              begin
                CopyFile(ExpandConstant('{tmp}\tokenizer.json'), 
                         ExpandConstant('{app}\Plugins\LanguageDetection\Models\tokenizer.json'), 
                         False);
                Log('Copied Hugging Face tokenizer.json');
              end;
              
              // Copy config.json
              if FileExists(ExpandConstant('{tmp}\config.json')) then
              begin
                CopyFile(ExpandConstant('{tmp}\config.json'), 
                         ExpandConstant('{app}\Plugins\LanguageDetection\Models\config.json'), 
                         False);
                Log('Copied Hugging Face config.json');
              end;
              
              // Copy tokenizer_config.json
              if FileExists(ExpandConstant('{tmp}\tokenizer_config.json')) then
              begin
                CopyFile(ExpandConstant('{tmp}\tokenizer_config.json'), 
                         ExpandConstant('{app}\Plugins\LanguageDetection\Models\tokenizer_config.json'), 
                         False);
                Log('Copied Hugging Face tokenizer_config.json');
              end;
              
              // Copy special_tokens_map.json
              if FileExists(ExpandConstant('{tmp}\special_tokens_map.json')) then
              begin
                CopyFile(ExpandConstant('{tmp}\special_tokens_map.json'), 
                         ExpandConstant('{app}\Plugins\LanguageDetection\Models\special_tokens_map.json'), 
                         False);
                Log('Copied Hugging Face special_tokens_map.json');
              end;
              
              // Copy sentencepiece.bpe.model
              if FileExists(ExpandConstant('{tmp}\sentencepiece.bpe.model')) then
              begin
                CopyFile(ExpandConstant('{tmp}\sentencepiece.bpe.model'), 
                         ExpandConstant('{app}\Plugins\LanguageDetection\Models\sentencepiece.bpe.model'), 
                         False);
                Log('Copied Hugging Face sentencepiece.bpe.model');
              end;
            end;
            
            // Log simple message for debugging
            Log('Finished copying model files');
              
            OnnxModelDownloaded := True;
          except
            // Handle download error
            OnnxModelDownloaded := False;
            Log('Error downloading AI models: ' + GetExceptionMessage);
            MsgBox('There was an error downloading the AI models. Some features may not work properly.\n\nPlease ensure you have an active internet connection and try reinstalling the application.', mbError, MB_OK);
          end;
        finally
          DownloadPage.Hide;
        end;
      end;
    end;
    
    // Only proceed if OCR plugin is selected
    if WizardIsComponentSelected('plugins\ocr') then
    begin
      // Create download page
      DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), 'Downloading OCR language files...', nil);
      
      // Create only one OCR model directory - the plugin-specific one
      OcrModelsDir := ExpandConstant('{app}\Plugins\OCR\Models');
      if not DirExists(OcrModelsDir) then
      begin
        if not ForceDirectories(OcrModelsDir) then
        begin
          MsgBox('Failed to create OCR models directory. OCR language files may not be installed correctly.', mbError, MB_OK);
          Exit;
        end;
      end;
      
      // Clear download page
      DownloadPage.Clear;
      
      // ONNX Vision models section removed
      
      // Add language files to download based on selection
      // English is always required if OCR is selected
      if WizardIsComponentSelected('plugins\ocr\english') then
        DownloadPage.Add('https://drive.google.com/uc?export=download&id=1X3oKEDsfYBCfPL8CgMZOM7oFwzppeMI6', 'eng.traineddata', '');
      
      if WizardIsComponentSelected('plugins\ocr\arabic') then
        DownloadPage.Add('https://drive.google.com/uc?export=download&id=1cLcCsGqD5Db5TcD9mgkr8VpqByuL-9KS', 'ara.traineddata', '');
      
      if WizardIsComponentSelected('plugins\ocr\french') then
        DownloadPage.Add('https://drive.google.com/uc?export=download&id=1hdyxRC5zeVYH3IcNOHh-AjtJsc_gpXfk', 'fra.traineddata', '');
      
      if WizardIsComponentSelected('plugins\ocr\german') then
        DownloadPage.Add('https://drive.google.com/uc?export=download&id=1w26gZktKkGOl1m76RpHYvmPnSmVzUZwX', 'deu.traineddata', '');
      
      if WizardIsComponentSelected('plugins\ocr\spanish') then
        DownloadPage.Add('https://drive.google.com/uc?export=download&id=1MmB5gYX_AaxsyaNBvT-gr-IsA-A-zjfv', 'spa.traineddata', '');
      
      // Show download page and download files
      DownloadPage.Show;
      try
        try
          // Download the files
          DownloadPage.Download;
          
          // Copy ONNX Vision models if selected
          if WizardIsComponentSelected('plugins\ocr\engines\onnx') then
          begin
            if FileExists(ExpandConstant('{tmp}\EasyOCRDetector.onnx')) then
            begin
              FileCopy(ExpandConstant('{tmp}\EasyOCRDetector.onnx'), ExpandConstant('{app}\Plugins\OCR\Models\EasyOCRDetector.onnx'), False);
              Log('Copied ONNX Vision detector model');
            end;
            
            if FileExists(ExpandConstant('{tmp}\EasyOCRRecognizer.onnx')) then
            begin
              FileCopy(ExpandConstant('{tmp}\EasyOCRRecognizer.onnx'), ExpandConstant('{app}\Plugins\OCR\Models\EasyOCRRecognizer.onnx'), False);
              Log('Copied ONNX Vision recognizer model');
            end;
          end;
          
          // Copy English language file if selected
          if WizardIsComponentSelected('plugins\ocr\english') then
          begin
            if FileExists(ExpandConstant('{tmp}\eng.traineddata')) then
            begin
              FileCopy(ExpandConstant('{tmp}\eng.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\eng.traineddata'), False);
              Log('Copied English OCR language file');
            end;
          end;
          
          // Copy Arabic language file if selected
          if WizardIsComponentSelected('plugins\ocr\arabic') then
          begin
            if FileExists(ExpandConstant('{tmp}\ara.traineddata')) then
            begin
              FileCopy(ExpandConstant('{tmp}\ara.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\ara.traineddata'), False);
              Log('Copied Arabic OCR language file');
            end;
          end;
          
          // Copy French language file if selected
          if WizardIsComponentSelected('plugins\ocr\french') then
          begin
            if FileExists(ExpandConstant('{tmp}\fra.traineddata')) then
            begin
              FileCopy(ExpandConstant('{tmp}\fra.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\fra.traineddata'), False);
              Log('Copied French OCR language file');
            end;
          end;
          
          // Copy German language file if selected
          if WizardIsComponentSelected('plugins\ocr\german') then
          begin
            if FileExists(ExpandConstant('{tmp}\deu.traineddata')) then
            begin
              FileCopy(ExpandConstant('{tmp}\deu.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\deu.traineddata'), False);
              Log('Copied German OCR language file');
            end;
          end;
          
          // Copy Spanish language file if selected
          if WizardIsComponentSelected('plugins\ocr\spanish') then
          begin
            if FileExists(ExpandConstant('{tmp}\spa.traineddata')) then
            begin
              FileCopy(ExpandConstant('{tmp}\spa.traineddata'), ExpandConstant('{app}\Plugins\OCR\Models\spa.traineddata'), False);
              Log('Copied Spanish OCR language file');
            end;
          end;
        except
          MsgBox('Error downloading OCR language files: ' + GetExceptionMessage, mbError, MB_OK);
        end;
      finally
        DownloadPage.Hide;
      end;
    end;
  end;
end;
