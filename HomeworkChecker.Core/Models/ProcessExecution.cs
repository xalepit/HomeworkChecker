using HomeworkChecker.Core.Utilities;

namespace HomeworkChecker.Core.Models
{
    /// <summary>
    /// 表示外部程序的一次执行状态。
    /// </summary>
    public enum ProcessExecutionStatus
    {
        Completed,
        TimedOut,
        Cancelled,
        StartFailed,
        OutputLimitExceeded
    }

    /// <summary>
    /// 表示一次外部程序执行请求。
    /// </summary>
    public sealed class ProcessRunRequest
    {
        public string FilePath { get; set; } = string.Empty;
        public string StandardInput { get; set; } = string.Empty;
        public IReadOnlyList<string> Arguments { get; set; } = [];
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public int EncodingCodePage { get; set; } = TextEncodingHelper.GbkCodePage;
    }

    /// <summary>
    /// 保存外部程序的执行状态、输出和诊断信息。
    /// </summary>
    public sealed class ProcessExecutionResult
    {
        public ProcessExecutionStatus Status { get; set; }
        public int? ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public byte[] StandardOutputBytes { get; set; } = [];
        public byte[] StandardErrorBytes { get; set; } = [];
        public TimeSpan Elapsed { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
