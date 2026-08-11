namespace Componentry.Core
{
    /*
     * These are not the same kind of number as the ones in Constants. Those describe how the bar is drawn and are the same for everybody;
     * these are only starting points, and each one is overwritten the first time somebody moves the setting it belongs to.
     * Changing one here changes what a person sees on a machine where they have never opened the settings, and nothing at all for anybody who has.
     *
     * Worth their own file for that reason: gathering them where the settings are read would put a decision in the middle of the plumbing,
     * and the plumbing is not what anybody opening this package wants to read first.
     * 
     */
    /// <summary>
    /// What every setting is before anybody changes it, in one place.
    /// </summary>
    public static class Defaults
    {
        
        // On, because a package that has to be turned on after installing it is a package most people never see working.
        public const bool ENABLED = true;
        
        public const BarSize SIZE = BarSize.DEFAULT;
        
        // Names only where the icon is not enough on its own. See ChipLabelMode.
        public const ChipLabelMode LABELS = ChipLabelMode.WHEN_NEEDED;
        
        // The Show All chip keeps out of the way until there is something to show all of.
        public const bool SHOW_ALL_WHEN_NEEDED = true;
        
        // Off. A search by name is answered by the Inspector's own editors, which is what most people mean;
        // searching inside components answers with the serialized fields behind them, which is a more technical view than most are asking for.
        public const bool SEARCH_PROPERTIES = false;
        
        // On. A bar on an object with nothing but a Transform says only that the object has the one component every object has.
        public const bool HIDE_ON_TRANSFORM_ONLY = true;
        
        // On. The bar is where the components are, so adding one belongs with them.
        public const bool ADD_COMPONENT = true;
        
        // How many rows of chips before the rest are left out. Six is a lot of components before the bar stops being something read at a glance, which is all it is for.
        public const int MAX_ROWS = 6;
        
    }
}
