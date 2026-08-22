namespace Componentry.Inspecting
{
    /// <summary>
    /// What sort of thing an Inspector is showing. Only GameObjects are of any interest, and the two that are differ in how the Inspector lays itself out above the components.
    /// </summary>
    public enum InspectedKind
    {
        NONE, 
        SCENE_OBJECT, 
        PREFAB_ASSET
    }
}
