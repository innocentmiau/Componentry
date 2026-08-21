using UnityEditor;
using UnityEngine;

namespace Componentry.Inspecting
{
    /*
     * The window itself is internal, so there is no call that opens it under a rectangle of our choosing.
     * The menu item that opens it is not internal, and it is the same window with the same list, the same search and the same "new script" flow at the end of it,
     * so that is what is used. What is given up is the placing: Unity opens the window where it wants rather than under the plus.
     * That is worth giving up, since the alternative is reflection into the editor's internals, which is not allowed on the Asset Store.
     *
     * The menu item adds to what is selected rather than to an object handed to it, so an object that is not the selection is selected first.
     * That only comes up in a locked Inspector, where the bar is showing what it was locked onto: a component added from that bar belongs on that object,
     * and selecting it is the honest way to say so, rather than quietly adding it to something else.
     */
    /// <summary>
    /// Unity's Add Component search, opened for a chosen object through the editor's own menu item.
    /// </summary>
    public static class AddComponentWindowAccess
    {

        private const string MENU_PATH = "Component/Add...";

        /// <summary>
        /// Opens the Add Component search for an object, selecting that object first when it is not already the selection.
        /// </summary>
        /// <param name="target">The object the component should be added to.</param>
        /// <returns>True when the menu item was run, false when there was no object to add to.</returns>
        public static bool Open(GameObject target)
        {
            if (!target) return false;

            if (Selection.activeGameObject != target)
                Selection.activeGameObject = target;

            return EditorApplication.ExecuteMenuItem(MENU_PATH);
        }

    }
}
