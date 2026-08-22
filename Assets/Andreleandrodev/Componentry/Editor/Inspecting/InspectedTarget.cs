using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Componentry.Inspecting
{
    /// <summary>
    /// The GameObject one Inspector is showing, and where a row of our own belongs in it.
    /// </summary>
    public readonly struct InspectedTarget
    {
        
        public static readonly InspectedTarget NONE = new InspectedTarget(null, InspectedKind.NONE);
        
        private readonly GameObject _gameObject;
        private readonly InspectedKind _kind;

        public GameObject GameObject => _gameObject;
        public bool IsValid => _gameObject && _kind != InspectedKind.NONE;
        
        /*
         * The list holds one child per editor the Inspector is drawing, in the order it draws them, so index 0 is the GameObject header with the name,
         * tag and layer, and index 1 is the Transform. Inserting at 1 puts the bar between them.
         * A prefab opened from the Project window carries an extra header above all of that, which pushes everything down by one.
         */
        /// <summary>
        /// Where the bar goes among the children of the editors list.
        /// </summary>
        public int BarIndex => _kind == InspectedKind.PREFAB_ASSET ? 2 : 1;
        
        private InspectedTarget(GameObject gameObject, InspectedKind kind)
        {
            _gameObject = gameObject;
            _kind = kind;
        }
        
        public static InspectedTarget Of(Object inspected)
        {
            if (inspected is not GameObject gameObject) return NONE;
            
            if (!AssetDatabase.Contains(gameObject)) return new InspectedTarget(gameObject, InspectedKind.SCENE_OBJECT);
            
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
            bool isPrefab = prefabType is PrefabAssetType.Regular or PrefabAssetType.Variant;
            
            return isPrefab ? new InspectedTarget(gameObject, InspectedKind.PREFAB_ASSET) : NONE;
        }
        
        public bool IsSameAs(InspectedTarget other) => _gameObject == other._gameObject && _kind == other._kind;
        
    }
}
