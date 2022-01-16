using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public abstract class BlockyParentSetter : ScriptableObject
    {
        public virtual void Init(BlockyEditorWindow window) {}
        public abstract Transform GetParent(BlockyObject block);
        public virtual void SetBoundsVisualization(Vector3Int target, int gridHeight) {}
        

    }
}