using PeartreeGames.Blocky.World.BlockyMap;
using UnityEngine;

namespace PeartreeGames.Blocky.World
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

