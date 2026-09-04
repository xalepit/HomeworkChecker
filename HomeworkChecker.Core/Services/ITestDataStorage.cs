namespace HomeworkChecker.Core.Services
{
    /// <summary>
    /// 提供最后一次测试数据编辑快照的读取与保存能力。
    /// </summary>
    public interface ITestDataStorage
    {
        /// <summary>
        /// 读取最后保存的测试数据快照。
        /// </summary>
        /// <returns>完整测试数据文本；无缓存时返回空字符串。</returns>
        Task<string> LoadTestDataAsync();

        /// <summary>
        /// 保存完整测试数据快照。
        /// </summary>
        /// <param name="rawText">待保存的测试数据文本。</param>
        Task SaveTestDataAsync(string rawText);
    }
}
