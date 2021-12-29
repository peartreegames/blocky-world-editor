using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor
{
    public interface IBlockyPiece
    {
        string Name { get; }

        BlockyLayer Layer { get; }
        BlockyObject GetPrefab(BlockyObjectMap map, BlockyObjectKey key);
        Texture2D GetTexture();
    }
}

