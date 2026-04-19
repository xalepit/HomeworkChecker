using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace HomeworkChecker.UI.Helpers
{
    internal class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || parameter is null)
                return false;

            string parameterString = parameter.ToString()!;

            return value.ToString()!.Equals(parameterString, StringComparison.Ordinal);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is null)
                return Binding.DoNothing;

            if (value is bool isChecked && isChecked)
            {
                return Enum.Parse(targetType, parameter.ToString()!);
            }

            return Binding.DoNothing;
        }
    }
}
