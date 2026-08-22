using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Componentry.Inspecting
{
    /// <summary>
    /// Finding the properties inside components that match what was typed. The deeper of the two searches, and the one that is off until it is asked for.
    /// </summary>
    public static class PropertySearch
    {
        
        private const string SCRIPT_PROPERTY = "m_Script";
        
        /// <summary>
        /// Looks inside every component for properties whose display name matches. A component whose own name matches contributes all of its properties rather than none.
        /// </summary>
        /// <param name="components">The components to look inside.</param>
        /// <param name="query">What was typed. Trimmed here, and an empty one matches nothing.</param>
        /// <param name="into">List filled with one result per matching component. Disposed and cleared first, so the caller does not have to.</param>
        public static void Run(List<Component> components, string query, List<ComponentSearchResult> into)
        {
            Clear(into);
            
            if (string.IsNullOrWhiteSpace(query)) return;
            
            string needle = query.Trim();
            
            foreach (Component component in components)
            {
                if (!component) continue;
                ComponentSearchResult result = Match(component, needle);
                if (result != null) into.Add(result);
            }
        }
        
        /// <summary>
        /// Disposes every result and empties the list. Has to be used rather than clearing it, since each result owns a SerializedObject.
        /// </summary>
        /// <param name="results">The results to dispose and clear.</param>
        public static void Clear(List<ComponentSearchResult> results)
        {
            foreach (ComponentSearchResult result in results)
                result.Dispose();
            results.Clear();
        }
        
        public static bool AnyStale(List<ComponentSearchResult> results)
        {
            foreach (ComponentSearchResult result in results)
                if (result.IsStale) return true;
            return false;
        }
        
        private static ComponentSearchResult Match(Component component, string needle)
        {
            bool wholeComponent = ComponentNameSearch.Matches(component.GetType(), needle);
            
            SerializedObject serialized = new SerializedObject(component);
            List<SerializedProperty> properties = new List<SerializedProperty>();
            
            SerializedProperty property = serialized.GetIterator();
            
            if (property.NextVisible(true))
            {
                do
                {
                    if (!wholeComponent && !Contains(property.displayName, needle)) continue;
                    if (!wholeComponent && property.propertyPath == SCRIPT_PROPERTY) continue;
                    properties.Add(property.Copy());
                }
                while (property.NextVisible(false));
            }
            
            if (properties.Count > 0) return new ComponentSearchResult(component, serialized, properties);
            
            serialized.Dispose();
            return null;
        }
        
        private static bool Contains(string text, string needle) => text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        
    }
}
