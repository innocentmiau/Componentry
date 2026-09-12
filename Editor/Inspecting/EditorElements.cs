using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Componentry.Inspecting
{
    /*
     * Each box is matched to its component by the object that box is editing, rather than by counting along the Inspector's list.
     * Counting was what this did first, and it is wrong the moment the list holds a child that is not one of the component editors.
     * The one that turns up in practice is a box left behind by an editor that died while the scene was being reloaded,
     * which the Inspector keeps in place while rebuilding around it. Every box after it is then one step further down than it was counted to be,
     * so picking a chip hid the component that was picked and showed an empty box instead, until the selection was changed and the list built again from nothing.
     * Reading the target off each box costs one cached property lookup per box and cannot drift out of step with the list.
     *
     * The type behind a box and the interface it implements are both internal, so both are found by name.
     * A name that stops matching some day leaves every box unrecognised, which reads as no filtering at all rather than as the wrong thing being hidden.
     */
    /// <summary>
    /// Showing and hiding the editors the Inspector has already built.
    /// </summary>
    public static class EditorElements
    {

        private const string EDITOR_INTERFACE = "IEditorElement";
        private const string EDITOR_PROPERTY = "editor";

        private static readonly Dictionary<Type, PropertyInfo> EDITOR_PROPERTIES = new Dictionary<Type, PropertyInfo>();
        private static readonly HashSet<int> COMPONENT_IDS = new HashSet<int>();

        /// <summary>
        /// Shows only the picked components and hides the rest, or shows everything when nothing is picked.
        /// </summary>
        /// <param name="editorsList">The Inspector's editor list element.</param>
        /// <param name="startIndex">Where the component editors start in it, past the header and the bar.</param>
        /// <param name="components">The components a chip was drawn for, from VisibleComponents.CollectChips.</param>
        /// <param name="shown">Instance ids of the components to keep on screen. Empty means no filtering.</param>
        /// <param name="missingPicked">Whether the missing scripts chip is picked, since those have no instance id to put in the set.</param>
        /// <returns>Whether a box was found for every one of the components, which is false while the Inspector is still building the list.</returns>
        public static bool Apply(VisualElement editorsList, int startIndex, List<Component> components, HashSet<int> shown, bool missingPicked)
        {
            if (editorsList == null || startIndex < 0) return false;

            bool filtering = shown.Count > 0 || missingPicked;
            bool materials = !filtering || AnyPickedDrawsMaterials(components, shown);

            CollectIds(components);

            int found = 0;

            for (int i = startIndex; i < editorsList.childCount; i++)
            {
                VisualElement element = editorsList[i];
                PropertyInfo property = EditorPropertyOf(element.GetType());

                if (property == null) continue;

                Object target = TargetOf(element, property);

                // The object's own header sits above the bar rather than after it, so this only ever matters if the Inspector one day puts it somewhere else.
                if (target is GameObject) continue;

                // Past the components come the materials, which belong to whichever renderer is on the object rather than to the object itself.
                if (target && target is not Component)
                {
                    SetVisible(element, materials);
                    continue;
                }

                int id = target ? target.GetInstanceID() : 0;

                // Either a missing script or a box whose editor died while the Inspector was rebuilding. Neither has a chip of its own, so the missing chip speaks for both.
                if (!COMPONENT_IDS.Contains(id))
                {
                    SetVisible(element, !filtering || missingPicked);
                    continue;
                }

                found++;
                SetVisible(element, !filtering || shown.Contains(id));
            }

            return found == COMPONENT_IDS.Count;
        }

        /// <summary>
        /// Everything the Inspector would have drawn for the object, out of the way, so that a search can put its own answer there instead.
        /// </summary>
        /// <param name="editorsList">The Inspector's editor list element.</param>
        /// <param name="startIndex">Where the component editors start in it.</param>
        /// <param name="components">The components a chip was drawn for, read only to tell whether the list has finished being built.</param>
        /// <returns>Whether a box was found for every one of the components, which is false while the Inspector is still building the list.</returns>
        public static bool HideComponents(VisualElement editorsList, int startIndex, List<Component> components)
        {
            if (editorsList == null || startIndex < 0) return false;

            CollectIds(components);

            int found = 0;

            for (int i = startIndex; i < editorsList.childCount; i++)
            {
                VisualElement element = editorsList[i];
                PropertyInfo property = EditorPropertyOf(element.GetType());

                if (property == null) continue;

                Object target = TargetOf(element, property);

                if (target is GameObject) continue;

                if (target && COMPONENT_IDS.Contains(target.GetInstanceID())) found++;

                SetVisible(element, false);
            }

            return found == COMPONENT_IDS.Count;
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

        private static void CollectIds(List<Component> components)
        {
            COMPONENT_IDS.Clear();

            foreach (Component component in components)
                if (component) COMPONENT_IDS.Add(component.GetInstanceID());
        }

        private static bool AnyPickedDrawsMaterials(List<Component> components, HashSet<int> shown)
        {
            foreach (Component component in components)
                if (component is Renderer && shown.Contains(component.GetInstanceID())) return true;

            return false;
        }

        private static Object TargetOf(VisualElement element, PropertyInfo property)
        {
            return property.GetValue(element) is Editor editor && editor ? editor.target : null;
        }

        // Null for anything that is not one of the Inspector's editor boxes, which is how the bar, the results panel and the prefab rows are left alone.
        private static PropertyInfo EditorPropertyOf(Type type)
        {
            if (EDITOR_PROPERTIES.TryGetValue(type, out PropertyInfo known)) return known;

            PropertyInfo property = IsEditorElement(type) ? type.GetProperty(EDITOR_PROPERTY, BindingFlags.Public | BindingFlags.Instance) : null;

            if (property?.PropertyType != typeof(Editor)) property = null;

            EDITOR_PROPERTIES[type] = property;
            return property;
        }

        private static bool IsEditorElement(Type type)
        {
            foreach (Type contract in type.GetInterfaces())
                if (contract.Name == EDITOR_INTERFACE) return true;

            return false;
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
