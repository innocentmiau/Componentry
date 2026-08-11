using UnityEditor;

namespace Componentry.UI
{
    /// <summary>
    /// The same settings again, under Edit > Preferences, for anyone who looks there first.
    /// </summary>
    public static class ComponentrySettingsProvider
    {
        
        private const string PATH = "Preferences/Componentry";
        
        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(PATH, SettingsScope.User)
            {
                label = "Componentry",
                guiHandler = _ => ComponentrySettingsGui.Draw(),
                keywords = new[] { "inspector", "component", "bar", "size" }
            };
        }
        
    }
}
