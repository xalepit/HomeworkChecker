namespace HomeworkChecker.Core.Models
{
    /// <summary>
    /// 定义文本比较器使用的全部比较选项。
    /// </summary>
    public sealed class CompareOptions
    {
        /// <summary>
        /// 获取或设置每行首尾空格和制表符的忽略方式。
        /// </summary>
        public TrimType TrimMode { get; set; } = TrimType.None;

        /// <summary>
        /// 获取或设置两个文本同时跳过的有效行数，范围为 0 到 100。
        /// </summary>
        public int LineSkip { get; set; }

        /// <summary>
        /// 获取或设置行偏移；负数跳过文本一，正数跳过文本二，范围为 -100 到 100。
        /// </summary>
        public int LineOffset { get; set; }

        /// <summary>
        /// 获取或设置是否忽略 trim 后内容为空的行。
        /// </summary>
        public bool IgnoreBlank { get; set; }

        /// <summary>
        /// 获取或设置是否区分 CR、LF 和 CRLF 行结束符。
        /// </summary>
        public bool CrCrLfNotEqual { get; set; }

        /// <summary>
        /// 获取或设置达到多少个差异行后停止，0 表示不限制，范围为 0 到 100。
        /// </summary>
        public int MaxDiffCount { get; set; }

        /// <summary>
        /// 获取或设置最多比较多少行，0 表示不限制，范围为 0 到 10000。
        /// </summary>
        public int MaxLineCount { get; set; }
    }

    /// <summary>
    /// 指定每行文本首尾空格和制表符的忽略方式。
    /// </summary>
    public enum TrimType
    {
        None,
        Left,
        Right,
        All
    }
}
