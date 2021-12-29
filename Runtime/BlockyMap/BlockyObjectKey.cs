using System;
using UnityEngine;

namespace PeartreeGames.BlockyWorldEditor
{
    public struct BlockyObjectKey : IEquatable<BlockyObjectKey>
    {
        public Vector3Int Cell => _cell;
        public BlockyLayer Layer { get; }

        private Vector3Int _cell;

        public BlockyObjectKey(BlockyObject obj)
        {
            _cell = Vector3Int.RoundToInt(obj.transform.position);
            Layer = obj.Layer;
        }

        public BlockyObjectKey(Vector3Int pos, BlockyLayer layer)
        {
            _cell = pos;
            Layer = layer;
        }

        public bool Equals(BlockyObjectKey other) => _cell.Equals(other._cell) && Equals(Layer, other.Layer);
        public override bool Equals(object obj) => obj is BlockyObjectKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_cell, Layer);
    }
}

