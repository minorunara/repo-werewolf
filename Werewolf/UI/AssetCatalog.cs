using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    public static class AssetCatalog
    {
        private const string EmbeddedNamespace = "Werewolf.Assets";
        private const string BundleFileName = "werewolf_assets";

        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> _graySprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        private static AssetBundle _bundle;
        private static bool _bundleAttempted;

        public static Sprite GetSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_sprites.TryGetValue(key, out var cached)) return cached;

            var tex = GetTexture(key);
            Sprite sp = null;
            if (tex != null)
            {
                try
                {
                    sp = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
                }
                catch (Exception e)
                {
                    LogFallback(key, "sprite_create_failed", e.GetType().Name);
                    sp = null;
                }
            }
            _sprites[key] = sp;
            return sp;
        }

        public static Sprite GetGraySprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_graySprites.TryGetValue(key, out var cached)) return cached;

            Sprite gray = null;
            var src = GetTexture(key);
            if (src != null)
            {
                try
                {
                    var pixels = src.GetPixels();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        var c = pixels[i];
                        float y = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                        pixels[i] = new Color(y, y, y, c.a);
                    }
                    var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                    tex.SetPixels(pixels);
                    tex.Apply(false, false);
                    gray = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                catch (Exception e)
                {
                    LogFallback(key, "gray_build_failed", e.GetType().Name);
                    gray = null;
                }
            }
            _graySprites[key] = gray;
            return gray;
        }

        public static Texture2D GetTexture(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_textures.TryGetValue(key, out var cached)) return cached;

            Texture2D tex = null;
            try
            {
                var bytes = LoadEmbeddedBytes(key + ".png");
                if (bytes != null && bytes.Length > 0)
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!tex.LoadImage(bytes))
                    {
                        LogFallback(key, "png_decode_failed");
                        UnityEngine.Object.Destroy(tex);
                        tex = null;
                    }
                }
                else
                {
                    LogFallback(key, "png_missing");
                }
            }
            catch (Exception e)
            {
                LogFallback(key, "png_exception", e.GetType().Name);
                tex = null;
            }
            _textures[key] = tex;
            return tex;
        }

        public static GameObject GetPrefab(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_prefabs.TryGetValue(key, out var cached)) return cached;

            GameObject prefab = null;
            var bundle = TryLoadBundle();
            if (bundle != null)
            {
                try
                {
                    prefab = bundle.LoadAsset<GameObject>(key);
                    if (prefab == null)
                    {
                        LogFallback(key, "bundle_key_missing");
                    }
                }
                catch (Exception e)
                {
                    LogFallback(key, "bundle_load_exception", e.GetType().Name);
                    prefab = null;
                }
            }
            else
            {
                LogFallback(key, "bundle_missing");
            }
            _prefabs[key] = prefab;
            return prefab;
        }

        public static AudioClip GetClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_clips.TryGetValue(key, out var cached)) return cached;

            AudioClip clip = null;
            try
            {
                var bytes = LoadEmbeddedBytes(key + ".wav");
                if (bytes != null && bytes.Length > 0)
                {
                    clip = DecodePcm16Wav(key, bytes);
                }
                else
                {
                    LogFallback(key, "wav_missing");
                }
            }
            catch (Exception e)
            {
                LogFallback(key, "wav_exception", e.GetType().Name);
                clip = null;
            }
            _clips[key] = clip;
            return clip;
        }

        public static Sprite GetBombIcon()
        {
            var s = GetSprite("img_bomb");
            if (s != null) return s;
            return BombIconPlaceholder();
        }

        public static AudioClip GetHeartbeatClip()
        {
            var c = GetClip("sfx_heartbeat");
            if (c != null) return c;
            return HeartbeatPlaceholder();
        }

        private static Sprite _bombIconPlaceholder;

        private static Sprite BombIconPlaceholder()
        {
            if (_bombIconPlaceholder != null) return _bombIconPlaceholder;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            var body = new Vector2(size * 0.5f, size * 0.45f);
            float bodyR = size * 0.32f;
            var spark = new Vector2(size * 0.72f, size * 0.85f);
            float sparkR = size * 0.10f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = new Color(0f, 0f, 0f, 0f);
                    float db = Vector2.Distance(new Vector2(x, y), body);
                    if (db <= bodyR)
                    {
                        c = new Color(0.08f, 0.08f, 0.08f, 1f);
                        if (db > bodyR - 3f) c = new Color(0.02f, 0.02f, 0.02f, 1f);
                    }
                    float fx = (x - size * 0.63f);
                    float fy = (y - size * 0.72f);
                    if (Mathf.Abs(fx - fy) < 2.0f && x >= size * 0.60f && x <= size * 0.72f
                        && y >= size * 0.66f && y <= size * 0.80f)
                    {
                        c = new Color(0.55f, 0.35f, 0.10f, 1f);
                    }
                    float ds = Vector2.Distance(new Vector2(x, y), spark);
                    if (ds <= sparkR)
                    {
                        float t = 1f - ds / sparkR;
                        c = new Color(1f, Mathf.Lerp(0.35f, 0.85f, t), 0.1f, 1f);
                    }
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _bombIconPlaceholder = Sprite.Create(tex,
                new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _bombIconPlaceholder.name = "WW_BombIconPlaceholder";
            return _bombIconPlaceholder;
        }

        private static AudioClip _heartbeatPlaceholder;

        private static AudioClip HeartbeatPlaceholder()
        {
            if (_heartbeatPlaceholder != null) return _heartbeatPlaceholder;

            const int sampleRate = 22050;
            const float durationSec = 1.0f;
            int total = (int)(sampleRate * durationSec);
            var samples = new float[total];
            void AddPulse(float startSec, float amp)
            {
                int start = (int)(startSec * sampleRate);
                int len = sampleRate / 5;
                for (int i = 0; i < len && start + i < total; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Exp(-8f * t);
                    float w = Mathf.Sin(2f * Mathf.PI * 60f * t);
                    samples[start + i] += amp * env * w;
                }
            }
            AddPulse(0.02f, 0.75f);
            AddPulse(0.35f, 0.55f);

            var clip = AudioClip.Create("WW_HeartbeatPlaceholder", total, 1, sampleRate, false);
            if (clip == null) return null;
            if (!clip.SetData(samples, 0)) return null;
            _heartbeatPlaceholder = clip;
            return clip;
        }

        internal static void ResetForTests()
        {
            _sprites.Clear();
            _graySprites.Clear();
            _textures.Clear();
            _prefabs.Clear();
            _clips.Clear();
            _bundle = null;
            _bundleAttempted = false;
            _bombIconPlaceholder = null;
            _heartbeatPlaceholder = null;
        }

        private static byte[] LoadEmbeddedBytes(string fileName)
        {
            var asm = typeof(AssetCatalog).Assembly;
            var resourceName = EmbeddedNamespace + "." + fileName;
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                var buffer = new byte[stream.Length];
                int offset = 0;
                while (offset < buffer.Length)
                {
                    int read = stream.Read(buffer, offset, buffer.Length - offset);
                    if (read <= 0) break;
                    offset += read;
                }
                if (offset != buffer.Length)
                {
                    var trimmed = new byte[offset];
                    Buffer.BlockCopy(buffer, 0, trimmed, 0, offset);
                    return trimmed;
                }
                return buffer;
            }
        }

        private static AssetBundle TryLoadBundle()
        {
            if (_bundle != null) return _bundle;
            if (_bundleAttempted) return null;
            _bundleAttempted = true;
            try
            {
                var dllPath = typeof(AssetCatalog).Assembly.Location;
                if (string.IsNullOrEmpty(dllPath)) return null;
                var dir = Path.GetDirectoryName(dllPath);
                if (string.IsNullOrEmpty(dir)) return null;
                var bundlePath = Path.Combine(dir, BundleFileName);
                if (!File.Exists(bundlePath)) return null;
                _bundle = AssetBundle.LoadFromFile(bundlePath);
                return _bundle;
            }
            catch (Exception e)
            {
                LogFallback(BundleFileName, "bundle_exception", e.GetType().Name);
                return null;
            }
        }

        private static AudioClip DecodePcm16Wav(string key, byte[] bytes)
        {
            if (bytes.Length < 44) { LogFallback(key, "wav_too_short"); return null; }

            if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F' ||
                bytes[8] != (byte)'W' || bytes[9] != (byte)'A' || bytes[10] != (byte)'V' || bytes[11] != (byte)'E')
            {
                LogFallback(key, "wav_not_riff");
                return null;
            }

            int pos = 12;
            short channels = 0;
            int sampleRate = 0;
            short bitsPerSample = 0;
            short audioFormat = 0;
            byte[] pcm = null;

            while (pos + 8 <= bytes.Length)
            {
                string chunkId = new string(new[]
                    { (char)bytes[pos], (char)bytes[pos + 1], (char)bytes[pos + 2], (char)bytes[pos + 3] });
                int chunkSize = BitConverter.ToInt32(bytes, pos + 4);
                int chunkDataStart = pos + 8;
                if (chunkDataStart + chunkSize > bytes.Length)
                {
                    LogFallback(key, "wav_chunk_overflow");
                    return null;
                }
                if (chunkId == "fmt ")
                {
                    if (chunkSize < 16) { LogFallback(key, "wav_fmt_short"); return null; }
                    audioFormat   = BitConverter.ToInt16(bytes, chunkDataStart + 0);
                    channels      = BitConverter.ToInt16(bytes, chunkDataStart + 2);
                    sampleRate    = BitConverter.ToInt32(bytes, chunkDataStart + 4);
                    bitsPerSample = BitConverter.ToInt16(bytes, chunkDataStart + 14);
                }
                else if (chunkId == "data")
                {
                    pcm = new byte[chunkSize];
                    Buffer.BlockCopy(bytes, chunkDataStart, pcm, 0, chunkSize);
                }
                int advance = 8 + chunkSize + (chunkSize & 1);
                pos += advance;
                if (pcm != null && channels > 0 && sampleRate > 0) break;
            }

            if (pcm == null || channels <= 0 || sampleRate <= 0 || bitsPerSample != 16 || audioFormat != 1)
            {
                LogFallback(key, "wav_unsupported",
                    "fmt=" + audioFormat + ",ch=" + channels + ",sr=" + sampleRate + ",bps=" + bitsPerSample);
                return null;
            }

            int frameCount = pcm.Length / (2 * channels);
            if (frameCount <= 0) { LogFallback(key, "wav_no_frames"); return null; }

            var samples = new float[frameCount * channels];
            const float inv = 1f / 32768f;
            for (int i = 0; i < samples.Length; i++)
            {
                short s = (short)(pcm[i * 2] | (pcm[i * 2 + 1] << 8));
                samples[i] = s * inv;
            }

            var clip = AudioClip.Create(key, frameCount, channels, sampleRate, false);
            if (clip == null) { LogFallback(key, "wav_clip_create_failed"); return null; }
            if (!clip.SetData(samples, 0))
            {
                LogFallback(key, "wav_clip_setdata_failed");
                return null;
            }
            return clip;
        }

        private static void LogFallback(string key, string reason, string detail = null)
        {
            try
            {
                if (detail == null)
                {
                    WLog.Line("asset_fallback", secret: false, ("key", key), ("reason", reason));
                }
                else
                {
                    WLog.Line("asset_fallback", secret: false,
                        ("key", key), ("reason", reason), ("detail", detail));
                }
            }
            catch
            {
            }
        }
    }
}
