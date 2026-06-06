using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PeartreeGames.Blocky.World.BlockyMap
{
    public class BlockyObjectHeightMap : Dictionary<BlockyObjectHeightKey, int>
    {
        public void Add(BlockyObject obj, bool useUndo = false)
        {
#if UNITY_EDITOR
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() !=
                null) return;
#endif
            var key = new BlockyObjectHeightKey(obj);
            var value = Mathf.RoundToInt(obj.transform.position.y);
            Add(key, value);
        }

        public new void Add(BlockyObjectHeightKey key, int value)
        {
            if (base.TryGetValue(key, out var prev)) this[key] = Mathf.Max(value, prev);
            else this[key] = value;
        }

        public void Remove(BlockyObject obj) => base.Remove(new BlockyObjectHeightKey(obj));
        public bool Contains(BlockyObject obj) => ContainsKey(new BlockyObjectHeightKey(obj));

        public bool TryGetValue(BlockyObject blocky, out int obj) =>
            TryGetValue(new BlockyObjectHeightKey(blocky), out obj);

        public new bool TryGetValue(BlockyObjectHeightKey key, out int obj) =>
            base.TryGetValue(key, out obj);
    }
}