using System.Diagnostics;
using System.Text;

namespace HomeworkChecker.Core.Services
{
    // 负责启动exe、喂数据、捕获输出、超时控制

    //    public enum ProcessRunStatus
    //    {
    //        Ok = 0,
    //        StartFailed,
    //        Timeout,
    //        MaxOutput,
    //        Killed,
    //        RuntimeError
    //}

    //public sealed class ProcessRunResult
    //{
    //    public ProcessRunStatus Status { get; set; } = ProcessRunStatus.Ok;
    //    public string StdOut { get; set; } = string.Empty;
    //    public string StdErr { get; set; } = string.Empty;
    //    public double ElapsedSeconds { get; set; }
    //}
    public sealed class ProcessRunner
    {
        //public async Task<ProcessRunResult> RunAsync(
        //        string exePath,
        //            string inputText,
        //            int timeoutSeconds,
        //            int maxOutputLength,
        //            CancellationToken cancellationToken = default)
        //        {
        //            var result = new ProcessRunResult();
        //        var sw = Stopwatch.StartNew();

        //            try
        //            {
        //                using var process = new Process
        //                      {
        //                          StartInfo = new ProcessStartInfo
        //                          {
        //                              FileName = exePath,
        //                              UseShellExecute = false,
        //                              RedirectStandardInput = true,
        //                              RedirectStandardOutput = true,
        //                              RedirectStandardError = true,
        //                              CreateNoWindow = true
        //                          }
        //                      };

        //                if (!process.Start())
        //                {
        //                    result.Status = ProcessRunStatus.StartFailed;
        //                    return result;
        //                }

        //                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        //                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        //                    cancellationToken, timeoutCts.Token);

        //                // 异步写入输入并关闭 stdin，防止被测程序一直等输入
        //                await process.StandardInput.WriteAsync(inputText ?? string.Empty);
        //    process.StandardInput.Close();

        //                var outputTask = ReadWithLimitAsync(process.StandardOutput, maxOutputLength, linkedCts.Token);
        //    var errorTask = process.StandardError.ReadToEndAsync(linkedCts.Token);
        //    var waitTask = process.WaitForExitAsync(linkedCts.Token);

        //                try
        //                {
        //                    await Task.WhenAll(outputTask, errorTask, waitTask);
        //}
        //                catch (OperationCanceledException)
        //                {
        //    if (timeoutCts.IsCancellationRequested)
        //    {
        //        result.Status = ProcessRunStatus.Timeout;
        //    }
        //    else
        //    {
        //        result.Status = ProcessRunStatus.Killed;
        //    }

        //    TryKillProcessTree(process);
        //    return result;
        //}

        //result.StdOut = outputTask.Result.Text;
        //result.StdErr = errorTask.Result;

        //if (outputTask.Result.HitLimit)
        //{
        //    result.Status = ProcessRunStatus.MaxOutput;
        //    TryKillProcessTree(process);
        //    return result;
        //}

        //result.Status = process.ExitCode == 0 ? ProcessRunStatus.Ok : ProcessRunStatus.RuntimeError;
        //return result;
        //            }
        //            catch
        //            {
        //    result.Status = ProcessRunStatus.StartFailed;
        //    return result;
        //}
        //            finally
        //            {
        //    sw.Stop();
        //    result.ElapsedSeconds = sw.Elapsed.TotalSeconds;
        //}
        //        }

        //        private static async Task<(string Text, bool HitLimit)> ReadWithLimitAsync(
        //            StreamReader reader,
        //            int maxOutputLength,
        //            CancellationToken ct)
        //{
        //    var sb = new StringBuilder();
        //    var buffer = new char[1024];

        //    while (true)
        //    {
        //        var n = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
        //        if (n <= 0) break;

        //        sb.Append(buffer, 0, n);

        //        if (maxOutputLength > 0 && sb.Length >= maxOutputLength)
        //        {
        //            return (sb.ToString(0, maxOutputLength), true);
        //        }
        //    }

        //    return (sb.ToString(), false);
        //}

        //private static void TryKillProcessTree(Process process)
        //{
        //    try
        //    {
        //        if (!process.HasExited)
        //            process.Kill(entireProcessTree: true);
        //    }
        //    catch
        //    {
        //        // 忽略杀进程异常，保持 runner 可返回
        //    }
        //}
    }
}
