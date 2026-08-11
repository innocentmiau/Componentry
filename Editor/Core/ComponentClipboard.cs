using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Componentry.Core
{
    /*
     * What is held is a copy taken at the moment of copying, not the components that were copied.
     * That is the difference between this and remembering which components they were, and it is the whole behaviour of a clipboard:
     * copy a collider, change it, paste, and what lands is the collider as it was when it was copied.
     * Holding on to the components themselves would paste whatever they had become by then, which is not copying at all.
     *
     * The copy is made by handing each component to Unity's own copy and paste, onto a GameObject nobody can see.
     * So the copy is a real component of the same type, made the same way the Inspector makes one, and every kind of field that survives Copy Component survives this.
     * Pasting then copies from that one instead of from the original.
     *
     * The hidden object lives in a preview scene, which is Unity's own idea of a scene that is not part of the project:
     * nothing in it is saved, nothing is drawn, and none of it makes the scene you are working in dirty.
     */
    /// <summary>
    /// The components picked up to be put on something else.
    /// </summary>
    public static class ComponentClipboard
    {
        
        private const string HOLDER_NAME = "Componentry Clipboard";
        
        private static readonly List<Component> COPIED = new List<Component>();
        
        private static Scene _scene;
        private static GameObject _holder;
        
        public static int Count
        {
            get
            {
                Prune();
                
                return COPIED.Count;
            }
        }
        
        /// <summary>
        /// Takes a copy of each component as it stands right now, replacing whatever was held before.
        /// Also leaves the last of them on the editor's own clipboard, so Unity's Paste Component entries work from any header afterwards.
        /// </summary>
        /// <param name="components">The components to copy. Transforms are skipped, and anything that cannot be copied is left out rather than counted.</param>
        public static void Copy(List<Component> components)
        {
            Clear();
            
            foreach (Component component in components)
            {
                // The Transform is never pasted onto anything, since everything already has one, so carrying it would only ever be a count that lied.
                if (!component || component is Transform) continue;
                
                Component frozen = Freeze(component);
                
                if (frozen) COPIED.Add(frozen);
            }
            
            /*
             * Copying is exactly what the Inspector's own Copy Component means, and the last thing Freeze did was that call,
             * so the editor's clipboard is already holding the last of them.
             * Its Paste Component Values and Paste Component As New work from any header afterwards,
             * with the same values, taken at the same moment. That last one is also what is remembered as the kind of thing on the clipboard.
             */
            CopyOrder.ComponentCopied(COPIED.Count > 0 ? COPIED[COPIED.Count - 1].GetType() : null);
        }
        
        /// <summary>
        /// What is currently held, with anything that has since been destroyed dropped first.
        /// </summary>
        /// <param name="into">List filled with the held copies, cleared first.</param>
        public static void Fill(List<Component> into)
        {
            Prune();
            
            into.Clear();
            into.AddRange(COPIED);
        }
        
        /// <summary>
        /// Drops everything held and tears down the hidden object and the preview scene behind it.
        /// The editor's own clipboard is left alone, since that is shared with the rest of Unity and is not ours to empty.
        /// </summary>
        public static void Clear()
        {
            COPIED.Clear();
            
            if (_holder) Object.DestroyImmediate(_holder);
            
            _holder = null;
            
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            
            _scene = default;
        }
        
        /// <summary>
        /// One component copied onto the hidden object, by the same two calls anybody would use to copy it anywhere else, and handed back as the copy that was made.
        /// </summary>
        private static Component Freeze(Component source)
        {
            EnsureHolder();
            
            if (!_holder) return null;
            
            Component[] before = _holder.GetComponents<Component>();
            
            if (!ComponentUtility.CopyComponent(source)) return null;
            if (!ComponentUtility.PasteComponentAsNew(_holder)) return null;
            
            return Added(before, _holder.GetComponents<Component>(), source.GetType());
        }
        
        /// <summary>
        /// Which component the paste actually added. Not simply the last one: a component that says it requires others brings them along,
        /// so several may have appeared and only one of them is the one that was asked for.
        /// </summary>
        /// <param name="before">Component array before</param>
        /// <param name="after">Component array after</param>
        /// <param name="type">Candidate Type</param>
        /// <returns></returns>
        private static Component Added(Component[] before, Component[] after, Type type)
        {
            Component fallback = null;
            
            foreach (Component candidate in after)
            {
                if (Array.IndexOf(before, candidate) >= 0) continue;
                
                if (candidate.GetType() == type) return candidate;
                
                fallback = fallback ? fallback : candidate;
            }
            
            return fallback;
        }
        
        /*
         * Hidden and never saved, but deliberately not 'HideAndDontSave'.
         *
         * That flag, and 'DontSave' with it, quietly includes 'NotEditable',
         * and a component cannot be pasted onto something Unity has been told is not editable.
         * The paste simply answers no, nothing is copied, and the only sign of it is a clipboard that stays empty.
         * So the flags are spelled out one at a time, and the one that would have made the whole thing a no-op is left off.
         */
        private const HideFlags HOLDER_FLAGS = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        
        private static void EnsureHolder()
        {
            if (_holder) return;
            
            if (!_scene.IsValid()) _scene = EditorSceneManager.NewPreviewScene();
            
            _holder = new GameObject(HOLDER_NAME) { hideFlags = HOLDER_FLAGS };
            
            SceneManager.MoveGameObjectToScene(_holder, _scene);
        }
        
        /// <summary>
        /// A script recompiling or play mode starting takes the hidden object with it, since nothing in a preview scene is meant to outlive the moment.
        /// What is left behind is a list of components that are gone, and this is what notices.
        /// </summary>
        private static void Prune()
        {
            for (int i = COPIED.Count - 1; i >= 0; i--)
                if (!COPIED[i]) COPIED.RemoveAt(i);
        }
        
    }
}
