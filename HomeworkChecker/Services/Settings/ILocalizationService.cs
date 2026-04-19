using HomeworkChecker.UI.Models.Settings;

namespace HomeworkChecker.UI.Services.Settings
{
    public interface ILocalizationService
    {
        void Apply(LanguagePreference preference);
    }
}
