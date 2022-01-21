using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor
{
    public class BlockyObject : MonoBehaviour, IBlockyPiece
    {
        [SerializeField] private BlockyLayer layer;
        public BlockyLayer Layer => layer;
        public string Name => name;

        public bool allowRandomRotation;

        public BlockyObject GetPrefab(BlockyObjectMap map, BlockyObjectKey key)
        {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.InstantiatePrefab(this) as BlockyObject;
#else
            return null;
#endif
        }

        public GameObject GetPlacement() => gameObject;

        public Texture2D GetTexture()
        {
#if UNITY_EDITOR
            var editor = UnityEditor.Editor.CreateEditor(gameObject);
            var texture =
                editor.RenderStaticPreview(UnityEditor.AssetDatabase.GetAssetPath(gameObject), null, 200, 200);
            DestroyImmediate(editor);
            if (texture == null) texture = Texture2D.grayTexture;
            return texture;
#else
            return null;
#endif
        }
    }
}