using System.Collections;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace HomeworkChecker.UI.Views.Controls
{
    /// <summary>
    /// 将一行文本中的不可见字符可视化，并高亮比较器报告的字符位置。
    /// </summary>
    public sealed class DiffLineTextBlock : TextBlock
    {
        public static readonly DependencyProperty LineTextProperty = DependencyProperty.Register(
            nameof(LineText),
            typeof(string),
            typeof(DiffLineTextBlock),
            new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

        public static readonly DependencyProperty DifferentPositionsProperty = DependencyProperty.Register(
            nameof(DifferentPositions),
            typeof(IEnumerable),
            typeof(DiffLineTextBlock),
            new PropertyMetadata(null, OnDisplayPropertyChanged));

        public string LineText
        {
            get => (string)GetValue(LineTextProperty);
            set => SetValue(LineTextProperty, value);
        }

        public IEnumerable? DifferentPositions
        {
            get => (IEnumerable?)GetValue(DifferentPositionsProperty);
            set => SetValue(DifferentPositionsProperty, value);
        }

        /// <summary>
        /// 在文本或差异位置变化后重新生成内联显示内容。
        /// </summary>
        /// <param name="dependencyObject">发生变化的控件。</param>
        /// <param name="eventArgs">依赖属性变化参数。</param>
        private static void OnDisplayPropertyChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            ((DiffLineTextBlock)dependencyObject).RebuildInlines();
        }

        /// <summary>
        /// 合并连续的相同样式字符，生成尽量少的 Run 元素。
        /// </summary>
        private void RebuildInlines()
        {
            Inlines.Clear();
            var positions = DifferentPositions?.Cast<int>().ToHashSet() ?? [];
            var segment = new StringBuilder();
            bool? isDifferentSegment = null;

            for (var index = 0; index < LineText.Length; index++)
            {
                var isDifferent = positions.Contains(index);
                if (isDifferentSegment != isDifferent && segment.Length > 0)
                {
                    AddSegment(segment.ToString(), isDifferentSegment == true);
                    segment.Clear();
                }

                isDifferentSegment = isDifferent;
                segment.Append(VisualizeCharacter(LineText[index]));
            }

            if (segment.Length > 0)
            {
                AddSegment(segment.ToString(), isDifferentSegment == true);
            }
        }

        /// <summary>
        /// 添加普通或高亮的文本片段。
        /// </summary>
        /// <param name="text">已经可视化的文本。</param>
        /// <param name="isDifferent">是否为差异字符。</param>
        private void AddSegment(string text, bool isDifferent)
        {
            var run = new Run(text);
            if (isDifferent)
            {
                run.Background = new SolidColorBrush(Color.FromArgb(0x66, 0xD1, 0x34, 0x38));
                run.TextDecorations = System.Windows.TextDecorations.Underline;
                run.FontWeight = FontWeights.SemiBold;
            }

            Inlines.Add(run);
        }

        /// <summary>
        /// 将空格以外的空白、控制字符和不可见格式字符转换为单字符可见表示。
        /// </summary>
        /// <param name="character">原始字符。</param>
        /// <returns>普通空格原样返回，其他不可见字符返回句点，其余字符原样返回。</returns>
        private static string VisualizeCharacter(char character) =>
            character != ' ' &&
            (char.IsWhiteSpace(character) ||
             char.IsControl(character) ||
             char.GetUnicodeCategory(character) == UnicodeCategory.Format)
                ? "."
                : character.ToString();
    }
}
