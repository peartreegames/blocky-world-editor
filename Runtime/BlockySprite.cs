using UnityEngine;

namespace PeartreeGames.Blocky.World
{
    public class BlockySprite : BlockyObject
    {
        public override Texture2D GetTexture()
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return null;
            var sprite = sr.sprite;
            if (sprite == null) return null;
            var tex = new Texture2D(Mathf.RoundToInt(sprite.rect.width),
                Mathf.RoundToInt(sprite.rect.height));
            var startX = Mathf.RoundToInt(sprite.rect.x);
            var startY = Mathf.RoundToInt(sprite.rect.y);
            for (var y = 0; y <= sprite.rect.height; y++)
            {
                for (var x = 0; x <= sprite.rect.width; x++)
                {
                    tex.SetPixel(x, y, sprite.texture.GetPixel(startX + x, startY + y));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}