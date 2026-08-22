using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Componentry.UI
{
    /*
     * Every lookup is cached, because these are asked for while laying out every chip on every repaint and both calls behind them hit the disk on a miss.
     * The cache is keyed by skin as well as by name, so switching between the light and dark themes does not serve back the wrong set.
     * A null answer is cached too: a name that resolved to nothing will resolve to nothing again, and the point is to stop asking.
     *
     * Dark variants are tried first under the pro skin, since that is Unity's own naming convention for its editor icons and for the ones shipped here.
     * Both lookups fall back to the plain name rather than failing, because plenty of icons have no dark variant and are meant to be used as they are.
     */
    /// <summary>
    /// Editor icons by name, from Unity's built-in set or from this package's own Icons folder, cached per skin.
    /// </summary>
    public static class ComponentryIcons
    {
        
        private const string ICONS_FOLDER = "Editor/Icons";
        private const string ANCHOR_FOLDER = "Editor/UI";
        private const string ANCHOR_SUFFIX = "/" + ANCHOR_FOLDER + "/" + nameof(ComponentryIcons) + ".cs";
        
        private static readonly Dictionary<string, Texture> BY_NAME = new Dictionary<string, Texture>();
        private static readonly Dictionary<string, Texture> CUSTOM = new Dictionary<string, Texture>();
        
        private static string _rootPath;
        private static bool _rootResolved;
        
        /// <summary>
        /// An icon shipped inside this package, from its Editor/Icons folder.
        /// </summary>
        /// <param name="name">File name without the extension and without the 'd_' prefix, which is added when the dark skin is on.</param>
        /// <returns>The texture, or null when there is no file of that name.</returns>
        public static Texture Custom(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            bool dark = EditorGUIUtility.isProSkin;
            string key = dark ? $"d:{name}" : $"l:{name}";
            
            if (CUSTOM.TryGetValue(key, out Texture found) && found) return found;
            
            string folder = RootPath;
            if (string.IsNullOrEmpty(folder)) return null;
            
            string file = dark ? $"d_{name}" : name;
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{ICONS_FOLDER}/{file}.png");
            
            icon = icon ? icon : AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{ICONS_FOLDER}/{name}.png");
            CUSTOM[key] = icon;
            return icon;
        }
        
        /*
         * Asked for once and then remembered, including when it comes back as nothing, because the search underneath walks the whole asset database
         * and a folder that was not found this time will not be found next time either.
         *
         * The package manager knows where a package lives, but only while this is installed as one. Dropped into Assets as a plain folder,
         * which is how the Asset Store delivers it, it is not a package at all and there is nothing to ask, so the folder is found the way anything else is found:
         * by looking for a file that is known to be in it. The old hardcoded Packages path answered the first case and quietly broke the second.
         */
        private static string RootPath
        {
            get
            {
                if (_rootResolved) return _rootPath;
                
                _rootResolved = true;
                _rootPath = InstalledAsPackage() ?? FoundByAnchor();
                
                return _rootPath;
            }
        }
        
        private static string InstalledAsPackage()
        {
            string path = PackageInfo.FindForAssembly(typeof(ComponentryIcons).Assembly)?.assetPath;
            return string.IsNullOrEmpty(path) ? null : path;
        }
        
        /*
         * The anchor is this file, since it is the one file guaranteed to be beside the icons whatever the folder above it is called.
         * A project may well hold another ComponentryIcons.cs, so the match is on the whole tail of the path rather than the name alone,
         * and the folder it points at has to actually hold the icons before it is believed.
         */
        private static string FoundByAnchor()
        {
            foreach (string guid in AssetDatabase.FindAssets($"{nameof(ComponentryIcons)} t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(ANCHOR_SUFFIX)) continue;
                
                string root = path.Substring(0, path.Length - ANCHOR_SUFFIX.Length);
                if (AssetDatabase.IsValidFolder($"{root}/{ICONS_FOLDER}")) return root;
            }
            
            return null;
        }
        
        /// <summary>
        /// One of Unity's own built-in editor icons.
        /// </summary>
        /// <param name="name">The built-in icon name, without the 'd_' prefix, which is tried first when the dark skin is on.</param>
        /// <returns>The texture, or null when Unity has no icon of that name.</returns>
        public static Texture Named(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            bool dark = EditorGUIUtility.isProSkin;
            string key = dark ? $"d:{name}" : $"l:{name}";
            
            if (BY_NAME.TryGetValue(key, out Texture found) && found) return found;
            
            Texture icon = dark ? EditorGUIUtility.FindTexture($"d_{name}") : null;
            icon = icon ? icon : EditorGUIUtility.FindTexture(name);
            
            BY_NAME[key] = icon;
            return icon;
        }
        
        /// <summary>
        /// The first of several built-in names that resolves, for icons Unity has renamed between versions.
        /// </summary>
        /// <param name="names">Names to try in order, most preferred first.</param>
        /// <returns>The first texture found, or null when none of the names resolve.</returns>
        public static Texture First(params string[] names)
        {
            foreach (string name in names)
            {
                Texture icon = Named(name);
                if (icon) return icon;
            }
            return null;
        }
        
    }
}
