using System;
using System.Collections.Generic;
using System.Text;

namespace HomeworkChecker.Core.Models
{
    // 单个测试用例（输入数据字符串）
    public class TestCase
    {
        public int Index { get; set; }
        public string InputData { get; set; } = string.Empty;
    }
}
