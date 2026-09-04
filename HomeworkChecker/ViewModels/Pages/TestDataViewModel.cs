using HomeworkChecker.Core.Services;
using HomeworkChecker.Core.Utilities;
using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.Services;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    /// <summary>
    /// 管理测试数据的导入、页内编辑和缓存保存。
    /// </summary>
    public partial class TestDataViewModel : ObservableObject, INavigationAware
    {
        private readonly ITestDataStorage _testDataStorage;
        private readonly IFilePickerService _filePickerService;
        private readonly ISnackbarService _snackbarService;
        private bool _isInitialized;

        [ObservableProperty]
        private string _testDataText = string.Empty;

        /// <summary>
        /// 创建测试数据页面 ViewModel。
        /// </summary>
        /// <param name="testDataStorage">测试数据缓存服务。</param>
        /// <param name="filePickerService">测试数据文件选择服务。</param>
        /// <param name="snackbarService">操作结果提示服务。</param>
        public TestDataViewModel(
            ITestDataStorage testDataStorage,
            IFilePickerService filePickerService,
            ISnackbarService snackbarService)
        {
            _testDataStorage = testDataStorage;
            _filePickerService = filePickerService;
            _snackbarService = snackbarService;
        }

        /// <summary>
        /// 首次进入页面时恢复上次保存的测试数据快照。
        /// </summary>
        public async Task OnNavigatedToAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                TestDataText = await _testDataStorage.LoadTestDataAsync();
                _isInitialized = true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _snackbarService.Show(
                    Translations.TestData_LoadFailed,
                    exception.Message,
                    ControlAppearance.Danger);
            }
        }

        /// <summary>
        /// 离开页面时保存当前测试数据快照。
        /// </summary>
        public async Task OnNavigatedFromAsync()
        {
            try
            {
                await SaveIfInitializedAsync();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _snackbarService.Show(
                    Translations.TestData_SaveFailed,
                    exception.Message,
                    ControlAppearance.Danger);
            }
        }

        /// <summary>
        /// 导入用户选择的 txt 或 dat 文件，并立即更新应用缓存。
        /// </summary>
        [RelayCommand]
        private async Task ImportAsync()
        {
            var path = _filePickerService.SelectTestDataFile(Translations.FilePicker_TestDataTitle);
            if (path is null)
            {
                return;
            }

            try
            {
                TestDataText = TextEncodingHelper.Decode(await File.ReadAllBytesAsync(path));
                _isInitialized = true;
                await _testDataStorage.SaveTestDataAsync(TestDataText);
                _snackbarService.Show(
                    Translations.TestData_ImportSuccess,
                    Path.GetFileName(path),
                    ControlAppearance.Success);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _snackbarService.Show(
                    Translations.TestData_ImportFailed,
                    exception.Message,
                    ControlAppearance.Danger);
            }
        }

        /// <summary>
        /// 将编辑器中的测试数据保存到应用缓存。
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                _isInitialized = true;
                await _testDataStorage.SaveTestDataAsync(TestDataText);
                _snackbarService.Show(
                    Translations.TestData_SaveSuccess,
                    Translations.TestData_SaveSuccessDescription,
                    ControlAppearance.Success);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _snackbarService.Show(
                    Translations.TestData_SaveFailed,
                    exception.Message,
                    ControlAppearance.Danger);
            }
        }

        /// <summary>
        /// 仅在页面已加载或编辑状态已建立时保存，避免空内容覆盖旧缓存。
        /// </summary>
        public Task SaveIfInitializedAsync() =>
            _isInitialized
                ? _testDataStorage.SaveTestDataAsync(TestDataText)
                : Task.CompletedTask;
    }
}
