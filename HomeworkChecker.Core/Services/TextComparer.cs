using HomeworkChecker.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeworkChecker.Core.Services
{
    // 负责调用文本比对逻辑
    public class TextComparer
    {
        private CompareOptions _options;

        public TextComparer(CompareOptions options)
        {
            _options = options ?? new CompareOptions();
        }
        public ComparisonResult Compare(string text1, string text2)
        {
            ComparisonResult result = new ComparisonResult();

            return result;
        }
        //        // 保留旧版高亮标记，便于 UI 侧后续做高亮渲染。
//        private const string HighlightStart = "\u0001HS\u0001";
//        private const string HighlightEnd = "\u0001HE\u0001";

//        private readonly CompareOptions _options;

//        public TextComparer(CompareOptions? options = null)
//        {
//            _options = options ?? new CompareOptions();
//        }

//        /// <summary>
//        /// 比较两段文本。
//        /// </summary>
//        public ComparisonResult Compare(string? text1, string? text2)
//        {
//            text1 ??= string.Empty;
//            text2 ??= string.Empty;

//            var result = new ComparisonResult();

//            var cursor1 = new TextCursor(text1);
//            var cursor2 = new TextCursor(text2);

//            var state1 = new LineReadState();
//            var state2 = new LineReadState();

//            // 1) line_offset 优先于 line_skip
//            if (_options.LineOffset < 0)
//            {
//                SkipLine(cursor1, state1, -_options.LineOffset);
//            }
//            else if (_options.LineOffset > 0)
//            {
//                SkipLine(cursor2, state2, _options.LineOffset);
//            }

//            SkipLine(cursor1, state1, _options.LineSkip);
//            SkipLine(cursor2, state2, _options.LineSkip);

//            // 2) 逐行比较
//            var comparedLines = 0;

//            while (true)
//            {
//                ReadNextLine(cursor1, state1);
//                ReadNextLine(cursor2, state2);

//                TrimLine(state1);
//                TrimLine(state2);

//                // 忽略空行（trim 后）
//                if (_options.IgnoreBlank)
//                {
//                    while (!state1.IsEof && IsBlankLine(state1.Line))
//                    {
//                        ReadNextLine(cursor1, state1);
//                        TrimLine(state1);
//                    }

//                    while (!state2.IsEof && IsBlankLine(state2.Line))
//                    {
//                        ReadNextLine(cursor2, state2);
//                        TrimLine(state2);
//                    }
//                }

//                comparedLines++;
//                if (_options.MaxLineCount > 0 && comparedLines > _options.MaxLineCount)
//                {
//                    break;
//                }

//                var sameContent = state1.Line == state2.Line;
//                var sameEndType = IsSameEndType(state1.Ending, state2.Ending);

//                // 两边都 EOF
//                if (state1.IsEof && state2.IsEof)
//                {
//                    if (!sameContent || !sameEndType)
//                    {
//                        AddDiffDetail(result, state1, state2, sameContent, sameEndType);
//                    }

//                    break;
//                }

//                // 只有一边 EOF
//                if ((state1.IsEof && !state2.IsEof) || (!state1.IsEof && state2.IsEof))
//                {
//                    AddDiffDetail(result, state1, state2, sameContent, sameEndType);
//                    break;
//                }

//                // 普通差异
//                if (!sameContent || !sameEndType)
//                {
//                    AddDiffDetail(result, state1, state2, sameContent, sameEndType);
//                }

//                // 达到最大差异行数
//                if (_options.MaxDiffCount > 0 && result.DiffLineCount >= _options.MaxDiffCount)
//                {
//                    break;
//                }
//            }

//            result.IsPassed = result.DiffLineCount == 0;
//            return result;
//        }

//        #region 差异构建

//        private static void AddDiffDetail(
//            ComparisonResult result,
//            LineReadState state1,
//            LineReadState state2,
//            bool sameContent,
//            bool sameEndType)
//        {
//            var line1 = state1.Line;
//            var line2 = state2.Line;

//            var len1 = line1.Length;
//            var len2 = line2.Length;
//            var maxLen = Math.Max(len1, len2);

//            var diffFlags1 = new bool[maxLen];
//            var diffFlags2 = new bool[maxLen];
//            var firstDiffPos = -1;

//            for (var i = 0; i < maxLen; i++)
//            {
//                var ch1 = i < len1 ? line1[i] : '\0';
//                var ch2 = i < len2 ? line2[i] : '\0';

//                var diff = ch1 != ch2;
//                diffFlags1[i] = diff;
//                diffFlags2[i] = diff;

//                if (diff && firstDiffPos < 0)
//                {
//                    firstDiffPos = i;
//                }
//            }

//            var reasonBody = BuildReason(state1, state2, sameContent, sameEndType, firstDiffPos, len1, len2);

//            var detail = new DiffDetail
//            {
//                LineNumber1 = state1.LineNumber,
//                LineNumber2 = state2.LineNumber,
//                Reason = $"第[{state1.LineNumber} / {state2.LineNumber}]行 - {reasonBody}",
//                Ruler = BuildRuler(maxLen),
//                File1Content = $"文件1 : {BuildHighlightedLine(line1, diffFlags1)}{GetEndingTag(state1.Ending)}",
//                File2Content = $"文件2 : {BuildHighlightedLine(line2, diffFlags2)}{GetEndingTag(state2.Ending)}",
//                HexDump1 = $"文件1(HEX) : {BuildHexDump(line1, state1.Ending)}",
//                HexDump2 = $"文件2(HEX) : {BuildHexDump(line2, state2.Ending)}"
//            };

//            result.DiffDetails.Add(detail);
//            result.DiffLineCount++;
//        }

//        private static string BuildReason(
//            LineReadState state1,
//            LineReadState state2,
//            bool sameContent,
//            bool sameEndType,
//            int firstDiffPos,
//            int len1,
//            int len2)
//        {
//            if (sameContent && !sameEndType)
//            {
//                return "行结束符不同";
//            }

//            if (state1.IsEof && !state2.IsEof)
//            {
//                return "文件1已结束/文件2仍有内容";
//            }

//            if (!state1.IsEof && state2.IsEof)
//            {
//                return "文件2已结束/文件1仍有内容";
//            }

//            if (firstDiffPos == len1 && len1 < len2)
//            {
//                return "文件2有多余字符";
//            }

//            if (firstDiffPos == len2 && len2 < len1)
//            {
//                return "文件1有多余字符";
//            }

//            if (firstDiffPos < 0)
//            {
//                firstDiffPos = 0;
//            }

//            return $"第[{firstDiffPos}]个字符开始有差异";
//        }

//        #endregion

//        #region 读取/预处理（对应 C++ read_next_line / trim / skip）

//        private static void ReadNextLine(TextCursor cursor, LineReadState state)
//        {
//            state.Line = string.Empty;
//            state.LineNumber++;

//            var c = cursor.Peek();
//            if (c == -1)
//            {
//                state.IsEof = true;
//                state.Ending = LineEndingType.EOF;
//                return;
//            }

//            var sb = new StringBuilder();

//            while (cursor.Peek() != -1)
//            {
//                c = cursor.Read();

//                if (c == '\r')
//                {
//                    var next = cursor.Peek();
//                    if (next == '\n')
//                    {
//                        cursor.Read(); // 吃掉 \n
//                        state.Line = sb.ToString();
//                        state.IsEof = false;
//                        state.Ending = LineEndingType.CRLF;
//                        return;
//                    }

//                    if (next == -1)
//                    {
//                        state.Line = sb.ToString();
//                        state.IsEof = false;
//                        state.Ending = LineEndingType.CR;
//                        return;
//                    }

//                    // 与旧版行为一致：孤立 '\r' 且后续不是 '\n' 时，直接忽略该字符继续读。
//                    continue;
//                }

//                if (c == '\n')
//                {
//                    state.Line = sb.ToString();
//                    state.IsEof = false;
//                    state.Ending = LineEndingType.LF;
//                    return;
//                }

//                sb.Append((char)c);
//            }

//            // 到达末尾（最后一行无换行符）
//            state.Line = sb.ToString();
//            state.IsEof = true;
//            state.Ending = LineEndingType.EOF;
//        }

//        private void SkipLine(TextCursor cursor, LineReadState state, int skipCount)
//        {
//            if (skipCount <= 0)
//            {
//                return;
//            }

//            for (var i = 0; i < skipCount; i++)
//            {
//                ReadNextLine(cursor, state);
//                TrimLine(state);

//                // 到 EOF 后不再循环回退，避免空输入造成死循环。
//                if (state.IsEof)
//                {
//                    break;
//                }

//                // ignore_blank 时，空行不计入跳过行数。
//                if (_options.IgnoreBlank && IsBlankLine(state.Line))
//                {
//                    i--;
//                }
//            }
//        }

//        private void TrimLine(LineReadState state)
//        {
//            state.Line = _options.TrimMode switch
//            {
//                TrimType.Left => state.Line.TrimStart(' ', '\t'),
//                TrimType.Right => state.Line.TrimEnd(' ', '\t'),
//                TrimType.All => state.Line.Trim(' ', '\t'),
//                _ => state.Line
//            };
//        }

//        private static bool IsBlankLine(string line) => line.Length == 0;

//        private bool IsSameEndType(LineEndingType e1, LineEndingType e2)
//        {
//            if (_options.CrCrLfNotEqual)
//            {
//                return e1 == e2;
//            }

//            if (e1 == e2)
//            {
//                return true;
//            }

//            // 忽略 CR/LF/CRLF 差异，EOF 仅与 EOF 相等
//            var textEnding1 = e1 is LineEndingType.CR or LineEndingType.LF or LineEndingType.CRLF;
//            var textEnding2 = e2 is LineEndingType.CR or LineEndingType.LF or LineEndingType.CRLF;
//            return textEnding1 && textEnding2;
//        }

//        #endregion

//        #region 输出辅助（标尺/高亮/HEX）

//        private static string BuildRuler(int maxLen)
//        {
//            var max = (maxLen / 10 + 2) * 10 + 1;
//            var sb = new StringBuilder();

//            sb.Append("        ");
//            sb.AppendLine(new string('-', max));

//            sb.Append("        ");
//            for (var i = 0; i <= max / 10; i++)
//            {
//                sb.Append(i % 10);
//                sb.Append("         ");
//            }

//            sb.AppendLine();

//            sb.Append("        ");
//            for (var i = 0; i < max; i++)
//            {
//                sb.Append(i % 10);
//            }

//            sb.AppendLine();

//            sb.Append("        ");
//            sb.Append(new string('-', max));

//            return sb.ToString();
//        }

//        private static string BuildHighlightedLine(string line, bool[] diffFlags)
//        {
//            var sb = new StringBuilder();

//            for (var i = 0; i < line.Length; i++)
//            {
//                if (diffFlags[i])
//                {
//                    sb.Append(HighlightStart);
//                }

//                var ch = line[i];
//                if (ch is '\r' or '\n' or '\v' or '\b' or '\a')
//                {
//                    sb.Append('X');
//                }
//                else
//                {
//                    sb.Append(ch);
//                }

//                if (diffFlags[i])
//                {
//                    sb.Append(HighlightEnd);
//                }
//            }

//            return sb.ToString();
//        }

//        private static string BuildHexDump(string line, LineEndingType ending)
//        {
//            var sb = new StringBuilder();

//            sb.Append("HEX: ");

//            // 内容部分：用 UTF-8 字节输出，便于与跨平台输出对齐。
//            var contentBytes = Encoding.UTF8.GetBytes(line);
//            foreach (var b in contentBytes)
//            {
//                sb.Append(b.ToString("X2")).Append(' ');
//            }

//            // 行结束符字节
//            switch (ending)
//            {
//                case LineEndingType.CR:
//                    sb.Append("0D ");
//                    break;
//                case LineEndingType.LF:
//                    sb.Append("0A ");
//                    break;
//                case LineEndingType.CRLF:
//                    sb.Append("0D 0A ");
//                    break;
//            }

//            sb.Append("| ").Append(GetEndingTag(ending));
//            return sb.ToString().TrimEnd();
//        }

//        private static string GetEndingTag(LineEndingType ending) =>
//            ending switch
//            {
//                LineEndingType.CR => "<CR>",
//                LineEndingType.LF => "<LF>",
//                LineEndingType.CRLF => "<CR><LF>",
//                LineEndingType.EOF => "<EOF>",
//                _ => string.Empty
//            };

//        #endregion

//        #region 内部实现类型（不放 Models，属于比较器内部状态）

//        /// <summary>
//        /// 行结束符类型（对应旧版 EndType）。
//        /// </summary>
//        private enum LineEndingType
//        {
//            None = 0,
//            CR,
//            LF,
//            CRLF,
//            EOF
//        }

//        /// <summary>
//        /// 行读取状态（旧版 FileStatus 的字符串读取版）。
//        /// </summary>
//        private sealed class LineReadState
//        {
//            public int LineNumber { get; set; }
//            public bool IsEof { get; set; }
//            public LineEndingType Ending { get; set; } = LineEndingType.None;
//            public string Line { get; set; } = string.Empty;
//        }

//        /// <summary>
//        /// 字符串游标读取器（替代 stream + peek/get）。
//        /// </summary>
//        private sealed class TextCursor
//        {
//            private readonly string _text;
//            private int _index;

//            public TextCursor(string text)
//            {
//                _text = text;
//                _index = 0;
//            }

//            public int Peek() => _index < _text.Length ? _text[_index] : -1;

//            public int Read() => _index < _text.Length ? _text[_index++] : -1;
//        }

//        #endregion
//    }
//}
//```
    }
}
