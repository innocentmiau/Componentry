using System.Collections.Generic;
using Componentry.Inspecting;
using UnityEditor;

namespace Componentry.UI
{
    /// <summary>
    /// What a search shows in place of the Inspector: the components that matched, each under the header the Inspector would have given it, holding the properties that matched.
    /// </summary>
    public static class SearchResultsView
    {
        
        private const string SCRIPT_PROPERTY = "m_Script";
        
        public static void Draw(List<ComponentSearchResult> results, string query, bool searchedProperties)
        {
            if (results.Count == 0)
            {
                string what = searchedProperties ? "component or property" : "component";
                EditorGUILayout.LabelField($"No {what} matches \"{query}\".", EditorStyles.miniLabel);
                return;
            }
            
            foreach (ComponentSearchResult result in results)
            {
                if (result.IsStale) continue;
                DrawResult(result);
            }
        }
        
        private static void DrawResult(ComponentSearchResult result)
        {
            result.Serialized.Update();
            
            EditorGUILayout.InspectorTitlebar(true, result.Component, false);
            
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            
            foreach (SerializedProperty property in result.Properties)
            {
                using (new EditorGUI.DisabledScope(property.propertyPath == SCRIPT_PROPERTY))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
            
            if (EditorGUI.EndChangeCheck()) result.Serialized.ApplyModifiedProperties();
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
        
    }
}
