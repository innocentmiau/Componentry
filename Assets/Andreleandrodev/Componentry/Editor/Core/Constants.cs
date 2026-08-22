using UnityEngine;

namespace Componentry.Core
{
    /*
     * The settings offer a handful of sizes because a menu with a slider in it would be a worse menu, but a handful will not suit everybody.
     * Anyone who wants the bar a shape the presets do not offer has one file to open and no code to read:
     * change a number here, let Unity recompile, and the bar comes back the new size.
     *
     * The sizes below are the bar at a scale of 1, which is SMALL. Every one of them is multiplied by the chosen preset's scale before it is used,
     * so changing one changes it at every size and the presets keep their proportions.
     */
    /// <summary>
    /// Every number the component bar is made with, gathered here on purpose. The purpose of life for this package.
    /// </summary>
    public static class Constants
    {
        
        // The chip: a rounded button with an icon on the left and the type name beside it.
        public const float CHIP_HEIGHT = 18f;
        public const float CHIP_PADDING = 5f;
        public const float CHIP_SPACING = 3f;
        public const int FONT_SIZE = 9;
        
        // The icon inside the chip. Drawn at a fixed size rather than the texture's own, since component icons come in several sizes and a row of mismatched ones is a mess.
        public const float ICON_SIZE = 14f;
        public const float ICON_GAP = 3f;
        
        // Between rows when the chips do not fit on one line, and around the bar as a whole.
        public const float ROW_SPACING = 2f;
        public const float BAR_MARGIN = 4f;
        
        // How far the rows setting can be moved. Not how many rows there are: that is a setting, and where it starts is in Defaults.
        // One row is a bar; ten is most of an Inspector, and past that the bar has stopped being a thing read at a glance.
        public const int ROWS_MINIMUM = 1;
        public const int ROWS_MAXIMUM = 10;
        
        // The search field above the chips, and the gap between it and them.
        public const float SEARCH_HEIGHT = 18f;
        public const float SEARCH_GAP = 4f;
        
        // The magnifier at the start of the field. Drawn here rather than left to the editor's own search field style,
        // whose magnifier is part of a background image and stays the size it was drawn at however large the surrounding field gets.
        public const float SEARCH_ICON_SIZE = 12f;
        
        // The cross at the end of the search field that empties it. The field always keeps a gap this wide at its end,
        // so that text never runs under the cross and nothing moves when it appears.
        public const float CLEAR_SIZE = 11f;
        
        // How long after the last key is pressed before the search is run. Long enough that typing a word is one search rather than one per letter,
        // short enough that it still feels like it is answering as you type.
        public const double SEARCH_DELAY = .15;
        
        // How far a press has to move before it is a drag rather than a click. Too small and picking a component shuffles the bar; too large and reordering feels stuck.
        public const float DRAG_THRESHOLD = 5f;
        
        // How far a chip fades when its component is turned off, so that the bar says what is switched off as well as what is there.
        // Faded rather than crossed out or recolored, which is what the Inspector does to the component itself underneath.
        public const float DISABLED_FADE = .4f;
        
        // The outline around the chip being dragged. Yellow because it is the color these packages use for something that has been set up and has not happened yet,
        // which is exactly what a chip being carried about is: nothing has moved until it is dropped.
        public const float DRAG_OUTLINE = 1.5f;
        public static readonly Color DRAGGING = new Color(1f, .86f, .4f);
        
        // The chip standing for the components whose script cannot be found. Red because it is the color these packages use for something being thrown away,
        // which is both what has happened to the script and the only thing left to do about it.
        public static readonly Color MISSING = new Color(1f, .5f, .45f);
        
        // The chip at the front of the bar that drops the filter. Its name is short on purpose: it is in front of the components at every size and should not crowd them.
        public const string SHOW_ALL_NAME = "All";
        
        // The line along the bottom of a chip that has been picked. A line rather than a tint over the whole chip, which washes out against the dark skin at this size.
        // The blue is the one the rest of these packages use for a control doing the thing the window is for.
        public const float ACCENT_HEIGHT = 2f;
        public static readonly Color ACCENT = new Color(.55f, .78f, 1f);
        
        // What each preset multiplies all of the sizes above by. SMALL is 1, so the numbers above are the bar at that size, read as they are written.
        public const float SCALE_COMPACT = .85f;
        public const float SCALE_SMALL = 1f;
        public const float SCALE_DEFAULT = 1.25f;
        public const float SCALE_LARGE = 1.5f;
        public const float SCALE_EXTRA_LARGE = 2f;
        
    }
}
