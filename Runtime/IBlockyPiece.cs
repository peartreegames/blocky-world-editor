using PeartreeGames.Blocky.WorldEditor.BlockyMap;
using UnityEngine;

namespace PeartreeGames.Blocky.WorldEditor
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

