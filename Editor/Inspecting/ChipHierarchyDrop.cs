using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Componentry.Inspecting
{
    /*
     * Dropping them on an object puts them on that object, which is the copy and paste already in the bar said as one gesture instead of three.
     * Dropping them on nothing makes an empty GameObject and puts them on that,
     * which is the quickest way there is to lift a working set of components out of one object and into a new one of its own.
     *
     * Unity's own drag and drop does the carrying. What is dragged is not an object, so there is nothing for it to put in objectReferences;
     * a private note is left on the drag instead, and the handler below refuses any drag that is not carrying that note, so this never touches somebody else's drag.
     */
    /// <summary>
    /// Carrying components out of the bar and into the Hierarchy.
    /// </summary>
    public static class ChipHierarchyDrop
    {
        
        private const string KEY = "ComponentryChips";
        private const string CREATED_NAME = "GameObject";
        
        private static readonly List<Component> CARRIED = new List<Component>();
        
        private static bool _listening;
        
        public static bool Carrying => CARRIED.Count > 0;
        
        public static void Begin(List<Component> components)
        {
            Stop();
            
            CARRIED.Clear();
            
            foreach (Component component in components)
                if (component && component is not Transform) CARRIED.Add(component);
            
            if (CARRIED.Count == 0) return;
            
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = Array.Empty<Object>();
            DragAndDrop.SetGenericData(KEY, true);
            DragAndDrop.StartDrag(Title());
        }
        
        /// <summary>
        /// Called while the Hierarchy is drawing, which is the only time the drop can be offered to it.
        /// The handler is added when a drag of ours arrives over the window and taken off again when it leaves, so nothing of ours is left registered afterward.
        /// </summary>
        public static void OnHierarchyGUI()
        {
            if (!Carrying) return;
            
            if (Event.current.type == EventType.DragUpdated && !_listening)
            {
                DragAndDrop.AddDropHandlerV2(Drop);
                _listening = true;
            }
            
            if (Event.current.type == EventType.DragExited) Stop();
        }
        
        public static void Stop()
        {
            if (_listening)
            {
                DragAndDrop.RemoveDropHandlerV2(Drop);
                _listening = false;
            }
            
            CARRIED.Clear();
        }
        
        /*
         * Entering play mode with the domain reload turned off leaves every static here exactly as it was.
         * A drag still in the air at that moment would go on carrying components the change has since destroyed,
         * and the drop handler registered for it would stay registered for the rest of the session, answering for a drag that is long over.
         * Stop puts both back, and it is the same call the end of an ordinary drag makes.
         *
         * Only when the reload is off. With it on the statics are cleared out anyway, and this would be asking for something that has already happened.
         */
        [InitializeOnEnterPlayMode]
        private static void OnEnterPlayMode(EnterPlayModeOptions options)
        {
            if (!options.HasFlag(EnterPlayModeOptions.DisableDomainReload)) return;
            
            Stop();
        }
        
        /*
         * The V2 handler, which differs from the older one only in taking an EntityId where that took an int.
         * The older pair is deprecated as of the Unity this package asks for, and both are the same id underneath.
         */
        private static DragAndDropVisualMode Drop(EntityId dropTarget, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            // Somebody else's drag, which happens to be passing over a window we are watching.
            if (!Carrying || DragAndDrop.GetGenericData(KEY) is not bool) return DragAndDropVisualMode.None;
            
            GameObject target = EditorUtility.EntityIdToObject(dropTarget) as GameObject;
            bool onObject = target && dropMode.HasFlag(HierarchyDropFlags.DropUpon);
            
            if (!perform) return DragAndDropVisualMode.Copy;
            
            GameObject landed = onObject ? target : CreateEmpty(parentForDraggedObjects);
            
            if (landed)
            {
                ComponentActions.PasteAll(landed, CARRIED);
                
                // Selected afterwards, so that what was just made or just changed is what the
                // Inspector is showing. Delayed because the drop is still being handled and
                // changing the selection underneath that is asking for trouble.
                GameObject selected = landed;
                EditorApplication.delayCall += () => Selection.activeObject = selected;
            }
            
            Stop();
            
            return DragAndDropVisualMode.Copy;
        }
        
        // What Unity's own Create Empty makes: an object called GameObject, in the scene the drop landed in, under whatever it was dropped inside, and undoable.
        private static GameObject CreateEmpty(Transform parent)
        {
            GameObject created = new GameObject(CREATED_NAME);
            
            Undo.RegisterCreatedObjectUndo(created, "Create Empty");
            
            if (parent)
            {
                Undo.SetTransformParent(created.transform, parent, "Create Empty");
                created.transform.localPosition = Vector3.zero;
                created.transform.localRotation = Quaternion.identity;
                created.transform.localScale = Vector3.one;
            }
            
            return created;
        }
        
        private static string Title() => CARRIED.Count == 1 ? CARRIED[0].GetType().Name : $"{CARRIED.Count} Components";
        
    }
}
