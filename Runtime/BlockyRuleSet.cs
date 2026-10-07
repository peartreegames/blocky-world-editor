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
            var (result, rotation, scale) = Resolve(map, key);
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

        #if UNITY_EDITOR
        private (BlockyObject block, Vector3 rotation, Vector3 scale) Resolve(BlockyObjectMap map, BlockyObjectKey key) =>
            Resolve(BlockyRule.Neighbours.Select(p => IsSame(map, key, p)).ToArray());

        private (BlockyObject block, Vector3 rotation, Vector3 scale) Resolve(bool[] neighbours)
        {
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
            return (result, rotation, scale);
        }
        #endif

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
            #if UNITY_EDITOR
            if (preview == null && defaultBlock != null) preview = RenderThumbnail();
            return preview;
            #else
            return Texture2D.grayTexture;
            #endif
        }

        #if UNITY_EDITOR
        [NonSerialized] private Texture2D preview;

        private const int ShapeSize = 7;

        public Texture2D RenderThumbnail()
        {
            var shape = SearchShape();
            var cells = Enumerable.Range(0, shape.Length).Where(i => shape[i])
                .Select(i => new Vector3Int(i % ShapeSize, 0, i / ShapeSize)).ToArray();
            var render = new UnityEditor.PreviewRenderUtility();
            try
            {
                foreach (var cell in cells)
                {
                    var (block, rotation, scale) = Resolve(ShapeNeighbours(shape, cell.x, cell.z));
                    var go = render.InstantiatePrefabInScene(block.gameObject);
                    go.transform.SetPositionAndRotation(cell, Quaternion.Euler(rotation));
                    go.transform.localScale = scale;
                }

                var min = cells.Aggregate(Vector3Int.Min);
                var max = cells.Aggregate(Vector3Int.Max);
                var span = Mathf.Max(max.x - min.x, max.z - min.z) + 1;
                var cam = render.camera;
                cam.fieldOfView = 30;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100;
                cam.transform.rotation = Quaternion.Euler(40, 45, 0);
                cam.transform.position = (Vector3)(min + max) / 2 + Vector3.down * 0.5f - cam.transform.forward * (2.1f * span);
                render.lights[0].intensity = 1.6f;
                render.lights[0].transform.rotation = Quaternion.Euler(50, 30, 0);
                render.lights[1].intensity = 0.8f;
                render.ambientColor = new Color(0.5f, 0.5f, 0.5f);
                render.BeginStaticPreview(new Rect(0, 0, 200, 200));
                render.Render(true);
                return render.EndStaticPreview();
            }
            finally
            {
                render.Cleanup();
            }
        }

        // hill-climb a patch that shows as many different blocks as it can in one connected piece;
        // fixed seeds keep the thumbnail stable between regenerations
        private bool[] SearchShape()
        {
            var resolved = new Dictionary<int, BlockyObject>();
            bool[] best = null;
            var bestScore = int.MinValue;
            for (var seed = 0; seed < 8; seed++)
            {
                var rnd = new System.Random(seed);
                var shape = new bool[ShapeSize * ShapeSize];
                for (var x = 1; x < ShapeSize - 1; x++)
                for (var z = 1; z < ShapeSize - 1; z++)
                    shape[z * ShapeSize + x] = true;
                var score = ScoreShape(shape, resolved);
                for (var i = 0; i < 3000; i++)
                {
                    var next = (bool[])shape.Clone();
                    next[rnd.Next(next.Length)] ^= true;
                    if (rnd.NextDouble() < 0.3) next[rnd.Next(next.Length)] ^= true;
                    var nextScore = ScoreShape(next, resolved);
                    if (nextScore < score) continue;
                    shape = next;
                    score = nextScore;
                }
                if (score <= bestScore) continue;
                best = shape;
                bestScore = score;
            }
            return best;
        }

        // a new block is worth 100, a second piece costs 150, so the patch only splits for two more blocks
        private int ScoreShape(bool[] shape, Dictionary<int, BlockyObject> resolved)
        {
            var shown = new HashSet<BlockyObject>();
            var cells = 0;
            for (var i = 0; i < shape.Length; i++)
            {
                if (!shape[i]) continue;
                cells++;
                var neighbours = ShapeNeighbours(shape, i % ShapeSize, i / ShapeSize);
                var mask = neighbours.Select((same, bit) => same ? 1 << bit : 0).Sum();
                if (!resolved.TryGetValue(mask, out var block)) resolved[mask] = block = Resolve(neighbours).block;
                shown.Add(block);
            }
            if (cells == 0) return int.MinValue;
            return shown.Count * 100 - (CountPieces(shape) - 1) * 150 - cells;
        }

        private static bool[] ShapeNeighbours(bool[] shape, int x, int z) => BlockyRule.Neighbours.Select(p =>
        {
            int nx = x + p.x, nz = z + p.z;
            return nx >= 0 && nz >= 0 && nx < ShapeSize && nz < ShapeSize && shape[nz * ShapeSize + nx];
        }).ToArray();

        private static int CountPieces(bool[] shape)
        {
            var seen = new bool[shape.Length];
            var pieces = 0;
            for (var start = 0; start < shape.Length; start++)
            {
                if (!shape[start] || seen[start]) continue;
                pieces++;
                var stack = new Stack<int>();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    if (i < 0 || i >= shape.Length || seen[i] || !shape[i]) continue;
                    seen[i] = true;
                    if (i % ShapeSize > 0) stack.Push(i - 1);
                    if (i % ShapeSize < ShapeSize - 1) stack.Push(i + 1);
                    stack.Push(i - ShapeSize);
                    stack.Push(i + ShapeSize);
                }
            }
            return pieces;
        }
        #endif

        private void OnValidate()
        {
            #if UNITY_EDITOR
            preview = null;
            #endif
            foreach (var rule in rules.Where(rule => rule.grid.Length != 9)) rule.grid = new BlockyCellRule[9];
        }
    }
}
