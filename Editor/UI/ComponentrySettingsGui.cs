using System;
using Componentry.Core;
using UnityEditor;
using UnityEngine;

namespace Componentry.UI
{
    /// <summary>
    /// The settings, drawn once and shown in two places: the window under Tools, and the page in the editor's own preferences. Both are the same controls writing the same EditorPrefs, so it does not matter which one anybody reaches for.
    /// </summary>
    public static class ComponentrySettingsGui
    {
        
        private static readonly string[] SIZE_NAMES = { "Compact", "Small", "Default", "Large", "Extra Large" };
        private static readonly BarSize[] SIZES = { BarSize.COMPACT, BarSize.SMALL, BarSize.DEFAULT, BarSize.LARGE, BarSize.EXTRA_LARGE };
        
        private static readonly string[] LABEL_NAMES = { "Always", "Only when needed", "Never" };
        private static readonly ChipLabelMode[] LABEL_MODES = { ChipLabelMode.ALWAYS, ChipLabelMode.WHEN_NEEDED, ChipLabelMode.NEVER };
        
        private static readonly string[] LABEL_HELP =
        {
            "Every chip shows the component's type name.",
            "A chip shows its name only when its icon is not enough to tell it apart. Scripts keep their names, since they share one icon; a Transform is left as the icon alone.",
            "Icons only. The component is named in the tooltip."
        };
        
        // big cause big is better and big fits everything.
        private const float LABEL_WIDTH = 200f;
        
        private static GUIStyle _wrappedMiniLabel;
        
        private static GUIStyle WrappedMiniLabel => _wrappedMiniLabel ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        
        public static void Draw()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            EditorGUILayout.Space();

            ComponentrySettings.Enabled = EditorGUILayout.Toggle(
                new GUIContent("Component Bar", "Show the row of components at the top of every Inspector, and click a chip to show only that component."),
                ComponentrySettings.Enabled);
            
            EditorGUILayout.Space();
            
            using (new EditorGUI.DisabledScope(!ComponentrySettings.Enabled))
            {
                DrawAddComponent();
                EditorGUILayout.Space();
                DrawTransformOnly();
                EditorGUILayout.Space();
                DrawSize();
                EditorGUILayout.Space();
                DrawLabels();
                EditorGUILayout.Space();
                DrawShowAll();
                EditorGUILayout.Space();
                DrawSearch();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "These sizes are not everybody's sizes. Every measurement the bar is drawn "
                + "with, these included, is a number in Core/Constants.cs inside the package, "
                + "which can be edited directly for a size the presets do not offer.",
                MessageType.Info);
            
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }
        
        private static void DrawAddComponent()
        {
            ComponentrySettings.AddComponent = EditorGUILayout.Toggle(
                new GUIContent("Add Component Button", "Put a plus at the front of the bar that opens Unity's Add Component search."),
                ComponentrySettings.AddComponent);
            
            GUILayout.Label(
                ComponentrySettings.AddComponent
                    ? "A plus at the very front of the bar, in the same place on every object, opening the same search the button at the bottom of the Inspector opens."
                    : "No plus. Adding a component is the button at the bottom of the Inspector, as it always was.",
                WrappedMiniLabel);
        }
        
        private static void DrawTransformOnly()
        {
            ComponentrySettings.HideOnTransformOnly = EditorGUILayout.Toggle(
                new GUIContent("Hide When Only A Transform", "Leave the bar off objects that have nothing on them but a Transform."),
                ComponentrySettings.HideOnTransformOnly);
            
            GUILayout.Label(
                ComponentrySettings.HideOnTransformOnly
                    ? "Empties used as parents and markers get no bar, since a bar there would only say the object has the one component every object has. The row comes back on its own while components are waiting to be pasted, so there is still somewhere to put them down."
                    : "Every object gets a bar, including the ones with only a Transform.",
                WrappedMiniLabel);
        }
        
        private static void DrawSize()
        {
            int current = Array.IndexOf(SIZES, ComponentrySettings.Size);
            int chosen = EditorGUILayout.Popup(new GUIContent("Size", "How large the chips are drawn."), Mathf.Max(current, 0), SIZE_NAMES);
            
            if (chosen != current) ComponentrySettings.Size = SIZES[chosen];
            
            // Worth stating outright, since it is the number in Constants that every
            // measurement is multiplied by and the one to reason about when editing them.
            // Remove because I think the normal user doesn't need to explicit see the values and mention the Constants file in the menu.
            //EditorGUILayout.LabelField(" ", $"{ComponentrySettings.Scale:0.##}x the sizes in Constants", EditorStyles.miniLabel);
            
            EditorGUILayout.Space();
            
            ComponentrySettings.MaxRows = EditorGUILayout.IntSlider(
                new GUIContent("Rows At Most", "How many rows of chips the bar grows to before leaving the rest out."),
                ComponentrySettings.MaxRows,
                Constants.ROWS_MINIMUM,
                Constants.ROWS_MAXIMUM);
            
            GUILayout.Label(
                "The bar is only ever as tall as the chips need. This is the point past which it stops growing and an object with more components than fit says less about itself rather than pushing the Inspector down.",
                WrappedMiniLabel);
        }
        
        private static void DrawLabels()
        {
            int current = Array.IndexOf(LABEL_MODES, ComponentrySettings.Labels);
            current = Mathf.Max(current, 0);
            
            int chosen = EditorGUILayout.Popup(new GUIContent("Names", "Whether a chip shows the component's type name beside its icon."), current, LABEL_NAMES);
            
            if (chosen != current) ComponentrySettings.Labels = LABEL_MODES[chosen];
            GUILayout.Label(LABEL_HELP[chosen], WrappedMiniLabel);
        }
        
        private static void DrawSearch()
        {
            ComponentrySettings.SearchProperties = EditorGUILayout.Toggle(
                new GUIContent("Search Inside Properties", "Look at the fields inside components as well as at their names."),
                ComponentrySettings.SearchProperties);
            
            GUILayout.Label(
                ComponentrySettings.SearchProperties
                    ? "A search also finds fields by name, wherever they are, and draws what it found in place of the Inspector. Those are the serialized fields behind a component, the same ones the Inspector's own Debug mode shows, so a component with a custom editor is drawn plainly rather than the way its editor would have drawn it."
                    : "A search looks at component names only, and the Inspector draws what matched through its own editors, exactly as it always would.",
                WrappedMiniLabel);
        }
        
        private static void DrawShowAll()
        {
            ComponentrySettings.ShowAllWhenNeeded = EditorGUILayout.Toggle(
                new GUIContent("Show All Only When Needed", "Keep the Show All chip out of the bar until something has been filtered."),
                ComponentrySettings.ShowAllWhenNeeded);
            
            GUILayout.Label(
                ComponentrySettings.ShowAllWhenNeeded
                    ? "The chip that drops the filter appears at the front of the bar once something has been picked, and is not there the rest of the time."
                    : "The chip that drops the filter is always at the front of the bar, greyed out while there is nothing to drop.",
                WrappedMiniLabel);
        }
        
    }
}
