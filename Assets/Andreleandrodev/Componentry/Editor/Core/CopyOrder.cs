using System;
using UnityEditor;

namespace Componentry.Core
{
    /*
     * The editor keeps components and values in different places. Copying a Mesh Collider does not touch whatever position was copied an hour ago,
     * and copying a position does not touch the component, so both can be full at once and neither knows about the other.
     * Left alone, that means right-clicking a Transform offers to paste a position somebody copied before lunch, long after they stopped thinking about it.
     *
     * Nothing here clears either clipboard. Emptying the values one means writing over what the machine has on its clipboard,
     * which is very often a piece of text somebody wanted and would not expect a Unity package to take away.
     * What is remembered instead is what the clipboard held at the moment a component was copied:
     * if it still holds exactly that, then nothing has been copied since, and the values in it are the older of the two.
     */
    /// <summary>
    /// Which of the two clipboards was written to last.
    /// </summary>
    public static class CopyOrder
    {
        
        private static string _clipboardWhenComponentCopied;
        private static Type _componentCopied;
        
        /*
         * The editor's own component clipboard cannot be read, so this is the only way to know, and it only knows about copies made from the bar.
         * A copy made from a component's own header leaves this saying whatever it said before,
         * which is why what it says is used to grey an entry and never to hide one.
         */
        /// <summary>
        /// What kind of component was last copied through this package, or null when nothing has been,
        /// which is not the same thing: null means unknown, and unknown is not a reason to refuse anything.
        /// </summary>
        public static Type ComponentCopiedType => _componentCopied;
        
        /// <summary>
        /// True when the values on the clipboard were already there before the last component was copied, which is what makes offering to paste them misleading.
        /// </summary>
        public static bool ValuesAreStale
        {
            get
            {
                if (_clipboardWhenComponentCopied == null) return false;
                
                return EditorGUIUtility.systemCopyBuffer == _clipboardWhenComponentCopied;
            }
        }
        
        /// <summary>
        /// Marks that a component was just copied through this package, remembering what the values clipboard held at that moment.
        /// </summary>
        /// <param name="type">The kind of component copied, used later to grey out paste entries that could not work.</param>
        public static void ComponentCopied(Type type)
        {
            _clipboardWhenComponentCopied = EditorGUIUtility.systemCopyBuffer;
            _componentCopied = type;
        }
        
        /// <summary>
        /// Whether a component of that exact kind can be pasted, as far as this can tell.
        /// </summary>
        /// <param name="type">Type for component to check.</param>
        /// <returns>True when nothing is known, since an entry that might work is worth offering and an entry that certainly will not is not.</returns>
        public static bool CouldPasteValuesOnto(Type type) => _componentCopied == null || _componentCopied == type;
        
        /// <summary>
        /// Anything written to the values clipboard is newer than the last component copy by definition, so the mark is dropped rather than kept and compared against.
        /// </summary>
        public static void ValuesCopied() => _clipboardWhenComponentCopied = null;
        
    }
}
