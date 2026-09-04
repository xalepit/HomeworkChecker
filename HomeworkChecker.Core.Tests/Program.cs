using HomeworkChecker.Core.Models;
using HomeworkChecker.Core.Services;
using HomeworkChecker.Core.Utilities;

namespace HomeworkChecker.Core.Tests
{
    internal static class Program
    {
        /// <summary>
        /// 运行测试数据解析器的最小回归检查。
        /// </summary>
        /// <returns>全部检查通过时返回 0。</returns>
        private static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--process-runner-child")
            {
                return await RunProcessRunnerChildAsync(args);
            }

            var parser = new TestDataParser();
            var groupedCases = parser.Parse(
                "# 文件说明\r\n\r\n[03]\r\nA\r\n\r\n[01] # 第二组\nB\n[03]\n[ abc ]def ] // 特殊组名");

            AssertEqual(4, groupedCases.Count, "分组数量");
            AssertEqual(1, groupedCases[0].Index, "第一组顺序编号");
            AssertEqual("03", groupedCases[0].GroupName, "第一组组名");
            AssertEqual("A\r\n\r\n", groupedCases[0].InputData, "CRLF 输入内容");
            AssertEqual(2, groupedCases[1].Index, "第二组顺序编号");
            AssertEqual("01", groupedCases[1].GroupName, "不连续数字组名");
            AssertEqual("B\n", groupedCases[1].InputData, "LF 输入内容");
            AssertEqual("03", groupedCases[2].GroupName, "重复组名不合并");
            AssertEqual(string.Empty, groupedCases[2].InputData, "空输入用例");
            AssertEqual("abc ]def", groupedCases[3].GroupName, "配置文件组名规则");

            var simpleCases = parser.Parse("\r\nvalue\n  # 忽略说明\n;保留分号\n//保留斜杠");
            AssertEqual(1, simpleCases.Count, "无组名文本作为单个用例");
            AssertEqual(string.Empty, simpleCases[0].GroupName, "无组名用例标签");
            AssertEqual("\r\nvalue\n;保留分号\n//保留斜杠", simpleCases[0].InputData, "无分组正文保留开头空白行");

            var emptyGroupCases = parser.Parse("[]\n");
            AssertEqual(1, emptyGroupCases.Count, "空组名仍产生用例");
            AssertEqual(string.Empty, emptyGroupCases[0].GroupName, "空组名内容");

            AssertEqual(0, parser.Parse("# 只有注释\r\n\r\n").Count, "空文件不产生用例");

            try
            {
                parser.Parse(null!);
                throw new InvalidOperationException("null 输入未被拒绝。");
            }
            catch (ArgumentNullException)
            {
            }

            RunArgumentParserChecks();
            RunTextComparerChecks();
            RunTextEncodingChecks();
            await RunLocalFileStorageChecksAsync();
            await RunProcessRunnerChecksAsync();
            await RunBatchComparerChecksAsync();

            Console.WriteLine("Core checks passed.");
            return 0;
        }

        /// <summary>
        /// 验证参数指令解析、旧输入保留和错误行号。
        /// </summary>
        private static void RunArgumentParserChecks()
        {
            var parser = new TestDataParser();
            var cases = parser.Parse(
                "[case-1]\r\n# ordinary comment\r\n# @args: --value=123 --name=\"two words\" \"\" 中文 \\\"quoted\\\"\r\ninput\r\n\r\n" +
                "[case-2]\n# @args: --only=argument");

            AssertEqual(2, cases.Count, "参数分组数量");
            AssertEqual(
                "--value=123 --name=\"two words\" \"\" 中文 \\\"quoted\\\"",
                cases[0].ArgumentText,
                "原始参数文本");
            AssertEqual(
                "--value=123|--name=two words||中文|\"quoted\"",
                string.Join('|', cases[0].Arguments),
                "引号及中文参数拆分");
            AssertEqual("input\r\n\r\n", cases[0].InputData, "参数指令不进入标准输入");
            AssertEqual("--only=argument", cases[1].Arguments.Single(), "仅参数用例");
            AssertEqual(string.Empty, cases[1].InputData, "仅参数用例无标准输入");

            cases = parser.Parse("# @args: --label \"two words\"\n");
            AssertEqual(1, cases.Count, "无分组参数用例");
            AssertEqual("--label|two words", string.Join('|', cases[0].Arguments), "无分组参数拆分");

            cases = parser.Parse("[case]\ninput\n# @args: --late");
            AssertEqual("input\n", cases.Single().InputData, "输入后的参数指令按普通注释忽略");
            AssertEqual(0, cases.Single().Arguments.Count, "输入后的参数指令不生效");
            AssertFormatError(
                parser,
                "[case]\n# @args: --one\n# @args: --two",
                TestDataFormatError.DuplicateArguments,
                3,
                "重复参数指令");
            AssertFormatError(
                parser,
                "# @args: --name=\"unfinished",
                TestDataFormatError.UnclosedArgumentQuote,
                1,
                "未闭合引号");
        }

        /// <summary>
        /// 断言解析指定文本时会产生预期类型和行号的格式错误。
        /// </summary>
        /// <param name="parser">测试数据解析器。</param>
        /// <param name="text">预期解析失败的文本。</param>
        /// <param name="expectedError">预期错误类型。</param>
        /// <param name="expectedLineNumber">预期行号。</param>
        /// <param name="name">检查项目名称。</param>
        private static void AssertFormatError(
            TestDataParser parser,
            string text,
            TestDataFormatError expectedError,
            int expectedLineNumber,
            string name)
        {
            try
            {
                parser.Parse(text);
                throw new InvalidOperationException($"{name}未被拒绝。");
            }
            catch (TestDataFormatException exception)
            {
                AssertEqual(expectedError, exception.Error, $"{name}错误类型");
                AssertEqual(expectedLineNumber, exception.LineNumber, $"{name}错误行号");
            }
        }

        /// <summary>
        /// 验证本地缓存文件可承受短暂占用，且长期占用会明确失败。
        /// </summary>
        private static async Task RunLocalFileStorageChecksAsync()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                $"HomeworkChecker-CoreTests-{Guid.NewGuid():N}");
            var path = Path.Combine(directory, "cache.txt");

            try
            {
                await LocalFileStorage.WriteAllTextAsync(path, "初始内容");
                AssertEqual("初始内容", await LocalFileStorage.ReadAllTextAsync(path), "缓存正常读写");

                using (var temporaryLock = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    var pendingRead = LocalFileStorage.ReadAllTextAsync(path);
                    await Task.Delay(150);
                    temporaryLock.Dispose();
                    AssertEqual("初始内容", await pendingRead, "短暂文件占用重试");
                }

                using (var temporaryLock = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    var pendingWrite = LocalFileStorage.WriteAllTextAsync(path, "更新内容");
                    await Task.Delay(150);
                    temporaryLock.Dispose();
                    await pendingWrite;
                }

                AssertEqual("更新内容", await LocalFileStorage.ReadAllTextAsync(path), "占用解除后原子写入");

                using (var persistentLock = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    try
                    {
                        await LocalFileStorage.WriteAllTextAsync(path, "不可写入");
                        throw new InvalidOperationException("长期文件占用未被报告。");
                    }
                    catch (Exception exception) when (
                        exception is IOException or UnauthorizedAccessException)
                    {
                    }
                }

                AssertEqual("更新内容", await LocalFileStorage.ReadAllTextAsync(path), "写入失败不破坏旧缓存");
                AssertEqual(0, Directory.GetFiles(directory, "*.tmp").Length, "写入失败不遗留临时文件");
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        /// <summary>
        /// 作为 ProcessRunner 的子进程执行指定测试行为。
        /// </summary>
        /// <param name="args">包含子进程模式及其参数的命令行参数。</param>
        /// <returns>指定模式对应的进程退出码。</returns>
        private static async Task<int> RunProcessRunnerChildAsync(string[] args)
        {
            var mode = args.ElementAtOrDefault(1);
            switch (mode)
            {
                case "echo":
                    Console.Write(await Console.In.ReadToEndAsync());
                    return 0;

                case "stderr-exit":
                    Console.Error.Write("child-error");
                    return 7;

                case "flood":
                    Console.Write(new string('O', 200_000));
                    Console.Error.Write(new string('E', 200_000));
                    return 0;

                case "near-limit":
                    Console.Write(new string('O', 60_000));
                    Console.Error.Write(new string('E', 60_000));
                    return 0;

                case "stderr-flood":
                    Console.Error.Write(new string('E', 200_000));
                    return 0;

                case "sleep":
                    await Task.Delay(int.Parse(args[2]));
                    return 0;

                case "args":
                    Console.Write(string.Join('|', args.Skip(2)));
                    return 0;

                case "args-stdin":
                    Console.Write($"{string.Join('|', args.Skip(2))}::{await Console.In.ReadToEndAsync()}");
                    return 0;

                case "raw-utf8":
                    await Console.OpenStandardOutput().WriteAsync(
                        System.Text.Encoding.UTF8.GetBytes("中文输出"));
                    return 0;

                case "raw-gbk":
                    await Console.OpenStandardOutput().WriteAsync(
                        TextEncodingHelper.GetEncoding(TextEncodingHelper.GbkCodePage)
                            .GetBytes("中文输出"));
                    return 0;

                default:
                    Console.Error.Write("unknown-child-mode");
                    return 2;
            }
        }

        /// <summary>
        /// 运行外部程序重定向、超时、取消和失败路径的最小回归检查。
        /// </summary>
        private static async Task RunProcessRunnerChecksAsync()
        {
            var executablePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("无法确定回归检查程序路径。");
            var runner = new ProcessRunner();

            var result = await runner.RunAsync(CreateRunRequest(
                executablePath,
                "echo",
                "A\r\n\r\nB\n"));
            AssertEqual(ProcessExecutionStatus.Completed, result.Status, "正常执行状态");
            AssertEqual("A\r\n\r\nB\n", result.StandardOutput, "标准输入空行保留");
            AssertEqual(0, result.ExitCode, "正常退出码");
            AssertEqual(true, result.StandardOutputBytes.Length > 0, "原始输出字节");

            result = await runner.RunAsync(CreateRunRequest(executablePath, "stderr-exit"));
            AssertEqual(ProcessExecutionStatus.Completed, result.Status, "非零退出仍完成");
            AssertEqual(7, result.ExitCode, "非零退出码");
            AssertEqual("child-error", result.StandardError, "标准错误捕获");

            result = await runner.RunAsync(CreateRunRequest(executablePath, "flood"));
            AssertEqual(ProcessExecutionStatus.OutputLimitExceeded, result.Status, "输出超限状态");
            AssertEqual(ProcessRunner.MaximumOutputBytes, result.StandardOutputBytes.Length, "标准输出保留上限");
            AssertEqual(true, result.StandardErrorBytes.Length <= ProcessRunner.MaximumOutputBytes, "标准错误保留上限");

            result = await runner.RunAsync(CreateRunRequest(executablePath, "near-limit"));
            AssertEqual(ProcessExecutionStatus.Completed, result.Status, "限制内大量输出状态");
            AssertEqual(60_000, result.StandardOutput.Length, "限制内标准输出");
            AssertEqual(60_000, result.StandardError.Length, "限制内标准错误");

            result = await runner.RunAsync(CreateRunRequest(executablePath, "stderr-flood"));
            AssertEqual(ProcessExecutionStatus.OutputLimitExceeded, result.Status, "标准错误超限状态");
            AssertEqual(ProcessRunner.MaximumOutputBytes, result.StandardErrorBytes.Length, "标准错误保留上限");

            result = await runner.RunAsync(CreateRunRequest(
                executablePath,
                "args",
                arguments: ["value with spaces", "second"]));
            AssertEqual("value with spaces|second", result.StandardOutput, "参数列表传递");

            result = await runner.RunAsync(CreateRunRequest(executablePath, "raw-utf8"));
            AssertEqual("中文输出", result.StandardOutput, "UTF-8 中文输出自动识别");

            result = await runner.RunAsync(CreateRunRequest(executablePath, "raw-gbk"));
            AssertEqual("中文输出", result.StandardOutput, "GBK 中文输出回退解码");

            result = await runner.RunAsync(CreateRunRequest(
                executablePath,
                "sleep",
                timeout: TimeSpan.FromMilliseconds(100),
                arguments: ["2000"]));
            AssertEqual(ProcessExecutionStatus.TimedOut, result.Status, "执行超时");

            using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
            {
                result = await runner.RunAsync(
                    CreateRunRequest(
                        executablePath,
                        "sleep",
                        timeout: TimeSpan.FromSeconds(2),
                        arguments: ["2000"]),
                    cancellationSource.Token);
            }
            AssertEqual(ProcessExecutionStatus.Cancelled, result.Status, "用户取消");

            result = await runner.RunAsync(new ProcessRunRequest
            {
                FilePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.exe"),
                Timeout = TimeSpan.FromSeconds(1)
            });
            AssertEqual(ProcessExecutionStatus.StartFailed, result.Status, "路径不存在");
        }

        /// <summary>
        /// 运行批量调度、状态分类、并发和取消的最小回归检查。
        /// </summary>
        private static async Task RunBatchComparerChecksAsync()
        {
            var demoPath = Environment.ProcessPath
                ?? throw new InvalidOperationException("无法确定回归检查程序路径。");
            var studentPath = typeof(TestCase).Assembly.Location;
            var progressValues = new List<int>();
            var statusComparer = new BatchComparer((request, _) =>
                Task.FromResult(CreateScriptedExecution(request, demoPath)));
            var request = CreateSessionRequest(
                demoPath,
                studentPath,
                ["pass", "different", "student-timeout", "demo-timeout", "student-start", "cancelled"]);

            var result = await statusComparer.RunAsync(
                request,
                new ImmediateProgress<int>(progressValues.Add));
            AssertEqual(6, result.TotalCount, "批量结果数量");
            AssertEqual(TestCaseRunStatus.Passed, result.Results[0].Status, "通过状态");
            AssertEqual(TestCaseRunStatus.Failed, result.Results[1].Status, "未通过状态");
            AssertEqual(TestCaseRunStatus.TimedOut, result.Results[2].Status, "学生超时状态");
            AssertEqual(TestCaseRunStatus.InvalidTestData, result.Results[3].Status, "Demo 超时状态");
            AssertEqual(ExecutionTarget.Demo, result.Results[3].FailedTarget, "Demo 超时归属");
            AssertEqual(TestCaseRunStatus.ExecutionFailed, result.Results[4].Status, "学生启动失败状态");
            AssertEqual(ExecutionTarget.Student, result.Results[4].FailedTarget, "学生启动失败归属");
            AssertEqual(TestCaseRunStatus.Cancelled, result.Results[5].Status, "用例取消状态");
            AssertEqual("1,2,3,4,5,6", string.Join(',', result.Results.Select(item => item.TestCase.Index)), "并行结果顺序");
            AssertEqual("1,2,3,4,5,6", string.Join(',', progressValues), "完成进度单调递增");

            var invalidOutputComparer = new BatchComparer((processRequest, _) =>
                Task.FromResult(new ProcessExecutionResult
                {
                    Status = ProcessExecutionStatus.Completed,
                    StandardOutput = processRequest.FilePath == demoPath
                        ? processRequest.StandardInput == "demo-empty" ? string.Empty : "A\n"
                        : processRequest.StandardInput == "student-empty" ? string.Empty : "A\n",
                    ExitCode = 7
                }));
            result = await invalidOutputComparer.RunAsync(CreateSessionRequest(
                demoPath,
                studentPath,
                ["demo-empty", "student-empty", "nonzero-exit"]));
            AssertEqual(TestCaseRunStatus.InvalidTestData, result.Results[0].Status, "Demo 空输出状态");
            AssertEqual(TestCaseRunStatus.Failed, result.Results[1].Status, "学生空输出状态");
            AssertEqual(TestCaseRunStatus.Passed, result.Results[2].Status, "非零退出码仍按输出比较");

            var activeCount = 0;
            var maximumActiveCount = 0;
            var concurrencyComparer = new BatchComparer(async (_, token) =>
            {
                var currentCount = Interlocked.Increment(ref activeCount);
                UpdateMaximum(ref maximumActiveCount, currentCount);
                try
                {
                    await Task.Delay(30, token);
                    return new ProcessExecutionResult
                    {
                        Status = ProcessExecutionStatus.Completed,
                        StandardOutput = "A\n"
                    };
                }
                finally
                {
                    Interlocked.Decrement(ref activeCount);
                }
            });
            var concurrencyRequest = CreateSessionRequest(
                demoPath,
                studentPath,
                Enumerable.Range(1, 12).Select(index => $"case-{index}").ToArray());
            concurrencyRequest.MaxParallelism = 3;
            await concurrencyComparer.RunAsync(concurrencyRequest);
            AssertEqual(3, maximumActiveCount, "最大并行数");

            var cancellationComparer = new BatchComparer(async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return new ProcessExecutionResult { Status = ProcessExecutionStatus.Completed };
            });
            var cancellationProgress = new List<int>();
            using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            result = await cancellationComparer.RunAsync(
                CreateSessionRequest(demoPath, studentPath, ["A", "B", "C", "D", "E"]),
                new ImmediateProgress<int>(cancellationProgress.Add),
                cancellationToken: cancellationSource.Token);
            AssertEqual(5, result.CancelledCount, "取消补齐全部用例");
            AssertEqual(5, cancellationProgress.Last(), "取消后进度达到总数");

            var parser = new TestDataParser();
            var argumentCases = parser.Parse(
                "[args-1]\n# @args: --process-runner-child args --value=123\n" +
                "[args-2]\n# @args: --process-runner-child args --label \"two words\"\n");
            result = await new BatchComparer().RunAsync(new TestSessionRequest
            {
                DemoExePath = demoPath,
                StudentExePath = demoPath,
                Timeout = TimeSpan.FromSeconds(3),
                MaxParallelism = 2,
                TestCases = argumentCases
            });
            AssertEqual(2, result.PassedCount, "真实参数批量执行");
            AssertEqual("--value=123", result.Results[0].DemoExecution?.StandardOutput, "Demo 第一组参数");
            AssertEqual("--value=123", result.Results[0].StudentExecution?.StandardOutput, "学生第一组参数");
            AssertEqual("--label|two words", result.Results[1].DemoExecution?.StandardOutput, "Demo 第二组参数");
            AssertEqual("--label|two words", result.Results[1].StudentExecution?.StandardOutput, "学生第二组参数");

            var combinedCases = parser.Parse(
                "[combined]\n# @args: --process-runner-child args-stdin --label \"two words\"\ninput\n");
            result = await new BatchComparer().RunAsync(new TestSessionRequest
            {
                DemoExePath = demoPath,
                StudentExePath = demoPath,
                Timeout = TimeSpan.FromSeconds(3),
                TestCases = combinedCases
            });
            AssertEqual(1, result.PassedCount, "标准输入与参数组合执行");
            AssertEqual("--label|two words::input\n", result.Results[0].DemoExecution?.StandardOutput, "组合执行内容");
        }

        /// <summary>
        /// 根据测试输入和执行目标生成状态分类测试所需的程序结果。
        /// </summary>
        /// <param name="request">程序运行请求。</param>
        /// <param name="demoPath">用于区分 Demo 的文件路径。</param>
        /// <returns>脚本化程序执行结果。</returns>
        private static ProcessExecutionResult CreateScriptedExecution(
            ProcessRunRequest request,
            string demoPath)
        {
            var isDemo = request.FilePath == demoPath;
            return request.StandardInput switch
            {
                "different" => new ProcessExecutionResult
                {
                    Status = ProcessExecutionStatus.Completed,
                    StandardOutput = isDemo ? "A\n" : "B\n"
                },
                "student-timeout" when !isDemo =>
                    new ProcessExecutionResult { Status = ProcessExecutionStatus.TimedOut },
                "demo-timeout" when isDemo =>
                    new ProcessExecutionResult { Status = ProcessExecutionStatus.TimedOut },
                "student-start" when !isDemo =>
                    new ProcessExecutionResult
                    {
                        Status = ProcessExecutionStatus.StartFailed,
                        ErrorMessage = "start failed"
                    },
                "cancelled" when !isDemo =>
                    new ProcessExecutionResult { Status = ProcessExecutionStatus.Cancelled },
                _ => new ProcessExecutionResult
                {
                    Status = ProcessExecutionStatus.Completed,
                    StandardOutput = "A\n",
                    ExitCode = 0
                }
            };
        }

        /// <summary>
        /// 创建使用指定输入标签的批量会话请求。
        /// </summary>
        /// <param name="demoPath">Demo 文件路径。</param>
        /// <param name="studentPath">学生程序文件路径。</param>
        /// <param name="inputs">依次作为用例输入的标签。</param>
        /// <returns>符合批量调度范围的测试请求。</returns>
        private static TestSessionRequest CreateSessionRequest(
            string demoPath,
            string studentPath,
            IReadOnlyList<string> inputs) =>
            new()
            {
                DemoExePath = demoPath,
                StudentExePath = studentPath,
                Timeout = TimeSpan.FromSeconds(3),
                MaxParallelism = 4,
                TestCases = inputs.Select((input, index) => new TestCase
                {
                    Index = index + 1,
                    GroupName = $"case-{index + 1}",
                    InputData = input
                }).ToArray()
            };

        /// <summary>
        /// 以无锁比较交换方式更新并发峰值。
        /// </summary>
        /// <param name="maximumValue">当前最大值。</param>
        /// <param name="candidateValue">新的候选值。</param>
        private static void UpdateMaximum(ref int maximumValue, int candidateValue)
        {
            while (true)
            {
                var currentValue = Volatile.Read(ref maximumValue);
                if (candidateValue <= currentValue ||
                    Interlocked.CompareExchange(ref maximumValue, candidateValue, currentValue) == currentValue)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 创建调用当前回归检查程序的 ProcessRunner 请求。
        /// </summary>
        /// <param name="executablePath">当前测试程序路径。</param>
        /// <param name="mode">子进程测试模式。</param>
        /// <param name="standardInput">写入子进程的标准输入。</param>
        /// <param name="timeout">执行超时时间；未指定时为两秒。</param>
        /// <param name="arguments">模式之后附加的命令行参数。</param>
        /// <returns>使用 UTF-8 读写的执行请求。</returns>
        private static ProcessRunRequest CreateRunRequest(
            string executablePath,
            string mode,
            string standardInput = "",
            TimeSpan? timeout = null,
            IReadOnlyList<string>? arguments = null)
        {
            var allArguments = new List<string> { "--process-runner-child", mode };
            if (arguments is not null)
            {
                allArguments.AddRange(arguments);
            }

            return new ProcessRunRequest
            {
                FilePath = executablePath,
                StandardInput = standardInput,
                Arguments = allArguments,
                Timeout = timeout ?? TimeSpan.FromSeconds(2),
                EncodingCodePage = TextEncodingHelper.GbkCodePage
            };
        }

        /// <summary>
        /// 运行测试数据文件常见编码的最小解码检查。
        /// </summary>
        private static void RunTextEncodingChecks()
        {
            const string text = "测试数据\r\n中文输入";
            var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(text);
            AssertEqual(text, TextEncodingHelper.Decode(utf8Bytes), "无 BOM UTF-8");

            var utf8WithBom = System.Text.Encoding.UTF8.Preamble.ToArray().Concat(utf8Bytes).ToArray();
            AssertEqual(text, TextEncodingHelper.Decode(utf8WithBom), "带 BOM UTF-8");

            var gbkBytes = TextEncodingHelper.GetEncoding(TextEncodingHelper.GbkCodePage).GetBytes(text);
            AssertEqual(text, TextEncodingHelper.Decode(gbkBytes), "GBK");

            var utf16Bytes = System.Text.Encoding.Unicode.Preamble.ToArray()
                .Concat(System.Text.Encoding.Unicode.GetBytes(text))
                .ToArray();
            AssertEqual(text, TextEncodingHelper.Decode(utf16Bytes), "UTF-16 LE BOM");
        }

        /// <summary>
        /// 运行与 tc 2.0.3 行为对齐的文本比较器回归检查。
        /// </summary>
        private static void RunTextComparerChecks()
        {
            var result = new TextComparer().Compare("A\r\nB\r\n", "A\r\nB\r\n");
            AssertEqual(ComparisonStatus.Passed, result.Status, "相同文本状态");
            AssertEqual(true, result.IsPassed, "相同文本通过");

            result = new TextComparer().Compare("A\n", "B\n");
            AssertEqual(1, result.DiffLineCount, "普通字符差异数量");
            AssertEqual(DifferenceType.ContentMismatch, result.DiffDetails[0].Type, "普通字符差异类型");
            AssertEqual(0, result.DiffDetails[0].FirstDifferenceIndex, "首个差异字符");
            AssertEqual(1, result.DiffDetails[0].DifferentPositions1.Count, "文本一高亮数量");
            AssertEqual(1, result.DiffDetails[0].DifferentPositions2.Count, "文本二高亮数量");

            result = new TextComparer().Compare("A\n", "AB\n");
            AssertEqual(DifferenceType.File2HasExtraCharacters, result.DiffDetails[0].Type, "文本二多余字符");
            AssertEqual(1, result.DiffDetails[0].FirstDifferenceIndex, "文本二多余字符位置");

            result = new TextComparer().Compare("AB\n", "A\n");
            AssertEqual(DifferenceType.File1HasExtraCharacters, result.DiffDetails[0].Type, "文本一多余字符");

            result = new TextComparer().Compare("A\rB\r", "AB\r");
            AssertEqual(2, result.DiffLineCount, "纯 CR 行差异数量");
            AssertEqual(DifferenceType.File2HasExtraCharacters, result.DiffDetails[0].Type, "纯 CR 首行差异");
            AssertEqual(DifferenceType.File2Ended, result.DiffDetails[1].Type, "纯 CR 文件末尾差异");

            result = new TextComparer().Compare("A\rB\r", "A\nB\n");
            AssertEqual(true, result.IsPassed, "默认忽略合法换行风格差异");

            result = new TextComparer(new CompareOptions { CrCrLfNotEqual = true })
                .Compare("A\rB\r", "A\nB\n");
            AssertEqual(2, result.DiffLineCount, "严格换行差异数量");
            AssertEqual(DifferenceType.LineEndingMismatch, result.DiffDetails[0].Type, "严格换行差异类型");

            result = new TextComparer().Compare("A", "A\n");
            AssertEqual(DifferenceType.LineEndingMismatch, result.DiffDetails[0].Type, "EOF 与换行符差异");
            AssertEqual(false, result.DiffDetails[0].HexDump1?.Contains("1a") == true, "EOF 不伪造 1A 字节");
            AssertEqual("41 ", result.DiffDetails[0].HexDump1?[11..14], "无换行结尾真实字节");

            result = new TextComparer().Compare("A\rB\n", "A\nB\n");
            AssertEqual(ComparisonStatus.InvalidInput, result.Status, "混合换行输入状态");
            AssertEqual(ComparisonErrorType.MixedLineEndings, result.ErrorType, "混合换行错误类型");
            AssertEqual(1, result.InvalidInputNumber, "混合换行输入编号");

            result = new TextComparer().Compare(string.Empty, "A\n");
            AssertEqual(ComparisonErrorType.EmptyText, result.ErrorType, "空文本错误类型");

            result = new TextComparer().Compare("A\n", "A\nX\nY\nZ\n");
            AssertEqual(1, result.DiffLineCount, "EOF 后只报告首个多余行");
            AssertEqual(DifferenceType.File1Ended, result.DiffDetails[0].Type, "文本一结束差异");
            AssertEqual("X", result.DiffDetails[0].File2Content, "EOF 差异只包含首个多余行");

            result = new TextComparer(new CompareOptions { TrimMode = TrimType.All })
                .Compare(" A \n", "A\n");
            AssertEqual(true, result.IsPassed, "trim all");

            result = new TextComparer(new CompareOptions { TrimMode = TrimType.Left })
                .Compare(" A \n", "A\n");
            AssertEqual(DifferenceType.File1HasExtraCharacters, result.DiffDetails[0].Type, "trim left");

            result = new TextComparer(new CompareOptions { IgnoreBlank = true })
                .Compare("A\n\nB\n", "A\nB\n");
            AssertEqual(true, result.IsPassed, "忽略空行");

            result = new TextComparer(new CompareOptions { IgnoreBlank = true })
                .Compare("A\n \t\nB\n", "A\nB\n");
            AssertEqual(false, result.IsPassed, "未 trim 时空白字符行不算空行");

            result = new TextComparer(new CompareOptions { IgnoreBlank = true, TrimMode = TrimType.All })
                .Compare("A\n \t\nB\n", "A\nB\n");
            AssertEqual(true, result.IsPassed, "trim 后忽略空行");

            result = new TextComparer(new CompareOptions { LineSkip = 1 })
                .Compare("X\nA\n", "Y\nA\n");
            AssertEqual(true, result.IsPassed, "同时跳过前置行");

            result = new TextComparer(new CompareOptions { LineOffset = -1 })
                .Compare("X\nA\n", "A\n");
            AssertEqual(true, result.IsPassed, "负行偏移跳过文本一");

            result = new TextComparer(new CompareOptions { LineOffset = 1 })
                .Compare("A\n", "X\nA\n");
            AssertEqual(true, result.IsPassed, "正行偏移跳过文本二");

            result = new TextComparer(new CompareOptions { MaxLineCount = 1 })
                .Compare("A\nB\n", "A\nX\n");
            AssertEqual(true, result.IsPassed, "最大比较行数");

            result = new TextComparer(new CompareOptions { MaxDiffCount = 1 })
                .Compare("A\nB\nC\n", "X\nY\nZ\n");
            AssertEqual(1, result.DiffLineCount, "最大差异行数");

            result = new TextComparer().Compare("你A\n", "你B\n");
            AssertEqual(1, result.DiffDetails[0].FirstDifferenceIndex, "Unicode 字符级位置");
            AssertEqual($"0  {Environment.NewLine}012", result.DiffDetails[0].Ruler1, "中文字符显示宽度标尺");
            AssertEqual(true, result.DiffDetails[0].HexDump1?.Contains("0a") == true, "十六进制转储包含换行");

            result = new TextComparer().Compare("A\tB\n", "A B\n");
            AssertEqual($"0  {Environment.NewLine}012", result.DiffDetails[0].Ruler1, "不可见字符统一单列标尺");

            result = new TextComparer().Compare("0123456789ABCDEF\n", "X\n");
            var firstHexRow = result.DiffDetails[0].HexDump1?.Split(Environment.NewLine)[0];
            AssertEqual(16, firstHexRow?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length - 2, "HEX 每行显示 16 字节");

            var gbk = TextEncodingHelper.GetEncoding(TextEncodingHelper.GbkCodePage);
            var gbkBytes = gbk.GetBytes("中A\r\n\r\n尾");
            var utf8Bytes = System.Text.Encoding.UTF8.GetBytes("中B\r\nX\r\n尾");
            result = new TextComparer(new CompareOptions { CrCrLfNotEqual = true })
                .Compare(gbkBytes, utf8Bytes);
            AssertEqual("GBK", result.EncodingName1, "文本一编码名称");
            AssertEqual("UTF-8", result.EncodingName2, "文本二编码名称");
            AssertEqual(true, result.DiffDetails[0].File1RawBytes.SequenceEqual(gbk.GetBytes("中A\r\n")), "GBK 真实行字节");
            AssertEqual(true, result.DiffDetails[0].File2RawBytes.SequenceEqual(System.Text.Encoding.UTF8.GetBytes("中B\r\n")), "UTF-8 真实行字节");
            AssertEqual(true, result.DiffDetails.Any(item => item.File1Content.Length == 0), "实际空行被保留");

            result = new TextComparer().Compare(
                System.Text.Encoding.UTF8.GetBytes("A\n"),
                System.Text.Encoding.UTF8.GetBytes("A\nB\n"));
            AssertEqual(false, result.DiffDetails[0].File1HasLine, "提前 EOF 标记文本一无对应行");
            AssertEqual(true, result.DiffDetails[0].File2HasLine, "提前 EOF 标记文本二有对应行");
            AssertEqual(0, result.DiffDetails[0].File1RawBytes.Length, "提前 EOF 没有虚构字节");

            result = new TextComparer(new CompareOptions { TrimMode = TrimType.All, LineSkip = 1 })
                .Compare(
                    System.Text.Encoding.UTF8.GetBytes("skip\n A \n"),
                    System.Text.Encoding.UTF8.GetBytes("other\nB\n"));
            AssertEqual(2, result.DiffDetails[0].LineNumber1, "trim 和跳行后保留原始行号");
            AssertEqual(true, result.DiffDetails[0].File1RawBytes.SequenceEqual(System.Text.Encoding.UTF8.GetBytes(" A \n")), "trim 后保留原始字节");

            var utf16Left = System.Text.Encoding.Unicode.Preamble.ToArray()
                .Concat(System.Text.Encoding.Unicode.GetBytes("甲A\r\n"))
                .ToArray();
            var utf16Right = System.Text.Encoding.Unicode.Preamble.ToArray()
                .Concat(System.Text.Encoding.Unicode.GetBytes("甲B\r\n"))
                .ToArray();
            result = new TextComparer().Compare(utf16Left, utf16Right);
            AssertEqual("UTF-16 LE", result.EncodingName1, "UTF-16 LE 编码名称");
            AssertEqual(true, result.DiffDetails[0].File1RawBytes.SequenceEqual(utf16Left), "UTF-16 首行真实字节包含 BOM");

            result = new TextComparer().Compare(
                [0x41, 0x1A, 0x0A],
                [0x41, 0x42, 0x0A]);
            AssertEqual(true, result.DiffDetails[0].HexDump1?.Contains("1a") == true, "真实 1A 字节保留");
            AssertEqual(1, result.DiffDetails[0].FirstDifferenceIndex, "真实 1A 作为普通字符比较");

            result = new TextComparer(new CompareOptions { IgnoreBlank = true })
                .Compare("\nA\nC\n", "A\nB\n");
            AssertEqual(3, result.DiffDetails[0].LineNumber1, "忽略空行后文本一原始行号");
            AssertEqual(2, result.DiffDetails[0].LineNumber2, "忽略空行后文本二原始行号");

            result = new TextComparer().Compare("0123456789ABC\n", "012X456Y89ABZ\n");
            AssertEqual("3,7,12", string.Join(',', result.DiffDetails[0].DifferentPositions1), "长行不连续差异位置");
            AssertEqual("3,7,12", string.Join(',', result.DiffDetails[0].DifferentPositions2), "长行两侧高亮位置一致");

            try
            {
                new TextComparer(new CompareOptions { LineOffset = 101 }).Compare("A\n", "A\n");
                throw new InvalidOperationException("非法参数未被拒绝。");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        /// <summary>
        /// 比较期望值和实际值，不一致时终止回归检查。
        /// </summary>
        /// <typeparam name="T">待比较值的类型。</typeparam>
        /// <param name="expected">期望值。</param>
        /// <param name="actual">实际值。</param>
        /// <param name="name">检查项目名称。</param>
        private static void AssertEqual<T>(T expected, T actual, string name)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{name}检查失败：期望 [{expected}]，实际 [{actual}]。");
            }
        }

        /// <summary>
        /// 将进度回调同步转发给测试断言，避免线程池调度影响顺序检查。
        /// </summary>
        /// <typeparam name="T">进度值类型。</typeparam>
        private sealed class ImmediateProgress<T> : IProgress<T>
        {
            private readonly Action<T> _report;

            /// <summary>
            /// 创建同步进度转发器。
            /// </summary>
            /// <param name="report">进度接收操作。</param>
            public ImmediateProgress(Action<T> report)
            {
                _report = report;
            }

            /// <summary>
            /// 立即报告一个进度值。
            /// </summary>
            /// <param name="value">进度值。</param>
            public void Report(T value) => _report(value);
        }
    }
}
