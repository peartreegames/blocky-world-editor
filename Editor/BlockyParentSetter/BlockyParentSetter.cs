using UnityEngine;

namespace PeartreeGames.Blocky.World.Editor.BlockyParentSetter
{
    public abstract class BlockyParentSetter : ScriptableObject
    {
        public abstract Transform GetParent(BlockyObject block);
        public virtual bool CanPlace() => true;
        public virtual void SetVisualization(Vector3Int target, int gridHeight) {}
    }
}