namespace HomeworkChecker.UI.Services
{
    /// <summary>
    /// 提供文件选择对话框，避免页面 ViewModel 直接依赖具体的 WPF 对话框类型。
    /// </summary>
    public interface IFilePickerService
    {
        /// <summary>
        /// 显示可执行文件选择对话框。
        /// </summary>
        /// <param name="title">对话框标题。</param>
        /// <returns>用户选择的完整路径；取消选择时返回 <see langword="null"/>。</returns>
        string? SelectExecutableFile(string title);

        /// <summary>
        /// 显示测试数据文件选择对话框。
        /// </summary>
        /// <param name="title">对话框标题。</param>
        /// <returns>用户选择的完整路径；取消选择时返回 <see langword="null"/>。</returns>
        string? SelectTestDataFile(string title);
    }
}
