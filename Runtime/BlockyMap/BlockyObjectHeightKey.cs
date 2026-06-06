using System;
using UnityEngine;

namespace PeartreeGames.Blocky.World.BlockyMap
{
    public struct BlockyObjectHeightKey : IEquatable<BlockyObjectHeightKey>
    {
        public Vector2Int Cell => _cell;
        public BlockyLayer Layer { get; }

        private Vector2Int _cell;

        public BlockyObjectHeightKey(BlockyObject obj)
        {
            _cell = Vector2Int.RoundToInt(new Vector2(obj.transform.position.x,
                obj.transform.position.z));
            Layer = obj.Layer;
        }

        public BlockyObjectHeightKey(Vector2Int pos, BlockyLayer layer)
        {
            _cell = pos;
            Layer = layer;
        }
        
        public BlockyObjectHeightKey(Vector3 pos, BlockyLayer layer)
        {
            _cell = Vector2Int.RoundToInt(new Vector2(pos.x, pos.z));
            Layer = layer;
        }

        public bool Equals(BlockyObjectHeightKey other) => _cell.Equals(other._cell) && Equals(Layer, other.Layer);
        public override bool Equals(object obj) => obj is BlockyObjectHeightKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_cell, Layer);
    }
}

