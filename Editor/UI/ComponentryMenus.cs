using Componentry.Core;
using UnityEditor;

namespace Componentry.UI
{
    /*
     * Under Tools rather than only in Preferences, because Tools is the menu people already look in for a package,
     * and a toggle worth reaching for often should not be four clicks into a settings window.
     * The same values are reachable from Preferences as well, which is the secondary door rather than the way in.
     *
     * The bar toggle carries a validate function purely to draw the tick beside it.
     * It always returns true, since the entry is never greyed out, it only reports whether the bar is currently on.
     */
    /// <summary>
    /// The package's entries under the Tools menu.
    /// </summary>
    public static class ComponentryMenus
    {
        
        private const string SETTINGS_PATH = "Tools/Componentry/Settings";
        private const string ENABLED_PATH = "Tools/Componentry/Component Bar";
        
        [MenuItem(SETTINGS_PATH, false, 0)]
        private static void OpenSettings() => ComponentrySettingsWindow.Open();
        
        [MenuItem(ENABLED_PATH, false, 20)]
        private static void ToggleComponentBar() => ComponentrySettings.Enabled = !ComponentrySettings.Enabled;
        
        [MenuItem(ENABLED_PATH, true, 20)]
        private static bool ToggleComponentBarValidate()
        {
            Menu.SetChecked(ENABLED_PATH, ComponentrySettings.Enabled);
            return true;
        }
        
    }
}
