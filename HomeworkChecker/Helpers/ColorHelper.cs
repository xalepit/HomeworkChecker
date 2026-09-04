using System.Globalization;
using System.Windows.Media;

namespace HomeworkChecker.UI.Helpers
{
    /// <summary>
    /// 提供主题色使用的十六进制与 HSV 颜色转换。
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        /// 将十六进制颜色文本解析为不透明 WPF 颜色。
        /// </summary>
        /// <param name="hexColor">#RRGGBB、RRGGBB、#AARRGGBB 或 AARRGGBB 格式的文本。</param>
        /// <param name="color">解析成功时得到的颜色。</param>
        /// <returns>格式和数值均有效时返回 <see langword="true"/>。</returns>
        public static bool TryParseHex(string? hexColor, out Color color)
        {
            color = Colors.Transparent;
            var value = hexColor?.Trim().TrimStart('#');

            if (value is null || (value.Length != 6 && value.Length != 8))
            {
                return false;
            }

            if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var numericColor))
            {
                return false;
            }

            if (value.Length == 8)
            {
                numericColor &= 0x00FFFFFF;
            }

            color = Color.FromRgb(
                (byte)(numericColor >> 16),
                (byte)(numericColor >> 8),
                (byte)numericColor);
            return true;
        }

        /// <summary>
        /// 将 WPF 颜色格式化为统一的大写 #RRGGBB 文本。
        /// </summary>
        /// <param name="color">待格式化颜色。</param>
        /// <returns>不包含透明度的十六进制颜色。</returns>
        public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        /// <summary>
        /// 将 HSV 分量转换为不透明 RGB 颜色。
        /// </summary>
        /// <param name="hue">色相，范围为 0 到 360。</param>
        /// <param name="saturation">饱和度，范围为 0 到 1。</param>
        /// <param name="value">明度，范围为 0 到 1。</param>
        /// <returns>转换后的 WPF 颜色。</returns>
        public static Color FromHsv(double hue, double saturation, double value)
        {
            hue = ((hue % 360) + 360) % 360;
            saturation = Math.Clamp(saturation, 0, 1);
            value = Math.Clamp(value, 0, 1);

            var chroma = value * saturation;
            var intermediate = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
            var match = value - chroma;

            var (red, green, blue) = hue switch
            {
                < 60 => (chroma, intermediate, 0d),
                < 120 => (intermediate, chroma, 0d),
                < 180 => (0d, chroma, intermediate),
                < 240 => (0d, intermediate, chroma),
                < 300 => (intermediate, 0d, chroma),
                _ => (chroma, 0d, intermediate)
            };

            return Color.FromRgb(
                (byte)Math.Round((red + match) * 255),
                (byte)Math.Round((green + match) * 255),
                (byte)Math.Round((blue + match) * 255));
        }

        /// <summary>
        /// 将 RGB 颜色转换为 HSV 分量。
        /// </summary>
        /// <param name="color">待转换颜色。</param>
        /// <param name="hue">转换后的色相，范围为 0 到 360。</param>
        /// <param name="saturation">转换后的饱和度，范围为 0 到 1。</param>
        /// <param name="value">转换后的明度，范围为 0 到 1。</param>
        public static void ToHsv(Color color, out double hue, out double saturation, out double value)
        {
            var red = color.R / 255d;
            var green = color.G / 255d;
            var blue = color.B / 255d;
            var maximum = Math.Max(red, Math.Max(green, blue));
            var minimum = Math.Min(red, Math.Min(green, blue));
            var delta = maximum - minimum;

            hue = delta switch
            {
                0 => 0,
                _ when maximum == red => 60 * (((green - blue) / delta) % 6),
                _ when maximum == green => 60 * ((blue - red) / delta + 2),
                _ => 60 * ((red - green) / delta + 4)
            };

            if (hue < 0)
            {
                hue += 360;
            }

            saturation = maximum == 0 ? 0 : delta / maximum;
            value = maximum;
        }
    }
}
