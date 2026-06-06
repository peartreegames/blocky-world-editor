using UnityEngine;

namespace PeartreeGames.Blocky.World
{
    public class BlockySprite : BlockyObject
    {
        [System.NonSerialized] private Texture2D _cachedTexture;

        public override Texture2D GetTexture()
        {
            if (_cachedTexture != null) return _cachedTexture;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return null;
            var sprite = sr.sprite;
            if (sprite == null) return null;
            var w = Mathf.RoundToInt(sprite.rect.width);
            var h = Mathf.RoundToInt(sprite.rect.height);
            var tex = new Texture2D(w, h);
            var startX = Mathf.RoundToInt(sprite.rect.x);
            var startY = Mathf.RoundToInt(sprite.rect.y);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, sprite.texture.GetPixel(startX + x, startY + y));
                }
            }

            tex.Apply();
            _cachedTexture = tex;
            return tex;
        }
    }
}