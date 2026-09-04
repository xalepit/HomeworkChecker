using System.Text;

namespace HomeworkChecker.Core.Utilities
{
    /// <summary>
    /// 为应用本地 UTF-8 文本文件提供共享读取、有限重试和原子写入。
    /// </summary>
    public static class LocalFileStorage
    {
        private const int RetryCount = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        /// <summary>
        /// 读取完整 UTF-8 文本，并允许其他进程同时读取或原子替换文件。
        /// </summary>
        /// <param name="path">待读取文件路径。</param>
        /// <returns>文件完整文本。</returns>
        public static string ReadAllText(string path) =>
            ReadAllTextAsync(path).GetAwaiter().GetResult();

        /// <summary>
        /// 异步读取完整 UTF-8 文本，并允许其他进程同时读取或原子替换文件。
        /// </summary>
        /// <param name="path">待读取文件路径。</param>
        /// <returns>文件完整文本。</returns>
        public static Task<string> ReadAllTextAsync(string path) =>
            RetrySharingViolationAsync(async () =>
            {
                await using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var reader = new StreamReader(
                    stream,
                    Utf8WithoutBom,
                    detectEncodingFromByteOrderMarks: true);
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            });

        /// <summary>
        /// 将文本原子写入 UTF-8 文件，避免读取方看到截断内容。
        /// </summary>
        /// <param name="path">目标文件路径。</param>
        /// <param name="text">待保存文本。</param>
        public static void WriteAllText(string path, string text) =>
            WriteAllTextAsync(path, text).GetAwaiter().GetResult();

        /// <summary>
        /// 将文本异步原子写入 UTF-8 文件，避免读取方看到截断内容。
        /// </summary>
        /// <param name="path">目标文件路径。</param>
        /// <param name="text">待保存文本。</param>
        public static async Task WriteAllTextAsync(string path, string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            var directory = Path.GetDirectoryName(path)
                ?? throw new ArgumentException("目标文件必须包含目录。", nameof(path));
            Directory.CreateDirectory(directory);

            await RetrySharingViolationAsync(async () =>
            {
                var temporaryPath = Path.Combine(
                    directory,
                    $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
                try
                {
                    await File.WriteAllTextAsync(temporaryPath, text, Utf8WithoutBom)
                        .ConfigureAwait(false);
                    File.Move(temporaryPath, path, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 对 Windows 文件共享冲突进行短暂重试，其他 I/O 错误立即返回调用方。
        /// </summary>
        /// <typeparam name="T">文件操作返回值类型。</typeparam>
        /// <param name="operation">单次文件操作。</param>
        /// <returns>成功操作的返回值。</returns>
        private static async Task<T> RetrySharingViolationAsync<T>(Func<Task<T>> operation)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception exception) when (
                    IsRetryableFileLock(exception) && attempt < RetryCount)
                {
                    await Task.Delay(RetryDelay).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 对无返回值文件操作复用共享冲突重试逻辑。
        /// </summary>
        /// <param name="operation">单次文件操作。</param>
        private static async Task RetrySharingViolationAsync(Func<Task> operation)
        {
            await RetrySharingViolationAsync(async () =>
            {
                await operation().ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 判断异常是否为 Windows 共享冲突、锁冲突或覆盖被占用文件时的拒绝访问。
        /// </summary>
        /// <param name="exception">文件操作异常。</param>
        /// <returns>错误码为 5、32 或 33 时返回 true。</returns>
        private static bool IsRetryableFileLock(Exception exception)
        {
            var errorCode = exception.HResult & 0xFFFF;
            return exception switch
            {
                IOException => errorCode is 32 or 33,
                UnauthorizedAccessException => errorCode == 5,
                _ => false
            };
        }
    }
}
