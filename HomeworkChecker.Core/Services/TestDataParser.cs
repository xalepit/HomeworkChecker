using HomeworkChecker.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HomeworkChecker.Core.Services
{
    // 负责从txt/dat文件导入测试数据
    public sealed class TestDataParser
    {
        //    private static readonly Regex HeaderRegex = new(@"^\[(\d+)\]\s*$", RegexOptions.Compiled);

        //    public List<TestCase> Parse(string rawText)
        //    {
        //        rawText ??= string.Empty;

        //        var result = new List<TestCase>();
        //        var reader = new StringReader(rawText);

        //        int? currentIndex = null;
        //        var currentLines = new List<string>();

        //        // 把当前块落盘为一个 TestCase
        //        void FlushCurrent()
        //        {
        //            if (currentIndex is null) return;

        //            result.Add(new TestCase
        //            {
        //                Index = currentIndex.Value,
        //                // 保留用户输入的多行结构；空行也会保留
        //                InputData = string.Join(Environment.NewLine, currentLines)
        //            });

        //            currentLines.Clear();
        //        }

        //        string? line;
        //        while ((line = reader.ReadLine()) is not null)
        //        {
        //            var match = HeaderRegex.Match(line);
        //            if (match.Success)
        //            {
        //                // 遇到新 [n]，先收尾前一组
        //                FlushCurrent();
        //                currentIndex = int.Parse(match.Groups[1].Value);
        //                continue;
        //            }

        //            // 不做格式错误检查：没有 [n] 前缀的孤立行直接忽略
        //            if (currentIndex is not null)
        //            {
        //                currentLines.Add(line);
        //            }
        //        }

        //        FlushCurrent();
        //        return result;
        //    }
        //
    }
}
