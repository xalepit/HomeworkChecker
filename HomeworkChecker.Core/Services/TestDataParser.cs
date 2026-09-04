using HomeworkChecker.Core.Models;
using System.Text;

namespace HomeworkChecker.Core.Services
{
    /// <summary>
    /// 按 get_input_data 的组格式将原始文本解析为顺序测试用例。
    /// </summary>
    public sealed class TestDataParser
    {
        /// <summary>
        /// 解析测试数据文本；无分组输入保留开头空白行，组标题前的空白行不生成用例。
        /// </summary>
        /// <param name="rawText">待解析的完整测试数据文本。</param>
        /// <returns>按文本出现顺序排列的测试用例。</returns>
        public IReadOnlyList<TestCase> Parse(string rawText)
        {
            ArgumentNullException.ThrowIfNull(rawText);

            var testCases = new List<TestCase>();
            var inputBuilder = new StringBuilder();
            var currentGroupName = string.Empty;
            var argumentText = string.Empty;
            IReadOnlyList<string> arguments = [];
            var hasCurrentCase = false;
            var hasArgumentsDirective = false;
            var canReadArguments = true;
            var position = 0;
            var lineNumber = 0;

            while (position < rawText.Length)
            {
                lineNumber++;
                var lineStart = position;
                while (position < rawText.Length && rawText[position] is not '\r' and not '\n')
                {
                    position++;
                }

                var contentEnd = position;
                if (position < rawText.Length && rawText[position] == '\r')
                {
                    position++;
                    if (position < rawText.Length && rawText[position] == '\n')
                    {
                        position++;
                    }
                }
                else if (position < rawText.Length)
                {
                    position++;
                }

                var lineContent = rawText[lineStart..contentEnd];
                var trimmedStart = lineContent.TrimStart(' ', '\t');
                if (canReadArguments &&
                    TryGetArgumentsDirective(lineContent, out var currentArgumentText))
                {
                    if (hasArgumentsDirective)
                    {
                        throw new TestDataFormatException(
                            TestDataFormatError.DuplicateArguments,
                            lineNumber);
                    }

                    argumentText = currentArgumentText;
                    arguments = ParseArguments(argumentText, lineNumber);
                    hasArgumentsDirective = true;
                    hasCurrentCase = true;
                    continue;
                }

                if (trimmedStart.StartsWith('#'))
                {
                    continue;
                }

                if (TryGetGroupName(lineContent, out var groupName))
                {
                    if (hasCurrentCase)
                    {
                        testCases.Add(new TestCase
                        {
                            Index = testCases.Count + 1,
                            GroupName = currentGroupName,
                            InputData = inputBuilder.ToString(),
                            ArgumentText = argumentText,
                            Arguments = arguments
                        });
                    }

                    inputBuilder.Clear();
                    currentGroupName = groupName;
                    argumentText = string.Empty;
                    arguments = [];
                    hasArgumentsDirective = false;
                    canReadArguments = true;
                    hasCurrentCase = true;
                    continue;
                }

                if (!hasCurrentCase && lineContent.Trim(' ', '\t').Length == 0)
                {
                    // 首个有效内容尚未确定时暂存空白；若随后出现组标题，上面的 Clear 会将其作为前导排版丢弃。
                    inputBuilder.Append(rawText, lineStart, position - lineStart);
                    canReadArguments = false;
                    continue;
                }

                hasCurrentCase = true;
                canReadArguments = false;
                inputBuilder.Append(rawText, lineStart, position - lineStart);
            }

            if (hasCurrentCase)
            {
                testCases.Add(new TestCase
                {
                    Index = testCases.Count + 1,
                    GroupName = currentGroupName,
                    InputData = inputBuilder.ToString(),
                    ArgumentText = argumentText,
                    Arguments = arguments
                });
            }

            return testCases;
        }

        /// <summary>
        /// 判断一行是否为命令行参数指令，并提取冒号后的原始参数文本。
        /// </summary>
        /// <param name="line">不含行结束符的原始行。</param>
        /// <param name="argumentText">识别成功时返回去除首尾空白的参数文本。</param>
        /// <returns>该行以“# @args:”开头时返回 true。</returns>
        private static bool TryGetArgumentsDirective(string line, out string argumentText)
        {
            const string prefix = "# @args:";
            var trimmedStart = line.TrimStart(' ', '\t');
            if (!trimmedStart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                argumentText = string.Empty;
                return false;
            }

            argumentText = trimmedStart[prefix.Length..].Trim();
            return true;
        }

        /// <summary>
        /// 将课程作业常用的引号参数文本拆分为独立参数。
        /// </summary>
        /// <param name="argumentText">参数指令中冒号后的文本。</param>
        /// <param name="lineNumber">指令所在的从 1 开始行号。</param>
        /// <returns>保留空参数及引号内空格的参数列表。</returns>
        /// <exception cref="TestDataFormatException">双引号未闭合时抛出。</exception>
        private static IReadOnlyList<string> ParseArguments(string argumentText, int lineNumber)
        {
            var arguments = new List<string>();
            var currentArgument = new StringBuilder();
            var isInQuotes = false;
            var hasToken = false;

            for (var index = 0; index < argumentText.Length; index++)
            {
                var character = argumentText[index];
                if (character == '\\' &&
                    index + 1 < argumentText.Length &&
                    argumentText[index + 1] == '"')
                {
                    currentArgument.Append('"');
                    hasToken = true;
                    index++;
                }
                else if (character == '"')
                {
                    isInQuotes = !isInQuotes;
                    hasToken = true;
                }
                else if (char.IsWhiteSpace(character) && !isInQuotes)
                {
                    if (hasToken)
                    {
                        arguments.Add(currentArgument.ToString());
                        currentArgument.Clear();
                        hasToken = false;
                    }
                }
                else
                {
                    currentArgument.Append(character);
                    hasToken = true;
                }
            }

            if (isInQuotes)
            {
                throw new TestDataFormatException(
                    TestDataFormatError.UnclosedArgumentQuote,
                    lineNumber);
            }

            if (hasToken)
            {
                arguments.Add(currentArgument.ToString());
            }

            return arguments;
        }

        /// <summary>
        /// 按配置文件组规则判断一行是否为组标题并提取组名。
        /// </summary>
        /// <param name="line">不含行结束符的原始行。</param>
        /// <param name="groupName">识别成功时返回去除内部首尾空格和制表符的组名。</param>
        /// <returns>该行符合组标题规则时返回 true，否则返回 false。</returns>
        private static bool TryGetGroupName(string line, out string groupName)
        {
            var commentIndex = line.Length;
            var semicolonIndex = line.IndexOf(';');
            var hashIndex = line.IndexOf('#');
            var slashIndex = line.IndexOf("//", StringComparison.Ordinal);

            if (semicolonIndex >= 0)
            {
                commentIndex = Math.Min(commentIndex, semicolonIndex);
            }

            if (hashIndex >= 0)
            {
                commentIndex = Math.Min(commentIndex, hashIndex);
            }

            if (slashIndex >= 0)
            {
                commentIndex = Math.Min(commentIndex, slashIndex);
            }

            var effectiveContent = line[..commentIndex].Trim(' ', '\t');
            if (effectiveContent.Length >= 2 && effectiveContent[0] == '[' && effectiveContent[^1] == ']')
            {
                groupName = effectiveContent[1..^1].Trim(' ', '\t');
                return true;
            }

            groupName = string.Empty;
            return false;
        }
    }
}
