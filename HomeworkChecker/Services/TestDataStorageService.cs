using HomeworkChecker.Core.Services;
using HomeworkChecker.Core.Utilities;
using System.IO;

namespace HomeworkChecker.UI.Services
{
    /// <summary>
    /// 在应用数据目录中保存最后一次测试数据编辑快照。
    /// </summary>
    public sealed class TestDataStorageService : ITestDataStorage
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HomeworkChecker",
            "TestData",
            "last-testdata.txt");

        /// <summary>
        /// 读取最后一次测试数据快照。
        /// </summary>
        /// <returns>无缓存时返回空字符串。</returns>
        public async Task<string> LoadTestDataAsync()
        {
            if (!File.Exists(FilePath))
            {
                return string.Empty;
            }

            return await LocalFileStorage.ReadAllTextAsync(FilePath).ConfigureAwait(false);
        }

        /// <summary>
        /// 原子保存当前测试数据快照。
        /// </summary>
        /// <param name="rawText">待保存的完整测试数据。</param>
        public Task SaveTestDataAsync(string rawText) =>
            LocalFileStorage.WriteAllTextAsync(FilePath, rawText);
    }
}
