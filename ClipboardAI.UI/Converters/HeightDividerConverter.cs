using System;
using System.Globalization;
using System.Windows.Data;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Converter that divides a height value by a specified factor
    /// </summary>
    public class HeightDividerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), out double divisor) && divisor > 0)
                {
                    return height / divisor;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
