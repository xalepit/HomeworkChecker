using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using HomeworkChecker.UI.Models.Settings;

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
                SaveToDisk(settings);
            }
        }

        private static AppSettings LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                // JSON 损坏时回落默认值，避免启动崩溃
                return new AppSettings();
            }
        }

        private static void SaveToDisk(AppSettings settings)
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsPath, json);
        }
    }
}
