namespace Componentry.Core
{
    /// <summary>
    /// How large the component bar is drawn. Each one is a multiplier over the numbers in Constants rather than a set of measurements of its own, so the four stay in proportion.
    /// </summary>
    public enum BarSize
    {
        COMPACT, // tiny
        SMALL, // small
        DEFAULT, // base
        LARGE, // big
        EXTRA_LARGE // mega big
    }
}
