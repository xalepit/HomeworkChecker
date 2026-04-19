using HomeworkChecker.UI.Models.Settings;

namespace HomeworkChecker.UI.Services.Settings
{
    public interface ISettingsService
    {
        // 获取当前设置快照（用于初始化 UI）
        AppSettings GetCurrent();

        // 按字段更新（逐项保存）
        void Update(Action<AppSettings> updateAction);
    }
}
