using System;
using Componentry.Core;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Componentry.Inspecting
{
    /*
     * The class holding all of it is internal, so this is reflection, and every entry point answers quietly rather than throwing if a future Unity moves it.
     */
    /// <summary>
    /// Copying and pasting the pieces of a Transform, through the very code the Inspector's own Copy and Paste submenus run.
    /// </summary>
    public static class TransformClipboardAccess
    {
        
        private static readonly Type MENU = typeof(Editor).Assembly.GetType("UnityEditor.ClipboardContextMenu");
        
        private static readonly MethodInfo COPY_WORLD = Find("CopyTransformWorldPlacementMenu");
        private static readonly MethodInfo COPY_POSITION = Find("CopyTransformPositionMenu");
        private static readonly MethodInfo COPY_ROTATION = Find("CopyTransformRotationMenu");
        private static readonly MethodInfo COPY_SCALE = Find("CopyTransformScaleMenu");
        
        private static readonly MethodInfo PASTE_WORLD = Find("PasteTransformWorldPlacementMenu");
        private static readonly MethodInfo PASTE_POSITION = Find("PasteTransformPositionMenu");
        private static readonly MethodInfo PASTE_ROTATION = Find("PasteTransformRotationMenu");
        private static readonly MethodInfo PASTE_SCALE = Find("PasteTransformScaleMenu");
        
        private static readonly MethodInfo CAN_WORLD = Find("PasteTransformWorldPlacementMenuValidate");
        private static readonly MethodInfo CAN_POSITION = Find("PasteTransformPositionMenuValidate");
        private static readonly MethodInfo CAN_ROTATION = Find("PasteTransformRotationMenuValidate");
        private static readonly MethodInfo CAN_SCALE = Find("PasteTransformScaleMenuValidate");
        
        public static bool Available => COPY_WORLD != null && PASTE_WORLD != null;
        
        public static void Copy(TransformPart part, Transform transform)
        {
            Run(CopyMethod(part), transform);
            CopyOrder.ValuesCopied();
        }
        
        public static void Paste(TransformPart part, Transform transform) => Run(PasteMethod(part), transform);
        
        // 
        public static bool CanPaste(TransformPart part, Transform transform)
        {
            MethodInfo validate = ValidateMethod(part);
            
            if (validate == null || !transform) return false;
            
            try
            {
                return validate.Invoke(null, new object[] { new MenuCommand(transform) }) is true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public static string NameOf(TransformPart part) => part switch
        {
            TransformPart.WORLD => "World Transform",
            TransformPart.POSITION => "Position",
            TransformPart.ROTATION => "Rotation",
            _ => "Scale"
        };
        
        private static void Run(MethodInfo method, Transform transform)
        {
            if (method == null || !transform) return;
            
            try
            {
                method.Invoke(null, new object[] { new MenuCommand(transform) });
            }
            catch (Exception)
            {
                // Nothing useful to say and nothing to put right.
            }
        }
        
        private static MethodInfo CopyMethod(TransformPart part) => part switch
        {
            TransformPart.WORLD => COPY_WORLD,
            TransformPart.POSITION => COPY_POSITION,
            TransformPart.ROTATION => COPY_ROTATION,
            _ => COPY_SCALE
        };
        
        private static MethodInfo PasteMethod(TransformPart part) => part switch
        {
            TransformPart.WORLD => PASTE_WORLD,
            TransformPart.POSITION => PASTE_POSITION,
            TransformPart.ROTATION => PASTE_ROTATION,
            _ => PASTE_SCALE
        };
        
        private static MethodInfo ValidateMethod(TransformPart part) => part switch
        {
            TransformPart.WORLD => CAN_WORLD,
            TransformPart.POSITION => CAN_POSITION,
            TransformPart.ROTATION => CAN_ROTATION,
            _ => CAN_SCALE
        };
        
        private static MethodInfo Find(string name)
        {
            return MENU?.GetMethod(
                name,
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(MenuCommand) },
                null);
        }
        
    }
}
