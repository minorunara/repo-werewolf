using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;

namespace Werewolf.Scenarios.Game
{
    internal sealed partial class ScenarioController
    {
        private const float DecodeFocusVisualScale = 1.5f;

        private readonly ScenarioDecodePanel _decodePanel = new ScenarioDecodePanel();

        private bool _decodeStarted;
        private bool _decodeDone;
        private float _decodeElapsed;
        private IReadOnlyList<string> _decodeUnits;
        private int _decodeEmojiShown;
        private string _decodeBubbleText;
        private float _decodeGlitchRollAt;
        private int _decodeTypeSfxCount;
        private bool _decodeGlitchSfxPlayed;
        private System.Random _decodeRng;

        private bool TickDecodeIntro(bool keysFree)
        {
            if (_decodeDone) return false;

            string emojiLine = _definition != null ? _definition.IntroDecodeEmoji : null;
            if (string.IsNullOrEmpty(emojiLine) || _flow == null || _flow.Finished)
            {
                _decodeDone = true;
                return false;
            }

            if (!EmojiSprites.Ready || !EmojiSprites.VanillaReady)
            {
                _decodeDone = true;
                WLog.Line("scenario_decode_skipped", secret: false, ("reason", "emoji_sprites"));
                return false;
            }

            _d.EnsurePanelBuilt(_guide);
            _d.EnsurePanelBuilt(_decodePanel);
            if (!_guide.Exists || !_decodePanel.Exists)
            {
                _decodeDone = true;
                WLog.Line("scenario_decode_skipped", secret: false, ("reason", "panel_build"));
                return false;
            }

            if (!_decodeStarted)
            {
                _decodeStarted = true;
                _decodeElapsed = 0f;
                _decodeUnits = ScenarioDecodeIntro.EmojiUnits(emojiLine);
                _decodeRng = new System.Random();
                WLog.Line("scenario_decode_started", secret: false,
                    ("emoji", _decodeUnits.Count));
            }
            else
            {
                _decodeElapsed += Time.deltaTime;
            }

            if (keysFree && InputDown(GameKey.Menu))
            {
                FinishDecode("skipped");
                return false;
            }

            ScenarioDecodeIntro.State state =
                ScenarioDecodeIntro.At(_decodeElapsed, _decodeUnits.Count);
            if (state.Finished)
            {
                FinishDecode("finished");
                return false;
            }

            if (state.EmojiShown > _decodeTypeSfxCount)
            {
                _decodeTypeSfxCount = state.EmojiShown;
                _d.PlaySfx("sfx_decode_type");
            }

            if (state.Glitching && !_decodeGlitchSfxPlayed)
            {
                _decodeGlitchSfxPlayed = true;
                _d.PlaySfx("sfx_shaman_storm");
            }

            if (state.Returning) _decodePanel.Hide();
            else _decodePanel.Show(BuildDecodeCenterText(state));
            ShowDecodeBubble(state);

            _guide.Tick();
            _highlight.Hide();
            _highlight.Tick();
            return true;
        }

        private static string BuildDecodeCenterText(ScenarioDecodeIntro.State state)
        {
            if (!state.Line1Visible) return null;

            var sb = new StringBuilder();
            sb.Append(Texts.Get(TextId.ScenarioDecodeReceiving));
            if (state.Line1Done) sb.Append(' ').Append(Texts.Get(TextId.ScenarioDecodeDone));
            if (state.Line2Visible)
            {
                sb.Append('\n').Append(Texts.Get(TextId.ScenarioDecodeInitializing));
                if (state.Line2Done) sb.Append(' ').Append(Texts.Get(TextId.ScenarioDecodeDone));
            }
            return sb.ToString();
        }

        private void ShowDecodeBubble(ScenarioDecodeIntro.State state)
        {
            ScenarioPage page = _flow.Current;

            if (state.Glitching)
            {
                if (_decodeBubbleText == null || _decodeElapsed >= _decodeGlitchRollAt)
                {
                    _decodeBubbleText = ScenarioDecodeIntro.Garble(JoinLines(page.Lines), _decodeRng);
                    _decodeGlitchRollAt = _decodeElapsed + ScenarioDecodeIntro.GlitchRollSec;
                }
                _guide.ShowPage(_decodeBubbleText, ScenarioAnchor.DecodeFocus,
                    FontScale, ScenarioGuideHint.None, DecodeFocusVisualScale);
                return;
            }

            if (state.Translated)
            {
                string translated = JoinLines(page.Lines);
                if (_decodeBubbleText != translated) _decodeBubbleText = translated;

                ScenarioAnchor anchor = state.Returning
                    ? page.Anchor
                    : ScenarioAnchor.DecodeFocus;
                float visualScale = state.Returning ? 1f : DecodeFocusVisualScale;
                _guide.ShowPage(_decodeBubbleText, anchor, FontScale,
                    ScenarioGuideHint.None, visualScale);
                return;
            }

            if (state.EmojiShown <= 0)
            {
                _guide.Hide();
                return;
            }

            if (state.EmojiShown != _decodeEmojiShown || _decodeBubbleText == null)
            {
                _decodeEmojiShown = state.EmojiShown;
                var sb = new StringBuilder();
                for (int i = 0; i < state.EmojiShown; i++) sb.Append(_decodeUnits[i]);
                _decodeBubbleText = EmojiSprites.Substitute(sb.ToString());
            }
            _guide.ShowPage(_decodeBubbleText, ScenarioAnchor.DecodeFocus,
                FontScale, ScenarioGuideHint.None, DecodeFocusVisualScale);
        }

        private void FinishDecode(string reason)
        {
            _decodeDone = true;
            _decodePanel.Hide();
            WLog.Line("scenario_decode_finished", secret: false, ("reason", reason));
        }

        private void ResetDecode()
        {
            _decodeStarted = false;
            _decodeDone = false;
            _decodeElapsed = 0f;
            _decodeUnits = null;
            _decodeEmojiShown = 0;
            _decodeBubbleText = null;
            _decodeGlitchRollAt = 0f;
            _decodeTypeSfxCount = 0;
            _decodeGlitchSfxPlayed = false;
            _decodeRng = null;
            _decodePanel.Hide();
        }
    }
}
