using System;
using PeartreeGames.Blocky.World.BlockyMap;
using UnityEngine;

namespace PeartreeGames.Blocky.World
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

        public virtual Texture2D GetTexture()
        {
#if UNITY_EDITOR
            // Getting Failed to restore override lighting settings 
            // since upgrading to 2021.2.8f when compiling when using RenderStaticPreview
            var path = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
            var editor = UnityEditor.Editor.CreateEditor(gameObject);
            Texture2D texture = null;
            try
            {
                texture = editor.RenderStaticPreview(path, null, 200, 200);
            }
            catch (Exception)
            {
                // ignored
            }
            

            DestroyImmediate(editor);
            return texture;
#else
            return null;
#endif
        }

        private void OnDrawGizmosSelected()
        {
            var col = Color.blue;
            col.a = 0.5f;
            Gizmos.color = col;
            Gizmos.DrawWireCube(transform.position - Vector3.up * 0.5f, Vector3.one);
        }
    }
}