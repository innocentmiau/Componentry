using System;
using System.Globalization;
using System.Reflection;
using Componentry.Core;
using UnityEditor;
using UnityEngine;

namespace Componentry.Inspecting
{
    /*
     * Position, rotation and scale are written straight to the editor's value clipboard, which is the machine's own clipboard and nothing more.
     * Unity puts them there as plain text, in the two shapes below, and reads them back the same way,
     * so a position copied here pastes from the Transform's own header or into any Vector3 field, and one copied there pastes here.
     * That is the whole of the interoperation, and none of it needs anything Unity keeps to itself.
     *
     * The shapes are Unity's, not ours. They are what its own copy writes, and writing anything else would put text on the clipboard
     * that only this package could read, which is the one thing worth avoiding here.
     * Invariant throughout, because the separator between the numbers is a comma and half the world writes decimals with one.
     */
    /// <summary>
    /// Copying and pasting the pieces of a Transform, through the same clipboard the Inspector's own Copy and Paste entries use.
    /// </summary>
    public static class TransformClipboardAccess
    {
        
        private const string VECTOR3_NAME = "Vector3";
        private const string QUATERNION_NAME = "Quaternion";
        
        private const string VECTOR3_FORMAT = "Vector3({0:g9},{1:g9},{2:g9})";
        
        private static readonly float[] NUMBERS = new float[4];
        
        /*
         * Everything above is public API. This is not, and it is the one piece that has no public way of being done at all.
         *
         * World placement is not a Vector3 or a Quaternion on the clipboard, the way the other three are.
         * Unity carries it as a custom value of type 'UnityEditor.TransformWorldPlacement', which is internal,
         * written through 'Clipboard.SetCustomValue' behind a prefix of its own, and 'UnityEditor.Clipboard' is internal as well.
         * There is no public type to build, no public call to write it with, and no menu item to run instead:
         * the entries on the Transform's own header are added in code rather than registered as menu items, so 'ExecuteMenuItem' has no path to aim at.
         * Checked against the assemblies of the Unity this package asks for, not from memory.
         *
         * Producing the same text by hand would mean copying out the private serialisation of a type we cannot see, which is the same reach by another route,
         * and it would break the moment either changes.
         *
         * So this stays reflection, knowingly and only here. It fails quiet: if a future Unity moves any of it, 'Supports' turns false,
         * the two World Transform entries stop being offered, and the other six carry on untouched.
         * The Asset Store forbids reaching into the editor's internals this way, so if a review refuses the package over it, the answer is to drop these two entries,
         * not to look for a cleverer way in. Everything needed to do that is in this one place.
         */
        private static readonly Type MENU = typeof(Editor).Assembly.GetType("UnityEditor.ClipboardContextMenu");
        
        private static readonly MethodInfo COPY_WORLD = Find("CopyTransformWorldPlacementMenu");
        private static readonly MethodInfo PASTE_WORLD = Find("PasteTransformWorldPlacementMenu");
        private static readonly MethodInfo CAN_WORLD = Find("PasteTransformWorldPlacementMenuValidate");
        
        /// <summary>
        /// Whether a part can be copied and pasted at all. Only ever false for the world placement, and only on a Unity that has moved it.
        /// </summary>
        /// <param name="part">The piece of the Transform being offered.</param>
        /// <returns>True when the entries for that part are worth putting in a menu.</returns>
        public static bool Supports(TransformPart part) => part != TransformPart.WORLD || (COPY_WORLD != null && PASTE_WORLD != null);
        
        /// <summary>
        /// Puts one piece of a Transform on the clipboard, replacing whatever values were there.
        /// </summary>
        /// <param name="part">Which piece to copy.</param>
        /// <param name="transform">The Transform to read it from.</param>
        public static void Copy(TransformPart part, Transform transform)
        {
            if (!transform) return;
            
            if (part == TransformPart.WORLD)
            {
                Run(COPY_WORLD, transform);
                CopyOrder.ValuesCopied();
                
                return;
            }
            
            EditorGUIUtility.systemCopyBuffer = part switch
            {
                TransformPart.POSITION => Text(transform.localPosition),
                TransformPart.ROTATION => Text(TransformUtils.GetInspectorRotation(transform)),
                _ => Text(transform.localScale)
            };
            
            CopyOrder.ValuesCopied();
        }
        
        /// <summary>
        /// Reads one piece back off the clipboard and onto a Transform. Does nothing when the clipboard is not holding that kind of thing.
        /// </summary>
        /// <param name="part">Which piece to paste.</param>
        /// <param name="transform">The Transform to write it to.</param>
        public static void Paste(TransformPart part, Transform transform)
        {
            if (!transform) return;
            
            if (part == TransformPart.WORLD)
            {
                Run(PASTE_WORLD, transform);
                
                return;
            }
            
            string text = EditorGUIUtility.systemCopyBuffer;
            
            if (part == TransformPart.ROTATION)
            {
                if (!TryRotation(text, out Vector3 euler)) return;
                
                Undo.RecordObject(transform, "Paste Rotation");
                TransformUtils.SetInspectorRotation(transform, euler);
                
                return;
            }
            
            if (!TryVector3(text, out Vector3 value)) return;
            
            if (part == TransformPart.POSITION)
            {
                Undo.RecordObject(transform, "Paste Position");
                transform.localPosition = value;
                
                return;
            }
            
            Undo.RecordObject(transform, "Paste Scale");
            transform.localScale = value;
        }
        
        /// <summary>
        /// Whether the clipboard is holding something that piece could actually take, which is what greys the paste entries out.
        /// </summary>
        /// <param name="part">Which piece is being offered.</param>
        /// <param name="transform">The Transform it would be pasted onto.</param>
        /// <returns>True when a paste would do something.</returns>
        public static bool CanPaste(TransformPart part, Transform transform)
        {
            if (!transform) return false;
            
            if (part == TransformPart.WORLD) return Validate(transform);
            
            string text = EditorGUIUtility.systemCopyBuffer;
            
            return part == TransformPart.ROTATION ? TryRotation(text, out _) : TryVector3(text, out _);
        }
        
        /// <summary>
        /// What that piece is called in a menu.
        /// </summary>
        /// <param name="part">The piece being named.</param>
        /// <returns>The label, without the leading Copy or Paste.</returns>
        public static string NameOf(TransformPart part) => part switch
        {
            TransformPart.WORLD => "World Transform",
            TransformPart.POSITION => "Position",
            TransformPart.ROTATION => "Rotation",
            _ => "Scale"
        };
        
        private static string Text(Vector3 value) => string.Format(CultureInfo.InvariantCulture, VECTOR3_FORMAT, value.x, value.y, value.z);
        
        /*
         * A rotation goes on the clipboard as the three euler angles the Inspector shows, which is what the Transform's own Copy Rotation writes.
         * Those are not the same numbers as 'localEulerAngles': the Inspector keeps its own, so that a rotation typed as 370 stays 370 rather than turning into 10,
         * and 'TransformUtils' is how they are read and written.
         *
         * A quaternion is taken as well, since the editor's clipboard has a slot of its own for one and something else may have put it there.
         * It is only ever read, never written, so nothing here changes what the clipboard looks like to anybody else.
         */
        private static bool TryRotation(string text, out Vector3 euler)
        {
            if (TryVector3(text, out euler)) return true;
            
            if (TryNumbers(text, QUATERNION_NAME, 4))
            {
                euler = new Quaternion(NUMBERS[0], NUMBERS[1], NUMBERS[2], NUMBERS[3]).eulerAngles;
                
                return true;
            }
            
            euler = Vector3.zero;
            
            return false;
        }
        
        private static bool TryVector3(string text, out Vector3 value)
        {
            if (!TryNumbers(text, VECTOR3_NAME, 3))
            {
                value = Vector3.zero;
                
                return false;
            }
            
            value = new Vector3(NUMBERS[0], NUMBERS[1], NUMBERS[2]);
            
            return true;
        }
        
        // Fills the shared buffer rather than handing an array back, since this is asked twice per entry while a menu is being built and none of it outlives the answer.
        private static bool TryNumbers(string text, string name, int count)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (!text.StartsWith($"{name}(", StringComparison.Ordinal) || !text.EndsWith(")", StringComparison.Ordinal)) return false;
            
            string body = text.Substring(name.Length + 1, text.Length - name.Length - 2);
            string[] pieces = body.Split(',');
            
            if (pieces.Length != count) return false;
            
            for (int i = 0; i < count; i++)
                if (!float.TryParse(pieces[i], NumberStyles.Float, CultureInfo.InvariantCulture, out NUMBERS[i])) return false;
            
            return true;
        }
        
        private static bool Validate(Transform transform)
        {
            if (CAN_WORLD == null) return false;
            
            try
            {
                return CAN_WORLD.Invoke(null, new object[] { new MenuCommand(transform) }) is true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        private static void Run(MethodInfo method, Transform transform)
        {
            if (method == null) return;
            
            try
            {
                method.Invoke(null, new object[] { new MenuCommand(transform) });
            }
            catch (Exception)
            {
                // Nothing useful to say and nothing to put right.
            }
        }
        
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
