using System.Collections.Generic;
using Componentry.Core;
using Componentry.Inspecting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Componentry.UI
{
    /*
     * There is no hook for "an Inspector was opened", so the list of windows is compared against ours on the editor's update instead.
     * That is once a frame and costs a field read plus a walk of a list that is almost always one item long.
     */
    /// <summary>
    /// Keeps exactly one component bar per open Inspector, and keeps each of them pointed at whatever its window is showing.
    /// </summary>
    [InitializeOnLoad]
    public static class ComponentBarInstaller
    {

        private static readonly List<ComponentBar> BARS = new List<ComponentBar>();
        private static readonly List<EditorWindow> WINDOWS = new List<EditorWindow>();

        private static bool _running;

        static ComponentBarInstaller()
        {
            ComponentrySettings.Changed += OnSettingsChanged;

            // Delayed, because a static constructor runs while the editor is still loading and the Inspector windows are not there to be found yet.
            EditorApplication.delayCall += OnSettingsChanged;
        }

        private static void OnSettingsChanged()
        {
            if (!ComponentrySettings.Enabled)
            {
                Stop();
                return;
            }

            Start();

            // Every measurement is taken when a bar is built, so the ones already on screen are thrown away and built again at whatever the size is now.
            foreach (ComponentBar bar in BARS)
                bar.Refresh();
        }

        /*
         * Unity does announce a component being added or removed, and this is the announcement.
         * Without it a bar would only notice on its own next update, and only by the count having moved, which misses components being reordered.
         *
         * Only the structural changes are listened for. The event that fires for a property being edited fires while a value is being dragged,
         * and there is nothing in a bar that a property can change.
         */
        private static void OnObjectsChanged(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    case ObjectChangeKind.ChangeGameObjectStructure:
                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    case ObjectChangeKind.UpdatePrefabInstances:
                        MarkAllDirty();
                        return;
                }
            }
        }

        // Once per row of the Hierarchy, so the work is done in the drop itself rather than here: this only notices that a drag of ours has arrived.
        private static void OnHierarchyItem(int instanceId, Rect area) => ChipHierarchyDrop.OnHierarchyGUI();

        private static void MarkAllDirty()
        {
            foreach (ComponentBar bar in BARS)
                bar.MarkDirty();
        }

        private static void Start()
        {
            if (_running || !InspectorWindowAccess.Available) return;

            _running = true;

            EditorApplication.update += Update;
            ObjectChangeEvents.changesPublished += OnObjectsChanged;
            Undo.undoRedoPerformed += MarkAllDirty;

            // The Hierarchy has to be watched for a chip being dropped into it, and this is the only callback that runs while that window is drawing.
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItem;
        }

        private static void Stop()
        {
            if (!_running) return;

            _running = false;

            EditorApplication.update -= Update;
            ObjectChangeEvents.changesPublished -= OnObjectsChanged;
            Undo.undoRedoPerformed -= MarkAllDirty;
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItem;

            ChipHierarchyDrop.Stop();

            foreach (ComponentBar bar in BARS)
                bar.Detach();

            BARS.Clear();
        }

        private static void Update()
        {
            SyncWindows();

            Object selected = SelectedObject();

            foreach (ComponentBar bar in BARS)
            {
                bar.Follow(selected);
                bar.Update();
            }
        }

        private static void SyncWindows()
        {
            InspectorWindowAccess.CollectInto(WINDOWS);

            for (int i = BARS.Count - 1; i >= 0; i--)
            {
                if (!BARS[i].Window || !WINDOWS.Contains(BARS[i].Window))
                    BARS.RemoveAt(i);
            }

            foreach (EditorWindow window in WINDOWS)
            {
                if (HasBar(window)) continue;

                BARS.Add(new ComponentBar(window));
            }
        }

        private static bool HasBar(EditorWindow window)
        {
            foreach (ComponentBar bar in BARS)
            {
                if (bar.Window == window) return true;
            }

            return false;
        }

        /*
         * Only ever one thing. With several selected the Inspector draws the components they have in common, which is not the same list as the active object's,
         * and a bar that disagrees with the headers under it would be worse than no bar.
         *
         * Selection.count rather than Selection.gameObjects.Length, which builds an array every time it is read and this is read every frame.
         */
        private static Object SelectedObject() => Selection.count == 1 ? Selection.activeObject : null;

    }
}
