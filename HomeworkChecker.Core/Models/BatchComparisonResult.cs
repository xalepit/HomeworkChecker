using System;
using System.Collections.Generic;
using System.Text;

namespace HomeworkChecker.Core.Models
{
    // 批量比对的总结果（所有TestCase的结果集合）
    public class BatchComparisonResult
    {
        public int TotalCount { get; set; }
        public int PassedCount { get; set; }
        public List<ComparisonResult> Results { get; set; } = new();
    }
}
