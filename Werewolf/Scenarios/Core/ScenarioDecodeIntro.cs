using System;
using System.Collections.Generic;
using System.Text;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioDecodeIntro
    {
        public const float EmojiIntervalSec = 0.2f;

        public const float DoneDelaySec = 1.0f;

        public const float LineGapSec = 0.3f;

        public const float HoldSec = 0.3f;

        public const float GlitchSec = 1.0f;

        public const float GlitchRollSec = 0.06f;

        public const float TranslationHoldSec = 1.0f;

        public const float ReturnSec = 0.65f;

        private const float Epsilon = 1e-4f;

        private const string NarrowGlitchChars = "0123456789#$%&*+=?!/|;:~^";
        private const string WideGlitchChars =
            "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
            "０１２３４５６７８９・■□▲△●○◆※";

        public readonly struct State
        {
            public State(int emojiShown, bool line1Visible, bool line1Done,
                         bool line2Visible, bool line2Done, bool glitching,
                         bool translated, bool returning, bool finished)
            {
                EmojiShown = emojiShown;
                Line1Visible = line1Visible;
                Line1Done = line1Done;
                Line2Visible = line2Visible;
                Line2Done = line2Done;
                Glitching = glitching;
                Translated = translated;
                Returning = returning;
                Finished = finished;
            }

            public int EmojiShown { get; }

            public bool Line1Visible { get; }

            public bool Line1Done { get; }

            public bool Line2Visible { get; }

            public bool Line2Done { get; }

            public bool Glitching { get; }

            public bool Translated { get; }

            public bool Returning { get; }

            public bool Finished { get; }
        }

        public static State At(float elapsedSec, int emojiCount)
        {
            float t = elapsedSec + Epsilon;

            int shown = emojiCount <= 0 ? 0 : Math.Min(emojiCount, (int)(t / EmojiIntervalSec));

            float line1At = emojiCount * EmojiIntervalSec;
            float line1DoneAt = line1At + DoneDelaySec;
            float line2At = line1DoneAt + LineGapSec;
            float line2DoneAt = line2At + DoneDelaySec;
            float glitchAt = line2DoneAt + HoldSec;
            float translatedAt = glitchAt + GlitchSec;
            float returnAt = translatedAt + TranslationHoldSec;
            float finishAt = returnAt + ReturnSec;

            bool finished = t >= finishAt;
            return new State(shown,
                line1Visible: t >= line1At,
                line1Done: t >= line1DoneAt,
                line2Visible: t >= line2At,
                line2Done: t >= line2DoneAt,
                glitching: t >= glitchAt && t < translatedAt,
                translated: !finished && t >= translatedAt,
                returning: !finished && t >= returnAt,
                finished: finished);
        }

        public static IReadOnlyList<string> EmojiUnits(string emojiLine)
        {
            var units = new List<string>();
            if (string.IsNullOrEmpty(emojiLine)) return units;

            for (int i = 0; i < emojiLine.Length; i++)
            {
                if (VanillaEmojiToken.TryReadAt(emojiLine, i, out _, out int next))
                {
                    units.Add(emojiLine.Substring(i, next - i));
                    i = next - 1;
                    continue;
                }

                int size = 1;
                if (char.IsHighSurrogate(emojiLine[i]) && i + 1 < emojiLine.Length
                    && char.IsLowSurrogate(emojiLine[i + 1]))
                {
                    size = 2;
                }
                if ((emojiLine[i] == (char)0xFE0F || emojiLine[i] == ' ') && units.Count > 0)
                {
                    units[units.Count - 1] += emojiLine[i];
                    continue;
                }
                units.Add(emojiLine.Substring(i, size));
                i += size - 1;
            }
            return units;
        }

        public static string Garble(string text, Random rng)
        {
            if (string.IsNullOrEmpty(text) || rng == null) return text;

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    sb.Append(c);
                    continue;
                }
                string pool = c < 0x80 ? NarrowGlitchChars : WideGlitchChars;
                sb.Append(pool[rng.Next(pool.Length)]);
            }
            return sb.ToString();
        }
    }
}
