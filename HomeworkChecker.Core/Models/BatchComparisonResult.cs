using HomeworkChecker.Core.Utilities;

namespace HomeworkChecker.Core.Models
{
    /// <summary>
    /// 表示单个测试用例的最终状态。
    /// </summary>
    public enum TestCaseRunStatus
    {
        Passed,
        Failed,
        TimedOut,
        InvalidTestData,
        ExecutionFailed,
        Cancelled
    }

    /// <summary>
    /// 标识执行错误来自 Demo 还是学生程序。
    /// </summary>
    public enum ExecutionTarget
    {
        None,
        Demo,
        Student
    }

    /// <summary>
    /// 定义一次完整测试会话的输入快照。
    /// </summary>
    public sealed class TestSessionRequest
    {
        public string DemoExePath { get; set; } = string.Empty;
        public string StudentExePath { get; set; } = string.Empty;
        public IReadOnlyList<TestCase> TestCases { get; set; } = [];
        public CompareOptions CompareOptions { get; set; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxParallelism { get; set; } = 4;
        public int EncodingCodePage { get; set; } = TextEncodingHelper.GbkCodePage;
    }

    /// <summary>
    /// 保存一个测试用例的执行、比较和诊断结果。
    /// </summary>
    public sealed class TestCaseRunResult
    {
        public TestCase TestCase { get; set; } = new();
        public TestCaseRunStatus Status { get; set; }
        public ExecutionTarget FailedTarget { get; set; }
        public ProcessExecutionResult? DemoExecution { get; set; }
        public ProcessExecutionResult? StudentExecution { get; set; }
        public ComparisonResult? Comparison { get; set; }
        public string DiagnosticMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 保存按测试数据顺序排列的一次测试会话结果。
    /// </summary>
    public sealed class BatchComparisonResult
    {
        public IReadOnlyList<TestCaseRunResult> Results { get; set; } = [];
        public TimeSpan Elapsed { get; set; }
        public int TotalCount => Results.Count;
        public int PassedCount => Count(TestCaseRunStatus.Passed);
        public int FailedCount => Count(TestCaseRunStatus.Failed);
        public int TimedOutCount => Count(TestCaseRunStatus.TimedOut);
        public int InvalidTestDataCount => Count(TestCaseRunStatus.InvalidTestData);
        public int ExecutionFailedCount => Count(TestCaseRunStatus.ExecutionFailed);
        public int CancelledCount => Count(TestCaseRunStatus.Cancelled);

        /// <summary>
        /// 统计指定最终状态的测试用例数量。
        /// </summary>
        /// <param name="status">待统计状态。</param>
        /// <returns>匹配状态的用例数量。</returns>
        private int Count(TestCaseRunStatus status) =>
            Results.Count(result => result.Status == status);
    }
}
