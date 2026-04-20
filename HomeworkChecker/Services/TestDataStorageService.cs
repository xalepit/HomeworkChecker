using System.IO;
using HomeworkChecker.Core.Services;

namespace HomeworkChecker.UI.Services
{
    public class TestDataStorageService : ITestDataStorage
    {
        private static readonly string FilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "HomeworkChecker",
    "TestData",
    "last-testdata.txt");

        public async Task<string> LoadTestDataAsync()
        {
            if (!File.Exists(FilePath))
            {
                return string.Empty;
            }
            return await File.ReadAllTextAsync(FilePath);
        }

        public async Task SaveTestDataAsync(string rawText)
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(FilePath, rawText);
        }
    }
}
