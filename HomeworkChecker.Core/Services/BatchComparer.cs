using HomeworkChecker.Core.Models;
using HomeworkChecker.Core.Utilities;
using System.Diagnostics;

namespace HomeworkChecker.Core.Services
{
    /// <summary>
    /// 以受限并行度运行测试用例，并将程序执行结果交给文本比较器。
    /// </summary>
    public sealed class BatchComparer
    {
        private readonly Func<ProcessRunRequest, CancellationToken, Task<ProcessExecutionResult>> _runProcessAsync;

        /// <summary>
        /// 创建使用标准 ProcessRunner 的测试会话调度器。
        /// </summary>
        public BatchComparer()
            : this(new ProcessRunner().RunAsync)
        {
        }

        /// <summary>
        /// 创建使用指定程序运行委托的测试会话调度器。
        /// </summary>
        /// <param name="runProcessAsync">程序运行委托，仅供确定性回归测试替换。</param>
        internal BatchComparer(
            Func<ProcessRunRequest, CancellationToken, Task<ProcessExecutionResult>> runProcessAsync)
        {
            _runProcessAsync = runProcessAsync;
        }

        /// <summary>
        /// 运行完整测试会话，并按测试数据原始顺序返回全部结果。
        /// </summary>
        /// <param name="request">测试会话输入快照。</param>
        /// <param name="progress">已完成用例数量回调。</param>
        /// <param name="cancellationToken">用户取消标记。</param>
        /// <returns>包含全部用例状态和耗时的会话结果。</returns>
        public async Task<BatchComparisonResult> RunAsync(
            TestSessionRequest request,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var stopwatch = Stopwatch.StartNew();
            var results = new TestCaseRunResult?[request.TestCases.Count];
            var completedCount = 0;
            var progressLock = new object();

            try
            {
                await Parallel.ForEachAsync(
                    Enumerable.Range(0, request.TestCases.Count),
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = request.MaxParallelism,
                        CancellationToken = cancellationToken
                    },
                    async (position, token) =>
                    {
                        results[position] = await RunCaseAsync(
                            request.TestCases[position],
                            request,
                            token);
                        ReportProgress(progress, progressLock, ref completedCount);
                    });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                for (var position = 0; position < results.Length; position++)
                {
                    if (results[position] is not null)
                    {
                        continue;
                    }

                    results[position] = CreateCancelledResult(request.TestCases[position]);
                    ReportProgress(progress, progressLock, ref completedCount);
                }
            }

            stopwatch.Stop();
            return new BatchComparisonResult
            {
                Results = results.Select(result => result!).ToArray(),
                Elapsed = stopwatch.Elapsed
            };
        }

        /// <summary>
        /// 依次运行一个用例的 Demo 和学生程序，并生成最终状态。
        /// </summary>
        /// <param name="testCase">当前测试用例。</param>
        /// <param name="request">测试会话输入快照。</param>
        /// <param name="cancellationToken">用户取消标记。</param>
        /// <returns>当前用例最终结果。</returns>
        private async Task<TestCaseRunResult> RunCaseAsync(
            TestCase testCase,
            TestSessionRequest request,
            CancellationToken cancellationToken)
        {
            var demoExecution = await RunSafelyAsync(
                request.DemoExePath,
                testCase.InputData,
                testCase.Arguments,
                request,
                cancellationToken);
            var demoFailure = ClassifyExecution(testCase, demoExecution, ExecutionTarget.Demo);
            if (demoFailure is not null)
            {
                return demoFailure;
            }

            var studentExecution = await RunSafelyAsync(
                request.StudentExePath,
                testCase.InputData,
                testCase.Arguments,
                request,
                cancellationToken);
            var studentFailure = ClassifyExecution(testCase, studentExecution, ExecutionTarget.Student);
            if (studentFailure is not null)
            {
                studentFailure.DemoExecution = demoExecution;
                return studentFailure;
            }

            var comparison = new TextComparer(request.CompareOptions).Compare(
                GetOutputBytes(demoExecution, request.EncodingCodePage),
                GetOutputBytes(studentExecution, request.EncodingCodePage),
                request.EncodingCodePage);
            var status = comparison.Status switch
            {
                ComparisonStatus.Passed => TestCaseRunStatus.Passed,
                ComparisonStatus.InvalidInput when comparison.InvalidInputNumber == 1 =>
                    TestCaseRunStatus.InvalidTestData,
                _ => TestCaseRunStatus.Failed
            };

            return new TestCaseRunResult
            {
                TestCase = testCase,
                Status = status,
                FailedTarget = status == TestCaseRunStatus.InvalidTestData
                    ? ExecutionTarget.Demo
                    : ExecutionTarget.None,
                DemoExecution = demoExecution,
                StudentExecution = studentExecution,
                Comparison = comparison,
                DiagnosticMessage = comparison.ErrorMessage
            };
        }

        /// <summary>
        /// 获取程序原始输出；测试委托只提供字符串时按会话代码页补齐字节。
        /// </summary>
        /// <param name="execution">程序执行结果。</param>
        /// <param name="encodingCodePage">字符串回退编码代码页。</param>
        /// <returns>用于保真比较的输出字节。</returns>
        private static byte[] GetOutputBytes(
            ProcessExecutionResult execution,
            int encodingCodePage)
        {
            return execution.StandardOutputBytes.Length > 0 || execution.StandardOutput.Length == 0
                ? execution.StandardOutputBytes
                : TextEncodingHelper.GetEncoding(encodingCodePage).GetBytes(execution.StandardOutput);
        }

        /// <summary>
        /// 调用程序运行器，并将意外异常转换为可显示的启动失败结果。
        /// </summary>
        /// <param name="filePath">待运行程序路径。</param>
        /// <param name="standardInput">测试用例标准输入。</param>
        /// <param name="arguments">同时传给 Demo 和学生程序的参数列表。</param>
        /// <param name="request">测试会话输入快照。</param>
        /// <param name="cancellationToken">用户取消标记。</param>
        /// <returns>程序执行结果。</returns>
        private async Task<ProcessExecutionResult> RunSafelyAsync(
            string filePath,
            string standardInput,
            IReadOnlyList<string> arguments,
            TestSessionRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _runProcessAsync(
                    new ProcessRunRequest
                    {
                        FilePath = filePath,
                        StandardInput = standardInput,
                        Arguments = arguments,
                        Timeout = request.Timeout,
                        EncodingCodePage = request.EncodingCodePage
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new ProcessExecutionResult { Status = ProcessExecutionStatus.Cancelled };
            }
            catch (Exception exception)
            {
                return new ProcessExecutionResult
                {
                    Status = ProcessExecutionStatus.StartFailed,
                    ErrorMessage = exception.Message
                };
            }
        }

        /// <summary>
        /// 将程序执行状态转换为需要提前结束用例的最终结果。
        /// </summary>
        /// <param name="testCase">当前测试用例。</param>
        /// <param name="execution">程序执行结果。</param>
        /// <param name="target">当前执行的是 Demo 还是学生程序。</param>
        /// <returns>需要提前结束时返回结果；正常完成时返回空。</returns>
        private static TestCaseRunResult? ClassifyExecution(
            TestCase testCase,
            ProcessExecutionResult execution,
            ExecutionTarget target)
        {
            var status = execution.Status switch
            {
                ProcessExecutionStatus.Completed => (TestCaseRunStatus?)null,
                ProcessExecutionStatus.Cancelled => TestCaseRunStatus.Cancelled,
                ProcessExecutionStatus.TimedOut when target == ExecutionTarget.Demo =>
                    TestCaseRunStatus.InvalidTestData,
                ProcessExecutionStatus.TimedOut => TestCaseRunStatus.TimedOut,
                _ => TestCaseRunStatus.ExecutionFailed
            };

            return status is null
                ? null
                : new TestCaseRunResult
                {
                    TestCase = testCase,
                    Status = status.Value,
                    FailedTarget = target,
                    DemoExecution = target == ExecutionTarget.Demo ? execution : null,
                    StudentExecution = target == ExecutionTarget.Student ? execution : null,
                    DiagnosticMessage = execution.ErrorMessage
                };
        }

        /// <summary>
        /// 创建尚未开始便被用户取消的用例结果。
        /// </summary>
        /// <param name="testCase">被取消用例。</param>
        /// <returns>取消结果。</returns>
        private static TestCaseRunResult CreateCancelledResult(TestCase testCase) =>
            new()
            {
                TestCase = testCase,
                Status = TestCaseRunStatus.Cancelled,
                DiagnosticMessage = "测试会话已取消。"
            };

        /// <summary>
        /// 串行递增并报告已完成数量，保证并行完成时进度仍然单调。
        /// </summary>
        /// <param name="progress">进度接收方。</param>
        /// <param name="progressLock">进度同步锁。</param>
        /// <param name="completedCount">已完成数量。</param>
        private static void ReportProgress(
            IProgress<int>? progress,
            object progressLock,
            ref int completedCount)
        {
            lock (progressLock)
            {
                completedCount++;
                progress?.Report(completedCount);
            }
        }

        /// <summary>
        /// 校验测试会话的路径、用例、超时和并行范围。
        /// </summary>
        /// <param name="request">待校验会话请求。</param>
        private static void ValidateRequest(TestSessionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.TestCases);
            ArgumentNullException.ThrowIfNull(request.CompareOptions);

            if (!File.Exists(request.DemoExePath))
            {
                throw new FileNotFoundException("Demo 可执行文件不存在。", request.DemoExePath);
            }

            if (!File.Exists(request.StudentExePath))
            {
                throw new FileNotFoundException("学生可执行文件不存在。", request.StudentExePath);
            }

            if (request.TestCases.Count == 0)
            {
                throw new ArgumentException("测试会话至少需要一个测试用例。", nameof(request));
            }

            if (request.Timeout < TimeSpan.FromSeconds(3) ||
                request.Timeout > TimeSpan.FromSeconds(10))
            {
                throw new ArgumentOutOfRangeException(nameof(request), "超时时间必须为 3 到 10 秒。");
            }

            if (request.MaxParallelism is < 1 or > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "并行数必须为 1 到 8。");
            }
        }
    }
}
