using System;
using System.Collections.Generic;
using System.Text;

namespace HomeworkChecker.Core.Services
{
    public interface ITestDataStorage
    {
        Task<string> LoadTestDataAsync();
        Task SaveTestDataAsync(string rawText);
    }
}
