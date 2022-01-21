using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor
{
    public interface IBlockyPiece
    {
        string Name { get; }

        BlockyLayer Layer { get; }
        BlockyObject GetPrefab(BlockyObjectMap map, BlockyObjectKey key);
        GameObject GetPlacement();
        Texture2D GetTexture();
    }
}

