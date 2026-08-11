using System;
using UnityEditor;
using UnityEngine;

namespace Componentry.Core
{
    /*
     * Kept in EditorPrefs rather than anywhere in the project: this is how one person likes their Inspector,
     * so it should follow them between projects instead of being saved with any one of them and turning up in somebody else's checkout.
     *
     * Read from EditorPrefs once and held in memory after that. EditorPrefs is a native call out to a store on disk,
     * and these are read while laying out every chip on every repaint, which turned that into hundreds of them a frame.
     * Nothing else writes these keys, so the copy in memory cannot go stale; a domain reload drops it and the next read loads it again.
     */
    /// <summary>
    /// The settings values stuff for the package.
    /// </summary>
    public static class ComponentrySettings
    {
        
        private const string ENABLED_PREF = "Componentry.ComponentBar.Enabled";
        private const string SIZE_PREF = "Componentry.ComponentBar.Size";
        private const string LABELS_PREF = "Componentry.ComponentBar.Labels";
        private const string SHOW_ALL_WHEN_NEEDED_PREF = "Componentry.ComponentBar.ShowAllWhenNeeded";
        private const string SEARCH_PROPERTIES_PREF = "Componentry.ComponentBar.SearchProperties";
        private const string TRANSFORM_ONLY_PREF = "Componentry.ComponentBar.HideOnTransformOnly";
        private const string ADD_COMPONENT_PREF = "Componentry.ComponentBar.AddComponent";
        private const string MAX_ROWS_PREF = "Componentry.ComponentBar.MaxRows";
        
        private static bool _loaded;
        private static bool _enabled;
        private static BarSize _size;
        private static ChipLabelMode _labels;
        private static bool _showAllWhenNeeded;
        private static bool _searchProperties;
        private static bool _hideOnTransformOnly;
        private static bool _addComponent;
        private static int _maxRows;
        
        /// <summary>
        /// Raised whenever anything here is written, whichever of the settings window, the preferences page or the menu did the writing,
        /// so the bars can be rebuilt at the new size without anything having to poll.
        /// </summary>
        public static event Action Changed;
        
        public static bool Enabled
        {
            get
            {
                Load();
                return _enabled;
            }
            set
            {
                if (value == Enabled) return;
                
                _enabled = value;
                EditorPrefs.SetBool(ENABLED_PREF, value);
                Changed?.Invoke();
            }
        }
        
        public static BarSize Size
        {
            get
            {
                Load();
                return _size;
            }
            set
            {
                if (value == Size) return;
                
                _size = value;
                EditorPrefs.SetInt(SIZE_PREF, (int)value);
                Changed?.Invoke();
            }
        }
        
        public static ChipLabelMode Labels
        {
            get
            {
                Load();
                return _labels;
            }
            set
            {
                if (value == Labels) return;
                
                _labels = value;
                EditorPrefs.SetInt(LABELS_PREF, (int)value);
                Changed?.Invoke();
            }
        }
        
        /// <summary>
        /// Whether the Show All chip keeps out of the way until there is something to show all of, which is how it starts, or sits at the front of the bar at all times.
        /// </summary>
        public static bool ShowAllWhenNeeded
        {
            get
            {
                Load();
                return _showAllWhenNeeded;
            }
            set
            {
                if (value == ShowAllWhenNeeded) return;
                
                _showAllWhenNeeded = value;
                EditorPrefs.SetBool(SHOW_ALL_WHEN_NEEDED_PREF, value);
                Changed?.Invoke();
            }
        }
        
        /// <summary>
        /// Whether a search looks inside components as well as at their names.
        /// Off, because the answer to a name is the Inspector's own editors filtered down to it, while the answer to a property is the serialized fields behind it,
        /// which is a different and more technical view of a component than most people are asking for.
        /// </summary>
        public static bool SearchProperties
        {
            get
            {
                Load();
                return _searchProperties;
            }
            set
            {
                if (value == SearchProperties) return;
                
                _searchProperties = value;
                EditorPrefs.SetBool(SEARCH_PROPERTIES_PREF, value);
                Changed?.Invoke();
            }
        }
        
        /// <summary>
        /// Whether an object with nothing on it but a Transform gets a bar at all.
        /// On, because a bar there is a row saying the object has the one component every object has,
        /// and scenes are full of empties used as parents and as markers.
        /// </summary>
        public static bool HideOnTransformOnly
        {
            get
            {
                Load();
                return _hideOnTransformOnly;
            }
            set
            {
                if (value == HideOnTransformOnly) return;
                
                _hideOnTransformOnly = value;
                EditorPrefs.SetBool(TRANSFORM_ONLY_PREF, value);
                Changed?.Invoke();
            }
        }
        
        /// <summary>
        /// Whether the bar carries a button that opens Unity's Add Component search. On, because the bar is where the components are and adding one belongs with them.
        /// </summary>
        public static bool AddComponent
        {
            get
            {
                Load();
                return _addComponent;
            }
            set
            {
                if (value == AddComponent) return;
                
                _addComponent = value;
                EditorPrefs.SetBool(ADD_COMPONENT_PREF, value);
                Changed?.Invoke();
            }
        }
        
        /// <summary>
        /// How many rows of chips the bar will grow to before leaving the rest out.
        /// Clamped on the way in as well as on the way out,
        /// so a number typed straight into EditorPrefs cannot give the bar a height nothing can scroll past.
        /// </summary>
        public static int MaxRows
        {
            get
            {
                Load();
                return _maxRows;
            }
            set
            {
                int rows = Mathf.Clamp(value, Constants.ROWS_MINIMUM, Constants.ROWS_MAXIMUM);
                
                if (rows == MaxRows) return;
                
                _maxRows = rows;
                EditorPrefs.SetInt(MAX_ROWS_PREF, rows);
                Changed?.Invoke();
            }
        }
        
        public static float Scale => ScaleOf(Size);
        
        private static float ScaleOf(BarSize size) => size switch
        {
            BarSize.COMPACT => Constants.SCALE_COMPACT,
            BarSize.SMALL => Constants.SCALE_SMALL,
            BarSize.LARGE => Constants.SCALE_LARGE,
            BarSize.EXTRA_LARGE => Constants.SCALE_EXTRA_LARGE,
            _ => Constants.SCALE_DEFAULT
        };
        
        private static void Load()
        {
            if (_loaded) return;
            
            _loaded = true;
            _enabled = EditorPrefs.GetBool(ENABLED_PREF, Defaults.ENABLED);
            _size = (BarSize)EditorPrefs.GetInt(SIZE_PREF, (int)Defaults.SIZE);
            _labels = (ChipLabelMode)EditorPrefs.GetInt(LABELS_PREF, (int)Defaults.LABELS);
            _showAllWhenNeeded = EditorPrefs.GetBool(SHOW_ALL_WHEN_NEEDED_PREF, Defaults.SHOW_ALL_WHEN_NEEDED);
            _searchProperties = EditorPrefs.GetBool(SEARCH_PROPERTIES_PREF, Defaults.SEARCH_PROPERTIES);
            _hideOnTransformOnly = EditorPrefs.GetBool(TRANSFORM_ONLY_PREF, Defaults.HIDE_ON_TRANSFORM_ONLY);
            _addComponent = EditorPrefs.GetBool(ADD_COMPONENT_PREF, Defaults.ADD_COMPONENT);
            _maxRows = Mathf.Clamp(EditorPrefs.GetInt(MAX_ROWS_PREF, Defaults.MAX_ROWS), Constants.ROWS_MINIMUM, Constants.ROWS_MAXIMUM);
        }
        
    }
}
