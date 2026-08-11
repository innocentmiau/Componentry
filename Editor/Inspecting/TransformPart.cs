namespace Componentry.Inspecting
{
    /*
     * POSITION, ROTATION and SCALE are the numbers as they are typed in the Inspector, which are local to whatever the object is parented under.
     * WORLD is the odd one and the useful one: it is where the object actually is in the scene,
     * so pasting it onto an object under a different parent works out the numbers that put it in the same place,
     * rather than copying the numbers across and moving it somewhere else entirely.
     */
    /// <summary>
    /// The pieces of a Transform that can be copied on their own.
    /// </summary>
    public enum TransformPart
    {
        WORLD, // where the object actually sits in the scene, worked out again on paste so it lands in the same place under any parent
        POSITION, // the local numbers, exactly as they are typed in the Inspector
        ROTATION, // the local numbers, exactly as they are typed in the Inspector
        SCALE // the local numbers, exactly as they are typed in the Inspector
    }
}
