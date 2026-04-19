namespace HomeworkChecker.UI.Resources
{
    public partial class Translations
    {
        public static event EventHandler? CultureChanged;

        private static readonly System.Resources.ResourceManager _resourceManager =
            new("HomeworkChecker.UI.Resources.Translations", typeof(Translations).Assembly);

        public static System.Globalization.CultureInfo? Culture { get; set; }

        public static void SetCulture(System.Globalization.CultureInfo culture)
        {
            Culture = culture;
            CultureChanged?.Invoke(null, EventArgs.Empty);
        }

        private static string Get(string key) =>
            _resourceManager.GetString(
                key,
                Culture ?? System.Globalization.CultureInfo.CurrentUICulture
            ) ?? key;

        public static string Settings_Personalization => Get(nameof(Settings_Personalization));
        public static string Settings_Theme_Title => Get(nameof(Settings_Theme_Title));
        public static string Settings_Theme_Description => Get(nameof(Settings_Theme_Description));
        public static string Settings_Theme_Light => Get(nameof(Settings_Theme_Light));
        public static string Settings_Theme_Dark => Get(nameof(Settings_Theme_Dark));
        public static string Settings_UseSystem => Get(nameof(Settings_UseSystem));

        public static string Settings_Accent_Title => Get(nameof(Settings_Accent_Title));
        public static string Settings_Accent_Description => Get(nameof(Settings_Accent_Description));
        public static string Settings_Accent_Default => Get(nameof(Settings_Accent_Default));
        public static string Settings_Accent_Custom => Get(nameof(Settings_Accent_Custom));
        public static string Settings_Accent_SelectColor => Get(nameof(Settings_Accent_SelectColor));

        public static string Settings_Scale_Title => Get(nameof(Settings_Scale_Title));
        public static string Settings_Scale_Description => Get(nameof(Settings_Scale_Description));

        public static string Settings_Language_Title => Get(nameof(Settings_Language_Title));
        public static string Settings_Language_Description => Get(nameof(Settings_Language_Description));

        public static string Settings_About_Section => Get(nameof(Settings_About_Section));
        public static string Settings_Help_Title => Get(nameof(Settings_Help_Title));
        public static string Settings_Help_Description => Get(nameof(Settings_Help_Description));
        public static string Settings_Help_OpenGuide => Get(nameof(Settings_Help_OpenGuide));

        public static string Settings_Feedback_Title => Get(nameof(Settings_Feedback_Title));
        public static string Settings_Feedback_Description => Get(nameof(Settings_Feedback_Description));
        public static string Settings_Feedback_Action => Get(nameof(Settings_Feedback_Action));

        public static string Settings_About_Title => Get(nameof(Settings_About_Title));
        public static string Settings_About_Description => Get(nameof(Settings_About_Description));
        public static string Settings_About_CheckUpdate => Get(nameof(Settings_About_CheckUpdate));

        public static string Navigation_Settings => Get(nameof(Navigation_Settings));
        public static string Page_Settings => Get(nameof(Page_Settings));

        public static string Snackbar_Saved_Title => Get(nameof(Snackbar_Saved_Title));
        public static string Snackbar_Restart_Message => Get(nameof(Snackbar_Restart_Message));
    }
}
