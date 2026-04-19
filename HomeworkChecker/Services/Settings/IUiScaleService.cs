using System;
using System.Collections.Generic;
using System.Text;
using HomeworkChecker.UI.Models.Settings;

namespace HomeworkChecker.UI.Services.Settings
{
    public interface IUiScaleService
    {
        void Apply(UiScalePreference preference);
    }
}
