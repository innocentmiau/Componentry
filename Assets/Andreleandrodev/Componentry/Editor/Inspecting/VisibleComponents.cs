using System.Collections.Generic;
using UnityEngine;

namespace Componentry.Inspecting
{
    /// <summary>
    /// What the Inspector draws for a GameObject, in the order it draws it.
    /// </summary>
    public static class VisibleComponents
    {
        
        private static readonly List<Component> BUFFER = new List<Component>();
        
        /// <summary>
        /// One entry per editor the Inspector builds, nulls and all.
        /// </summary>
        /// <param name="gameObject">The object to read from.</param>
        /// <param name="into">List filled with one entry per editor the Inspector will draw, cleared first. A null entry is a missing script, which still gets a box.</param>
        public static void CollectDrawn(GameObject gameObject, List<Component> into)
        {
            into.Clear();
            
            if (!gameObject) return;
            
            gameObject.GetComponents(BUFFER);
            
            foreach (Component component in BUFFER)
            {
                // A missing script. Nothing can be asked of it, not even its hide flags, but the Inspector draws its box and that box has to be counted.
                if (!component)
                {
                    into.Add(null);
                    continue;
                }
                if (IsVisible(component)) into.Add(component);
            }
        }
        
        /// <summary>
        /// The ones a chip can be drawn for, taken from the list above so the two cannot disagree about what the Inspector is showing.
        /// </summary>
        /// <param name="drawn">The list from CollectDrawn.</param>
        /// <param name="into">List filled with the non-null entries, cleared first.</param>
        public static void CollectChips(List<Component> drawn, List<Component> into)
        {
            into.Clear();
            
            foreach (Component component in drawn)
                if (component) into.Add(component);
        }
        
        /// <summary>
        /// How many missing scripts are on the object, which is how many boxes the Inspector draws that no chip can be made for.
        /// </summary>
        /// <param name="drawn">The list from CollectDrawn.</param>
        /// <returns>The count of null entries in it.</returns>
        public static int MissingIn(List<Component> drawn)
        {
            int missing = 0;
            
            foreach (Component component in drawn)
                if (!component) missing++;
            
            return missing;
        }
        
        private static bool IsVisible(Component component)
        {
            if (component.hideFlags.HasFlag(HideFlags.HideInInspector)) return false;
            
            // Drawn as part of the particle system's own editor
            // rather than as a header of its own,
            // so it is on the GameObject without ever being a row in the Inspector.
            return component is not ParticleSystemRenderer;
        }
        
    }
}
