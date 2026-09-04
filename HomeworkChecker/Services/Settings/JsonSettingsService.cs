using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using HomeworkChecker.Core.Utilities;
using HomeworkChecker.UI.Models.Settings;
using HomeworkChecker.UI.Resources;

namespace HomeworkChecker.UI.Services.Settings
{
    public sealed class JsonSettingsService : ISettingsService
    {
        // 建议放到 AppData，而不是项目目录
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HomeworkChecker",
            "AppSettings.json");

        // 线程安全锁
        private readonly object _syncRoot = new();

        // 进程内缓存：避免每次更新都重新反序列化
        private AppSettings? _cached;

        public AppSettings GetCurrent()
        {
            lock (_syncRoot)
            {
                _cached ??= LoadFromDisk();
                return _cached;
            }
        }

        public void Update(Action<AppSettings> updateAction)
        {
            lock (_syncRoot)
            {
                var settings = GetCurrent();
                updateAction(settings);
                try
                {
                    SaveToDisk(settings);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    MessageBox.Show(
                        $"{Translations.Settings_SaveFailedDescription}\n{exception.Message}",
                        Translations.Settings_SaveFailed,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 从磁盘恢复设置；文件不可读或 JSON 损坏时回落默认值。
        /// </summary>
        private static AppSettings LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = LocalFileStorage.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException)
            {
                return new AppSettings();
            }
        }

        /// <summary>
        /// 将当前设置原子写入应用数据目录。
        /// </summary>
        /// <param name="settings">待保存设置。</param>
        private static void SaveToDisk(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            LocalFileStorage.WriteAllText(SettingsPath, json);
        }
    }
}
