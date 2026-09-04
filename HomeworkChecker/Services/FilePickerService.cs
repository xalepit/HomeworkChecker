using Microsoft.Win32;

using HomeworkChecker.UI.Resources;

namespace HomeworkChecker.UI.Services
{
    /// <summary>
    /// 使用 Windows 文件选择对话框实现文件选择服务。
    /// </summary>
    public sealed class FilePickerService : IFilePickerService
    {
        /// <summary>
        /// 显示只允许选择单个现有可执行文件的对话框。
        /// </summary>
        /// <param name="title">对话框标题。</param>
        /// <returns>用户选择的完整路径；取消选择时返回 <see langword="null"/>。</returns>
        public string? SelectExecutableFile(string title)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                CheckFileExists = true,
                Multiselect = false,
                Filter = $"{Translations.FilePicker_ExecutableFiles} (*.exe)|*.exe|{Translations.FilePicker_AllFiles} (*.*)|*.*"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>
        /// 显示只允许选择单个现有 txt 或 dat 文件的对话框。
        /// </summary>
        /// <param name="title">对话框标题。</param>
        /// <returns>用户选择的完整路径；取消选择时返回 <see langword="null"/>。</returns>
        public string? SelectTestDataFile(string title)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                CheckFileExists = true,
                Multiselect = false,
                Filter = $"{Translations.FilePicker_TestDataFiles} (*.txt;*.dat)|*.txt;*.dat|{Translations.FilePicker_AllFiles} (*.*)|*.*"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
