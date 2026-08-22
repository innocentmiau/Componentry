using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Componentry.Inspecting
{
    /*
     * Neither is in public API. 'UnityEdotor.InspectorWindow' is internal, it keeps its own list of every instance in the 'm_AllInspectors',
     * and the lock toggle is public property on that internal type, so... reflection seems to be the only way in from outside.
     *
     * Everything here is to fail quiet, if a future UNity renames some type or fields,
     * 'Available' becomes false and the package simply does nothing instead of throwing error every frame on the editor.
     */
    /// <summary>
    /// Every Inspector that is currently open, and whether one of them is locked.
    /// </summary>
    public static class InspectorWindowAccess
    {
        
        private static readonly Type WINDOW_TYPE = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo ALL_INSPECTORS = WINDOW_TYPE?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly PropertyInfo IS_LOCKED = WINDOW_TYPE?.GetProperty("isLocked", BindingFlags.Public | BindingFlags.Instance);
        
        public static bool Available => ALL_INSPECTORS != null;
        
        // Fills the given list rather than returning a new one, since this is called every editor frame and there is no reason to leave garbage behind at that rate.
        public static void CollectInto(List<EditorWindow> into)
        {
            into.Clear();
            
            if (ALL_INSPECTORS?.GetValue(null) is not IList all) return;
            
            foreach (object entry in all)
                if (entry is EditorWindow window && window)
                    into.Add(window);
        }
        
        /*
         * A locked Inspector keeps showing what it was locked onto, so we can't follow the selection,
         * and seems like there's no notification for when it's locked, so the getter is to bound once into a delegate instead and called like any other method.
         *
         * Null is when the property has gone missing, and the caller reads that as unlocked, which is the harmless answer: the bar then follows the selection.
         */
        public static Func<bool> LockedGetter(EditorWindow window)
        {
            MethodInfo getter = IS_LOCKED?.GetGetMethod();
            if (getter == null || window == null) return null;
            
            return Delegate.CreateDelegate(typeof(Func<bool>), window, getter, false) as Func<bool>;
        }
        
    }
}
