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
        private const string PACKAGE_FALLBACK = "Packages/com.andreleandrodev.componentry";
        
        private static readonly Dictionary<string, Texture> BY_NAME = new Dictionary<string, Texture>();
        private static readonly Dictionary<string, Texture> CUSTOM = new Dictionary<string, Texture>();
        
        private static string _packagePath;
        
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
            
            string file = dark ? $"d_{name}" : name;
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PackagePath}/{ICONS_FOLDER}/{file}.png");
            
            icon = icon ? icon : AssetDatabase.LoadAssetAtPath<Texture2D>($"{PackagePath}/{ICONS_FOLDER}/{name}.png");
            CUSTOM[key] = icon;
            return icon;
        }
        
        private static string PackagePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_packagePath)) return _packagePath;
                
                _packagePath = PackageInfo.FindForAssembly(typeof(ComponentryIcons).Assembly)?.assetPath;
                _packagePath = string.IsNullOrEmpty(_packagePath) ? PACKAGE_FALLBACK : _packagePath;
                
                return _packagePath;
            }
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
