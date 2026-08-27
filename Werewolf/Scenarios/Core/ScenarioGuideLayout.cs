using System;
using Werewolf.Core;
using static Werewolf.Core.HudGeometry;

namespace Werewolf.Scenarios.Core
{
    public readonly struct GuidePlacement
    {
        public GuidePlacement(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }
    }

    public enum ScenarioPlayHud : byte
    {
        None = 0,

        WolfStatus = 1,

        Bomber = 2,
    }

    public static class ScenarioGuideLayout
    {
        public const float SideMargin = 44f;

        public const float BottomLeftMargin = 140f;

        public static float BottomLeftY(CanvasBounds canvas) => canvas.Bottom + BottomLeftMargin;

        public static float BottomButtonTopY(CanvasBounds canvas)
            => canvas.Bottom + Math.Max(MapToggle.Margin + MapToggle.Height, ChatToggle.Margin + ChatToggle.Height);

        public static float AboveButtonY(CanvasBounds canvas) => BottomButtonTopY(canvas) + Gap;

        public static float ReplayEntryTopY(CanvasBounds canvas)
            => canvas.Bottom + ReplayEntry.Margin + ReplayEntry.Height;

        public static float AboveReplayEntryY(CanvasBounds canvas) => ReplayEntryTopY(canvas) + Gap;

        public static float ChatPanelLeftX(CanvasBounds canvas)
            => canvas.Right - ChatPanel.MarginRight - ChatPanel.Width;

        public static float ChatButtonRightX(CanvasBounds canvas) => canvas.Right - ChatToggle.Margin;

        public const float MeetingGaugeBottomY = MeetingGauge.Bottom;

        public static float ManualIconRightX(CanvasBounds canvas)
            => canvas.Left + ManualIcon.LeftMargin + ManualIcon.Width;

        public static float ManualIconBottomY(CanvasBounds canvas)
            => canvas.Top - ManualIcon.TopMargin - ManualIcon.Height;

        public const float CheckmateTitleTopY = CheckmateReveal.TitleTop;

        public const float CheckmateGaugeBottomY = CheckmateReveal.GaugeBottom;

        public const float AboveCheckmateTitleY = CheckmateTitleTopY + Gap;

        public static float PlayGaugeTopY(CanvasBounds canvas) => canvas.Bottom + MiniGauge.Top;

        public static float PlayHudIconLeftX(CanvasBounds canvas)
            => canvas.Right - HudIcon.RightMargin - HudIcon.Width;

        public static float WolfStatusTopY(CanvasBounds canvas)
            => canvas.Bottom + WolfStatus.BottomMargin + WolfStatus.Height;

        public static float BomberHudTopY(CanvasBounds canvas)
            => canvas.Bottom + BomberHud.BottomMargin + BomberHud.Height;

        public static float AbovePlayGaugeY(CanvasBounds canvas) => PlayGaugeTopY(canvas) + Gap;

        public const float BottomMargin = 24f;

        public const float TopMargin = 24f;

        public const float DecodeFocusBottomY = DecodeWindow.Top + Gap;

        public const float Gap = 16f;

        public static ScenarioPlayHud PlayHudOf(Role role)
        {
            switch (role)
            {
                case Role.Werewolf: return ScenarioPlayHud.WolfStatus;
                case Role.Bomber: return ScenarioPlayHud.Bomber;
                default: return ScenarioPlayHud.None;
            }
        }

        public static float AbovePlayHudY(CanvasBounds canvas, ScenarioPlayHud playHud)
        {
            switch (playHud)
            {
                case ScenarioPlayHud.WolfStatus: return WolfStatusTopY(canvas) + Gap;
                case ScenarioPlayHud.Bomber: return BomberHudTopY(canvas) + Gap;
                default: return BottomLeftY(canvas);
            }
        }

        public static GuidePlacement Place(CanvasBounds canvas, ScenarioAnchor anchor, float width, float height,
                                           ScenarioPlayHud playHud = ScenarioPlayHud.None)
        {
            float left = canvas.Left + SideMargin;

            switch (anchor)
            {
                case ScenarioAnchor.MapButton:
                {
                    float bottom = AboveButtonY(canvas);
                    return new GuidePlacement(LeftX(canvas, bottom, height), bottom);
                }

                case ScenarioAnchor.ChatLog:
                    return new GuidePlacement(
                        ClampLeft(ChatPanelLeftX(canvas) - Gap - width, left), AboveButtonY(canvas));

                case ScenarioAnchor.ChatLogButton:
                    return new GuidePlacement(
                        ClampLeft(ChatButtonRightX(canvas) - width, left), AboveButtonY(canvas));

                case ScenarioAnchor.ReplayEntry:
                {
                    float bottom = AboveReplayEntryY(canvas);
                    return new GuidePlacement(LeftX(canvas, bottom, height), bottom);
                }

                case ScenarioAnchor.PlayHud:
                {
                    float playBottom = AbovePlayHudY(canvas, playHud);
                    return playHud == ScenarioPlayHud.None
                        ? new GuidePlacement(LeftX(canvas, playBottom, height), playBottom)
                        : new GuidePlacement(ManualIconRightX(canvas) + Gap, playBottom);
                }

                case ScenarioAnchor.PlayHudRight:
                    return new GuidePlacement(
                        ClampLeft(PlayHudIconLeftX(canvas) - Gap - width, left), AbovePlayGaugeY(canvas));

                case ScenarioAnchor.CheckmateStamp:
                    return new GuidePlacement(
                        LeftX(canvas, AboveCheckmateTitleY, height), AboveCheckmateTitleY);

                case ScenarioAnchor.DecodeFocus:
                {
                    float maxBottom = canvas.Top - TopMargin - height;
                    float bottom = DecodeFocusBottomY < maxBottom ? DecodeFocusBottomY : maxBottom;
                    return new GuidePlacement(ClampLeft(-width * 0.5f, left), ClampBottom(canvas, bottom));
                }

                case ScenarioAnchor.MeetingGauge:
                {
                    float bottom = ClampBottom(canvas, MeetingGaugeBottomY - Gap - height);
                    return new GuidePlacement(LeftX(canvas, bottom, height), bottom);
                }

                default:
                {
                    float bottom = BottomLeftY(canvas);
                    return new GuidePlacement(LeftX(canvas, bottom, height), bottom);
                }
            }
        }

        private static float LeftX(CanvasBounds canvas, float bottomY, float height)
            => bottomY + height > ManualIconBottomY(canvas)
                ? ManualIconRightX(canvas) + Gap
                : canvas.Left + SideMargin;

        private static float ClampLeft(float x, float left) => x < left ? left : x;

        private static float ClampBottom(CanvasBounds canvas, float y)
        {
            float bottom = canvas.Bottom + BottomMargin;
            return y < bottom ? bottom : y;
        }
    }
}
