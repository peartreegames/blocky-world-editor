using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor
{
    public class BlockyObjectMap : Dictionary<BlockyObjectKey, BlockyObject>
    {
        public void Add(BlockyObject obj, bool useUndo = false)
        {
#if UNITY_EDITOR
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) return;
#endif
            var key = new BlockyObjectKey(obj);
            if (base.TryGetValue(key, out var prev))
            {
                if (prev != null)
                {
                    if (useUndo) Undo.DestroyObjectImmediate(prev.gameObject); 
                    else Object.DestroyImmediate(prev.gameObject);   
                }
                this[key] = obj;
            }
            else Add(key, obj);
        }

        public void Remove(BlockyObject obj) => base.Remove(new BlockyObjectKey(obj));
        public bool Contains(BlockyObject obj) => ContainsKey(new BlockyObjectKey(obj));

        public bool TryGetValue(BlockyObject blocky, out BlockyObject obj) =>
            TryGetValue(new BlockyObjectKey(blocky), out obj);

        public new bool TryGetValue(BlockyObjectKey key, out BlockyObject obj)
        {
            if (!base.TryGetValue(key, out obj)) return false;
            if (obj != null) return true;
            Remove(key);
            return false;
        }
    }
}