using System;
using System.Collections.Generic;
using System.Text;

namespace HomeworkChecker.Core.Models
{
    public class CompareOptions
    {
        public TrimType TrimMode { get; set; }
        public int LineSkip { get; set; }
        public int LineOffset { get; set; }
        public bool IgnoreBlank { get; set; }
        public bool CrCrLfNotEqual { get; set; }
        public int MaxDiffCount { get; set; }
        public int MaxLineCount { get; set; }
    }
    public enum TrimType
    {
        None,
        Left,
        Right,
        All
    }
}
