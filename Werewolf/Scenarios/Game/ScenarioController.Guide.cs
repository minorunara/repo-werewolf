using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Vanilla;
using Werewolf.Scenarios.Vanilla;

namespace Werewolf.Scenarios.Game
{
    internal sealed partial class ScenarioController
    {
        private readonly ScenarioGuidePanel _guide = new ScenarioGuidePanel();

        private readonly ScenarioHighlightPanel _highlight = new ScenarioHighlightPanel();

        private readonly ScenarioTargetMarkPanel _targetMark = new ScenarioTargetMarkPanel();

        private readonly Vector3[] _corners = new Vector3[4];

        private bool _advanceHeld;

        private ScenarioNeed _prevUnmet;
        private long _advanceSwallowUntilMs;

        private string _shownLine;

        private const long NeedClearedSwallowMs = 250L;

        private const string LineBreak = "\n";

        private const float LineSfxVolume = 0.6f;

        internal bool GuideContainsPointer(Vector2 screenPoint) => _guide.ContainsPointer(screenPoint);

        private void TickGuide(long now)
        {
            bool keysFree = InputGate.KeysFree;
            bool advanceKey = ReadAdvanceKey(keysFree);

            if (_flow == null || !_d.ClientScenarioSession || _d.RoleRevealVisible)
            {
                HideGuide();
                return;
            }

            _guide.PlayHud = _definition != null
                ? ScenarioGuideLayout.PlayHudOf(_definition.Cast.PlayerRole)
                : ScenarioPlayHud.None;

            if (TickDecodeIntro(keysFree)) return;

            _d.EnsurePanelBuilt(_guide);
            _d.EnsurePanelBuilt(_highlight);
            _d.EnsurePanelBuilt(_targetMark);
            if (!_guide.Exists) return;

            _interrupt.StartDeferred(now);

            ScenarioPage page = _flow.Current;
            ScenarioNeed rawUnmet = UnmetNeed(page);

            ScenarioInterrupt interrupt = ResolveInterrupt(now, page, rawUnmet);
            ScenarioNeed unmet = interrupt == ScenarioInterrupt.None ? rawUnmet : ScenarioNeed.None;

            if (ScenarioNeedRules.ClearedByUiOperation(_prevUnmet) && rawUnmet == ScenarioNeed.None)
            {
                _advanceSwallowUntilMs = now + NeedClearedSwallowMs;
            }
            _prevUnmet = rawUnmet;

            ApplyHighlight(ShowGuide(interrupt, page, unmet));
            NoteGuideLine();
            _highlight.Tick();

            ScenarioTargetMarkKind markKind = ResolveTargetMark(out Vector3? markPos);
            _targetMark.Tick(markKind, markPos);

            bool clicked = _guide.Tick();
            HandleGuideInput(now, interrupt, unmet, keysFree, advanceKey || clicked);
        }

        private void HideGuide()
        {
            if (_guide.Exists)
            {
                _guide.Hide();
                _guide.Tick();
            }
            if (_highlight.Exists)
            {
                _highlight.Hide();
                _highlight.Tick();
            }
            _targetMark.Tick(ScenarioTargetMarkKind.None, null);
            NoteGuideLine();
        }

        private void NoteGuideLine()
        {
            string line = _guide.ShownLine;
            if (line == _shownLine) return;

            _shownLine = line;
            if (!string.IsNullOrEmpty(line)) _d.PlaySfx("sfx_decode_type", LineSfxVolume);
        }

        private ScenarioTargetMarkKind ResolveTargetMark(out Vector3? worldPos)
        {
            worldPos = BombTargetMarkPos();
            if (worldPos != null) return ScenarioTargetMarkKind.BombTarget;
            worldPos = CorpseHintMarkPos();
            if (worldPos != null) return ScenarioTargetMarkKind.Corpse;
            return ScenarioTargetMarkKind.None;
        }

        private ScenarioInterrupt ResolveInterrupt(long now, ScenarioPage page, ScenarioNeed unmet)
        {
            ScenarioInterrupt held = _interrupt.Resolve(now);
            if (held != ScenarioInterrupt.None) return held;
            return ShowsMapAside(page, unmet) ? ScenarioInterrupt.MapAside : ScenarioInterrupt.None;
        }

        private bool ShowsMapAside(ScenarioPage page, ScenarioNeed unmet)
            => unmet == ScenarioNeed.None
               && !_flow.Finished
               && page.Cue == ScenarioCue.ChatScript
               && _definition != null && _definition.Notices.MapAside.Count > 0
               && _d.MeetingMapOpen;

        private ScenarioHighlight ShowGuide(ScenarioInterrupt interrupt, ScenarioPage page,
                                            ScenarioNeed unmet)
        {
            switch (interrupt)
            {
                case ScenarioInterrupt.Broken:
                    _guide.ShowPage(BrokenNoticeText(_interrupt.BrokenLines), page.Anchor, FontScale,
                        ScenarioGuideHint.None);
                    return ScenarioHighlight.None;

                case ScenarioInterrupt.AbortConfirm:
                    _guide.ShowPage(Texts.Get(TextId.ScenarioAbortConfirm), page.Anchor, FontScale,
                        ScenarioGuideHint.AbortConfirm);
                    return ScenarioHighlight.None;

                case ScenarioInterrupt.MapAside:
                    _guide.ShowPage(JoinLines(_definition.Notices.MapAside),
                        ScenarioAnchor.BottomLeft, FontScale, ScenarioGuideHint.ActionOnly);
                    return ScenarioHighlight.None;
            }

            if (ScenarioInterruptState.IsNotice(interrupt))
            {
                int arg = _interrupt.NoticeArg(interrupt);
                _guide.ShowPage(
                    JoinLines(_interrupt.NoticeLines(interrupt), arg != 0 ? (object)arg : null),
                    page.Anchor, FontScale,
                    ScenarioInterruptState.IsDismissable(interrupt)
                        ? ScenarioGuideHint.Advance
                        : ScenarioGuideHint.ActionOnly);
                return ScenarioHighlight.None;
            }

            if (_flow.Finished)
            {
                _guide.Hide();
                return ScenarioHighlight.None;
            }

            if (unmet != ScenarioNeed.None && ScenarioNeedRules.TryGetNag(unmet, out ScenarioNeedNag nag))
            {
                _guide.ShowPage(ResolveLine(nag.Text, page.BombTargetParticipantId), nag.Anchor,
                    FontScale, ScenarioGuideHint.ActionOnly);
                return nag.Highlight;
            }

            if (page.HasText)
            {
                _guide.ShowPage(JoinLines(page.Lines), page.Anchor, FontScale,
                    _flow.AdvancesOnInput ? ScenarioGuideHint.Advance : ScenarioGuideHint.ActionOnly);
                return page.Highlight;
            }

            _guide.Hide();
            return page.Highlight;
        }

        private void HandleGuideInput(long now, ScenarioInterrupt interrupt, ScenarioNeed unmet,
                                      bool keysFree, bool advance)
        {
            if (interrupt == ScenarioInterrupt.Broken) return;

            if (advance && now < _advanceSwallowUntilMs)
            {
                WLog.Line("scenario_advance_swallowed", secret: false, ("page", _flow.Index));
                advance = false;
            }

            bool menuKey = keysFree && InputDown(GameKey.Menu);

            if (interrupt == ScenarioInterrupt.AbortConfirm)
            {
                if (menuKey)
                {
                    _interrupt.Dismiss(ScenarioInterrupt.AbortConfirm);
                    ScenarioRun.ExitToTitle("player_abort", completed: _flow.Finished);
                }
                else if (advance)
                {
                    _interrupt.Dismiss(ScenarioInterrupt.AbortConfirm);
                }
                return;
            }

            if (menuKey)
            {
                _interrupt.RequestAbort();
                return;
            }

            if (interrupt != ScenarioInterrupt.None)
            {
                if (advance) _interrupt.Dismiss(interrupt);
                return;
            }

            if (advance && unmet == ScenarioNeed.None) FeedSignal(ScenarioSignal.Advance);
        }

        private void ApplyHighlight(ScenarioHighlight highlight)
        {
            if (!_highlight.Exists) return;

            if (highlight == ScenarioHighlight.None)
            {
                _highlight.Hide();
                return;
            }

            bool hudIconsAboveGauge =
                _d.CorpseReportHudLayout == CorpseReportHudPanel.Layout.AboveMiniGauge;
            if (_highlight.TryGetCanvas(out CanvasBounds canvas)
                && ScenarioHighlightLayout.TryPlace(canvas, highlight, hudIconsAboveGauge, out GuideBox box))
            {
                _highlight.ShowBox(box);
                return;
            }

            if (highlight == ScenarioHighlight.MeetingGauge)
            {
                MeetingGaugePanel gauge = _d.GaugePanelView;
                if (gauge != null && gauge.Visible && TryScreenRect(gauge.PanelRect, out Rect gaugeRect))
                {
                    _highlight.ShowScreenRect(gaugeRect);
                    return;
                }
                _highlight.Hide();
                return;
            }

            int line = ScenarioHighlightLayout.RecapLineIndex(highlight);
            if (line >= 0 && _d.Chat != null && _d.Chat.TryGetRecapLineScreenRect(line, out Rect lineRect))
            {
                _highlight.ShowScreenRect(lineRect);
                return;
            }
            _highlight.Hide();
        }

        private bool TryScreenRect(RectTransform rect, out Rect screenRect)
        {
            screenRect = default;
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;

            rect.GetWorldCorners(_corners);
            screenRect = Rect.MinMaxRect(_corners[0].x, _corners[0].y, _corners[2].x, _corners[2].y);
            return screenRect.width > 1f && screenRect.height > 1f;
        }

        private string JoinLines(IReadOnlyList<TextId> ids, object arg = null)
        {
            if (ids == null || ids.Count == 0) return string.Empty;
            if (ids.Count == 1) return ResolveLine(ids[0], arg);

            var sb = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) sb.Append(LineBreak);
                sb.Append(ResolveLine(ids[i], arg));
            }
            return sb.ToString();
        }

        private string ResolveLine(TextId id, object arg = null)
            => ReplaceModKeyTags(ResolveHintLine(id, arg));

        private string ResolveHintLine(TextId id, object arg)
        {
            ScenarioKeyHint hint = _definition != null ? _definition.Notices.KeyHint : default;
            if (!hint.HasValue) return Resolve(id, arg);

            bool grab = _d.MeetingButtonUsesGrab;
            if (id == hint.FormatId)
            {
                return Texts.Format(id, grab ? "[grab]" : "[interact]");
            }
            if (id == hint.GrabNoteId)
            {
                return Resolve(grab ? hint.GrabNoteId : hint.InteractNoteId, arg);
            }
            return Resolve(id, arg);
        }

        private static string Resolve(TextId id, object arg)
            => arg == null ? Texts.Get(id) : Texts.Format(id, arg);

        private static string ReplaceModKeyTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = ReplaceModKeyTag(text, ScenarioKeyTags.WolfMode, Plugin.WolfModeKey);
            text = ReplaceModKeyTag(text, ScenarioKeyTags.Beacon, Plugin.BeaconKey);
            text = ReplaceModKeyTag(text, ScenarioKeyTags.CorpseReport, Plugin.CorpseReportKey);
            text = ReplaceModKeyTag(text, ScenarioKeyTags.BombPlant, Plugin.Bindings?.BomberPlantKey);
            text = ReplaceModKeyTag(text, ScenarioKeyTags.BombDetonate, Plugin.Bindings?.BomberDetonateKey);
            return text;
        }

        private static string ReplaceModKeyTag(
            string text, string tag, BepInEx.Configuration.ConfigEntry<KeyCode> key)
        {
            string name = key != null ? key.Value.ToString() : "?";
            return text.Replace(tag, ScenarioGuidePanel.KeyPrefix + name + ScenarioGuidePanel.KeySuffix);
        }

        private ScenarioNeed UnmetNeed(ScenarioPage page)
        {
            if (_flow.Finished || page.Need == ScenarioNeed.None) return ScenarioNeed.None;
            return ScenarioNeedRules.Unmet(page.Need, ReadNeedSample());
        }

        private ScenarioNeedSample ReadNeedSample()
        {
            ScenarioShamanSample shaman = _d.ShamanSenseSample;
            return new ScenarioNeedSample(
                meetingUiVisible: _d.UiManagerView != null
                                  && _d.UiManagerView.IsLayerVisible(WerewolfUIManager.MeetingLayer),
                chatLogOpen: _d.MeetingChatLogOpen,
                meetingMapOpen: _d.MeetingMapOpen,
                mapToolOpen: MapRtView.IsMapToolActive(),
                tranceActive: shaman.SenseArmed,
                senseCoolingDown: shaman.SenseCoolingDown,
                stormActive: shaman.StormTier != ShamanStormTier.None,
                corpseNear: _d.LocalNearCorpseBody,
                bombTargetNear: LocalNearBombTarget(),
                bombOnTarget: BombOnBombTarget());
        }

        private bool ReadAdvanceKey(bool keysFree)
        {
            bool held = keysFree && InputHold(GameKey.Interact);
            bool pressed = held && !_advanceHeld;
            _advanceHeld = held;
            return pressed;
        }

        private static float FontScale
            => Plugin.Bindings != null ? Plugin.Bindings.TutorialFontScale.Value : 1f;

        private static bool InputDown(GameKey key)
            => ScenarioVanillaProbe.InputDown(key, "guide_input_down");

        private static bool InputHold(GameKey key)
            => ScenarioVanillaProbe.InputHold(key, "guide_input_hold");

        public void DestroyUi()
        {
            _guide.Destroy();
            _highlight.Destroy();
            _targetMark.Destroy();
            _decodePanel.Destroy();
        }

        private void ResetGuide()
        {
            _advanceHeld = false;
            _prevUnmet = ScenarioNeed.None;
            _advanceSwallowUntilMs = 0;
            _shownLine = null;
            _guide.Hide();
            _highlight.Hide();
            _targetMark.Tick(ScenarioTargetMarkKind.None, null);
        }
    }
}
