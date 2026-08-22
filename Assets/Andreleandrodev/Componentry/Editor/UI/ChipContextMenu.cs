using System;
using System.Collections.Generic;
using Componentry.Core;
using Componentry.Inspecting;
using UnityEditor;
using UnityEngine;

namespace Componentry.UI
{
    /*
     * Switching components off, and carrying a set of them to another object.
     * Both are about several components at once, which is the whole reason the bar exists and the whole reason this menu does.
     */
    /// <summary>
    ///  What the right-clicking a chip offers: the things the bar can do that the Inspector cannot, and nothing that it can.
    /// </summary>
    public static class ChipContextMenu
    {
        
        private const double SAME_CLICK = .25;
        
        private static double _shownAt;
        
        private static readonly TransformPart[] PARTS =
        {
            TransformPart.WORLD, TransformPart.POSITION, TransformPart.ROTATION, TransformPart.SCALE
        };
        
        public static void Show(Component component, List<Component> picked, Action changed)
        {
            if (!component) return;
            if (EditorApplication.timeSinceStartup - _shownAt < SAME_CLICK) return;
            
            _shownAt = EditorApplication.timeSinceStartup;
            int copied = ComponentClipboard.Count;
            
            List<Component> targets = TargetsFor(component, picked);
            GenericMenu menu = new GenericMenu();
            
            // A Transform on its own gets a menu of its own. cause we want to ok?
            // (because normal unity has drop-down menus for copy stuff and I want all in one place easy quick access)
            bool alone = targets.Count == 1 && targets[0] is Transform;
            
            if (alone)
                AddTransform(menu, (Transform)targets[0], changed);
            else
                AddCopy(menu, targets);
            
            AddEnableDisable(menu, targets, changed);
            
            if (copied > 0)
            {
                GameObject target = component.gameObject;
                menu.AddItem(new GUIContent(Pasting(copied)), false, () =>
                {
                    Paste(target);
                    changed?.Invoke();
                });
                
                AddPasteValues(menu, targets, changed);
                menu.AddItem(new GUIContent("Forget Copied Components"), false, ComponentClipboard.Clear);
            }
            
            menu.ShowAsContext();
        }
        
        private static List<Component> TargetsFor(Component component, List<Component> picked)
        {
            if (picked.Count > 1 && picked.Contains(component)) return new List<Component>(picked);
            return new List<Component> { component };
        }
        
        private static void AddEnableDisable(GenericMenu menu, List<Component> targets, Action changed)
        {
            List<Component> toggleable = new List<Component>();
            
            foreach (Component target in targets)
                if (ComponentActions.CanToggleEnabled(target)) toggleable.Add(target);
            
            // A Transform, a Mesh Filter, anything with no checkbox on its header.
            // Offering to turn one of those off would be offering something that cannot be done.
            if (toggleable.Count == 0) return;
            
            if (toggleable.Count == 1)
            {
                Component only = toggleable[0];
                bool on = ComponentActions.IsEnabled(only);
                string name = ObjectNames.NicifyVariableName(only.GetType().Name);
                
                menu.AddItem(new GUIContent(on ? $"Disable {name}" : $"Enable {name}"), false, () =>
                {
                    ComponentActions.SetEnabled(toggleable, !on);
                    changed?.Invoke();
                });
            }
            else
            {
                // Both, rather than one that guesses. A set with some on and some off has no single opposite, and picking one for it would be picking for the person.
                menu.AddItem(new GUIContent($"Enable {toggleable.Count} Components"), false, () =>
                {
                    ComponentActions.SetEnabled(toggleable, true);
                    changed?.Invoke();
                });
                menu.AddItem(new GUIContent($"Disable {toggleable.Count} Components"), false, () =>
                {
                    ComponentActions.SetEnabled(toggleable, false);
                    changed?.Invoke();
                });
            }
            
            menu.AddSeparator(string.Empty);
        }
        
        private static void AddCopy(GenericMenu menu, List<Component> targets)
        {
            List<Component> copying = new List<Component>();
            
            foreach (Component target in targets)
                if (target && target is not Transform) copying.Add(target);
            
            if (copying.Count == 0) return;
            
            string label = copying.Count == 1 ? $"Copy {ObjectNames.NicifyVariableName(copying[0].GetType().Name)}" : $"Copy {copying.Count} Components";
            
            menu.AddItem(new GUIContent(label), false, () => ComponentClipboard.Copy(copying));
        }
        
        private static void AddPasteValues(GenericMenu menu, List<Component> targets, Action changed)
        {
            List<Component> carried = new List<Component>();
            ComponentClipboard.Fill(carried);
            
            if (carried.Count != 1 || !carried[0]) return;
            
            Component source = carried[0];
            List<Component> matching = new List<Component>();
            
            foreach (Component target in targets)
                if (target && target.GetType() == source.GetType()) matching.Add(target);
            
            if (matching.Count == 0) return;
            
            string name = ObjectNames.NicifyVariableName(source.GetType().Name);
            string label = matching.Count == 1 ? $"Paste {name} Values" : $"Paste {name} Values Into {matching.Count}";
            
            menu.AddItem(new GUIContent(label), false, () =>
            {
                ComponentActions.PasteValuesFrom(source, matching);
                changed?.Invoke();
            });
        }
        
        private static void AddTransform(GenericMenu menu, Transform transform, Action changed)
        {
            menu.AddItem(new GUIContent("Copy Component"), false, () => ComponentActions.Copy(transform));
            
            foreach (TransformPart part in PARTS)
            {
                if (!TransformClipboardAccess.Supports(part)) continue;
                
                TransformPart copying = part;
                menu.AddItem(new GUIContent($"Copy {TransformClipboardAccess.NameOf(part)}"), false, () => TransformClipboardAccess.Copy(copying, transform));
            }
            
            menu.AddSeparator(string.Empty);
            
            GUIContent values = new GUIContent("Paste Component Values");
            
            if (CopyOrder.CouldPasteValuesOnto(transform.GetType()))
            {
                menu.AddItem(values, false, () =>
                {
                    ComponentActions.PasteValues(transform);
                    changed?.Invoke();
                });
            }
            else
            {
                menu.AddDisabledItem(values);
            }
            
            bool stale = CopyOrder.ValuesAreStale;
            
            foreach (TransformPart part in PARTS)
            {
                if (!TransformClipboardAccess.Supports(part)) continue;
                
                TransformPart pasting = part;
                GUIContent label = new GUIContent($"Paste {TransformClipboardAccess.NameOf(part)}");
                
                if (stale || !TransformClipboardAccess.CanPaste(part, transform))
                {
                    menu.AddDisabledItem(label);
                    continue;
                }
                
                menu.AddItem(label, false, () =>
                {
                    TransformClipboardAccess.Paste(pasting, transform);
                    changed?.Invoke();
                });
            }
            
            menu.AddSeparator(string.Empty);
        }
        
        private static string Pasting(int count) => count == 1 ? "Paste 1 Component As New" : $"Paste {count} Components As New";
        
        private static void Paste(GameObject target)
        {
            List<Component> pasting = new List<Component>();
            ComponentClipboard.Fill(pasting);
            ComponentActions.PasteAll(target, pasting);
        }
        
    }
}
