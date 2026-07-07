// Generates cheap flat-color placeholder icon sprites so the gear grid reads before real art exists.
// Deterministic per name within a run; cached so repeated items share one sprite.
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    public static class PlaceholderIcon
    {
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite For(string key)
        {
            int hue = Mathf.Abs((key ?? string.Empty).GetHashCode()) % 360;
            if (Cache.TryGetValue(hue, out Sprite existing))
            {
                return existing;
            }

            Color color = Color.HSVToRGB(hue / 360f, 0.45f, 0.65f);
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            Cache[hue] = sprite;
            return sprite;
        }
    }
}
