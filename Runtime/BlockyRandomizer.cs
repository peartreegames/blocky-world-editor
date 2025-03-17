using System.Collections.Generic;
using PeartreeGames.Blocky.WorldEditor.BlockyMap;
using UnityEngine;

namespace PeartreeGames.Blocky.WorldEditor
{
    [CreateAssetMenu(fileName = "bRandom_", menuName = "Blocky/Randomizer", order = 0)]
    public class BlockyRandomizer : ScriptableObject, IBlockyPiece
    {
        [SerializeField] private BlockyLayer layer;
        public string Name => name;
        public BlockyLayer Layer => layer;

        [SerializeField] private List<Object> blocks;
        
        public List<IBlockyPiece> Blocks
        {
            get
            {
                var result = new List<IBlockyPiece>();
                foreach (var block in blocks)
                {
                    var iBlock = block switch
                    {
                        GameObject go => go.GetComponent<IBlockyPiece>(),
                        ScriptableObject so => so as IBlockyPiece,
                        _ => null
                    };
                    if (iBlock != null) result.Add(iBlock);
                }
                return result;
            }
        }

        public BlockyObject GetPrefab(BlockyObjectMap map, BlockyObjectKey key)
        {
            var pieces = Blocks;
            return pieces.Count > 0 ? pieces[Random.Range(0, pieces.Count)].GetPrefab(map, key) : null;
        }

        public GameObject GetPlacement()
        {
            var pieces = Blocks;
            return pieces.Count > 0 ? pieces[Random.Range(0, pieces.Count)].GetPlacement() : null;
        }


        public Texture2D GetTexture()
        {

            var pieces = Blocks.ToArray();
            if (pieces.Length == 0) return Texture2D.grayTexture;
            var textures = new Texture2D[pieces.Length];
            for (var i = 0; i < pieces.Length; i++)
            {
                var texture = pieces[i].GetTexture();
                textures[i] = texture;
            }
            if (textures[0] == null) return Texture2D.grayTexture;
            var w = textures[0].width;
            var h = textures[0].height;
            var rows = Mathf.Min(pieces.Length, 2);
            var cols = Mathf.CeilToInt(pieces.Length / 2f);

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
    }
}