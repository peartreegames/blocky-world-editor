using System;
using UnityEngine;

namespace PeartreeGames.Blocky.World
{
    [Serializable]
    public class BlockyRule
    {
        public static Vector3Int[] Neighbours =
        {
            new(-1, 0, 1),
            new(0,0,1),
            new(1,0,1),
            new(-1,0,0),
            new(0,0,0), // kept for ease of loops
            new(1,0,0),
            new(-1,0,-1),
            new(0,0,-1),
            new(1,0,-1) 
        };

        public BlockyCellRule[] grid;
        public GameObject block;
        public BlockyTransform transform;

        public BlockyRule()
        {
            grid = new BlockyCellRule[9];
        }

        public bool Matches(bool[] same)
        {
            for (var i = 0; i < same.Length; i++)
            {
                var match = grid[i] switch
                {
                    BlockyCellRule.None => true,
                    BlockyCellRule.Diff => !same[i],
                    BlockyCellRule.Same => same[i],
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (!match) return false;
            }

            return true;
        }
    }

    [Serializable]
    public enum BlockyCellRule
    {
        None = 0,
        Diff = 1,
        Same = 2
    }

    [Serializable]
    public enum BlockyTransform
    {
        Fixed,
        Rotate,
        MirrorX,
        MirrorY,
        MirrorXY
    }
}