using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using HomeworkChecker.UI.ViewModels.Dialogs;

namespace HomeworkChecker.UI.Views.Dialogs
{
    /// <summary>
    /// 提供通过色谱面板、色相滑块和文本字段选择颜色的界面。
    /// </summary>
    public partial class AccentColorPickerView
    {
        public AccentColorPickerViewModel ViewModel { get; }

        /// <summary>
        /// 创建调色盘并连接指定状态模型。
        /// </summary>
        /// <param name="viewModel">调色盘状态模型。</param>
        public AccentColorPickerView(AccentColorPickerViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// 鼠标按下时捕获指针并立即更新色谱位置。
        /// </summary>
        /// <param name="sender">色谱画布。</param>
        /// <param name="e">鼠标事件参数。</param>
        private void OnSpectrumMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SpectrumCanvas.CaptureMouse();
            UpdateColorFromPointer(e);
        }

        /// <summary>
        /// 拖动鼠标时连续更新色谱位置。
        /// </summary>
        /// <param name="sender">色谱画布。</param>
        /// <param name="e">鼠标事件参数。</param>
        private void OnSpectrumMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && SpectrumCanvas.IsMouseCaptured)
            {
                UpdateColorFromPointer(e);
            }
        }

        /// <summary>
        /// 鼠标释放时结束色谱拖动。
        /// </summary>
        /// <param name="sender">色谱画布。</param>
        /// <param name="e">鼠标事件参数。</param>
        private void OnSpectrumMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UpdateColorFromPointer(e);
            SpectrumCanvas.ReleaseMouseCapture();
        }

        /// <summary>
        /// 色谱尺寸变化后重新定位选择指示器。
        /// </summary>
        /// <param name="sender">色谱边框。</param>
        /// <param name="e">尺寸变化参数。</param>
        private void OnSpectrumSizeChanged(object sender, SizeChangedEventArgs e) => UpdateIndicatorPosition();

        /// <summary>
        /// HSV 状态变化后同步色谱选择指示器位置。
        /// </summary>
        /// <param name="sender">属性变化发送者。</param>
        /// <param name="e">属性变化参数。</param>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AccentColorPickerViewModel.Saturation) or nameof(AccentColorPickerViewModel.Brightness))
            {
                UpdateIndicatorPosition();
            }
        }

        /// <summary>
        /// 控件卸载时解除事件订阅。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Unloaded -= OnUnloaded;
        }

        /// <summary>
        /// 将鼠标位置换算为饱和度和明度。
        /// </summary>
        /// <param name="e">包含色谱相对坐标的鼠标事件。</param>
        private void UpdateColorFromPointer(MouseEventArgs e)
        {
            if (SpectrumCanvas.ActualWidth <= 0 || SpectrumCanvas.ActualHeight <= 0)
            {
                return;
            }

            var position = e.GetPosition(SpectrumCanvas);
            var saturation = Math.Clamp(position.X / SpectrumCanvas.ActualWidth, 0, 1);
            var brightness = 1 - Math.Clamp(position.Y / SpectrumCanvas.ActualHeight, 0, 1);
            ViewModel.SetSaturationAndBrightness(saturation, brightness);
        }

        /// <summary>
        /// 根据当前饱和度和明度定位色谱选择指示器。
        /// </summary>
        private void UpdateIndicatorPosition()
        {
            if (SpectrumCanvas.ActualWidth <= 0 || SpectrumCanvas.ActualHeight <= 0)
            {
                return;
            }

            var left = ViewModel.Saturation * SpectrumCanvas.ActualWidth - SpectrumIndicator.Width / 2;
            var top = (1 - ViewModel.Brightness) * SpectrumCanvas.ActualHeight - SpectrumIndicator.Height / 2;
            Canvas.SetLeft(SpectrumIndicator, left);
            Canvas.SetTop(SpectrumIndicator, top);
        }
    }
}
