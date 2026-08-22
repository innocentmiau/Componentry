using UnityEditor;
using UnityEngine;

namespace Componentry.UI
{
    /// <summary>
    /// The window behind Tools > Componentry > Settings. Draws the same controls as the Preferences page, both of them through ComponentrySettingsGui.
    /// </summary>
    public class ComponentrySettingsWindow : EditorWindow
    {
        
        private static readonly Vector2 MINIMUM_SIZE = new Vector2(430f, 320f);
        
        /// <summary>
        /// Opens the window as a utility window, or brings the open one to the front.
        /// </summary>
        public static void Open()
        {
            ComponentrySettingsWindow window = GetWindow<ComponentrySettingsWindow>(true, "Componentry");
            window.minSize = MINIMUM_SIZE;
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            ComponentrySettingsGui.Draw();
            EditorGUILayout.EndVertical();
        }
        
    }
}
