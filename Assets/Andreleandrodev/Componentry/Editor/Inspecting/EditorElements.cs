using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Componentry.Inspecting
{
    /// <summary>
    /// Showing and hiding the editors the Inspector has already built.
    /// </summary>
    public static class EditorElements
    {
        
        private const string EDITOR_INTERFACE = "IEditorElement";
        private static readonly Dictionary<Type, bool> IS_EDITOR = new Dictionary<Type, bool>();
        
        private static bool CanMap(VisualElement editorsList, int startIndex, int componentCount)
        {
            if (editorsList == null || startIndex < 0) return false;
            return editorsList.childCount - startIndex >= componentCount;
        }
        
        /// <summary>
        /// Shows only the picked components and hides the rest, or shows everything when nothing is picked.
        /// Does nothing at all if the list does not line up with what was expected, which is safer than hiding the wrong box.
        /// </summary>
        /// <param name="editorsList">The Inspector's editor list element.</param>
        /// <param name="startIndex">Where the component editors start in it, past the header and the bar.</param>
        /// <param name="drawn">One entry per editor, from VisibleComponents.CollectDrawn, so the indices line up with the children.</param>
        /// <param name="shown">Instance ids of the components to keep on screen. Empty means no filtering.</param>
        /// <param name="missingPicked">Whether the missing scripts chip is picked, since those have no instance id to put in the set.</param>
        public static void Apply(VisualElement editorsList, int startIndex, List<Component> drawn, HashSet<int> shown, bool missingPicked)
        {
            if (!CanMap(editorsList, startIndex, drawn.Count)) return;
            bool filtering = shown.Count > 0 || missingPicked;
            
            for (int i = 0; i < drawn.Count; i++)
            {
                bool visible = !filtering || (drawn[i] ? shown.Contains(drawn[i].GetInstanceID()) : missingPicked);
                SetVisible(editorsList[startIndex + i], visible);
            }
            
            ApplyToMaterials(editorsList, startIndex + drawn.Count, !filtering || AnyPickedDrawsMaterials(drawn, shown));
        }
        
        // Past the components come the materials.
        private static void ApplyToMaterials(VisualElement editorsList, int from, bool visible)
        {
            for (int i = from; i < editorsList.childCount; i++)
            {
                if (!IsEditor(editorsList[i])) continue;
                SetVisible(editorsList[i], visible);
            }
        }
        
        private static bool AnyPickedDrawsMaterials(List<Component> drawn, HashSet<int> shown)
        {
            foreach (Component component in drawn)
                if (component is Renderer && shown.Contains(component.GetInstanceID())) return true;
            return false;
        }
        
        // Unity's editor elements implement an interface of its own, which is internal.
        // A name that stops matching some day makes this answer false for everything, and the materials are then left showing,
        // which is where they were before any of this existed.
        private static bool IsEditor(VisualElement element)
        {
            Type type = element.GetType();
            if (IS_EDITOR.TryGetValue(type, out bool known)) return known;
            
            bool isEditor = false;
            foreach (Type contract in type.GetInterfaces())
            {
                if (contract.Name != EDITOR_INTERFACE) continue;
                isEditor = true;
                break;
            }
            
            IS_EDITOR[type] = isEditor;
            return isEditor;
        }
        
        /// <summary>
        /// Everything the Inspector would have drawn for the object, out of the way, so that a search can put its own answer there instead.
        /// </summary>
        /// <param name="editorsList">The Inspector's editor list element.</param>
        /// <param name="startIndex">Where the component editors start in it.</param>
        /// <param name="drawn">One entry per editor, from VisibleComponents.CollectDrawn.</param>
        public static void HideComponents(VisualElement editorsList, int startIndex, List<Component> drawn)
        {
            if (!CanMap(editorsList, startIndex, drawn.Count)) return;
            
            for (int i = 0; i < drawn.Count; i++)
                SetVisible(editorsList[startIndex + i], false);
            
            ApplyToMaterials(editorsList, startIndex + drawn.Count, false);
        }
        
        /// <summary>
        /// Puts every editor back on screen. Run whenever a bar goes away, so nothing is left hidden by a bar that is no longer there to unhide it.
        /// </summary>
        /// <param name="editorsList">The Inspector's editor list element. Safe to pass null or a detached one.</param>
        public static void ShowAll(VisualElement editorsList)
        {
            if (editorsList?.panel == null) return;
            for (int i = 0; i < editorsList.childCount; i++)
                SetVisible(editorsList[i], true);
        }
        
        // Written only when it would change, so that a filter that has not moved is not a style change and a layout pass on every editor frame.
        private static void SetVisible(VisualElement element, bool visible)
        {
            DisplayStyle wanted = visible ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (element.style.display.value == wanted) return;
            element.style.display = wanted;
        }
        
    }
}
