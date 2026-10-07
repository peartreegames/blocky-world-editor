using System;
using System.Collections.Generic;
using System.Linq;
using PeartreeGames.Blocky.World.BlockyMap;
using UnityEngine;

namespace PeartreeGames.Blocky.World
{
    [CreateAssetMenu(fileName = "bRuleSet_", menuName = "Blocky/RuleSet", order = 0)]
    public class BlockyRuleSet : ScriptableObject, IBlockyPiece
    {

        [SerializeField] private BlockyLayer layer;
        public BlockyLayer Layer => layer;

        [SerializeField] private GameObject defaultBlock;
        public List<BlockyRule> rules;
        [SerializeField] private Texture2D thumbnail;
        [SerializeField] private List<GameObject> slopes = new();
        [SerializeField] private Vector3Int slopeAscent = Vector3Int.left;
        public string Name => this == null ? null : name;
        public BlockyObject GetPrefab(BlockyObjectMap map, BlockyObjectKey key)
        {
            #if UNITY_EDITOR
            var neighbours = BlockyRule.Neighbours.Select(p => IsSame(map, key, p)).ToArray();
            BlockyObject result = null;
            var rotation = Vector3.zero;
            var scale = Vector3.one;
            foreach (var rule in rules)
            {
                var ruleNeighbours = neighbours;
                var block = rule.block.GetComponent<BlockyObject>();
                if (rule.Matches(ruleNeighbours))
                {
                    result = block;
                    break;
                }

                switch (rule.transform)
                {
                    case BlockyTransform.Fixed:
                        break;
                    case BlockyTransform.Rotate:
                        for (var i = 1; i < 4; i++)
                        {
                            ruleNeighbours = RotateNeighbours(ruleNeighbours);
                            if (!rule.Matches(ruleNeighbours)) continue;
                            result = block;
                            rotation = new Vector3(0, i * 90, 0);
                        }
                        break;
                    case BlockyTransform.MirrorX:
                        ruleNeighbours = MirrorNeighbours(ruleNeighbours, true, false);
                        if (rule.Matches(ruleNeighbours))
                        {
                            result = block;
                            scale = new Vector3(-1, 1, 1);
                        }
                        break;
                    case BlockyTransform.MirrorY:
                        ruleNeighbours = MirrorNeighbours(ruleNeighbours, false, true);
                        if (rule.Matches(ruleNeighbours))
                        {
                            result = block;
                            scale = new Vector3(1, 1, -1);
                        }
                        break;
                    case BlockyTransform.MirrorXY:
                        if (rule.Matches(MirrorNeighbours(ruleNeighbours, true, false)))
                        {
                            result = block;
                            scale = new Vector3(-1, 1, 1);
                        }
                        if (rule.Matches(MirrorNeighbours(ruleNeighbours, false, true)))
                        {
                            result = block;
                            scale = new Vector3(1, 1, -1);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (result != null) break;
            }

            if (result == null) result = defaultBlock.GetComponent<BlockyObject>();
            var prefab = UnityEditor.PrefabUtility.InstantiatePrefab(result) as BlockyObject;
            if (prefab == null) return null;
            var trans = prefab.transform;
            trans.rotation = Quaternion.Euler(rotation);
            trans.localScale = scale;
            var behaviour = prefab.gameObject.AddComponent<BlockyRuleBehaviour>();
            behaviour.ruleSet = this;
            return prefab;
            #else
            return null;
            #endif
        }

        public GameObject GetPlacement() => defaultBlock;

        #if UNITY_EDITOR
        public bool IsSlope(BlockyObject block) =>
            slopes.Contains(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(block.gameObject));

        private bool IsSame(BlockyObjectMap map, BlockyObjectKey key, Vector3Int offset)
        {
            if (offset == Vector3Int.zero) return true;
            if (map.TryGetValue(new BlockyObjectKey(key.Cell + offset, key.Layer), out var block))
            {
                if (block.TryGetComponent<BlockyRuleBehaviour>(out var rule) && rule.ruleSet == this) return true;
                // a slope at this level joins when it climbs up to us
                if (IsSlope(block) && Climbs(block, -offset)) return true;
            }
            // a slope one level up joins when it climbs away from us
            return map.TryGetValue(new BlockyObjectKey(key.Cell + offset + Vector3Int.up, key.Layer), out var above) &&
                   IsSlope(above) && Climbs(above, offset);
        }

        // true when the slope's ascent runs along either axis of dir, so diagonals join both ways
        private bool Climbs(BlockyObject slope, Vector3Int dir)
        {
            var ascent = Vector3Int.RoundToInt(slope.transform.rotation * (Vector3)slopeAscent);
            return ascent.x != 0 ? ascent.x == dir.x : ascent.z == dir.z;
        }
        #endif

        private static bool[] MirrorNeighbours(bool[] neighbours, bool x, bool y)
        {
            var result = new bool[9];
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    if (x && y) result[i * 3 + j] = neighbours[j * 3 + i];
                    else if (x) result[i * 3 + j] = neighbours[i * 3 + (2 - j)];
                    else if (y) result[i * 3 + j] = neighbours[(2 - i) * 3 + j];
                }
            }

            return result;
        }

        private static bool[] RotateNeighbours(bool[] neighbours)
        {
            var result = new bool[9];
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    result[i * 3 + j] = neighbours[j * 3 + 2 - i];
                }
            }

            return result;
        }

        public Texture2D GetTexture()
        {
            if (thumbnail != null) return thumbnail;
            var blocks = rules.Select(r => r.block == null ? null : r.block.GetComponent<BlockyObject>()).ToArray();
            if (blocks.Length == 0) return Texture2D.grayTexture;
            var textures = new Texture2D[blocks.Length];
            for (var i = 0; i < blocks.Length; i++)
            {
                textures[i] = blocks[i] == null ? null : blocks[i].GetTexture();
            }
            if (textures[0] == null) return Texture2D.grayTexture;
            var w = textures[0].width;
            var h = textures[0].height;
            var rows = Mathf.Min(blocks.Length, 2);
            var cols = Mathf.CeilToInt(blocks.Length / 2f);

            var wPerCol = w / cols;
            var hPerRow = h / rows;
            var result = new Texture2D(w, h);
            var pixels = result.GetPixels(0, 0, w, h);
            for (var i = 0; i < pixels.Length; i++)
            {
                var x = i % w;
                var y = i / w;
                var border = x != 0 && x % wPerCol == 0 || y != 0 && y % hPerRow == 0;
                if (border)
                {
                    pixels[i] = Color.black;
                    continue;
                }

                var quadX = x / wPerCol;
                var quadY = y / hPerRow;
                var index = quadX + cols * quadY;
                if (index >= textures.Length) continue;
                var item = textures[index];
                pixels[i] = item != null ? item.GetPixel(x, y) : Color.grey;
            }

            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private void OnValidate()
        {
            foreach (var rule in rules.Where(rule => rule.grid.Length != 9)) rule.grid = new BlockyCellRule[9];
        }
    }
}

























