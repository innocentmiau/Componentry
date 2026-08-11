using System.Collections.Generic;
using Componentry.Core;
using UnityEditor;
using UnityEngine;

namespace Componentry.UI
{
    /*
     * The question under WHEN_NEEDED is not whether the component is unique on the object, it is whether the icon is.
     */
    /// <summary>
    /// Works out which chips carry their type name and which are left as an icon alone.
    /// </summary>
    public class ChipLabelResolver
    {
        
        private readonly Dictionary<Texture, int> _counts = new Dictionary<Texture, int>();
        
        // Takes the icons already looked up rather than looking them up again: this runs when
        // the components change, and the caller is holding them anyway to draw with.
        public void Resolve(List<Texture> icons, ChipLabelMode mode, List<bool> into)
        {
            into.Clear();
            
            if (mode != ChipLabelMode.WHEN_NEEDED)
            {
                bool labelled = mode == ChipLabelMode.ALWAYS;
                
                foreach (Texture icon in icons)
                    into.Add(labelled);
                return;
            }
            
            Count(icons);
            
            foreach (Texture icon in icons)
                into.Add(IsShared(icon));
        }
        
        public static Texture IconOf(Component component) => AssetPreview.GetMiniThumbnail(component);
        
        private void Count(List<Texture> icons)
        {
            _counts.Clear();
            
            foreach (Texture icon in icons)
            {
                if (!icon) continue;
                _counts.TryGetValue(icon, out int count);
                _counts[icon] = count + 1;
            }
        }
        
        // A component with no icon at all is a chip with nothing in it, so it always keeps its name however many of them there are.
        private bool IsShared(Texture icon) => !icon || _counts[icon] > 1;
        
    }
}
