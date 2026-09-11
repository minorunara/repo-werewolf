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

        private const string VanillaAssetName = "emojis";

        private const int ProbeIntervalFrames = 30;

        private static TMP_SpriteAsset _asset;
        private static bool _attempted;

        private static TMP_SpriteAsset _vanilla;
        private static Dictionary<string, TMP_SpriteCharacter> _vanillaCharacters;
        private static Dictionary<string, Sprite> _vanillaSprites;
        private static int _vanillaProbeFrame = -ProbeIntervalFrames;

        private static readonly ITextPreprocessor _preprocessor = new SubstitutePreprocessor();

        public static TMP_SpriteAsset Asset
        {
            get
            {
                TMP_SpriteAsset own = Build();
                TMP_SpriteAsset vanilla = ResolveVanilla(own);
                return own != null ? own : vanilla;
            }
        }

        public static ITextPreprocessor Preprocessor => _preprocessor;

        public static bool Ready => Build() != null;

        public static bool VanillaReady => ResolveVanilla(Build()) != null;

        public static string Substitute(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            TMP_SpriteAsset own = Build();
            bool vanilla = ResolveVanilla(own) != null;
            if (own == null && !vanilla) return text;

            StringBuilder sb = null;
            int copied = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (vanilla && VanillaEmojiToken.TryReadAt(text, i, out string name, out int next))
                {
                    if (_vanillaCharacters.ContainsKey(name))
                    {
                        if (sb == null) sb = new StringBuilder(text.Length + 32);
                        sb.Append(text, copied, i - copied);
                        sb.Append(TagPrefix).Append(name).Append(TagSuffix);
                        copied = next;
                    }
                    i = next - 1;
                    continue;
                }

                if (own == null) continue;
                if (!ChatEmoji.TryReadSpriteAt(text, i, out string key, out int after)) continue;

                if (sb == null) sb = new StringBuilder(text.Length + 32);
                sb.Append(text, copied, i - copied);
                sb.Append(TagPrefix).Append(key).Append(TagSuffix);
                copied = after;
                i = after - 1;
            }

            if (sb == null) return text;
            sb.Append(text, copied, text.Length - copied);
            return sb.ToString();
        }

        public static Sprite VanillaSprite(string key)
        {
            if (!VanillaEmojiToken.TryParseWhole(key, out string name)) return null;
            if (ResolveVanilla(Build()) == null) return null;

            if (_vanillaSprites == null) _vanillaSprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            if (_vanillaSprites.TryGetValue(name, out Sprite cached)) return cached;

            Sprite cut = CutVanillaSprite(name);
            _vanillaSprites[name] = cut;
            return cut;
        }

        public static bool TryTakeLeadingSprite(string text, out Sprite sprite, out string rest)
        {
            sprite = null;
            rest = null;
            if (!ChatEmoji.TryReadSpriteAt(text, 0, out string key, out int next)) return false;
            sprite = AssetCatalog.GetSprite(key);
            if (sprite == null) return false;

            rest = text.Substring(next).TrimStart(' ');
            return true;
        }

        internal static void ResetForTests()
        {
            _asset = null;
            _attempted = false;
            _vanilla = null;
            _vanillaCharacters = null;
            _vanillaSprites = null;
            _vanillaProbeFrame = -ProbeIntervalFrames;
        }

        private sealed class SubstitutePreprocessor : ITextPreprocessor
        {
            public string PreprocessText(string text) => Substitute(text);
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

        private static TMP_SpriteAsset ResolveVanilla(TMP_SpriteAsset own)
        {
            if (_vanilla != null) return _vanilla;

            int frame = Time.frameCount;
            if (frame - _vanillaProbeFrame < ProbeIntervalFrames) return null;
            _vanillaProbeFrame = frame;

            TMP_SpriteAsset found = null;
            foreach (TMP_SpriteAsset candidate in Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>())
            {
                if (candidate != null && candidate.name == VanillaAssetName)
                {
                    found = candidate;
                    break;
                }
            }
            if (found == null) return null;

            var characters = new Dictionary<string, TMP_SpriteCharacter>(StringComparer.Ordinal);
            foreach (TMP_SpriteCharacter character in found.spriteCharacterTable)
            {
                if (character == null || string.IsNullOrEmpty(character.name)) continue;
                if (!characters.ContainsKey(character.name)) characters.Add(character.name, character);
            }
            if (characters.Count == 0) return null;

            _vanilla = found;
            _vanillaCharacters = characters;
            if (own != null && own != found)
            {
                if (own.fallbackSpriteAssets == null) own.fallbackSpriteAssets = new List<TMP_SpriteAsset>();
                if (!own.fallbackSpriteAssets.Contains(found)) own.fallbackSpriteAssets.Add(found);
            }
            return _vanilla;
        }

        private static Sprite CutVanillaSprite(string name)
        {
            var sheet = _vanilla.spriteSheet as Texture2D;
            if (sheet == null) return null;
            if (!_vanillaCharacters.TryGetValue(name, out TMP_SpriteCharacter character)) return null;
            if (character.glyph == null) return null;

            GlyphRect area = character.glyph.glyphRect;
            if (area.width <= 0 || area.height <= 0) return null;
            return Sprite.Create(sheet, new Rect(area.x, area.y, area.width, area.height),
                                 new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
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

            return asset;
        }
    }
}
