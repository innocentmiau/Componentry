using System;
using Componentry.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Componentry.Inspecting
{
    /*
     * Unity's own calls wherever Unity has one, so that a component pasted, moved or switched off from the bar behaves exactly as it would from the component's own header,
     * undo included, and there is nothing here quietly deciding any of it differently.
     *
     * Removing a component is deliberately not here. It is on the component's own header,
     * where Unity already asks the questions that have to be asked first,
     * and there is no public call that asks them.
     */
    /// <summary>
    /// What the bar does to components: pastes them onto an object, switches them off, moves them about.
    /// </summary>
    public static class ComponentActions
    {
        
        /// <summary>
        /// One component onto the editor's own component clipboard, which is what the Inspector's Copy Component does and writes where its pastes read from.
        /// </summary>
        /// <param name="component">The component to copy.</param>
        public static void Copy(Component component)
        {
            if (!component) return;
            
            ComponentUtility.CopyComponent(component);
            CopyOrder.ComponentCopied(component.GetType());
        }
        
        /// <summary>
        /// The values of whatever is on that clipboard poured into this component.
        /// Says so when there is nothing to pour, since whether anything is waiting is the one thing about that clipboard the editor keeps to itself.
        /// </summary>
        /// <param name="component">The component to paste onto. Nothing happens unless what was copied is of the same kind.</param>
        public static void PasteValues(Component component)
        {
            if (!component) return;
            if (ComponentUtility.PasteComponentValues(component)) return;
            EditorUtility.DisplayDialog("Nothing to Paste",$"Either no component has been copied, or what was copied is not a {Nice(component.GetType())}. Values can only be pasted onto a component of the same kind.", "Ok");
        }
        
        /// <summary>
        /// Puts a set of copied components onto an object, in the order they were copied. Each one goes through Unity's own copy and paste rather than through anything here,
        /// one at a time because that is the only shape those calls come in. That does mean the clipboard the Inspector's own Copy Component writes to is left holding the last of them afterward,
        /// which is a fair price for every kind of field pasting the way it does everywhere else. The whole lot is one entry in the undo history, since pasting six components was one thing the person did.
        /// </summary>
        /// <param name="target">the GameObject to paste the components onto.</param>
        /// <param name="components">the list of components we are trying to paste.</param>
        /// <returns>the amount of components successfully pasted onto the GameObject.</returns>
        public static int PasteAll(GameObject target, List<Component> components)
        {
            if (!target || components.Count == 0) return 0;
            
            int group = Undo.GetCurrentGroup();
            int pasted = 0;
            
            foreach (Component component in components)
            {
                if (!component) continue;
                if (!ComponentUtility.CopyComponent(component)) continue;
                if (ComponentUtility.PasteComponentAsNew(target)) pasted++;
            }
            
            Undo.SetCurrentGroupName(pasted == 1 ? "Paste Component" : "Paste Components");
            Undo.CollapseUndoOperations(group);
            
            return pasted;
        }
        
        /*
         * Pours one component's values into components that are already there, rather than adding anything.
         * The other half of pasting, and the half that answers "this collider should be set up like that one".
         *
         * Unity only pastes values between components of the same type, and that is checked by the caller so the entry is not offered where it would do nothing.
         */
        /// <summary>
        /// Pours one component's values into components that are already there, rather than adding anything.
        /// </summary>
        /// <param name="source">The component to take the values from. Left on the editor's clipboard afterwards.</param>
        /// <param name="targets">The components to pour them into, which have to be of the same type.</param>
        /// <returns>The amount of components successfully pasted onto.</returns>
        public static int PasteValuesFrom(Component source, List<Component> targets)
        {
            if (!source || targets.Count == 0) return 0;
            
            // Unity pastes from its own clipboard and has no way to be handed a component directly, so the source is put on that clipboard first.
            // It stays there afterwards, which is the same price of the other paste.
            if (!ComponentUtility.CopyComponent(source)) return 0;
            
            int group = Undo.GetCurrentGroup();
            int pasted = 0;
            
            foreach (Component target in targets)
            {
                if (!target) continue;
                if (ComponentUtility.PasteComponentValues(target)) pasted++;
            }
            
            if (pasted > 1)
            {
                Undo.SetCurrentGroupName("Paste Component Values");
                Undo.CollapseUndoOperations(group);
            }
            
            return pasted;
        }
        
        /*
         * Throws away the components whose script cannot be found, which is the only thing that can be done with one:
         * there is nothing left to point it back at.
         *
         * Asked about first, because this is not undoable and what goes with them is whatever was set on them in the Inspector before the script went missing.
         * Somebody who has just moved a script file and is about to move it back would not be happy with us.
         */
        /// <summary>
        /// Throws away the components whose script cannot be found, after asking. Not undoable, which is why it asks.
        /// </summary>
        /// <param name="gameObject">The object to clean up.</param>
        /// <param name="missing">How many are missing, used to word the question and to skip the work when there are none.</param>
        /// <returns>The amount removed, or zero when the question was answered with Cancel.</returns>
        public static int RemoveMissingScripts(GameObject gameObject, int missing)
        {
            if (!gameObject || missing <= 0) return 0;
            
            string what = missing == 1 ? "the missing script" : $"the {missing} missing scripts";
            bool confirmed = EditorUtility.DisplayDialog("Remove Missing Scripts", $"Remove {what} from {gameObject.name}?\n\nWhatever was set on them goes too, and this cannot be undone.", "Remove", "Cancel");
            
            if (!confirmed) return 0;
            
            return GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
        }
        
        /*
         * Asked of Unity rather than worked out from the type, which is the difference between right and nearly right:
         * 'Behaviour' has an 'enabled' and so does 'Renderer' and so does 'Collider', but they share no base that has one,
         * and the list of what else does is not something to keep up with. 'GetObjectEnabled' is the call the header's own checkbox is drawn from,
         * and it answers -1 for anything that has none.
         */
        /// <summary>
        /// Whether the component can be enabled/disabled. If a component has no toggle box then will return as false.
        /// </summary>
        /// <param name="component">Which component to check.</param>
        /// <returns>If component can be enabled/disabled</returns>
        public static bool CanToggleEnabled(Component component) => component && EditorUtility.GetObjectEnabled(component) >= 0;
        
        /// <summary>
        /// Whether the component is currently switched on.
        /// </summary>
        /// <param name="component">Which component to check.</param>
        /// <returns>True for a component with no checkbox at all, since something that cannot be turned off is never off(surprise there huh?).</returns>
        public static bool IsEnabled(Component component) => !component || EditorUtility.GetObjectEnabled(component) != 0;

        /// <summary>
        /// Switches a set of components on or off together. One entry in the undo history for the whole lot.
        /// </summary>
        /// <param name="components">The components to switch. Ones with no checkbox are skipped.</param>
        /// <param name="enabled">What to set them to.</param>
        /// <returns>How many actually changed, since nothing is written for a component that was already the way it was being asked to be.</returns>
        public static int SetEnabled(List<Component> components, bool enabled)
        {
            int group = Undo.GetCurrentGroup();
            int changed = 0;
            
            foreach (Component component in components)
            {
                if (!CanToggleEnabled(component)) continue;
                if (IsEnabled(component) == enabled) continue;
                
                Undo.RecordObject(component, enabled ? "Enable Component" : "Disable Component");
                EditorUtility.SetObjectEnabled(component, enabled);
                changed++;
            }
            
            if (changed > 1)
            {
                Undo.SetCurrentGroupName(enabled ? "Enable Components" : "Disable Components");
                Undo.CollapseUndoOperations(group);
            }
            
            return changed;
        }
        
        /// <summary>
        /// Whether the component is allowed to be dragged to a new place.
        /// </summary>
        /// <param name="component">Which component to check.</param>
        /// <returns>False for the Transform, which is the one component whose place on the object is not up for discussion.</returns>
        public static bool CanReorder(Component component) => component && component is not Transform;
        
        /*
         * Said as "put this next to that" rather than as a number of steps up or down: the bar leaves some components out,
         * the ones the Inspector does not draw, so a step in the bar and a step on the object are not the same distance.
         * Naming the neighbour has no such problem.
         *
         * Unity can be told exactly that in one call, but only through a method that is internal to the editor,
         * which is not allowed to be reached for. So the move is walked one step at a time instead and the steps are collapsed into a single undo entry,
         * which lands the component in the same place and reads the same way in the undo history.
         */
        /// <summary>
        /// Moves a component to a different position place.
        /// </summary>
        /// <param name="component">Which component to move</param>
        /// <param name="others">The rest of the components in the object.</param>
        /// <param name="slot">Which slot to move to.</param>
        /// <returns>True when the component was moved, false when it could not be or was already there.</returns>
        public static bool Reorder(Component component, List<Component> others, int slot)
        {
            if (!component || others.Count == 0) return false;
            
            bool above = slot < others.Count;
            Component neighbour = above ? others[slot] : others[others.Count - 1];
            
            if (!neighbour || neighbour == component) return false;
            
            return MoveInSteps(component, neighbour, above);
        }
        
        // make name nice. nice.
        private static string Nice(Type type) => ObjectNames.NicifyVariableName(type.Name);
        
        // Called from Reorder to walk the component to its slot one step at a time, so it ends in the right spot.
        // Since it's arrays we can't literally change one for another because that would change the new_spot component to the one we are swapping from(so if we swapped from 5th to 1st, then the 1st would become 5th, and this way we swap one by one keeping the rest of the orders.
        // yapping yappers.
        private static bool MoveInSteps(Component component, Component neighbour, bool above)
        {
            Component[] all = component.gameObject.GetComponents<Component>();
            
            int from = Array.IndexOf(all, component);
            int to = Array.IndexOf(all, neighbour);
            
            if (from < 0 || to < 0) return false;
            
            // Where it has to end up once it has been lifted out of where it is now, which shifts everything after it along by one.
            int destination = above ? (from > to ? to : to - 1) : (from < to ? to : to + 1);
            int steps = destination - from;
            
            if (steps == 0) return false;
            
            int group = Undo.GetCurrentGroup();
            
            for (int i = 0; i < Mathf.Abs(steps); i++)
            {
                bool moved = steps > 0 ? ComponentUtility.MoveComponentDown(component) : ComponentUtility.MoveComponentUp(component);
                if (!moved) break;
            }
            
            Undo.SetCurrentGroupName("Move Component");
            Undo.CollapseUndoOperations(group);
            
            return true;
        }
        
    }
}
