using System;
using UnityEngine;

namespace PeartreeGames.Blocky.WorldEditor
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

        public bool Matches(BlockyRuleSet set, BlockyObject[] neighbours)
        {
            for (var i = 0; i < neighbours.Length; i++)
            {
                var neighbour = neighbours[i];
                var cell = grid[i];
                var match = cell switch
                {
                    BlockyCellRule.None => true,
                    BlockyCellRule.Diff =>!NeighbourMatches(set, neighbour),
                    BlockyCellRule.Same => NeighbourMatches(set, neighbour),
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (!match) return false;
            }
            
            return true;
        }

        private bool NeighbourMatches(BlockyRuleSet set, BlockyObject neighbour) => neighbour != null &&
            neighbour.TryGetComponent<BlockyRuleBehaviour>(out var rule) && rule.ruleSet == set;
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