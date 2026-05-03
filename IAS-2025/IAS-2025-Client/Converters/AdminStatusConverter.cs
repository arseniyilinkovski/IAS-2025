using System;
using System.Globalization;
using System.Windows.Data;

namespace IAS_2025_Client.Converters
{
    public class AdminStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isAdmin && isAdmin) ? "👑 Administrator" : "👤 User";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}