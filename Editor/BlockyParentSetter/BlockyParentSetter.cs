using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public abstract class BlockyParentSetter : ScriptableObject
    {
        public abstract Transform GetParent(BlockyObject block);
        public virtual void SetVisualization(Vector3Int target, int gridHeight) {}
    }
}