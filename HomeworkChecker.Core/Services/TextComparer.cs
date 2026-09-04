using HomeworkChecker.Core.Models;
using HomeworkChecker.Core.Utilities;
using System.Text;

namespace HomeworkChecker.Core.Services
{
    /// <summary>
    /// 按 tc 2.0.3 的行级语义比较两段文本。
    /// </summary>
    public sealed class TextComparer
    {
        private readonly CompareOptions _options;

        /// <summary>
        /// 使用指定选项创建文本比较器。
        /// </summary>
        /// <param name="options">比较选项；空值表示使用默认设置。</param>
        public TextComparer(CompareOptions? options = null)
        {
            _options = options ?? new CompareOptions();
        }

        /// <summary>
        /// 比较两段文本并返回行为单位的差异。
        /// </summary>
        /// <param name="text1">第一段待比较文本。</param>
        /// <param name="text2">第二段待比较文本。</param>
        /// <returns>包含比较状态、输入错误或逐行差异的结果。</returns>
        public ComparisonResult Compare(string text1, string text2)
        {
            ArgumentNullException.ThrowIfNull(text1);
            ArgumentNullException.ThrowIfNull(text2);
            ValidateOptions();

            return CompareParsedLines(ParseLines(text1), ParseLines(text2), "UTF-8", "UTF-8");
        }

        /// <summary>
        /// 比较两侧原始输出字节，并保留真实编码和行字节用于差异详情。
        /// </summary>
        /// <param name="bytes1">第一段输出的原始字节。</param>
        /// <param name="bytes2">第二段输出的原始字节。</param>
        /// <param name="fallbackCodePage">无法按 BOM 或 UTF-8 识别时使用的代码页。</param>
        /// <returns>包含真实字节和编码信息的比较结果。</returns>
        public ComparisonResult Compare(
            byte[] bytes1,
            byte[] bytes2,
            int fallbackCodePage = TextEncodingHelper.GbkCodePage)
        {
            ArgumentNullException.ThrowIfNull(bytes1);
            ArgumentNullException.ThrowIfNull(bytes2);
            ValidateOptions();

            var decoded1 = TextEncodingHelper.DetectEncoding(bytes1, fallbackCodePage);
            var decoded2 = TextEncodingHelper.DetectEncoding(bytes2, fallbackCodePage);
            return CompareParsedLines(
                ParseLines(bytes1, decoded1.Encoding, decoded1.PreambleLength),
                ParseLines(bytes2, decoded2.Encoding, decoded2.PreambleLength),
                decoded1.DisplayName,
                decoded2.DisplayName);
        }

        /// <summary>
        /// 校验、裁剪并比较已经解析的两侧行集合。
        /// </summary>
        /// <param name="lines1">文本一的原始行。</param>
        /// <param name="lines2">文本二的原始行。</param>
        /// <param name="encodingName1">文本一实际编码名称。</param>
        /// <param name="encodingName2">文本二实际编码名称。</param>
        /// <returns>附带编码名称的比较结果。</returns>
        private ComparisonResult CompareParsedLines(
            List<ParsedLine> lines1,
            List<ParsedLine> lines2,
            string encodingName1,
            string encodingName2)
        {
            var error = ValidateInput(lines1, 1) ?? ValidateInput(lines2, 2);
            var result = error ?? CompareLines(ApplyTrim(lines1), ApplyTrim(lines2));
            result.EncodingName1 = encodingName1;
            result.EncodingName2 = encodingName2;
            return result;
        }

        /// <summary>
        /// 检查比较选项是否位于 tc 2.0.3 支持的范围内。
        /// </summary>
        private void ValidateOptions()
        {
            if (!Enum.IsDefined(_options.TrimMode))
            {
                throw new ArgumentOutOfRangeException(nameof(_options.TrimMode));
            }

            ArgumentOutOfRangeException.ThrowIfNegative(_options.LineSkip);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_options.LineSkip, 100);
            ArgumentOutOfRangeException.ThrowIfLessThan(_options.LineOffset, -100);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_options.LineOffset, 100);
            ArgumentOutOfRangeException.ThrowIfNegative(_options.MaxDiffCount);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_options.MaxDiffCount, 100);
            ArgumentOutOfRangeException.ThrowIfNegative(_options.MaxLineCount);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_options.MaxLineCount, 10000);
        }

        /// <summary>
        /// 将文本解析为保留 CR、LF、CRLF 和文件末尾标记的行集合。
        /// </summary>
        /// <param name="text">待解析文本。</param>
        /// <returns>按出现顺序排列的行。</returns>
        private static List<ParsedLine> ParseLines(string text)
        {
            var lines = new List<ParsedLine>();
            var position = 0;

            while (position < text.Length)
            {
                var start = position;
                while (position < text.Length && text[position] is not '\r' and not '\n')
                {
                    position++;
                }

                var content = text[start..position];
                var ending = LineEndingType.Eof;
                if (position < text.Length && text[position] == '\r')
                {
                    position++;
                    if (position < text.Length && text[position] == '\n')
                    {
                        position++;
                        ending = LineEndingType.CrLf;
                    }
                    else
                    {
                        ending = LineEndingType.Cr;
                    }
                }
                else if (position < text.Length)
                {
                    position++;
                    ending = LineEndingType.Lf;
                }

                var rawText = content + (ending switch
                {
                    LineEndingType.Cr => "\r",
                    LineEndingType.Lf => "\n",
                    LineEndingType.CrLf => "\r\n",
                    _ => string.Empty
                });
                lines.Add(new ParsedLine(
                    content,
                    ending,
                    lines.Count + 1,
                    false,
                    Encoding.UTF8.GetBytes(rawText)));
            }

            return lines;
        }

        /// <summary>
        /// 按已识别编码解析真实输出字节，并让每行保留自身的原始字节。
        /// </summary>
        /// <param name="bytes">包含可选 BOM 的完整输出字节。</param>
        /// <param name="encoding">实际采用的字符编码。</param>
        /// <param name="preambleLength">需要跳过的 BOM 长度。</param>
        /// <returns>按出现顺序排列的行；首行原始字节保留真实 BOM。</returns>
        private static List<ParsedLine> ParseLines(
            byte[] bytes,
            Encoding encoding,
            int preambleLength)
        {
            return encoding.CodePage is 1200 or 1201
                ? ParseUtf16Lines(bytes, encoding, preambleLength)
                : ParseSingleByteNewlineLines(bytes, encoding, preambleLength);
        }

        /// <summary>
        /// 解析 UTF-8、GBK 等以单字节表示 CR/LF 的输出。
        /// </summary>
        /// <param name="bytes">完整输出字节。</param>
        /// <param name="encoding">实际字符编码。</param>
        /// <param name="startPosition">正文起始位置。</param>
        /// <returns>解析后的行集合。</returns>
        private static List<ParsedLine> ParseSingleByteNewlineLines(
            byte[] bytes,
            Encoding encoding,
            int startPosition)
        {
            var lines = new List<ParsedLine>();
            var position = startPosition;
            while (position < bytes.Length)
            {
                var start = position;
                while (position < bytes.Length && bytes[position] is not 0x0D and not 0x0A)
                {
                    position++;
                }

                var contentEnd = position;
                var ending = LineEndingType.Eof;
                if (position < bytes.Length && bytes[position] == 0x0D)
                {
                    position++;
                    if (position < bytes.Length && bytes[position] == 0x0A)
                    {
                        position++;
                        ending = LineEndingType.CrLf;
                    }
                    else
                    {
                        ending = LineEndingType.Cr;
                    }
                }
                else if (position < bytes.Length)
                {
                    position++;
                    ending = LineEndingType.Lf;
                }

                var rawStart = lines.Count == 0 ? 0 : start;
                lines.Add(CreateParsedLine(
                    bytes,
                    encoding,
                    start,
                    contentEnd,
                    rawStart,
                    position,
                    ending,
                    lines.Count + 1));
            }

            return lines;
        }

        /// <summary>
        /// 解析带 BOM 的 UTF-16 LE 或 UTF-16 BE 输出。
        /// </summary>
        /// <param name="bytes">完整输出字节。</param>
        /// <param name="encoding">UTF-16 编码。</param>
        /// <param name="startPosition">BOM 后正文起始位置。</param>
        /// <returns>解析后的行集合。</returns>
        private static List<ParsedLine> ParseUtf16Lines(
            byte[] bytes,
            Encoding encoding,
            int startPosition)
        {
            var lines = new List<ParsedLine>();
            var position = startPosition;
            var isLittleEndian = encoding.CodePage == 1200;
            while (position < bytes.Length)
            {
                var start = position;
                while (position + 1 < bytes.Length && !IsUtf16Newline(bytes, position, isLittleEndian))
                {
                    position += 2;
                }

                if (position + 1 >= bytes.Length)
                {
                    position = bytes.Length;
                }

                var contentEnd = position;
                var ending = LineEndingType.Eof;
                if (position + 1 < bytes.Length)
                {
                    var isCr = ReadUtf16CodeUnit(bytes, position, isLittleEndian) == '\r';
                    position += 2;
                    if (isCr && position + 1 < bytes.Length &&
                        ReadUtf16CodeUnit(bytes, position, isLittleEndian) == '\n')
                    {
                        position += 2;
                        ending = LineEndingType.CrLf;
                    }
                    else
                    {
                        ending = isCr ? LineEndingType.Cr : LineEndingType.Lf;
                    }
                }

                var rawStart = lines.Count == 0 ? 0 : start;
                lines.Add(CreateParsedLine(
                    bytes,
                    encoding,
                    start,
                    contentEnd,
                    rawStart,
                    position,
                    ending,
                    lines.Count + 1));
            }

            return lines;
        }

        /// <summary>
        /// 判断指定 UTF-16 代码单元是否为 CR 或 LF。
        /// </summary>
        /// <param name="bytes">完整输出字节。</param>
        /// <param name="position">代码单元起始位置。</param>
        /// <param name="isLittleEndian">是否为小端序。</param>
        /// <returns>当前代码单元为换行字符时返回 true。</returns>
        private static bool IsUtf16Newline(byte[] bytes, int position, bool isLittleEndian) =>
            ReadUtf16CodeUnit(bytes, position, isLittleEndian) is '\r' or '\n';

        /// <summary>
        /// 从原始字节读取一个 UTF-16 代码单元。
        /// </summary>
        /// <param name="bytes">完整输出字节。</param>
        /// <param name="position">代码单元起始位置。</param>
        /// <param name="isLittleEndian">是否为小端序。</param>
        /// <returns>读取到的字符。</returns>
        private static char ReadUtf16CodeUnit(byte[] bytes, int position, bool isLittleEndian) =>
            isLittleEndian
                ? (char)(bytes[position] | bytes[position + 1] << 8)
                : (char)(bytes[position] << 8 | bytes[position + 1]);

        /// <summary>
        /// 从一个原始字节区间创建解析行。
        /// </summary>
        /// <param name="bytes">完整输出字节。</param>
        /// <param name="encoding">实际字符编码。</param>
        /// <param name="start">行内容起始位置。</param>
        /// <param name="contentEnd">行内容结束位置。</param>
        /// <param name="rawStart">真实字节起始位置；首行可包含 BOM。</param>
        /// <param name="rawEnd">包含真实结束符的结束位置。</param>
        /// <param name="ending">行结束类型。</param>
        /// <param name="number">原始行号。</param>
        /// <returns>保留真实字节的解析行。</returns>
        private static ParsedLine CreateParsedLine(
            byte[] bytes,
            Encoding encoding,
            int start,
            int contentEnd,
            int rawStart,
            int rawEnd,
            LineEndingType ending,
            int number)
        {
            return new ParsedLine(
                encoding.GetString(bytes, start, contentEnd - start),
                ending,
                number,
                false,
                bytes[rawStart..rawEnd]);
        }

        /// <summary>
        /// 按官方规则拒绝空文本和混合换行风格文本。
        /// </summary>
        /// <param name="lines">已经解析的行。</param>
        /// <param name="inputNumber">输入编号，取 1 或 2。</param>
        /// <returns>输入有效时返回空，否则返回错误结果。</returns>
        private static ComparisonResult? ValidateInput(IReadOnlyList<ParsedLine> lines, int inputNumber)
        {
            if (lines.Count == 0)
            {
                return CreateInvalidResult(
                    ComparisonErrorType.EmptyText,
                    inputNumber,
                    $"文本{inputNumber}为空，不适用文本比较。"
                );
            }

            var lineEndingCount = lines
                .Where(line => line.Ending != LineEndingType.Eof)
                .Select(line => line.Ending)
                .Distinct()
                .Take(2)
                .Count();
            if (lineEndingCount > 1)
            {
                return CreateInvalidResult(
                    ComparisonErrorType.MixedLineEndings,
                    inputNumber,
                    $"文本{inputNumber}混用了 CR、LF 或 CRLF 行结束符。"
                );
            }

            return null;
        }

        /// <summary>
        /// 创建比较前输入错误结果。
        /// </summary>
        /// <param name="errorType">错误类型。</param>
        /// <param name="inputNumber">发生错误的输入编号。</param>
        /// <param name="message">面向调用者的错误说明。</param>
        /// <returns>输入错误结果。</returns>
        private static ComparisonResult CreateInvalidResult(
            ComparisonErrorType errorType,
            int inputNumber,
            string message)
        {
            return new ComparisonResult
            {
                Status = ComparisonStatus.InvalidInput,
                ErrorType = errorType,
                InvalidInputNumber = inputNumber,
                ErrorMessage = message
            };
        }

        /// <summary>
        /// 按当前 trim 设置处理每行内容。
        /// </summary>
        /// <param name="lines">原始行集合。</param>
        /// <returns>保留行号和结束符的处理后行集合。</returns>
        private List<ParsedLine> ApplyTrim(IReadOnlyList<ParsedLine> lines)
        {
            return lines
                .Select(line => line with { Content = Trim(line.Content) })
                .ToList();
        }

        /// <summary>
        /// 按选项去除一行首尾的空格和制表符。
        /// </summary>
        /// <param name="content">原始行内容。</param>
        /// <returns>处理后的行内容。</returns>
        private string Trim(string content)
        {
            return _options.TrimMode switch
            {
                TrimType.Left => content.TrimStart(' ', '\t'),
                TrimType.Right => content.TrimEnd(' ', '\t'),
                TrimType.All => content.Trim(' ', '\t'),
                _ => content
            };
        }

        /// <summary>
        /// 应用偏移、跳行、空行忽略和停止条件并比较全部有效行。
        /// </summary>
        /// <param name="lines1">文本一的行。</param>
        /// <param name="lines2">文本二的行。</param>
        /// <returns>比较结果。</returns>
        private ComparisonResult CompareLines(
            IReadOnlyList<ParsedLine> lines1,
            IReadOnlyList<ParsedLine> lines2)
        {
            var index1 = 0;
            var index2 = 0;

            if (_options.LineOffset < 0)
            {
                index1 = SkipLines(lines1, index1, -_options.LineOffset);
            }
            else if (_options.LineOffset > 0)
            {
                index2 = SkipLines(lines2, index2, _options.LineOffset);
            }

            index1 = SkipLines(lines1, index1, _options.LineSkip);
            index2 = SkipLines(lines2, index2, _options.LineSkip);

            var result = new ComparisonResult();
            var comparedLineCount = 0;
            while (_options.MaxLineCount == 0 || comparedLineCount < _options.MaxLineCount)
            {
                index1 = SkipBlankLines(lines1, index1);
                index2 = SkipBlankLines(lines2, index2);

                var line1 = GetLineOrEof(lines1, index1);
                var line2 = GetLineOrEof(lines2, index2);
                if (line1.IsEndOfFile && line2.IsEndOfFile)
                {
                    break;
                }

                comparedLineCount++;
                var isSameContent = line1.Content == line2.Content;
                var isSameEnding = AreLineEndingsEqual(line1.Ending, line2.Ending);
                if (!isSameContent || !isSameEnding || line1.IsEndOfFile || line2.IsEndOfFile)
                {
                    var detail = CreateDiffDetail(line1, line2, isSameContent, isSameEnding);
                    detail.DifferenceNumber = result.DiffDetails.Count + 1;
                    result.DiffDetails.Add(detail);

                    // tc 2.0.3 在任意一侧 EOF 后只报告第一条剩余行。
                    if (line1.IsEndOfFile || line2.IsEndOfFile)
                    {
                        break;
                    }

                    if (_options.MaxDiffCount > 0 && result.DiffDetails.Count >= _options.MaxDiffCount)
                    {
                        break;
                    }
                }

                index1++;
                index2++;
            }

            result.DiffLineCount = result.DiffDetails.Count;
            result.IsPassed = result.DiffLineCount == 0;
            result.Status = result.IsPassed ? ComparisonStatus.Passed : ComparisonStatus.Different;
            return result;
        }

        /// <summary>
        /// 跳过指定数量的有效行；启用 IgnoreBlank 时空行不计入数量。
        /// </summary>
        /// <param name="lines">待处理行集合。</param>
        /// <param name="index">起始索引。</param>
        /// <param name="count">需要跳过的有效行数。</param>
        /// <returns>跳过后的索引。</returns>
        private int SkipLines(IReadOnlyList<ParsedLine> lines, int index, int count)
        {
            var skipped = 0;
            while (index < lines.Count && skipped < count)
            {
                if (!_options.IgnoreBlank || lines[index].Content.Length > 0)
                {
                    skipped++;
                }

                index++;
            }

            return index;
        }

        /// <summary>
        /// 启用 IgnoreBlank 时跳过当前位置开始的所有空行。
        /// </summary>
        /// <param name="lines">待处理行集合。</param>
        /// <param name="index">起始索引。</param>
        /// <returns>首个非空行或文件末尾索引。</returns>
        private int SkipBlankLines(IReadOnlyList<ParsedLine> lines, int index)
        {
            if (!_options.IgnoreBlank)
            {
                return index;
            }

            while (index < lines.Count && lines[index].Content.Length == 0)
            {
                index++;
            }

            return index;
        }

        /// <summary>
        /// 获取当前行；索引越过末尾时生成 EOF 哨兵。
        /// </summary>
        /// <param name="lines">行集合。</param>
        /// <param name="index">当前索引。</param>
        /// <returns>实际行或 EOF 哨兵。</returns>
        private static ParsedLine GetLineOrEof(IReadOnlyList<ParsedLine> lines, int index)
        {
            return index < lines.Count
                ? lines[index]
                : new ParsedLine(string.Empty, LineEndingType.Eof, lines.Count + 1, true, []);
        }

        /// <summary>
        /// 按严格或兼容模式判断两个行结束符是否相同。
        /// </summary>
        /// <param name="ending1">文本一结束符。</param>
        /// <param name="ending2">文本二结束符。</param>
        /// <returns>当前设置下视为相同时返回 true。</returns>
        private bool AreLineEndingsEqual(LineEndingType ending1, LineEndingType ending2)
        {
            if (ending1 == ending2)
            {
                return true;
            }

            return !_options.CrCrLfNotEqual &&
                ending1 != LineEndingType.Eof &&
                ending2 != LineEndingType.Eof;
        }

        /// <summary>
        /// 创建一对行为单位的结构化差异。
        /// </summary>
        /// <param name="line1">文本一当前行。</param>
        /// <param name="line2">文本二当前行。</param>
        /// <param name="isSameContent">行内容是否相同。</param>
        /// <param name="isSameEnding">行结束符是否相同。</param>
        /// <returns>差异详情。</returns>
        private static DiffDetail CreateDiffDetail(
            ParsedLine line1,
            ParsedLine line2,
            bool isSameContent,
            bool isSameEnding)
        {
            var firstDifference = FindFirstDifference(line1.Content, line2.Content);
            var type = GetDifferenceType(
                line1,
                line2,
                isSameContent,
                isSameEnding,
                firstDifference);
            var detail = new DiffDetail
            {
                Type = type,
                LineNumber1 = line1.Number,
                LineNumber2 = line2.Number,
                FirstDifferenceIndex = type == DifferenceType.LineEndingMismatch ? -1 : firstDifference,
                Reason = GetReason(type),
                Ruler = CreateRuler(
                    line1.Content.Length >= line2.Content.Length ? line1.Content : line2.Content),
                Ruler1 = CreateRuler(line1.Content),
                Ruler2 = CreateRuler(line2.Content),
                File1Content = line1.Content,
                File2Content = line2.Content,
                File1HasLine = !line1.IsEndOfFile,
                File2HasLine = !line2.IsEndOfFile,
                File1RawBytes = line1.RawBytes,
                File2RawBytes = line2.RawBytes,
                File1LineEnding = line1.Ending,
                File2LineEnding = line2.Ending,
                HexDump1 = CreateHexDump(line1),
                HexDump2 = CreateHexDump(line2)
            };

            AddDifferentPositions(detail, line1.Content, line2.Content);
            return detail;
        }

        /// <summary>
        /// 查找两行第一个不同字符的位置。
        /// </summary>
        /// <param name="content1">文本一行内容。</param>
        /// <param name="content2">文本二行内容。</param>
        /// <returns>首个差异索引；内容相同时返回 -1。</returns>
        private static int FindFirstDifference(string content1, string content2)
        {
            var commonLength = Math.Min(content1.Length, content2.Length);
            for (var index = 0; index < commonLength; index++)
            {
                if (content1[index] != content2[index])
                {
                    return index;
                }
            }

            return content1.Length == content2.Length ? -1 : commonLength;
        }

        /// <summary>
        /// 根据内容、结束符和 EOF 状态选择兼容的差异类型。
        /// </summary>
        /// <param name="line1">文本一当前行。</param>
        /// <param name="line2">文本二当前行。</param>
        /// <param name="isSameContent">内容是否相同。</param>
        /// <param name="isSameEnding">结束符是否相同。</param>
        /// <param name="firstDifference">首个差异字符索引。</param>
        /// <returns>差异类型。</returns>
        private static DifferenceType GetDifferenceType(
            ParsedLine line1,
            ParsedLine line2,
            bool isSameContent,
            bool isSameEnding,
            int firstDifference)
        {
            if (isSameContent && !isSameEnding)
            {
                return DifferenceType.LineEndingMismatch;
            }

            if (line1.IsEndOfFile)
            {
                return DifferenceType.File1Ended;
            }

            if (line2.IsEndOfFile)
            {
                return DifferenceType.File2Ended;
            }

            if (firstDifference == line1.Content.Length && line1.Content.Length < line2.Content.Length)
            {
                return DifferenceType.File2HasExtraCharacters;
            }

            if (firstDifference == line2.Content.Length && line2.Content.Length < line1.Content.Length)
            {
                return DifferenceType.File1HasExtraCharacters;
            }

            return DifferenceType.ContentMismatch;
        }

        /// <summary>
        /// 返回差异类型的中文说明。
        /// </summary>
        /// <param name="type">差异类型。</param>
        /// <returns>简短说明。</returns>
        private static string GetReason(DifferenceType type)
        {
            return type switch
            {
                DifferenceType.LineEndingMismatch => "行结束符不同",
                DifferenceType.File1Ended => "文本一已结束，文本二仍有内容",
                DifferenceType.File2Ended => "文本二已结束，文本一仍有内容",
                DifferenceType.File1HasExtraCharacters => "文本一有多余字符",
                DifferenceType.File2HasExtraCharacters => "文本二有多余字符",
                _ => "行内容不同"
            };
        }

        /// <summary>
        /// 记录字符级不同位置，供 UI 分别高亮两行。
        /// </summary>
        /// <param name="detail">待补充的差异详情。</param>
        /// <param name="content1">文本一行内容。</param>
        /// <param name="content2">文本二行内容。</param>
        private static void AddDifferentPositions(
            DiffDetail detail,
            string content1,
            string content2)
        {
            var commonLength = Math.Min(content1.Length, content2.Length);
            for (var index = 0; index < commonLength; index++)
            {
                if (content1[index] != content2[index])
                {
                    detail.DifferentPositions1.Add(index);
                    detail.DifferentPositions2.Add(index);
                }
            }

            for (var index = commonLength; index < content1.Length; index++)
            {
                detail.DifferentPositions1.Add(index);
            }

            for (var index = commonLength; index < content2.Length; index++)
            {
                detail.DifferentPositions2.Add(index);
            }
        }

        /// <summary>
        /// 创建字符定位标尺。
        /// </summary>
        /// <param name="content">需要定位的原始行内容。</param>
        /// <returns>十位标记和个位数字组成的两行标尺。</returns>
        private static string CreateRuler(string content)
        {
            if (content.Length == 0)
            {
                return string.Empty;
            }

            var displayWidth = content.Sum(GetVisualizedCharacterWidth);
            var tens = new StringBuilder(displayWidth);
            var units = new StringBuilder(displayWidth);
            for (var index = 0; index < displayWidth; index++)
            {
                tens.Append(index % 10 == 0 ? (char)('0' + index / 10 % 10) : ' ');
                units.Append((char)('0' + index % 10));
            }

            return $"{tens}{Environment.NewLine}{units}";
        }

        /// <summary>
        /// 返回字符在差异正文中的显示列宽，使标尺与可视化文本对齐。
        /// </summary>
        /// <param name="character">原始字符。</param>
        /// <returns>该字符可视化后的等宽显示列数。</returns>
        private static int GetVisualizedCharacterWidth(char character)
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                return 1;
            }

            return IsWideCharacter(character) ? 2 : 1;
        }

        /// <summary>
        /// 判断 BMP 字符是否通常以两个等宽列显示。
        /// </summary>
        /// <param name="character">待判断字符。</param>
        /// <returns>属于常见全角或东亚宽字符范围时返回 true。</returns>
        private static bool IsWideCharacter(char character)
        {
            return character is >= '\u1100' and <= '\u115F' or
                >= '\u2E80' and <= '\uA4CF' or
                >= '\uAC00' and <= '\uD7A3' or
                >= '\uF900' and <= '\uFAFF' or
                >= '\uFE10' and <= '\uFE6F' or
                >= '\uFF01' and <= '\uFF60' or
                >= '\uFFE0' and <= '\uFFE6';
        }

        /// <summary>
        /// 使用该行真实字节生成十六进制转储，不为 EOF 虚构字节。
        /// </summary>
        /// <param name="line">待转储行。</param>
        /// <returns>每行最多 16 字节的十六进制转储。</returns>
        private static string CreateHexDump(ParsedLine line)
        {
            var output = new StringBuilder();
            for (var offset = 0; offset < line.RawBytes.Length; offset += 16)
            {
                var count = Math.Min(16, line.RawBytes.Length - offset);
                output.Append(offset.ToString("x8"));
                output.Append(" : ");
                for (var index = 0; index < count; index++)
                {
                    output.Append(line.RawBytes[offset + index].ToString("x2"));
                    output.Append(' ');
                }

                output.AppendLine();
            }

            return output.ToString();
        }

        private sealed record ParsedLine(
            string Content,
            LineEndingType Ending,
            int Number,
            bool IsEndOfFile,
            byte[] RawBytes);
    }
}
