using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Componentry.Inspecting
{
    /*
     * This is the whole of what a search does by default, and it is deliberately shallow:
     * nothing is opened, nothing is serialized, and the answer is a set of components that the Inspector then shows through its own editors,
     * exactly as it would have drawn them. A custom editor stays a custom editor and a Transform still looks like a Transform.
     *
     * Matched against both the type's name and the name Unity would nicify it into, so that "mesh renderer" with the space finds MeshRenderer and "meshrenderer" does too.
     */
    /// <summary>
    /// Which components are called something like what was typed.
    /// </summary>
    public static class ComponentNameSearch
    {
        
        /// <summary>
        /// Which of these components are called something like what was typed.
        /// </summary>
        /// <param name="components">The components to look through.</param>
        /// <param name="query">What was typed. Trimmed here, and an empty one matches nothing rather than everything.</param>
        /// <param name="into">Set filled with the instance ids of the matches, cleared first.</param>
        public static void Run(List<Component> components, string query, HashSet<int> into)
        {
            into.Clear();
            if (string.IsNullOrWhiteSpace(query)) return;
            
            string needle = query.Trim();
            
            foreach (Component component in components)
            {
                if (!component) continue;
                if (Matches(component.GetType(), needle))
                    into.Add(component.GetInstanceID());
            }
        }
        
        // match, unlike your tinder
        /// <summary>
        /// Whether one component type is called something like what was typed.
        /// </summary>
        /// <param name="type">The component type to check.</param>
        /// <param name="needle">What was typed, already trimmed.</param>
        /// <returns>True when it matches either the raw type name or the nicified one, so "mesh renderer" and "meshrenderer" both find MeshRenderer.</returns>
        public static bool Matches(Type type, string needle)
        {
            return Contains(type.Name, needle) || Contains(ObjectNames.NicifyVariableName(type.Name), needle);
        }
        
        private static bool Contains(string text, string needle) => text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        
    }
}
