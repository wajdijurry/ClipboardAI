using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClipboardAI.Plugins;
using ClipboardAI.Common;

namespace ClipboardAI.Plugin.TableConversion
{
    /// <summary>
    /// Plugin that provides table conversion functionality
    /// </summary>
    public class TableConversionPlugin : FeaturePluginBase, IRefreshablePlugin
    {
        private Dictionary<string, object> _settings;
        
        /// <summary>
        /// Gets the unique identifier for the plugin
        /// </summary>
        public override string Id => "TableConversion";
        
        /// <summary>
        /// Gets the display name of the plugin
        /// </summary>
        public override string Name => "Table Conversion";
        
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
        public override string Description => "Converts between different table formats (CSV, Markdown, HTML)";
        
        /// <summary>
        /// Gets the feature type of this plugin
        /// </summary>
        public override AIFeatureType FeatureType => AIFeatureType.TableConversion;
        
        /// <summary>
        /// Gets the unique identifier for the feature provided by this plugin
        /// </summary>
        public override string FeatureId => "TableConversion";
        
        /// <summary>
        /// Gets the display name of the feature
        /// </summary>
        public override string FeatureName => "Table Conversion";
        
        /// <summary>
        /// Gets the menu option for this plugin to be displayed in the UI
        /// </summary>
        /// <returns>A MenuOption object containing the display information for this plugin</returns>
        public override MenuOption GetMenuOption()
        {
            return new MenuOption("📊", "Convert Table", AIFeatureType.TableConversion);
        }
        
        public TableConversionPlugin()
        {
            _settings = new Dictionary<string, object>
            {
                { "DefaultOutputFormat", "markdown" },
                { "DetectInputFormat", true },
                { "IncludeHeaders", true },
                { "TrimWhitespace", true }
            };
        }
        
        /// <summary>
        /// Process text by converting table formats
        /// </summary>
        /// <param name="text">Table text in CSV, Markdown, or HTML format</param>
        /// <param name="options">Optional parameters for conversion</param>
        /// <returns>Converted table</returns>
        public override async Task<string> ProcessTextAsync(string text, object options = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            
            try
            {
                Host?.LogMessage(Id, LogLevel.Information, "Converting table format");
                
                // Get settings
                string outputFormat = GetSettingValue<string>("DefaultOutputFormat", "markdown");
                bool detectInputFormat = GetSettingValue<bool>("DetectInputFormat", true);
                bool includeHeaders = GetSettingValue<bool>("IncludeHeaders", true);
                bool trimWhitespace = GetSettingValue<bool>("TrimWhitespace", true);
                
                // Override with options if provided
                if (options is Dictionary<string, object> optionsDict)
                {
                    if (optionsDict.TryGetValue("outputFormat", out object formatObj) && formatObj is string format)
                    {
                        outputFormat = format.ToLower();
                    }
                }
                
                // Detect input format if enabled
                string inputFormat = "csv"; // Default
                if (detectInputFormat)
                {
                    inputFormat = DetectTableFormat(text);
                    Host?.LogMessage(Id, LogLevel.Information, $"Detected input format: {inputFormat}");
                }
                
                // Parse the table based on input format
                List<List<string>> tableData = ParseTable(text, inputFormat, trimWhitespace);
                
                // Convert to the output format
                string result = ConvertTable(tableData, outputFormat, includeHeaders);
                
                return result;
            }
            catch (Exception ex)
            {
                Host?.LogMessage(Id, LogLevel.Error, $"Error converting table: {ex.Message}");
                return $"Error converting table: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Process image (not supported for table conversion)
        /// </summary>
        public override async Task<string> ProcessImageAsync(BitmapSource image, object options = null)
        {
            // Table conversion doesn't process images directly
            // In a real implementation, we might use OCR to extract tables from images
            return "Error: Table conversion does not directly support image processing. Use OCR first to extract text.";
        }
        
        /// <summary>
        /// Check if this feature supports a specific content type
        /// </summary>
        public override bool SupportsContentType(ContentType contentType)
        {
            return contentType == ContentType.Text || contentType == ContentType.Table;
        }
        
        /// <summary>
        /// Get the plugin settings
        /// </summary>
        public override Dictionary<string, object> GetSettings()
        {
            return _settings;
        }
        
        /// <summary>
        /// Update the plugin settings
        /// </summary>
        public override void UpdateSettings(Dictionary<string, object> settings)
        {
            foreach (var setting in settings)
            {
                if (_settings.ContainsKey(setting.Key))
                {
                    _settings[setting.Key] = setting.Value;
                }
            }
        }
        
        /// <summary>
        /// Detect the format of a table from its text
        /// </summary>
        private string DetectTableFormat(string text)
        {
            // Check for Markdown table format
            if (text.Contains("|") && text.Contains("---"))
            {
                return "markdown";
            }
            
            // Check for HTML table format
            if (text.Contains("<table") && text.Contains("<tr") && (text.Contains("<td") || text.Contains("<th")))
            {
                return "html";
            }
            
            // Default to CSV
            return "csv";
        }
        
        /// <summary>
        /// Parse a table from text based on its format
        /// </summary>
        private List<List<string>> ParseTable(string text, string format, bool trimWhitespace)
        {
            List<List<string>> tableData = new List<List<string>>();
            
            switch (format.ToLower())
            {
                case "csv":
                    tableData = ParseCsv(text, trimWhitespace);
                    break;
                case "markdown":
                    tableData = ParseMarkdown(text, trimWhitespace);
                    break;
                case "html":
                    tableData = ParseHtml(text, trimWhitespace);
                    break;
                default:
                    throw new ArgumentException($"Unsupported table format: {format}");
            }
            
            return tableData;
        }
        
        /// <summary>
        /// Parse CSV text into a table
        /// </summary>
        private List<List<string>> ParseCsv(string text, bool trimWhitespace)
        {
            List<List<string>> tableData = new List<List<string>>();
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                List<string> row = new List<string>();
                
                // Simple CSV parsing (doesn't handle quoted commas properly)
                string[] cells = line.Split(',');
                foreach (string cell in cells)
                {
                    row.Add(trimWhitespace ? cell.Trim() : cell);
                }
                
                tableData.Add(row);
            }
            
            return tableData;
        }
        
        /// <summary>
        /// Parse Markdown table text into a table
        /// </summary>
        private List<List<string>> ParseMarkdown(string text, bool trimWhitespace)
        {
            List<List<string>> tableData = new List<List<string>>();
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                // Skip separator lines
                if (line.Replace(" ", "").Replace("|", "").Replace("-", "").Replace(":", "").Length == 0)
                {
                    continue;
                }
                
                List<string> row = new List<string>();
                string[] cells = line.Split('|');
                
                // Skip first and last empty cells from outer pipes
                for (int i = 1; i < cells.Length - 1; i++)
                {
                    row.Add(trimWhitespace ? cells[i].Trim() : cells[i]);
                }
                
                // If there are no outer pipes, use all cells
                if (row.Count == 0 && cells.Length > 0)
                {
                    foreach (string cell in cells)
                    {
                        row.Add(trimWhitespace ? cell.Trim() : cell);
                    }
                }
                
                tableData.Add(row);
            }
            
            return tableData;
        }
        
        /// <summary>
        /// Parse HTML table text into a table
        /// </summary>
        private List<List<string>> ParseHtml(string text, bool trimWhitespace)
        {
            List<List<string>> tableData = new List<List<string>>();
            
            // Very simple HTML parsing (not robust)
            Regex rowRegex = new Regex(@"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline);
            Regex cellRegex = new Regex(@"<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline);
            
            MatchCollection rowMatches = rowRegex.Matches(text);
            foreach (Match rowMatch in rowMatches)
            {
                List<string> row = new List<string>();
                MatchCollection cellMatches = cellRegex.Matches(rowMatch.Groups[1].Value);
                
                foreach (Match cellMatch in cellMatches)
                {
                    string cellContent = cellMatch.Groups[1].Value;
                    // Remove HTML tags
                    cellContent = Regex.Replace(cellContent, @"<[^>]+>", "");
                    row.Add(trimWhitespace ? cellContent.Trim() : cellContent);
                }
                
                tableData.Add(row);
            }
            
            return tableData;
        }
        
        /// <summary>
        /// Convert table data to the specified format
        /// </summary>
        private string ConvertTable(List<List<string>> tableData, string outputFormat, bool includeHeaders)
        {
            if (tableData.Count == 0)
            {
                return "No table data found";
            }
            
            switch (outputFormat.ToLower())
            {
                case "csv":
                    return ConvertToCsv(tableData);
                case "markdown":
                    return ConvertToMarkdown(tableData, includeHeaders);
                case "html":
                    return ConvertToHtml(tableData, includeHeaders);
                default:
                    throw new ArgumentException($"Unsupported output format: {outputFormat}");
            }
        }
        
        /// <summary>
        /// Convert table data to CSV format
        /// </summary>
        private string ConvertToCsv(List<List<string>> tableData)
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (List<string> row in tableData)
            {
                for (int i = 0; i < row.Count; i++)
                {
                    // Escape commas and quotes
                    string cell = row[i];
                    if (cell.Contains(",") || cell.Contains("\""))
                    {
                        cell = "\"" + cell.Replace("\"", "\"\"") + "\"";
                    }
                    
                    sb.Append(cell);
                    
                    if (i < row.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Convert table data to Markdown format
        /// </summary>
        private string ConvertToMarkdown(List<List<string>> tableData, bool includeHeaders)
        {
            if (tableData.Count == 0)
            {
                return "";
            }
            
            StringBuilder sb = new StringBuilder();
            
            // Find the maximum number of columns
            int maxColumns = 0;
            foreach (List<string> row in tableData)
            {
                maxColumns = Math.Max(maxColumns, row.Count);
            }
            
            // Add header row
            for (int i = 0; i < maxColumns; i++)
            {
                sb.Append("| ");
                
                if (tableData[0].Count > i)
                {
                    sb.Append(tableData[0][i]);
                }
                
                sb.Append(" ");
            }
            sb.AppendLine("|");
            
            // Add separator row
            for (int i = 0; i < maxColumns; i++)
            {
                sb.Append("| --- ");
            }
            sb.AppendLine("|");
            
            // Add data rows
            int startRow = includeHeaders ? 1 : 0;
            for (int rowIndex = startRow; rowIndex < tableData.Count; rowIndex++)
            {
                List<string> row = tableData[rowIndex];
                
                for (int i = 0; i < maxColumns; i++)
                {
                    sb.Append("| ");
                    
                    if (row.Count > i)
                    {
                        sb.Append(row[i]);
                    }
                    
                    sb.Append(" ");
                }
                sb.AppendLine("|");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Convert table data to HTML format
        /// </summary>
        private string ConvertToHtml(List<List<string>> tableData, bool includeHeaders)
        {
            if (tableData.Count == 0)
            {
                return "";
            }
            
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("<table>");
            
            // Add header row
            if (includeHeaders && tableData.Count > 0)
            {
                sb.AppendLine("  <thead>");
                sb.AppendLine("    <tr>");
                
                foreach (string cell in tableData[0])
                {
                    sb.AppendLine($"      <th>{cell}</th>");
                }
                
                sb.AppendLine("    </tr>");
                sb.AppendLine("  </thead>");
            }
            
            // Add data rows
            sb.AppendLine("  <tbody>");
            
            int startRow = includeHeaders ? 1 : 0;
            for (int rowIndex = startRow; rowIndex < tableData.Count; rowIndex++)
            {
                sb.AppendLine("    <tr>");
                
                foreach (string cell in tableData[rowIndex])
                {
                    sb.AppendLine($"      <td>{cell}</td>");
                }
                
                sb.AppendLine("    </tr>");
            }
            
            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get a setting value with a default if not found or wrong type
        /// </summary>
        private T GetSettingValue<T>(string key, T defaultValue)
        {
            if (_settings.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Refreshes plugin settings from application settings
        /// </summary>
        public override void RefreshFromAppSettings()
        {
            var settings = GetAppSettings();
            if (settings != null)
            {
                // Get table conversion settings
                string defaultOutputFormat = settings.GetPluginSetting<string>(Id, "DefaultOutputFormat", "Markdown");
                bool preserveHeaderFormatting = settings.GetPluginSetting<bool>(Id, "PreserveHeaderFormatting", true);
                bool autoDetectDelimiter = settings.GetPluginSetting<bool>(Id, "AutoDetectDelimiter", true);
                
                // Update internal settings
                _settings["DefaultOutputFormat"] = defaultOutputFormat;
                _settings["PreserveHeaderFormatting"] = preserveHeaderFormatting;
                _settings["AutoDetectDelimiter"] = autoDetectDelimiter;
            }
        }
        
        /// <summary>
        /// Creates a UI control containing the plugin's settings
        /// </summary>
        /// <returns>A WPF control containing the plugin's settings UI</returns>
        public override System.Windows.FrameworkElement CreateSettingsControl()
        {
            // Get the base settings panel with the enabled checkbox
            var panel = (System.Windows.Controls.StackPanel)base.CreateSettingsControl();
            
            // Add default output format setting
            var formatLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Default Output Format:",
                Margin = new System.Windows.Thickness(0, 10, 0, 5)
            };
            panel.Children.Add(formatLabel);
            
            // Create radio buttons for different output formats
            var formatPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Margin = new System.Windows.Thickness(10, 0, 0, 10)
            };
            
            string defaultFormat = _settings.ContainsKey("DefaultOutputFormat") ? 
                _settings["DefaultOutputFormat"].ToString() : "Markdown";
            
            var markdownRadio = new System.Windows.Controls.RadioButton
            {
                Content = "Markdown",
                Tag = "Markdown",
                Margin = new System.Windows.Thickness(0, 2, 0, 2),
                IsChecked = (defaultFormat == "Markdown"),
                GroupName = "OutputFormat"
            };
            formatPanel.Children.Add(markdownRadio);
            
            var csvRadio = new System.Windows.Controls.RadioButton
            {
                Content = "CSV",
                Tag = "CSV",
                Margin = new System.Windows.Thickness(0, 2, 0, 2),
                IsChecked = (defaultFormat == "CSV"),
                GroupName = "OutputFormat"
            };
            formatPanel.Children.Add(csvRadio);
            
            var htmlRadio = new System.Windows.Controls.RadioButton
            {
                Content = "HTML",
                Tag = "HTML",
                Margin = new System.Windows.Thickness(0, 2, 0, 2),
                IsChecked = (defaultFormat == "HTML"),
                GroupName = "OutputFormat"
            };
            formatPanel.Children.Add(htmlRadio);
            
            panel.Children.Add(formatPanel);
            
            // Add checkbox for preserving header formatting
            var preserveHeaderCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Preserve Header Formatting",
                IsChecked = _settings.ContainsKey("PreserveHeaderFormatting") ? 
                    (bool)_settings["PreserveHeaderFormatting"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "PreserveHeaderFormatting"
            };
            panel.Children.Add(preserveHeaderCheckBox);
            
            // Add checkbox for auto-detecting delimiter
            var autoDetectCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "Auto-detect Delimiter",
                IsChecked = _settings.ContainsKey("AutoDetectDelimiter") ? 
                    (bool)_settings["AutoDetectDelimiter"] : true,
                Margin = new System.Windows.Thickness(0, 5, 0, 5),
                Tag = "AutoDetectDelimiter"
            };
            panel.Children.Add(autoDetectCheckBox);
            
            // Add description for the auto-detect delimiter option
            var descriptionText = new System.Windows.Controls.TextBlock
            {
                Text = "When enabled, the plugin will try to automatically detect the delimiter in CSV or tabular data. If disabled, it will default to comma.",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(10, 0, 0, 10),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
            panel.Children.Add(descriptionText);
            
            return panel;
        }
        
        /// <summary>
        /// Saves the settings from the provided control
        /// </summary>
        /// <param name="control">The settings control previously created by CreateSettingsControl</param>
        /// <returns>True if settings were saved successfully</returns>
        public override bool SaveSettings(System.Windows.FrameworkElement control)
        {
            // First save the base settings (enabled state)
            if (!base.SaveSettings(control))
                return false;
            
            try
            {
                if (control is System.Windows.Controls.StackPanel panel)
                {
                    var settings = GetAppSettings();
                    if (settings == null)
                        return false;
                    
                    // Process each control in the panel
                    foreach (var child in panel.Children)
                    {
                        // Handle output format radio buttons panel
                        if (child is System.Windows.Controls.StackPanel formatPanel && 
                            formatPanel.Orientation == System.Windows.Controls.Orientation.Vertical)
                        {
                            foreach (var radioButton in formatPanel.Children)
                            {
                                if (radioButton is System.Windows.Controls.RadioButton rb && 
                                    rb.IsChecked == true && 
                                    rb.Tag != null)
                                {
                                    string outputFormat = rb.Tag.ToString();
                                    _settings["DefaultOutputFormat"] = outputFormat;
                                    settings.SetPluginSetting(Id, "DefaultOutputFormat", outputFormat);
                                    break;
                                }
                            }
                        }
                        // Handle preserve header formatting checkbox
                        else if (child is System.Windows.Controls.CheckBox preserveHeaderCheckBox && 
                                preserveHeaderCheckBox.Tag?.ToString() == "PreserveHeaderFormatting")
                        {
                            bool preserveHeaderFormatting = preserveHeaderCheckBox.IsChecked ?? true;
                            _settings["PreserveHeaderFormatting"] = preserveHeaderFormatting;
                            settings.SetPluginSetting(Id, "PreserveHeaderFormatting", preserveHeaderFormatting);
                        }
                        // Handle auto-detect delimiter checkbox
                        else if (child is System.Windows.Controls.CheckBox autoDetectCheckBox && 
                                autoDetectCheckBox.Tag?.ToString() == "AutoDetectDelimiter")
                        {
                            bool autoDetectDelimiter = autoDetectCheckBox.IsChecked ?? true;
                            _settings["AutoDetectDelimiter"] = autoDetectDelimiter;
                            settings.SetPluginSetting(Id, "AutoDetectDelimiter", autoDetectDelimiter);
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving table conversion settings: {ex.Message}");
                return false;
            }
        }
    }
}
