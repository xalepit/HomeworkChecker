namespace HomeworkChecker.Core.Models
{
    /// <summary>
    /// 表示一次文本比较的状态。
    /// </summary>
    public enum ComparisonStatus
    {
        Passed,
        Different,
        InvalidInput
    }

    /// <summary>
    /// 表示比较前发现的输入错误。
    /// </summary>
    public enum ComparisonErrorType
    {
        None,
        EmptyText,
        MixedLineEndings
    }

    /// <summary>
    /// 表示一行文本的结束方式。
    /// </summary>
    public enum LineEndingType
    {
        Cr,
        Lf,
        CrLf,
        Eof
    }

    /// <summary>
    /// 表示 tc 2.0.3 可报告的行级差异类型。
    /// </summary>
    public enum DifferenceType
    {
        ContentMismatch,
        LineEndingMismatch,
        File1HasExtraCharacters,
        File2HasExtraCharacters,
        File1Ended,
        File2Ended
    }

    /// <summary>
    /// 表示单次文本比较的结果。
    /// </summary>
    public sealed class ComparisonResult
    {
        public int TestCaseIndex { get; set; }
        public ComparisonStatus Status { get; set; }
        public bool IsPassed { get; set; }
        public int DiffLineCount { get; set; }
        public ComparisonErrorType ErrorType { get; set; }
        public int? InvalidInputNumber { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string EncodingName1 { get; set; } = string.Empty;
        public string EncodingName2 { get; set; } = string.Empty;
        public List<DiffDetail> DiffDetails { get; set; } = [];
    }

    /// <summary>
    /// 表示一对行为单位的差异详情。
    /// </summary>
    public sealed class DiffDetail
    {
        public int DifferenceNumber { get; set; }
        public DifferenceType Type { get; set; }
        public int LineNumber1 { get; set; }
        public int LineNumber2 { get; set; }
        public int FirstDifferenceIndex { get; set; } = -1;
        public string Reason { get; set; } = string.Empty;
        public string Ruler { get; set; } = string.Empty;
        public string Ruler1 { get; set; } = string.Empty;
        public string Ruler2 { get; set; } = string.Empty;
        public string File1Content { get; set; } = string.Empty;
        public string File2Content { get; set; } = string.Empty;
        public bool File1HasLine { get; set; }
        public bool File2HasLine { get; set; }
        public byte[] File1RawBytes { get; set; } = [];
        public byte[] File2RawBytes { get; set; } = [];
        public LineEndingType File1LineEnding { get; set; }
        public LineEndingType File2LineEnding { get; set; }
        public List<int> DifferentPositions1 { get; set; } = [];
        public List<int> DifferentPositions2 { get; set; } = [];
        public string? HexDump1 { get; set; }
        public string? HexDump2 { get; set; }
    }
}
