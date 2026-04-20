using System;
using System.Collections.Generic;
using System.Text;

namespace HomeworkChecker.Core.Models
{
    // 单次比对的结果（是否通过、详细差异信息）
    public sealed class ComparisonResult
    {
        public int TestCaseIndex { get; set; } = 0;
        public bool IsPassed { get; set; } = false;
        public int DiffLineCount { get; set; } = 0;

        public List<DiffDetail> DiffDetails { get; set; } = new();
    }
    public sealed class DiffDetail
    {
        public int LineNumber1 { get; set; }
        public int LineNumber2 { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Ruler { get; set; } = string.Empty;         // 标尺字符串（原样输出）
        public string File1Content { get; set; } = string.Empty;  // 带高亮标记的文件1行内容
        public string File2Content { get; set; } = string.Empty;  // 带高亮标记的文件2行内容
        public string? HexDump1 { get; set; }                     // 详细模式的十六进制转储
        public string? HexDump2 { get; set; }
    }
}