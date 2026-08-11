using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Componentry.Inspecting
{
    /*
     * The window is internal, so this is reflection, but it is worth reaching for rather than rebuilding:
     * what is in that list, how it is ordered, the search over it and the "new script" flow at the bottom of it are a great deal of editor that already exists.
     *
     * It is asked for this object rather than for whatever happens to be selected, which matters in a locked Inspector:
     * the bar there is showing what it was locked onto, and a component added from that bar belongs on that object.
     */
    /// <summary>
    /// Unity's Add Component search, opened under a rectangle of our choosing.
    /// </summary>
    public static class AddComponentWindowAccess
    {
        
        private static readonly Type WINDOW_TYPE = typeof(Editor).Assembly.GetType("UnityEditor.AddComponent.AddComponentWindow");
        
        private static readonly MethodInfo SHOW = WINDOW_TYPE?.GetMethod(
            "Show", BindingFlags.NonPublic | BindingFlags.Static,
            null, new[] { typeof(Rect), typeof(GameObject[]) }, null);
        
        /// <summary>
        /// What the Add Component shortcut runs. No say in which object or where it opens, so it is only here for the day the call above moves.
        /// </summary>
        private static readonly MethodInfo EXECUTE = WINDOW_TYPE?.GetMethod(
            "ExecuteAddComponentMenuItem",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            Type.EmptyTypes,
            null);
        
        public static bool Available => SHOW != null || EXECUTE != null;
        
        public static bool Open(Rect under, GameObject target)
        {
            if (!target) return false;
            
            try
            {
                if (SHOW != null && SHOW.Invoke(null, new object[] { under, new[] { target } }) is true) return true;
                
                if (EXECUTE == null) return false;
                
                EXECUTE.Invoke(null, null);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    
    }
}
