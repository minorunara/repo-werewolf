namespace Werewolf.Core
{
    public static class HudGeometry
    {
        public static class IconToggle
        {
            public const float LabelGap = 4f;

            public static float Width(float iconSize, float labelWidth)
                => iconSize > labelWidth ? iconSize : labelWidth;

            public static float Height(float iconSize, float labelHeight)
                => iconSize + LabelGap + labelHeight;
        }

        public static class ChatToggle
        {
            public const float Margin = 24f;
            public const float IconSize = 96f;
            public const float LabelWidth = 320f;
            public const float LabelHeight = 32f;
            public const float Width = IconSize > LabelWidth ? IconSize : LabelWidth;
            public const float Height = IconSize + IconToggle.LabelGap + LabelHeight;
        }

        public static class MapToggle
        {
            public const float Margin = 24f;
            public const float IconSize = 96f;
            public const float LabelWidth = 240f;
            public const float LabelHeight = 32f;
            public const float Width = IconSize > LabelWidth ? IconSize : LabelWidth;
            public const float Height = IconSize + IconToggle.LabelGap + LabelHeight;
        }

        public static class ReplayEntry
        {
            public const float Margin = 24f;
            public const float IconSize = 288f;
            public const float LabelWidth = 240f;
            public const float LabelHeight = 32f;
            public const float Width = IconSize > LabelWidth ? IconSize : LabelWidth;
            public const float Height = IconSize + IconToggle.LabelGap + LabelHeight;
        }

        public static class ChatPanel
        {
            public const float Width = 330f;
            public const float Height = 690f;
            public const float MarginRight = 24f;
            public const float OffsetY = -10f;
        }

        public static class MapOverlay
        {
            public const float Width = 1200f;
            public const float Height = 720f;
        }

        public static class RoleBadge
        {
            public const float LeftMargin = 40f;
            public const float TopMargin = 220f;
            public const float RowHeight = 132f;
        }

        public static class ManualIcon
        {
            public const float LeftMargin = RoleBadge.LeftMargin;
            public const float TopMargin = RoleBadge.TopMargin + RoleBadge.RowHeight + 12f;
            public const float IconSize = 72f;
            public const float LabelWidth = 170f;
            public const float LabelHeight = 28f;
            public const float Width = IconSize > LabelWidth ? IconSize : LabelWidth;
            public const float Height = IconSize + IconToggle.LabelGap + LabelHeight;
        }

        public static class WolfStatus
        {
            public const float LeftMargin = 48f;
            public const float BottomMargin = 150f;
            public const float MainIconSize = 112f;
            public const float PerkIconSize = 84f;
            public const float SlotPadding = 8f;
            public const float SlotGap = 10f;
            public const float BeaconGap = 22f;
            public const float LabelHeight = 34f;
            public const float LabelGap = 4f;
            public const float MainSlot = MainIconSize + SlotPadding;
            public const float PerkSlot = PerkIconSize + SlotPadding;
            public const float PerkGrid = PerkSlot * 2f + SlotGap;
            public const float Width = MainSlot + SlotGap + PerkGrid + BeaconGap + MainSlot;
            public const float Height = LabelHeight + LabelGap + PerkGrid;
        }

        public static class BomberHud
        {
            public const float LeftMargin = 48f;
            public const float BottomMargin = 150f;
            public const float SlotSize = 112f;
            public const float SlotGap = 22f;
            public const float LabelHeight = 34f;
            public const float LabelGap = 4f;
            public const float Width = SlotSize * 2f + SlotGap;
            public const float Height = LabelHeight + LabelGap + SlotSize;
        }

        public static class MeetingGauge
        {
            public const float Width = 1560f;
            public const float Height = 104f;
            public const float CenterY = 400f;
            public const float Bottom = CenterY - Height * 0.5f;
            public const float TrackWidth = 1500f;
            public const float TrackHeight = 20f;
        }

        public static class MiniGauge
        {
            public const float PanelWidth = 1560f;
            public const float PanelHeight = 200f;
            public const float Scale = 0.45f;
            public const float RightMargin = 25f;
            public const float BottomMargin = 50f;
            public const float Width = PanelWidth * Scale;
            public const float Height = PanelHeight * Scale;
            public const float Top = BottomMargin + Height;
        }

        public static class HudIcon
        {
            public const float RightMargin = MiniGauge.RightMargin;
            public const float IconSize = 112f;
            public const float PlatePadding = 8f;
            public const float LabelHeight = 34f;
            public const float LabelGap = 4f;
            public const float StackGap = 12f;
            public const float AboveGaugeGap = 20f;
            public const float Width = IconSize + PlatePadding;
            public const float Height = Width + LabelGap + LabelHeight;

            public static float ReportBottom(bool aboveGauge)
                => aboveGauge ? MiniGauge.Top + AboveGaugeGap : MiniGauge.BottomMargin;

            public static float RecordBottom(bool aboveGauge)
                => ReportBottom(aboveGauge) + Height + StackGap;
        }

        public static class CheckmateReveal
        {
            public const float TitleCenterY = -290f;
            public const float TitleHeight = 130f;
            public const float TitleTop = TitleCenterY + TitleHeight * 0.5f;
            public const float GaugeCenterY = 220f;
            public const float GaugeScale = 0.8f;
            public const float LossTextOffsetY = -(MeetingGauge.TrackHeight + 36f);
            public const float LossTextHeight = 44f;
            public const float GaugeBottom = GaugeCenterY + (LossTextOffsetY - LossTextHeight * 0.5f) * GaugeScale;
        }

        public static class DecodeWindow
        {
            public const float Width = 1040f;
            public const float Height = 300f;
            public const float Top = Height * 0.5f;
        }
    }
}
