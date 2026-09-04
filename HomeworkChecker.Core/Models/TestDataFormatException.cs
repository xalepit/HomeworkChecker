namespace HomeworkChecker.Core.Models
{
    /// <summary>
    /// 指定测试数据参数指令的格式错误类型。
    /// </summary>
    public enum TestDataFormatError
    {
        DuplicateArguments,
        UnclosedArgumentQuote
    }

    /// <summary>
    /// 表示可由界面按当前语言呈现的测试数据格式错误。
    /// </summary>
    public sealed class TestDataFormatException : FormatException
    {
        /// <summary>
        /// 获取格式错误类型。
        /// </summary>
        public TestDataFormatError Error { get; }

        /// <summary>
        /// 获取从 1 开始的错误行号。
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// 创建包含错误类型和原始行号的测试数据格式异常。
        /// </summary>
        /// <param name="error">格式错误类型。</param>
        /// <param name="lineNumber">从 1 开始的错误行号。</param>
        public TestDataFormatException(TestDataFormatError error, int lineNumber)
            : base($"测试数据第 {lineNumber} 行格式错误：{error}。")
        {
            Error = error;
            LineNumber = lineNumber;
        }
    }
}
