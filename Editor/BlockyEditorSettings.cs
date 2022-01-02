using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public enum BlockyEditMode
    {
        None,
        Paint,
        Select
    }
    public class BlockyEditorSettings : ScriptableObject
    {
        public Vector3Int target;
        public BlockyEditMode editMode;
        public int gridHeight;
        public Vector3 rotation;
        [Range(0, 3)]
        public int brushSize;

        public BlockyPalette palette;
        public IBlockyPiece Selected;
        public BlockyParentSetter parentSetter;
    }
}