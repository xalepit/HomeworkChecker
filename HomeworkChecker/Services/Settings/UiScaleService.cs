using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using HomeworkChecker.UI.Models.Settings;

namespace HomeworkChecker.UI.Services.Settings
{
    public class UiScaleService : IUiScaleService
    {
        public void Apply(UiScalePreference preference)
        {
            var scale = preference switch
            {
                UiScalePreference.Percent100 => 1.00,
                UiScalePreference.Percent125 => 1.25,
                UiScalePreference.Percent150 => 1.50,
                UiScalePreference.Percent175 => 1.75,
                UiScalePreference.Percent200 => 2.00,
                _ => 1.00 // System 先按 100%，后续可扩展系统 DPI 读取
            };

            // 对当前窗口根元素应用缩放
            foreach (var window in Application.Current.Windows.OfType<Window>())
            {
                if (window.Content is FrameworkElement root)
                {
                    root.LayoutTransform = new ScaleTransform(scale, scale);
                }
            }
        }
    }
}
