using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Eco_Matic;

namespace Eco_Matic.Utilities
{
    public class ImagePathConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? path = value as string;
            return ImageLoader.LoadProductImage(path ?? "");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
