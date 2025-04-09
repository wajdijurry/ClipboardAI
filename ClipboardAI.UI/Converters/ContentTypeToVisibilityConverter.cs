using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Converts a ClipboardContentType to Visibility based on matching the parameter
    /// </summary>
    public class ContentTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ClipboardContentType contentType && parameter is string typeString)
            {
                // Special case for Image content type
                if (typeString == "Image")
                {
                    return contentType == ClipboardContentType.Image
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
                
                // Special case for Text content type (includes HTML and RichText)
                if (typeString == "Text")
                {
                    return (contentType == ClipboardContentType.Text ||
                            contentType == ClipboardContentType.Html ||
                            contentType == ClipboardContentType.RichText)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
                
                // Special case for File content type (includes FilePath and FileDrop)
                if (typeString == "File")
                {
                    return (contentType == ClipboardContentType.FilePath ||
                            contentType == ClipboardContentType.FileDrop)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
            
            return Visibility.Collapsed;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
