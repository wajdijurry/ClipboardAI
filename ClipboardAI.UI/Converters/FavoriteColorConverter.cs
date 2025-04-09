using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Converts a boolean IsFavorite value to a color for the favorite star
    /// </summary>
    public class FavoriteColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite && isFavorite)
            {
                // Gold color for favorites
                return new SolidColorBrush(Colors.Gold);
            }
            
            // Gray color for non-favorites
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
