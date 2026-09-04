using System.Text;

namespace HomeworkChecker.Core.Utilities
{
    /// <summary>
    /// 使用课程常见的 UTF 编码和 GBK 编码转换测试数据及程序输出。
    /// </summary>
    public static class TextEncodingHelper
    {
        public const int GbkCodePage = 936;

        /// <summary>
        /// 获取不抛出转换异常的指定代码页编码。
        /// </summary>
        /// <param name="codePage">Windows 代码页编号。</param>
        /// <returns>使用替换回退的编码。</returns>
        public static Encoding GetEncoding(int codePage)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(
                codePage,
                EncoderFallback.ReplacementFallback,
                DecoderFallback.ReplacementFallback);
        }

        /// <summary>
        /// 优先按 BOM 或严格 UTF-8 解码，无法识别时回退到指定代码页。
        /// </summary>
        /// <param name="bytes">待解码的原始字节。</param>
        /// <param name="fallbackCodePage">无 BOM 且不是合法 UTF-8 时使用的代码页。</param>
        /// <returns>不包含编码 BOM 的文本。</returns>
        public static string Decode(byte[] bytes, int fallbackCodePage = GbkCodePage)
            => DetectEncoding(bytes, fallbackCodePage).Text;

        /// <summary>
        /// 识别原始字节的编码，并返回解码文本、编码和 BOM 长度。
        /// </summary>
        /// <param name="bytes">待解码的原始字节。</param>
        /// <param name="fallbackCodePage">无 BOM 且不是合法 UTF-8 时使用的代码页。</param>
        /// <returns>包含实际编码信息的解码结果。</returns>
        internal static TextDecodingResult DetectEncoding(
            byte[] bytes,
            int fallbackCodePage = GbkCodePage)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
            {
                return CreateResult(bytes, Encoding.UTF8, Encoding.UTF8.Preamble.Length, "UTF-8");
            }

            if (bytes.AsSpan().StartsWith(Encoding.Unicode.Preamble))
            {
                return CreateResult(bytes, Encoding.Unicode, Encoding.Unicode.Preamble.Length, "UTF-16 LE");
            }

            if (bytes.AsSpan().StartsWith(Encoding.BigEndianUnicode.Preamble))
            {
                return CreateResult(
                    bytes,
                    Encoding.BigEndianUnicode,
                    Encoding.BigEndianUnicode.Preamble.Length,
                    "UTF-16 BE");
            }

            try
            {
                var strictUtf8 = new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true);
                return new TextDecodingResult(
                    strictUtf8.GetString(bytes),
                    Encoding.UTF8,
                    0,
                    "UTF-8");
            }
            catch (DecoderFallbackException)
            {
                var fallbackEncoding = GetEncoding(fallbackCodePage);
                var displayName = fallbackCodePage == GbkCodePage
                    ? "GBK"
                    : fallbackEncoding.EncodingName;
                return new TextDecodingResult(
                    fallbackEncoding.GetString(bytes),
                    fallbackEncoding,
                    0,
                    displayName);
            }
        }

        /// <summary>
        /// 使用已识别的 BOM 长度创建解码结果。
        /// </summary>
        /// <param name="bytes">完整原始字节。</param>
        /// <param name="encoding">实际编码。</param>
        /// <param name="preambleLength">需要跳过的 BOM 长度。</param>
        /// <param name="displayName">面向界面的编码名称。</param>
        /// <returns>不含 BOM 的解码结果。</returns>
        private static TextDecodingResult CreateResult(
            byte[] bytes,
            Encoding encoding,
            int preambleLength,
            string displayName) =>
            new(
                encoding.GetString(bytes.AsSpan(preambleLength)),
                encoding,
                preambleLength,
                displayName);
    }

    /// <summary>
    /// 表示一次编码识别和解码的结果。
    /// </summary>
    internal sealed record TextDecodingResult(
        string Text,
        Encoding Encoding,
        int PreambleLength,
        string DisplayName);
}
