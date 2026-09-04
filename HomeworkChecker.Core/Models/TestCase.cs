using System;

namespace HomeworkChecker.Core.Models
{
    /// <summary>
    /// 表示按测试数据文本出现顺序解析出的单个测试用例。
    /// </summary>
    public sealed class TestCase
    {
        /// <summary>
        /// 获取或设置从 1 开始的顺序编号；该编号与组名内容无关。
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 获取或设置方括号内的源组名；空字符串表示无组名或空组名。
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置实际写入被测程序标准输入的文本。
        /// </summary>
        public string InputData { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置参数指令中去除首尾空白后的原始参数文本。
        /// </summary>
        public string ArgumentText { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置传递给 Demo 与学生程序的参数列表。
        /// </summary>
        public IReadOnlyList<string> Arguments { get; set; } = [];
    }
}
