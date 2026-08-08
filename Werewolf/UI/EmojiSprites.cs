using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal static class EmojiSprites
    {
        private const int CellPadding = 4;

        private const string TagPrefix = "<sprite name=";
        private const string TagSuffix = ">";

        private static TMP_SpriteAsset _asset;
        private static bool _attempted;

        public static TMP_SpriteAsset Asset => Build();

        public static bool Ready => Build() != null;

        public static string Substitute(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (Build() == null) return text;

            StringBuilder sb = null;
            int copied = 0;
            for (int i = 0; i < text.Length; i++)
            {
                int size = 1;
                int codePoint = text[i];
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    size = 2;
                }
                if (!ChatEmoji.SpriteKeys.TryGetValue(codePoint, out string key)) continue;

                if (sb == null) sb = new StringBuilder(text.Length + 32);
                sb.Append(text, copied, i - copied);
                sb.Append(TagPrefix).Append(key).Append(TagSuffix);

                i += size - 1;
                if (i + 1 < text.Length && text[i + 1] == ChatEmoji.VariationSelector16) i++;
                copied = i + 1;
            }

            if (sb == null) return text;
            sb.Append(text, copied, text.Length - copied);
            return sb.ToString();
        }

        internal static void ResetForTests()
        {
            _asset = null;
            _attempted = false;
        }

        private static TMP_SpriteAsset Build()
        {
            if (_attempted) return _asset;
            _attempted = true;
            try
            {
                _asset = Compose();
            }
            catch (Exception e)
            {
                WLog.Line("emoji_sprite_fallback", secret: false,
                    ("reason", "exception"), ("err", e.GetType().Name + ":" + e.Message));
                _asset = null;
            }
            return _asset;
        }

        private static TMP_SpriteAsset Compose()
        {
            var keys = new List<string>();
            foreach (string key in ChatEmoji.SpriteKeys.Values)
            {
                if (!keys.Contains(key)) keys.Add(key);
            }
            keys.Sort(StringComparer.Ordinal);
            if (keys.Count == 0) return null;

            var textures = new List<Texture2D>(keys.Count);
            int cellInner = 1;
            foreach (string key in keys)
            {
                Texture2D tex = AssetCatalog.GetTexture(key);
                if (tex == null)
                {
                    WLog.Line("emoji_sprite_fallback", secret: false, ("reason", "png_missing"), ("key", key));
                    return null;
                }
                textures.Add(tex);
                cellInner = Mathf.Max(cellInner, Mathf.Max(tex.width, tex.height));
            }

            int cell = cellInner + CellPadding * 2;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(keys.Count));
            int rows = Mathf.CeilToInt(keys.Count / (float)columns);

            var atlas = new Texture2D(columns * cell, rows * cell, TextureFormat.RGBA32, false)
            {
                name = "WW_EmojiAtlas",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            atlas.SetPixels32(new Color32[atlas.width * atlas.height]);

            var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = "WW_EmojiSprites";
            asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            asset.spriteSheet = atlas;
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);

            for (int i = 0; i < keys.Count; i++)
            {
                Texture2D tex = textures[i];
                int x = (i % columns) * cell + CellPadding + (cellInner - tex.width) / 2;
                int y = (i / columns) * cell + CellPadding + (cellInner - tex.height) / 2;
                atlas.SetPixels32(x, y, tex.width, tex.height, tex.GetPixels32());

                var glyph = new TMP_SpriteGlyph((uint)i,
                    new GlyphMetrics(tex.width, tex.height, 0f, tex.height, tex.width),
                    new GlyphRect(x, y, tex.width, tex.height), 1f, 0);
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFEu, asset, glyph) { name = keys[i] });
            }
            atlas.Apply(false, false);

            asset.UpdateLookupTables();

            Shader shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null)
            {
                WLog.Line("emoji_sprite_fallback", secret: false, ("reason", "shader_missing"));
                return null;
            }
            var material = new Material(shader)
            {
                name = "WW_EmojiSpriteMaterial",
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            material.SetTexture("_MainTex", atlas);
            asset.material = material;
            asset.materialHashCode = TMP_TextUtilities.GetSimpleHashCode(material.name);

            WLog.Line("emoji_sprite_ready", secret: false,
                ("count", keys.Count), ("atlas", atlas.width + "x" + atlas.height));
            return asset;
        }
    }
}
