using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioLayoutTests
    {
        private static readonly CanvasBounds Wide = new CanvasBounds(1920f, 1080f);

        private static readonly CanvasBounds[] Canvases =
        {
            Wide,
            new CanvasBounds(2580f, 1080f),
            new CanvasBounds(3840f, 1080f),
            new CanvasBounds(1920f, 1200f),
            new CanvasBounds(1920f, 1440f),
        };

        private static string Tag(CanvasBounds canvas) => $"{canvas.Width}x{canvas.Height}";

        [Fact]
        public void Canvas_IsMeasuredOnlyWithARealSize()
        {
            Assert.True(Wide.IsMeasured);
            Assert.False(new CanvasBounds(0f, 0f).IsMeasured);
            Assert.False(new CanvasBounds(1920f, 0f).IsMeasured);
        }

        [Fact]
        public void Guide_StaysOnScreenForEveryAnchorAndSize()
        {
            foreach (CanvasBounds canvas in Canvases)
            foreach (ScenarioPlayHud hud in Enum.GetValues(typeof(ScenarioPlayHud)))
            foreach (ScenarioAnchor anchor in Enum.GetValues(typeof(ScenarioAnchor)))
            {
                float left = canvas.Left + ScenarioGuideLayout.SideMargin;
                foreach (float width in new[] { 200f, 600f, 1600f })
                {
                    foreach (float height in new[] { 200f, 600f, 1600f })
                    {
                        var placement = ScenarioGuideLayout.Place(canvas, anchor, width, height, hud);
                        Assert.True(placement.X >= left, $"{Tag(canvas)} {hud} {anchor} w={width}");
                        Assert.True(placement.Y >= canvas.Bottom, $"{Tag(canvas)} {hud} {anchor} h={height}");
                    }
                }
            }
        }

        [Fact]
        public void Guide_DefaultPositionKeepsItsMarginOnEveryAspectRatio()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                var placement = ScenarioGuideLayout.Place(canvas, ScenarioAnchor.BottomLeft, 648f, 260f);

                Assert.Equal(canvas.Left + ScenarioGuideLayout.SideMargin, placement.X, 3);
                Assert.Equal(canvas.Bottom + ScenarioGuideLayout.BottomLeftMargin, placement.Y, 3);
            }
        }

        [Fact]
        public void Guide_ChatLogAnchorStaysLeftOfTheLog()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                var placement = ScenarioGuideLayout.Place(canvas, ScenarioAnchor.ChatLog, 500f, 300f);

                Assert.True(placement.X + 500f <= ScenarioGuideLayout.ChatPanelLeftX(canvas), Tag(canvas));
            }
        }

        [Fact]
        public void Guide_ButtonAnchorsClearTheButtonTheyNameSo()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                Assert.True(ScenarioGuideLayout.Place(canvas, ScenarioAnchor.MapButton, 600f, 300f).Y
                            >= ScenarioGuideLayout.BottomButtonTopY(canvas), Tag(canvas));
                Assert.True(ScenarioGuideLayout.Place(canvas, ScenarioAnchor.ChatLogButton, 600f, 300f).Y
                            >= ScenarioGuideLayout.BottomButtonTopY(canvas), Tag(canvas));
            }
        }

        [Fact]
        public void Guide_GaugeAnchorHangsUnderTheGauge()
        {
            foreach (CanvasBounds canvas in Canvases)
            foreach (float height in new[] { 200f, 320f, 480f })
            {
                var placement = ScenarioGuideLayout.Place(canvas, ScenarioAnchor.MeetingGauge, 600f, height);
                Assert.True(placement.Y + height <= ScenarioGuideLayout.MeetingGaugeBottomY,
                    $"{Tag(canvas)} h={height}");
            }
        }

        [Fact]
        public void Guide_DecodeFocusIsCenteredAboveTheDecoderWindow()
        {
            const float width = 648f;
            const float height = 270f;

            foreach (CanvasBounds canvas in Canvases)
            {
                var placement = ScenarioGuideLayout.Place(canvas, ScenarioAnchor.DecodeFocus, width, height);

                Assert.Equal(0f, placement.X + width * 0.5f);
                Assert.True(placement.Y >= HudGeometry.DecodeWindow.Top, Tag(canvas));
                Assert.True(placement.Y + height <= canvas.Top - ScenarioGuideLayout.TopMargin, Tag(canvas));

                const float focusScale = 1.5f;
                var enlarged = ScenarioGuideLayout.Place(
                    canvas, ScenarioAnchor.DecodeFocus, width * focusScale, height * focusScale);
                Assert.Equal(0f, enlarged.X + width * focusScale * 0.5f);
                Assert.True(enlarged.Y + height * focusScale <= canvas.Top - ScenarioGuideLayout.TopMargin,
                    Tag(canvas));
            }
        }

        [Fact]
        public void Guide_ReplayEntryAnchorClearsTheEntryTheDefaultWouldCover()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.ReplayEntry, true, out GuideBox entry));

                Assert.True(ScenarioGuideLayout.Place(canvas, ScenarioAnchor.ReplayEntry, 600f, 300f).Y >= entry.Top,
                    Tag(canvas));
                Assert.True(ScenarioGuideLayout.BottomLeftY(canvas) < entry.Top, Tag(canvas));
            }
        }

        [Fact]
        public void Guide_CheckmateStampAnchorClearsTheTitleTheDefaultWouldCover()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                foreach (float height in new[] { 200f, 280f, 360f })
                {
                    var placement = ScenarioGuideLayout.Place(
                        canvas, ScenarioAnchor.CheckmateStamp, 600f, height);

                    Assert.True(placement.Y >= ScenarioGuideLayout.CheckmateTitleTopY, $"{Tag(canvas)} h={height}");
                    Assert.True(placement.Y + height <= ScenarioGuideLayout.CheckmateGaugeBottomY,
                        $"{Tag(canvas)} h={height}");
                }

                Assert.True(ScenarioGuideLayout.BottomLeftY(canvas) < ScenarioGuideLayout.CheckmateTitleTopY,
                    Tag(canvas));
            }
        }

        [Fact]
        public void Highlight_EveryTargetHasExactlyOneResolutionPath()
        {
            foreach (ScenarioHighlight highlight in Enum.GetValues(typeof(ScenarioHighlight)))
            {
                if (highlight == ScenarioHighlight.None) continue;
                bool placed = ScenarioHighlightLayout.TryPlace(Wide, highlight, true, out _);
                Assert.True(placed ^ ScenarioHighlightLayout.IsLive(highlight), highlight.ToString());
            }
        }

        [Fact]
        public void Highlight_StaticTargetsStayOnScreen()
        {
            foreach (CanvasBounds canvas in Canvases)
            foreach (bool aboveGauge in new[] { true, false })
            foreach (ScenarioHighlight highlight in Enum.GetValues(typeof(ScenarioHighlight)))
            {
                if (!ScenarioHighlightLayout.TryPlace(canvas, highlight, aboveGauge, out GuideBox box)) continue;

                string tag = $"{Tag(canvas)} {highlight}";
                Assert.True(box.Left >= canvas.Left, tag);
                Assert.True(box.Right <= canvas.Right, tag);
                Assert.True(box.Bottom >= canvas.Bottom, tag);
                Assert.True(box.Top <= canvas.Top, tag);
            }
        }

        [Fact]
        public void Highlight_CornerTargetsKeepTheirMarginOnEveryAspectRatio()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.PlayGauge, true, out GuideBox gauge));
                Assert.Equal(canvas.Right - HudGeometry.MiniGauge.RightMargin, gauge.Right, 3);
                Assert.Equal(canvas.Bottom + HudGeometry.MiniGauge.BottomMargin, gauge.Bottom, 3);

                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.RecordIcon, true, out GuideBox record));
                Assert.Equal(canvas.Right - HudGeometry.HudIcon.RightMargin, record.Right, 3);
                Assert.Equal(canvas.Bottom + HudGeometry.HudIcon.RecordBottom(true), record.Bottom, 3);

                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.WolfStatus, true, out GuideBox wolf));
                Assert.Equal(canvas.Left + HudGeometry.WolfStatus.LeftMargin, wolf.Left, 3);
                Assert.Equal(canvas.Bottom + HudGeometry.WolfStatus.BottomMargin, wolf.Bottom, 3);

                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.MapButton, true, out GuideBox map));
                Assert.Equal(canvas.Left + HudGeometry.MapToggle.Margin, map.Left, 3);
                Assert.Equal(canvas.Bottom + HudGeometry.MapToggle.Margin, map.Bottom, 3);

                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.ChatLogPanel, true, out GuideBox chat));
                Assert.Equal(canvas.Right - HudGeometry.ChatPanel.MarginRight, chat.Right, 3);

                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.MapOverlay, true, out GuideBox overlay));
                Assert.Equal(0f, overlay.X);
                Assert.Equal(0f, overlay.Y);
            }
        }

        [Fact]
        public void Guide_PlayHudAnchorClearsTheBottomLeftHudOfTheRole()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.WolfStatus, true, out GuideBox wolf));
                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.BomberHud, true, out GuideBox bomber));

                Assert.True(ScenarioGuideLayout.BottomLeftY(canvas) < wolf.Top, Tag(canvas));
                Assert.True(ScenarioGuideLayout.BottomLeftY(canvas) < bomber.Top, Tag(canvas));

                Assert.True(ScenarioGuideLayout.Place(
                    canvas, ScenarioAnchor.PlayHud, 600f, 300f, ScenarioPlayHud.WolfStatus).Y >= wolf.Top, Tag(canvas));
                Assert.True(ScenarioGuideLayout.Place(
                    canvas, ScenarioAnchor.PlayHud, 600f, 300f, ScenarioPlayHud.Bomber).Y >= bomber.Top, Tag(canvas));
                Assert.True(bomber.Top < wolf.Top);
                Assert.True(ScenarioGuideLayout.Place(
                                canvas, ScenarioAnchor.PlayHud, 600f, 300f, ScenarioPlayHud.Bomber).Y
                            < ScenarioGuideLayout.Place(
                                canvas, ScenarioAnchor.PlayHud, 600f, 300f, ScenarioPlayHud.WolfStatus).Y,
                    Tag(canvas));
            }
        }

        [Fact]
        public void Guide_PlayHudAnchorSitsAtTheDefaultHeightWithoutABottomLeftHud()
        {
            var placement = ScenarioGuideLayout.Place(
                Wide, ScenarioAnchor.PlayHud, 648f, 260f, ScenarioPlayHud.None);
            var bottomLeft = ScenarioGuideLayout.Place(Wide, ScenarioAnchor.BottomLeft, 648f, 260f);

            Assert.Equal(bottomLeft.X, placement.X);
            Assert.Equal(bottomLeft.Y, placement.Y);

            Assert.Equal(ScenarioPlayHud.None, ScenarioGuideLayout.PlayHudOf(Role.Shaman));
            Assert.Equal(ScenarioPlayHud.None, ScenarioGuideLayout.PlayHudOf(Role.Villager));
            Assert.Equal(ScenarioPlayHud.WolfStatus, ScenarioGuideLayout.PlayHudOf(Role.Werewolf));
            Assert.Equal(ScenarioPlayHud.Bomber, ScenarioGuideLayout.PlayHudOf(Role.Bomber));
        }

        [Fact]
        public void Highlight_BeaconSlotSitsInsideTheWolfStatusPanel()
        {
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.WolfStatus, true, out GuideBox wolf));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.BeaconSlot, true, out GuideBox beacon));

            Assert.True(beacon.Left >= wolf.Left);
            Assert.True(beacon.Right <= wolf.Right);
            Assert.True(beacon.Bottom >= wolf.Bottom);
            Assert.True(beacon.Top <= wolf.Top);
        }

        [Fact]
        public void Guide_PlayHudAnchorClearsTheManualIcon()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                foreach (ScenarioPlayHud hud in new[] { ScenarioPlayHud.WolfStatus, ScenarioPlayHud.Bomber })
                foreach (float height in new[] { 200f, 400f, 800f })
                {
                    var placement = ScenarioGuideLayout.Place(canvas, ScenarioAnchor.PlayHud, 600f, height, hud);
                    Assert.True(placement.X >= ScenarioGuideLayout.ManualIconRightX(canvas),
                        $"{Tag(canvas)} {hud} h={height}");
                }

                foreach (float height in new[] { 120f, 220f, 260f, 340f, 500f })
                {
                    var placement = ScenarioGuideLayout.Place(
                        canvas, ScenarioAnchor.PlayHud, 648f, height, ScenarioPlayHud.None);

                    bool clearsBelow = placement.Y + height <= ScenarioGuideLayout.ManualIconBottomY(canvas);
                    bool clearsBeside = placement.X >= ScenarioGuideLayout.ManualIconRightX(canvas);
                    Assert.True(clearsBelow || clearsBeside, $"{Tag(canvas)} h={height}");
                }
            }
        }

        [Fact]
        public void Guide_LeftColumnAnchorsClearTheManualIconWhenTheyGrowIntoIt()
        {
            var anchors = new[]
            {
                ScenarioAnchor.BottomLeft, ScenarioAnchor.MapButton, ScenarioAnchor.ReplayEntry,
                ScenarioAnchor.CheckmateStamp, ScenarioAnchor.MeetingGauge,
            };

            foreach (CanvasBounds canvas in Canvases)
            foreach (ScenarioAnchor anchor in anchors)
            {
                foreach (float height in new[] { 120f, 220f, 260f, 340f, 500f })
                {
                    var placement = ScenarioGuideLayout.Place(canvas, anchor, 648f, height);

                    bool clearsBelow = placement.Y + height <= ScenarioGuideLayout.ManualIconBottomY(canvas);
                    bool clearsBeside = placement.X >= ScenarioGuideLayout.ManualIconRightX(canvas);
                    Assert.True(clearsBelow || clearsBeside, $"{Tag(canvas)} {anchor} h={height}");
                }
            }
        }

        [Fact]
        public void Guide_LeftColumnAnchorsStayAtTheDefaultMarginWhenTheyDoNotReachTheIcon()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                float left = canvas.Left + ScenarioGuideLayout.SideMargin;
                var placement = ScenarioGuideLayout.Place(canvas, ScenarioAnchor.BottomLeft, 648f, 260f);

                Assert.True(placement.Y + 260f <= ScenarioGuideLayout.ManualIconBottomY(canvas), Tag(canvas));
                Assert.Equal(left, placement.X);
            }
        }

        [Fact]
        public void Guide_PlayHudRightAnchorSitsBesideTheBottomRightHud()
        {
            foreach (CanvasBounds canvas in Canvases)
            {
                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.PlayGauge, true, out GuideBox gauge));
                Assert.True(ScenarioHighlightLayout.TryPlace(canvas, ScenarioHighlight.RecordIcon, true, out GuideBox record));

                var placement = ScenarioGuideLayout.Place(canvas, ScenarioAnchor.PlayHudRight, 600f, 300f);

                Assert.True(placement.Y >= gauge.Top, Tag(canvas));
                Assert.True(placement.X + 600f <= record.Left, Tag(canvas));
            }
        }

        [Fact]
        public void Highlight_RecordIconSitsAboveTheReportIconSlot()
        {
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.PlayGauge, true, out GuideBox gauge));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.RecordIcon, true, out GuideBox record));

            float reportTop = Wide.Bottom
                              + HudGeometry.HudIcon.ReportBottom(true)
                              + HudGeometry.HudIcon.Height;

            Assert.True(record.Bottom > gauge.Top);
            Assert.True(record.Bottom >= reportTop);
            Assert.True(record.Right <= gauge.Right);
        }

        [Fact]
        public void Highlight_ReportIconSitsBetweenTheGaugeAndTheRecordIcon()
        {
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.PlayGauge, true, out GuideBox gauge));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.ReportIcon, true, out GuideBox report));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.RecordIcon, true, out GuideBox record));

            Assert.True(report.Bottom > gauge.Top);
            Assert.True(report.Top <= record.Bottom);
            Assert.Equal(record.X, report.X);
            Assert.Equal(record.Width, report.Width);
            Assert.False(ScenarioHighlightLayout.IsLive(ScenarioHighlight.ReportIcon));
        }

        [Fact]
        public void Highlight_HudIconStackDropsToTheGaugeSlotWhenTheGaugeIsHidden()
        {
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.PlayGauge, true, out GuideBox gauge));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.ReportIcon, true, out GuideBox reportAbove));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.ReportIcon, false, out GuideBox report));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.RecordIcon, false, out GuideBox record));

            Assert.Equal(gauge.Bottom, report.Bottom);
            Assert.True(report.Bottom < reportAbove.Bottom);
            Assert.Equal(reportAbove.X, report.X);
            Assert.Equal(reportAbove.Width, report.Width);
            Assert.True(report.Top <= record.Bottom);
            Assert.Equal(report.X, record.X);
        }

        [Fact]
        public void Highlight_OnlyTheHudIconStackFollowsTheGaugeVisibility()
        {
            foreach (ScenarioHighlight highlight in Enum.GetValues(typeof(ScenarioHighlight)))
            {
                bool above = ScenarioHighlightLayout.TryPlace(Wide, highlight, true, out GuideBox a);
                bool below = ScenarioHighlightLayout.TryPlace(Wide, highlight, false, out GuideBox b);
                Assert.Equal(above, below);
                if (!above) continue;

                bool stack = highlight == ScenarioHighlight.ReportIcon
                             || highlight == ScenarioHighlight.RecordIcon;
                Assert.Equal(stack, a.Y != b.Y);
                Assert.Equal(a.X, b.X);
                Assert.Equal(a.Width, b.Width);
                Assert.Equal(a.Height, b.Height);
            }
        }

        [Fact]
        public void Highlight_PlayHudTargetsDoNotOverlapEachOther()
        {
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.WolfStatus, true, out GuideBox wolf));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.PlayGauge, true, out GuideBox gauge));

            Assert.True(wolf.Right < gauge.Left);
        }

        [Fact]
        public void Highlight_BottomButtonsDoNotOverlap()
        {
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.MapButton, true, out GuideBox map));
            Assert.True(ScenarioHighlightLayout.TryPlace(Wide, ScenarioHighlight.ChatLogButton, true, out GuideBox chat));

            Assert.True(map.Right < chat.Left);
        }

        [Fact]
        public void Highlight_RecapLinesMatchTheMinutes()
        {
            var data = new MeetingRecapData(new string[0], 777, 111, 999, 0);
            var lines = MeetingRecap.BuildLines(data);

            Assert.Equal(0, ScenarioHighlightLayout.RecapLineIndex(ScenarioHighlight.RecapTitle));
            Assert.Equal(4, lines.Count);
            Assert.Equal(Texts.Get(TextId.RecapDeathsNone), Body(lines, ScenarioHighlight.RecapDeaths));
            Assert.Contains("777", Body(lines, ScenarioHighlight.RecapLost));
            Assert.Contains("999", Body(lines, ScenarioHighlight.RecapHaul));
            Assert.Equal(Texts.Get(TextId.RecapBeaconNone), Body(lines, ScenarioHighlight.RecapBeacon));
        }

        private static string Body(IReadOnlyList<string> lines, ScenarioHighlight highlight)
            => lines[ScenarioHighlightLayout.RecapLineIndex(highlight) - 1];
    }
}
