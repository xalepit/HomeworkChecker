using HomeworkChecker.Core.Models;
using HomeworkChecker.Core.Utilities;
using System.Diagnostics;

namespace HomeworkChecker.Core.Services
{
    /// <summary>
    /// 使用标准输入输出重定向运行单个外部程序。
    /// </summary>
    public sealed class ProcessRunner
    {
        public const int MaximumOutputBytes = 65_536;

        /// <summary>
        /// 执行外部程序，并在超时或取消时终止整个进程树。
        /// </summary>
        /// <param name="request">程序路径、输入、参数、编码和超时设置。</param>
        /// <param name="cancellationToken">用户取消标记。</param>
        /// <returns>包含原始输出字节和解码文本的执行结果。</returns>
        public async Task<ProcessExecutionResult> RunAsync(
            ProcessRunRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var stopwatch = Stopwatch.StartNew();
            var encoding = TextEncodingHelper.GetEncoding(request.EncodingCodePage);
            using var process = new Process
            {
                StartInfo = CreateStartInfo(request)
            };

            try
            {
                if (!process.Start())
                {
                    return CreateStartFailure(stopwatch.Elapsed, "操作系统未能启动该程序。");
                }
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return CreateStartFailure(stopwatch.Elapsed, exception.Message);
            }

            using var standardOutput = new MemoryStream();
            using var standardError = new MemoryStream();
            var outputLimitSource = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var outputTask = ReadLimitedAsync(
                process.StandardOutput.BaseStream,
                standardOutput,
                "stdout",
                outputLimitSource);
            var errorTask = ReadLimitedAsync(
                process.StandardError.BaseStream,
                standardError,
                "stderr",
                outputLimitSource);
            var inputTask = WriteInputAsync(process, encoding.GetBytes(request.StandardInput), cancellationToken);

            var status = ProcessExecutionStatus.Completed;
            var errorMessage = string.Empty;
            using var timeoutSource = new CancellationTokenSource(request.Timeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

            try
            {
                var waitTask = process.WaitForExitAsync(linkedSource.Token);
                var firstCompletedTask = await Task.WhenAny(waitTask, outputLimitSource.Task);
                if (firstCompletedTask == outputLimitSource.Task)
                {
                    status = ProcessExecutionStatus.OutputLimitExceeded;
                    errorMessage = $"{await outputLimitSource.Task} 输出超过 {MaximumOutputBytes} 字节限制。";
                    KillProcessTree(process);
                    await process.WaitForExitAsync();
                    await IgnoreClosedInputPipeAsync(inputTask);
                }
                else
                {
                    await waitTask;
                    await inputTask;
                }
            }
            catch (OperationCanceledException)
            {
                status = cancellationToken.IsCancellationRequested
                    ? ProcessExecutionStatus.Cancelled
                    : ProcessExecutionStatus.TimedOut;

                KillProcessTree(process);
                await process.WaitForExitAsync();
                await IgnoreClosedInputPipeAsync(inputTask);
            }

            await Task.WhenAll(outputTask, errorTask);
            if (status == ProcessExecutionStatus.Completed && outputLimitSource.Task.IsCompleted)
            {
                status = ProcessExecutionStatus.OutputLimitExceeded;
                errorMessage = $"{await outputLimitSource.Task} 输出超过 {MaximumOutputBytes} 字节限制。";
            }

            stopwatch.Stop();

            var outputBytes = standardOutput.ToArray();
            var errorBytes = standardError.ToArray();
            return new ProcessExecutionResult
            {
                Status = status,
                ExitCode = process.ExitCode,
                StandardOutput = TextEncodingHelper.Decode(outputBytes, request.EncodingCodePage),
                StandardError = TextEncodingHelper.Decode(errorBytes, request.EncodingCodePage),
                StandardOutputBytes = outputBytes,
                StandardErrorBytes = errorBytes,
                Elapsed = stopwatch.Elapsed,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// 持续排空一个输出流，但只保留前 64 KiB，并在首次超限时通知运行流程。
        /// </summary>
        /// <param name="source">子进程输出流。</param>
        /// <param name="destination">保存限定长度输出的内存流。</param>
        /// <param name="streamName">用于诊断的流名称。</param>
        /// <param name="outputLimitSource">首次超限通知源。</param>
        private static async Task ReadLimitedAsync(
            Stream source,
            MemoryStream destination,
            string streamName,
            TaskCompletionSource<string> outputLimitSource)
        {
            var buffer = new byte[4096];
            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer);
                if (bytesRead == 0)
                {
                    return;
                }

                var remainingBytes = MaximumOutputBytes - (int)destination.Length;
                if (remainingBytes > 0)
                {
                    await destination.WriteAsync(
                        buffer.AsMemory(0, Math.Min(bytesRead, remainingBytes)));
                }

                if (bytesRead > remainingBytes)
                {
                    outputLimitSource.TrySetResult(streamName);
                }
            }
        }

        /// <summary>
        /// 校验执行请求中会导致执行语义不明确的参数。
        /// </summary>
        /// <param name="request">待校验请求。</param>
        private static void ValidateRequest(ProcessRunRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                throw new ArgumentException("可执行文件路径不能为空。", nameof(request));
            }

            if (request.Timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "超时时间必须大于零。");
            }

            ArgumentNullException.ThrowIfNull(request.Arguments);
            ArgumentNullException.ThrowIfNull(request.StandardInput);
        }

        /// <summary>
        /// 创建仅使用标准流重定向的进程启动配置。
        /// </summary>
        /// <param name="request">程序执行请求。</param>
        /// <returns>可直接用于 Process 的启动配置。</returns>
        private static ProcessStartInfo CreateStartInfo(ProcessRunRequest request)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = request.FilePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var argument in request.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return startInfo;
        }

        /// <summary>
        /// 将输入字节原样写入标准输入并关闭管道，使程序能够收到 EOF。
        /// </summary>
        /// <param name="process">已启动的外部进程。</param>
        /// <param name="inputBytes">不带额外换行或编码前导的输入字节。</param>
        /// <param name="cancellationToken">用户取消标记。</param>
        private static async Task WriteInputAsync(
            Process process,
            byte[] inputBytes,
            CancellationToken cancellationToken)
        {
            try
            {
                await process.StandardInput.BaseStream.WriteAsync(inputBytes, cancellationToken);
                await process.StandardInput.BaseStream.FlushAsync(cancellationToken);
            }
            catch (IOException) when (process.HasExited)
            {
                // 程序提前退出会主动关闭输入管道，此时输出和退出码仍然有效。
            }
            finally
            {
                process.StandardInput.Close();
            }
        }

        /// <summary>
        /// 终止目标进程及其派生进程。
        /// </summary>
        /// <param name="process">待终止进程。</param>
        private static void KillProcessTree(Process process)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        /// <summary>
        /// 在进程已被终止后忽略标准输入管道关闭产生的预期异常。
        /// </summary>
        /// <param name="inputTask">标准输入写入任务。</param>
        private static async Task IgnoreClosedInputPipeAsync(Task inputTask)
        {
            try
            {
                await inputTask;
            }
            catch (Exception exception) when (
                exception is IOException or ObjectDisposedException or OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// 创建无法启动程序时的统一结果。
        /// </summary>
        /// <param name="elapsed">启动失败前已消耗的时间。</param>
        /// <param name="message">操作系统返回的错误信息。</param>
        /// <returns>启动失败结果。</returns>
        private static ProcessExecutionResult CreateStartFailure(TimeSpan elapsed, string message) =>
            new()
            {
                Status = ProcessExecutionStatus.StartFailed,
                Elapsed = elapsed,
                ErrorMessage = message
            };
    }
}
