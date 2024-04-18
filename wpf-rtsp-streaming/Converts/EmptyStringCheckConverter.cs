using System;
using System.Globalization;
using System.Windows.Data;

namespace wpf_rtsp_streaming.Converts
{
    public partial class EmptyStringCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.IsNullOrEmpty(value as string);
        }
    }
}