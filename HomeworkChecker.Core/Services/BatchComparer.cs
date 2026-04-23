using HomeworkChecker.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace HomeworkChecker.Core.Services
{
    public sealed class BatchComparer
    {
        //public async Task<BatchComparisonResult> CompareAllAsync(...)
        //{
        //    var results = new List<ComparisonResult>();
        //    var semaphore = new SemaphoreSlim(2); // 并发度

        //    var tasks = testCases.Select(async tc => {
        //        await semaphore.WaitAsync();
        //        try
        //        {
        //            // 1. 运行两个 exe
        //            string output1 = await ProcessRunner.RunAsync(demoExe, tc.InputData);
        //            string output2 = await ProcessRunner.RunAsync(studentExe, tc.InputData);

        //            // 2. 文本比对
        //            var diff = TextComparer.Compare(output1, output2, options);

        //            var result = new ComparisonResult
        //            {
        //                TestCaseIndex = tc.Index,
        //                IsPassed = diff.DiffLineCount == 0,
        //                DiffDetails = diff.Details
        //            };
        //            lock (results) results.Add(result);

        //        }
        //        finally
        //        {
        //            semaphore.Release();
        //        }
        //        return result;
        //    });

        //    await Task.WhenAll(tasks);
        //    return new BatchComparisonResult
        //    {
        //        TotalCount = testCases.Count,
        //        PassedCount = results.Count(r => r.IsPassed),
        //        Results = results
        //    };
        }
}
