using System.Windows.Media;
using HomeworkChecker.UI.Helpers;

namespace HomeworkChecker.UI.ViewModels.Dialogs
{
    /// <summary>
    /// 管理主题色调色盘的 HSV、RGB、十六进制颜色及新旧颜色预览状态。
    /// </summary>
    public partial class AccentColorPickerViewModel : ObservableObject
    {
        private bool _isSynchronizing;

        [ObservableProperty]
        private double _hue;

        [ObservableProperty]
        private double _saturation;

        [ObservableProperty]
        private double _brightness;

        [ObservableProperty]
        private int _red;

        [ObservableProperty]
        private int _green;

        [ObservableProperty]
        private int _blue;

        [ObservableProperty]
        private string _hexColor = "#0067C0";

        [ObservableProperty]
        private SolidColorBrush _selectedColorBrush = new(Colors.Transparent);

        [ObservableProperty]
        private SolidColorBrush _originalColorBrush = new(Colors.Transparent);

        [ObservableProperty]
        private SolidColorBrush _spectrumBaseBrush = new(Colors.Red);

        /// <summary>
        /// 使用已有颜色初始化调色盘。
        /// </summary>
        /// <param name="initialHexColor">#RRGGBB 格式的初始颜色。</param>
        public AccentColorPickerViewModel(string initialHexColor)
        {
            if (!ColorHelper.TryParseHex(initialHexColor, out var color))
            {
                color = Color.FromRgb(0, 103, 192);
            }

            OriginalColorBrush = new SolidColorBrush(color);
            SynchronizeFromColor(color);
        }

        /// <summary>
        /// 根据色谱面板坐标更新饱和度和明度。
        /// </summary>
        /// <param name="saturation">从左到右递增的饱和度。</param>
        /// <param name="brightness">从下到上递增的明度。</param>
        public void SetSaturationAndBrightness(double saturation, double brightness)
        {
            _isSynchronizing = true;
            Saturation = Math.Clamp(saturation, 0, 1);
            Brightness = Math.Clamp(brightness, 0, 1);
            _isSynchronizing = false;
            SynchronizeFromHsv();
        }

        /// <summary>
        /// 色相变化后重新计算当前颜色。
        /// </summary>
        /// <param name="value">新的色相。</param>
        partial void OnHueChanged(double value)
        {
            if (!_isSynchronizing)
            {
                SynchronizeFromHsv();
            }
        }

        /// <summary>
        /// 红色通道变化后重新计算 HSV 和预览。
        /// </summary>
        /// <param name="value">新的红色通道值。</param>
        partial void OnRedChanged(int value) => SynchronizeFromRgbInput();

        /// <summary>
        /// 绿色通道变化后重新计算 HSV 和预览。
        /// </summary>
        /// <param name="value">新的绿色通道值。</param>
        partial void OnGreenChanged(int value) => SynchronizeFromRgbInput();

        /// <summary>
        /// 蓝色通道变化后重新计算 HSV 和预览。
        /// </summary>
        /// <param name="value">新的蓝色通道值。</param>
        partial void OnBlueChanged(int value) => SynchronizeFromRgbInput();

        /// <summary>
        /// 十六进制文本成为有效颜色后同步全部颜色分量。
        /// </summary>
        /// <param name="value">新的十六进制颜色文本。</param>
        partial void OnHexColorChanged(string value)
        {
            if (_isSynchronizing)
            {
                return;
            }

            if (ColorHelper.TryParseHex(value, out var color))
            {
                SynchronizeFromColor(color);
                return;
            }

            _isSynchronizing = true;
            HexColor = ColorHelper.ToHex(SelectedColorBrush.Color);
            _isSynchronizing = false;
        }

        /// <summary>
        /// 将当前 HSV 值转换为 RGB、HEX 和预览画刷。
        /// </summary>
        private void SynchronizeFromHsv()
        {
            var color = ColorHelper.FromHsv(Hue, Saturation, Brightness);
            SynchronizeDisplayValues(color);
        }

        /// <summary>
        /// 将 RGB 输入限制到有效范围后同步 HSV 和预览。
        /// </summary>
        private void SynchronizeFromRgbInput()
        {
            if (_isSynchronizing)
            {
                return;
            }

            var color = Color.FromRgb(
                (byte)Math.Clamp(Red, 0, 255),
                (byte)Math.Clamp(Green, 0, 255),
                (byte)Math.Clamp(Blue, 0, 255));
            SynchronizeFromColor(color);
        }

        /// <summary>
        /// 从 RGB 颜色重新计算 HSV，并同步所有显示字段。
        /// </summary>
        /// <param name="color">新的 RGB 颜色。</param>
        private void SynchronizeFromColor(Color color)
        {
            ColorHelper.ToHsv(color, out var hue, out var saturation, out var brightness);

            _isSynchronizing = true;
            Hue = hue;
            Saturation = saturation;
            Brightness = brightness;
            _isSynchronizing = false;
            SynchronizeDisplayValues(color);
        }

        /// <summary>
        /// 同步 RGB、HEX、色谱底色和颜色预览。
        /// </summary>
        /// <param name="color">当前选中颜色。</param>
        private void SynchronizeDisplayValues(Color color)
        {
            _isSynchronizing = true;
            Red = color.R;
            Green = color.G;
            Blue = color.B;
            HexColor = ColorHelper.ToHex(color);
            SelectedColorBrush = new SolidColorBrush(color);
            SpectrumBaseBrush = new SolidColorBrush(ColorHelper.FromHsv(Hue, 1, 1));
            _isSynchronizing = false;
        }
    }
}
