using HomeworkChecker.Core.Models;
using HomeworkChecker.Core.Services;
using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.Services;
using HomeworkChecker.UI.Services.Settings;
using System.Collections.ObjectModel;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    /// <summary>
    /// 管理可执行文件选择、测试会话生命周期和主页结果展示。
    /// </summary>
    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        private const int ResultsPerPage = 24;
        private readonly IFilePickerService _filePickerService;
        private readonly ITestDataStorage _testDataStorage;
        private readonly ISettingsService _settingsService;
        private readonly ISnackbarService _snackbarService;
        private readonly TestDataParser _testDataParser;
        private readonly BatchComparer _batchComparer;
        private CancellationTokenSource? _runCancellationSource;
        private Task<BatchComparisonResult>? _activeRunTask;
        private BatchComparisonResult? _lastBatchResult;
        private bool _isInitialized;

        [ObservableProperty]
        private string _demoExePath = string.Empty;

        [ObservableProperty]
        private string _studentExePath = string.Empty;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private int _completedCount;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private ObservableCollection<TestCaseRunResult> _results = [];

        [ObservableProperty]
        private TestCaseRunResult? _selectedResult;

        [ObservableProperty]
        private double _currentPage = 1;

        public bool CanConfigure => !IsRunning;

        public bool CanStartCompare =>
            CanConfigure && File.Exists(DemoExePath) && File.Exists(StudentExePath);

        public double DemoExePathFontSize => GetPathFontSize(DemoExePath);

        public double StudentExePathFontSize => GetPathFontSize(StudentExePath);

        public string ProgressText =>
            string.Format(Translations.Current["Home_ProgressFormat"], CompletedCount, TotalCount);

        public string PathValidationMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DemoExePath) || string.IsNullOrWhiteSpace(StudentExePath))
                {
                    return Translations.Current["Home_PathRequired"];
                }

                if (!File.Exists(DemoExePath))
                {
                    return string.Format(Translations.Current["Home_PathMissingFormat"], DemoExePath);
                }

                if (!File.Exists(StudentExePath))
                {
                    return string.Format(Translations.Current["Home_PathMissingFormat"], StudentExePath);
                }

                return string.Empty;
            }
        }

        public string SelectedStatusText =>
            SelectedResult is null ? string.Empty : GetStatusText(SelectedResult.Status);

        public string SelectedDiagnosticText
        {
            get
            {
                if (SelectedResult?.Comparison is
                    { Status: ComparisonStatus.InvalidInput } comparison)
                {
                    var target = Translations.Current[comparison.InvalidInputNumber == 1
                        ? "Result_TargetDemo"
                        : "Result_TargetStudent"];
                    var errorKey = comparison.ErrorType == ComparisonErrorType.EmptyText
                        ? "ComparisonError_EmptyText"
                        : "ComparisonError_MixedLineEndings";
                    var errorMessage = string.Format(Translations.Current[errorKey], target);
                    return $"{Translations.Current["ResultDescription_InvalidTestData"]}{Environment.NewLine}{errorMessage}";
                }

                if (SelectedResult?.Status == TestCaseRunStatus.InvalidTestData)
                {
                    var summary = Translations.Current["ResultDescription_InvalidTestData"];
                    return SelectedResult.DiagnosticMessage is { Length: > 0 } invalidDataMessage
                        ? $"{summary}{Environment.NewLine}{invalidDataMessage}"
                        : summary;
                }

                if (SelectedResult?.DiagnosticMessage is { Length: > 0 } message)
                {
                    return message;
                }

                return SelectedResult is null
                    ? string.Empty
                    : Translations.Current[$"ResultDescription_{SelectedResult.Status}"];
            }
        }

        public string SelectedTargetText =>
            SelectedResult?.Status != TestCaseRunStatus.ExecutionFailed ||
            SelectedResult.FailedTarget == ExecutionTarget.None
                ? string.Empty
                : Translations.Current[$"Result_Target{SelectedResult.FailedTarget}"];

        public string SelectedArgumentText =>
            string.IsNullOrWhiteSpace(SelectedResult?.TestCase.ArgumentText)
                ? Translations.Current["Common_None"]
                : SelectedResult.TestCase.ArgumentText;

        public bool HasSelectedDifferences =>
            SelectedResult?.Comparison?.DiffDetails.Count > 0;

        public bool CanSelectPrevious => GetSelectedPosition() > 0;

        public bool CanSelectNext
        {
            get
            {
                var position = GetSelectedPosition();
                return position >= 0 && position < Results.Count - 1;
            }
        }

        public IReadOnlyList<TestCaseRunResult> PagedResults => Results
            .Skip((CurrentPageNumber - 1) * ResultsPerPage)
            .Take(ResultsPerPage)
            .ToArray();

        public int PageCount => Math.Max(1, (Results.Count + ResultsPerPage - 1) / ResultsPerPage);

        public int CurrentPageNumber => Math.Clamp((int)Math.Round(CurrentPage), 1, PageCount);

        public bool HasMultiplePages => PageCount > 1;

        public bool CanGoToPreviousPage => CurrentPageNumber > 1;

        public bool CanGoToNextPage => CurrentPageNumber < PageCount;

        public int ResultTotalCount => _lastBatchResult?.TotalCount ?? 0;

        public int ResultCompletedCount => ResultTotalCount - (_lastBatchResult?.CancelledCount ?? 0);

        public string CompletionRateText => FormatRate(ResultCompletedCount, ResultTotalCount);

        public string PassRateText => FormatRate(
            _lastBatchResult?.PassedCount ?? 0,
            ResultCompletedCount);

        public string CompletionRateColor => GetRateColor(ResultCompletedCount, ResultTotalCount);

        public string PassRateColor => GetRateColor(_lastBatchResult?.PassedCount ?? 0, ResultCompletedCount);

        public int PassedCount => _lastBatchResult?.PassedCount ?? 0;

        public int FailedCount => _lastBatchResult?.FailedCount ?? 0;

        public int TimedOutCount => _lastBatchResult?.TimedOutCount ?? 0;

        public int InvalidTestDataCount => _lastBatchResult?.InvalidTestDataCount ?? 0;

        public int ExecutionFailedCount => _lastBatchResult?.ExecutionFailedCount ?? 0;

        public int CancelledCount => _lastBatchResult?.CancelledCount ?? 0;

        public double TotalElapsedSeconds => _lastBatchResult?.Elapsed.TotalSeconds ?? 0;

        /// <summary>
        /// 创建主页 ViewModel 并接入测试会话所需的现有服务。
        /// </summary>
        /// <param name="filePickerService">可执行文件选择服务。</param>
        /// <param name="testDataStorage">测试数据缓存服务。</param>
        /// <param name="settingsService">应用设置服务。</param>
        /// <param name="snackbarService">操作提示服务。</param>
        /// <param name="testDataParser">测试数据解析器。</param>
        /// <param name="batchComparer">测试会话调度器。</param>
        public HomeViewModel(
            IFilePickerService filePickerService,
            ITestDataStorage testDataStorage,
            ISettingsService settingsService,
            ISnackbarService snackbarService,
            TestDataParser testDataParser,
            BatchComparer batchComparer)
        {
            _filePickerService = filePickerService;
            _testDataStorage = testDataStorage;
            _settingsService = settingsService;
            _snackbarService = snackbarService;
            _testDataParser = testDataParser;
            _batchComparer = batchComparer;
            Translations.CultureChanged += OnCultureChanged;
        }

        /// <summary>
        /// 首次进入主页时恢复上次选择的两个可执行文件路径。
        /// </summary>
        /// <returns>已完成的异步任务。</returns>
        public Task OnNavigatedToAsync()
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            var settings = _settingsService.GetCurrent();
            DemoExePath = settings.LastDemoExePath;
            StudentExePath = settings.LastStudentExePath;
            _isInitialized = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 离开主页时保留当前测试会话，不自动取消。
        /// </summary>
        /// <returns>已完成的异步任务。</returns>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// 选择并保存教师提供的 Demo 可执行文件。
        /// </summary>
        [RelayCommand]
        private void SelectDemoExe()
        {
            if (!CanConfigure)
            {
                return;
            }

            var selectedPath = _filePickerService.SelectExecutableFile(Translations.FilePicker_DemoTitle);
            if (selectedPath is null)
            {
                return;
            }

            DemoExePath = selectedPath;
            _settingsService.Update(settings => settings.LastDemoExePath = selectedPath);
        }

        /// <summary>
        /// 选择并保存学生自己的待检测可执行文件。
        /// </summary>
        [RelayCommand]
        private void SelectStudentExe()
        {
            if (!CanConfigure)
            {
                return;
            }

            var selectedPath = _filePickerService.SelectExecutableFile(Translations.FilePicker_StudentTitle);
            if (selectedPath is null)
            {
                return;
            }

            StudentExePath = selectedPath;
            _settingsService.Update(settings => settings.LastStudentExePath = selectedPath);
        }

        /// <summary>
        /// 校验可执行文件路径，从缓存读取并解析测试数据，然后运行完整测试会话。
        /// </summary>
        [RelayCommand]
        private async Task StartCompareAsync()
        {
            if (!CanStartCompare)
            {
                OnPropertyChanged(nameof(CanStartCompare));
                OnPropertyChanged(nameof(PathValidationMessage));
                _snackbarService.Show(
                    Translations.Current["Home_RunFailed"],
                    PathValidationMessage,
                    ControlAppearance.Caution);
                return;
            }

            try
            {
                var rawTestData = await _testDataStorage.LoadTestDataAsync();
                var testCases = _testDataParser.Parse(rawTestData);
                if (testCases.Count == 0)
                {
                    _snackbarService.Show(
                        Translations.Current["Home_NoTestCases"],
                        Translations.Current["Home_NoTestCasesDescription"],
                        ControlAppearance.Caution);
                    return;
                }

                var settings = _settingsService.GetCurrent();
                var request = new TestSessionRequest
                {
                    DemoExePath = DemoExePath,
                    StudentExePath = StudentExePath,
                    TestCases = testCases,
                    CompareOptions = CopyCompareOptions(settings.TextComparisonOptions),
                    Timeout = TimeSpan.FromSeconds(Math.Clamp(settings.ExecutionTimeoutSeconds, 3, 10)),
                    MaxParallelism = Math.Clamp(settings.MaxParallelism, 1, 8)
                };

                BeginSession(testCases.Count);
                var progress = new Progress<int>(value =>
                    CompletedCount = Math.Max(CompletedCount, value));
                _activeRunTask = _batchComparer.RunAsync(
                    request,
                    progress,
                    _runCancellationSource!.Token);
                var batchResult = await _activeRunTask;
                PublishResult(batchResult);
            }
            catch (TestDataFormatException exception)
            {
                _snackbarService.Show(
                    Translations.Current["Home_RunFailed"],
                    string.Format(
                        Translations.Current[$"TestData_Format_{exception.Error}"],
                        exception.LineNumber),
                    ControlAppearance.Danger);
            }
            catch (Exception exception)
            {
                _snackbarService.Show(
                    Translations.Current["Home_RunFailed"],
                    exception.Message,
                    ControlAppearance.Danger);
            }
            finally
            {
                IsRunning = false;
                _activeRunTask = null;
                _runCancellationSource?.Dispose();
                _runCancellationSource = null;
            }
        }

        /// <summary>
        /// 请求取消当前测试会话。
        /// </summary>
        [RelayCommand]
        private void CancelCompare() => _runCancellationSource?.Cancel();

        /// <summary>
        /// 将用户选择的结果设为详情区域的数据源。
        /// </summary>
        /// <param name="result">被选择的用例结果。</param>
        [RelayCommand]
        private void SelectResult(TestCaseRunResult result) => SelectedResult = result;

        /// <summary>
        /// 选择当前用例之前的一个结果。
        /// </summary>
        [RelayCommand]
        private void SelectPrevious()
        {
            var position = GetSelectedPosition();
            if (position > 0)
            {
                SelectedResult = Results[position - 1];
            }
        }

        /// <summary>
        /// 选择当前用例之后的一个结果。
        /// </summary>
        [RelayCommand]
        private void SelectNext()
        {
            var position = GetSelectedPosition();
            if (position >= 0 && position < Results.Count - 1)
            {
                SelectedResult = Results[position + 1];
            }
        }

        /// <summary>
        /// 切换到前一页检测结果。
        /// </summary>
        [RelayCommand]
        private void GoToPreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage = CurrentPageNumber - 1;
            }
        }

        /// <summary>
        /// 切换到后一页检测结果。
        /// </summary>
        [RelayCommand]
        private void GoToNextPage()
        {
            if (CanGoToNextPage)
            {
                CurrentPage = CurrentPageNumber + 1;
            }
        }

        /// <summary>
        /// 收起当前用例的详情区域。
        /// </summary>
        [RelayCommand]
        private void CollapseDetails() => SelectedResult = null;

        /// <summary>
        /// 返回当前选择在有序结果集合中的位置。
        /// </summary>
        /// <returns>未选择时返回 -1。</returns>
        private int GetSelectedPosition() =>
            SelectedResult is null ? -1 : Results.IndexOf(SelectedResult);

        /// <summary>
        /// 应用退出时取消并等待当前测试会话，确保子进程已经结束。
        /// </summary>
        public async Task CancelActiveRunAsync()
        {
            _runCancellationSource?.Cancel();
            var activeRunTask = _activeRunTask;
            if (activeRunTask is not null)
            {
                await activeRunTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 建立新的会话状态，并清除上一次结果。
        /// </summary>
        /// <param name="totalCount">本次测试用例数量。</param>
        private void BeginSession(int totalCount)
        {
            _runCancellationSource = new CancellationTokenSource();
            IsRunning = true;
            CompletedCount = 0;
            TotalCount = totalCount;
            Results = [];
            SelectedResult = null;
            CurrentPage = 1;
            _lastBatchResult = null;
            RefreshResultPresentation();
        }

        /// <summary>
        /// 在会话全部结束后一次性发布有序结果。
        /// </summary>
        /// <param name="batchResult">Core 返回的完整批量结果。</param>
        private void PublishResult(BatchComparisonResult batchResult)
        {
            _lastBatchResult = batchResult;
            Results = new ObservableCollection<TestCaseRunResult>(batchResult.Results);
            SelectedResult = Results.FirstOrDefault();
            CompletedCount = batchResult.TotalCount;
            RefreshResultPresentation();
        }

        /// <summary>
        /// 复制比较设置，避免运行过程中设置页修改同一对象。
        /// </summary>
        /// <param name="source">当前持久化比较设置。</param>
        /// <returns>本次会话独占的设置快照。</returns>
        private static CompareOptions CopyCompareOptions(CompareOptions? source)
        {
            source ??= new CompareOptions();
            return new CompareOptions
            {
                TrimMode = source.TrimMode,
                LineSkip = source.LineSkip,
                LineOffset = source.LineOffset,
                IgnoreBlank = source.IgnoreBlank,
                CrCrLfNotEqual = source.CrCrLfNotEqual,
                MaxDiffCount = source.MaxDiffCount,
                MaxLineCount = source.MaxLineCount
            };
        }

        /// <summary>
        /// 刷新分页、统计数量、比例和耗时等结果展示属性。
        /// </summary>
        private void RefreshResultPresentation()
        {
            OnPropertyChanged(nameof(PagedResults));
            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(CurrentPageNumber));
            OnPropertyChanged(nameof(HasMultiplePages));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(ResultTotalCount));
            OnPropertyChanged(nameof(ResultCompletedCount));
            OnPropertyChanged(nameof(CompletionRateText));
            OnPropertyChanged(nameof(PassRateText));
            OnPropertyChanged(nameof(CompletionRateColor));
            OnPropertyChanged(nameof(PassRateColor));
            OnPropertyChanged(nameof(PassedCount));
            OnPropertyChanged(nameof(FailedCount));
            OnPropertyChanged(nameof(TimedOutCount));
            OnPropertyChanged(nameof(InvalidTestDataCount));
            OnPropertyChanged(nameof(ExecutionFailedCount));
            OnPropertyChanged(nameof(CancelledCount));
            OnPropertyChanged(nameof(TotalElapsedSeconds));
        }

        /// <summary>
        /// 将分子、分母和对应的整数百分比格式化为比例文字。
        /// </summary>
        /// <param name="value">分子。</param>
        /// <param name="total">分母。</param>
        /// <returns>形如“m/n p%”的比例；分母为零时以破折号代替百分比。</returns>
        private static string FormatRate(int value, int total) =>
            total == 0
                ? $"{value}/{total} —"
                : $"{value}/{total} {value * 100d / total:F0}%";

        /// <summary>
        /// 根据比例返回绿、黄、红三级提示颜色。
        /// </summary>
        /// <param name="value">分子。</param>
        /// <param name="total">分母。</param>
        /// <returns>无分母时为灰色；100% 为绿色，低于 20% 为红色，其余为黄色。</returns>
        private static string GetRateColor(int value, int total)
        {
            if (total == 0)
            {
                return "#9E9E9E";
            }

            if (value == total)
            {
                return "#65B96E";
            }

            return value * 5 < total ? "#D96C6C" : "#D1A840";
        }

        /// <summary>
        /// 将用例状态转换为当前语言的显示文本。
        /// </summary>
        /// <param name="status">用例最终状态。</param>
        /// <returns>本地化状态文本。</returns>
        private static string GetStatusText(TestCaseRunStatus status) =>
            Translations.Current[$"Result_{status}"];

        /// <summary>
        /// 根据路径长度在 9–12 像素之间缩小显示字号，短路径保持默认字号。
        /// </summary>
        /// <param name="path">待显示的可执行文件路径。</param>
        /// <returns>适合当前路径长度的字号。</returns>
        private static double GetPathFontSize(string path) =>
            Math.Clamp(14 - (path.Length / 30d), 9, 12);

        /// <summary>
        /// 可执行文件路径变化后刷新开始条件和路径错误提示。
        /// </summary>
        /// <param name="value">新的 Demo 路径。</param>
        partial void OnDemoExePathChanged(string value)
        {
            OnPropertyChanged(nameof(CanStartCompare));
            OnPropertyChanged(nameof(PathValidationMessage));
            OnPropertyChanged(nameof(DemoExePathFontSize));
        }

        /// <summary>
        /// 可执行文件路径变化后刷新开始条件和路径错误提示。
        /// </summary>
        /// <param name="value">新的学生程序路径。</param>
        partial void OnStudentExePathChanged(string value)
        {
            OnPropertyChanged(nameof(CanStartCompare));
            OnPropertyChanged(nameof(PathValidationMessage));
            OnPropertyChanged(nameof(StudentExePathFontSize));
        }

        /// <summary>
        /// 运行状态变化后刷新可操作状态。
        /// </summary>
        /// <param name="value">新的运行状态。</param>
        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanConfigure));
            OnPropertyChanged(nameof(CanStartCompare));
        }

        /// <summary>
        /// 已完成数量变化后刷新进度文字。
        /// </summary>
        /// <param name="value">新的已完成数量。</param>
        partial void OnCompletedCountChanged(int value) =>
            OnPropertyChanged(nameof(ProgressText));

        /// <summary>
        /// 用例总数变化后刷新进度文字。
        /// </summary>
        /// <param name="value">新的用例总数。</param>
        partial void OnTotalCountChanged(int value) =>
            OnPropertyChanged(nameof(ProgressText));

        /// <summary>
        /// 详情选择变化后刷新本地化状态和诊断摘要。
        /// </summary>
        /// <param name="value">新选择的用例结果。</param>
        partial void OnSelectedResultChanged(TestCaseRunResult? value)
        {
            if (value is not null)
            {
                CurrentPage = Results.IndexOf(value) / ResultsPerPage + 1;
            }

            OnPropertyChanged(nameof(SelectedStatusText));
            OnPropertyChanged(nameof(SelectedDiagnosticText));
            OnPropertyChanged(nameof(SelectedTargetText));
            OnPropertyChanged(nameof(SelectedArgumentText));
            OnPropertyChanged(nameof(HasSelectedDifferences));
            OnPropertyChanged(nameof(CanSelectPrevious));
            OnPropertyChanged(nameof(CanSelectNext));
        }

        /// <summary>
        /// 结果集合替换后复位页码并刷新分页属性。
        /// </summary>
        /// <param name="value">新的有序结果集合。</param>
        partial void OnResultsChanged(ObservableCollection<TestCaseRunResult> value)
        {
            CurrentPage = 1;
            RefreshResultPresentation();
        }

        /// <summary>
        /// 页码输入变化后取整并限制到有效范围。
        /// </summary>
        /// <param name="value">NumberBox 提供的新页码。</param>
        partial void OnCurrentPageChanged(double value)
        {
            var normalized = double.IsNaN(value)
                ? 1
                : Math.Clamp(Math.Round(value), 1, PageCount);
            if (value != normalized)
            {
                CurrentPage = normalized;
                return;
            }

            OnPropertyChanged(nameof(PagedResults));
            OnPropertyChanged(nameof(CurrentPageNumber));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
        }

        /// <summary>
        /// 语言切换后刷新主页中由代码组合的文字。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(PathValidationMessage));
            OnPropertyChanged(nameof(SelectedStatusText));
            OnPropertyChanged(nameof(SelectedDiagnosticText));
            OnPropertyChanged(nameof(SelectedTargetText));
            OnPropertyChanged(nameof(SelectedArgumentText));
            RefreshResultPresentation();
        }
    }
}
