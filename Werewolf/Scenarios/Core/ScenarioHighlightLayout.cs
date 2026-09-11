using Werewolf.Core;
using static Werewolf.Core.HudGeometry;

namespace Werewolf.Scenarios.Core
{
    public readonly struct GuideBox
    {
        public GuideBox(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }

        public float Y { get; }

        public float Width { get; }

        public float Height { get; }

        public float Left => X - Width * 0.5f;

        public float Right => X + Width * 0.5f;

        public float Bottom => Y - Height * 0.5f;

        public float Top => Y + Height * 0.5f;
    }

    public static class ScenarioHighlightLayout
    {
        public const float Padding = 6f;

        public static int RecapLineIndex(ScenarioHighlight highlight)
        {
            switch (highlight)
            {
                case ScenarioHighlight.RecapTitle: return 0;
                case ScenarioHighlight.RecapDeaths: return 1;
                case ScenarioHighlight.RecapLost: return 2;
                case ScenarioHighlight.RecapHaul: return 3;
                case ScenarioHighlight.RecapBeacon: return 4;
                default: return -1;
            }
        }

        public static bool IsLive(ScenarioHighlight highlight)
            => highlight == ScenarioHighlight.MeetingGauge || RecapLineIndex(highlight) >= 0;

        public static bool OccupiesChatLog(ScenarioHighlight highlight)
            => highlight == ScenarioHighlight.ChatLogPanel || RecapLineIndex(highlight) >= 0;

        public static bool TryPlace(CanvasBounds canvas, ScenarioHighlight highlight, bool hudIconsAboveGauge,
                                    out GuideBox box)
        {
            switch (highlight)
            {
                case ScenarioHighlight.MapButton:
                    box = new GuideBox(
                        canvas.Left + MapToggle.Margin + MapToggle.Width * 0.5f,
                        canvas.Bottom + MapToggle.Margin + MapToggle.Height * 0.5f,
                        MapToggle.Width, MapToggle.Height);
                    return true;

                case ScenarioHighlight.ChatLogButton:
                    box = new GuideBox(
                        canvas.Right - ChatToggle.Margin - ChatToggle.Width * 0.5f,
                        canvas.Bottom + ChatToggle.Margin + ChatToggle.Height * 0.5f,
                        ChatToggle.Width, ChatToggle.Height);
                    return true;

                case ScenarioHighlight.ChatLogPanel:
                    box = new GuideBox(
                        canvas.Right - ChatPanel.MarginRight - ChatPanel.Width * 0.5f, ChatPanel.OffsetY,
                        ChatPanel.Width, ChatPanel.Height);
                    return true;

                case ScenarioHighlight.MapOverlay:
                    box = new GuideBox(0f, 0f, MapOverlay.Width, MapOverlay.Height);
                    return true;

                case ScenarioHighlight.ReplayEntry:
                    box = new GuideBox(
                        canvas.Left + ReplayEntry.Margin + ReplayEntry.Width * 0.5f,
                        canvas.Bottom + ReplayEntry.Margin + ReplayEntry.Height * 0.5f,
                        ReplayEntry.Width, ReplayEntry.Height);
                    return true;

                case ScenarioHighlight.WolfStatus:
                    box = new GuideBox(
                        canvas.Left + WolfStatus.LeftMargin + WolfStatus.Width * 0.5f,
                        canvas.Bottom + WolfStatus.BottomMargin + WolfStatus.Height * 0.5f,
                        WolfStatus.Width, WolfStatus.Height);
                    return true;

                case ScenarioHighlight.BeaconSlot:
                    box = new GuideBox(
                        canvas.Left + WolfStatus.LeftMargin + WolfStatus.Width - WolfStatus.MainSlot * 0.5f,
                        canvas.Bottom + WolfStatus.BottomMargin
                            + WolfStatus.LabelHeight + WolfStatus.LabelGap + WolfStatus.PerkGrid * 0.5f,
                        WolfStatus.MainSlot, WolfStatus.MainSlot);
                    return true;

                case ScenarioHighlight.BomberHud:
                    box = new GuideBox(
                        canvas.Left + BomberHud.LeftMargin + BomberHud.Width * 0.5f,
                        canvas.Bottom + BomberHud.BottomMargin + BomberHud.Height * 0.5f,
                        BomberHud.Width, BomberHud.Height);
                    return true;

                case ScenarioHighlight.PlayGauge:
                    box = new GuideBox(
                        canvas.Right - MiniGauge.RightMargin - MiniGauge.Width * 0.5f,
                        canvas.Bottom + MiniGauge.BottomMargin + MiniGauge.Height * 0.5f,
                        MiniGauge.Width, MiniGauge.Height);
                    return true;

                case ScenarioHighlight.RecordIcon:
                    box = new GuideBox(
                        canvas.Right - HudIcon.RightMargin - HudIcon.Width * 0.5f,
                        canvas.Bottom + HudIcon.RecordBottom(hudIconsAboveGauge) + HudIcon.Height * 0.5f,
                        HudIcon.Width, HudIcon.Height);
                    return true;

                case ScenarioHighlight.ReportIcon:
                    box = new GuideBox(
                        canvas.Right - HudIcon.RightMargin - HudIcon.Width * 0.5f,
                        canvas.Bottom + HudIcon.ReportBottom(hudIconsAboveGauge) + HudIcon.Height * 0.5f,
                        HudIcon.Width, HudIcon.Height);
                    return true;

                default:
                    box = default;
                    return false;
            }
        }
    }
}
