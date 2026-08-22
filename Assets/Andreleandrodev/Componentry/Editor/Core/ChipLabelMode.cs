namespace Componentry.Core
{
    /// <summary>
    /// Whether a chip carries the component's type name beside its icon.
    /// WHEN_NEEDED is the best one: an icon that appears once on the object says which component it is on its own,
    /// and the name beside it is a word taking up room to repeat what the picture already said.
    /// An icon that appears more than once does not, which is what happens to scripts,
    /// since a MonoBehaviour without an icon of its own is drawn with the same one as every other, and those keep their names.
    /// </summary>
    public enum ChipLabelMode
    {
        ALWAYS, // always show the name of the component next to the icon
        WHEN_NEEDED, // best of all no questions asked: only show text when there's multiple of the same component type so we know which is which, like 2 scripts shows their name, just 1 collider doesn't show and we know it's THE collider
        NEVER // sigma alpha mode: trust yourself to remember and know every component is what it is. for what is worth: I don't trust anyone to remember that much but for simple projects it's nice.
    }
}
