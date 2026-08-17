using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayRecorderTests
    {
        private static ReplaySegmentHeader Header(
            List<ReplayValuableInfo> vals = null, List<ReplayEpInfo> eps = null)
        {
            return new ReplaySegmentHeader
            {
                LevelName = "Level - Test",
                StartedAtIso = "2026-08-11T00:00:00+09:00",
                IsHost = true,
                LocalActor = 1,
                Valuables = vals ?? new List<ReplayValuableInfo>(),
                ExtractionPoints = eps ?? new List<ReplayEpInfo>(),
            };
        }

        private static ReplayEntitySample Player(int id, float x = 0f)
            => new ReplayEntitySample(ReplayEntityKind.Player, id, x, 0f, 0f);

        private static ReplayEntitySample Valuable(int id, float x)
            => new ReplayEntitySample(ReplayEntityKind.Valuable, id, x, 0f, 0f);

        private static ReplayEntitySample Item(int id, float x)
            => new ReplayEntitySample(ReplayEntityKind.Item, id, x, 0f, 0f);

        private static ReplayEntitySample Corpse(int actor, float x)
            => new ReplayEntitySample(ReplayEntityKind.Corpse, actor, x, 0f, 0f);

        [Fact]
        public void ShouldSample_FirstImmediately_ThenGatedByInterval()
        {
            var rec = new ReplayRecorder();
            Assert.False(rec.ShouldSample(100.0));

            rec.BeginSegment(Header(), 100.0);
            Assert.True(rec.ShouldSample(100.0));

            rec.Sample(100.0, new[] { Player(1) });
            Assert.False(rec.ShouldSample(100.1));
            Assert.False(rec.ShouldSample(100.24));
            Assert.True(rec.ShouldSample(100.25));
        }

        [Fact]
        public void Sample_FixedKindsAlwaysRecorded_ValuablesGatedByMoveThreshold()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);

            rec.Sample(0.0, new[] { Player(1), Valuable(10, 0f) });
            Assert.Equal(2, rec.PositionEntryCount);

            rec.Sample(0.25, new[] { Player(1), Valuable(10, 0.05f) });
            Assert.Equal(3, rec.PositionEntryCount);

            rec.Sample(0.5, new[] { Player(1), Valuable(10, 0.2f) });
            Assert.Equal(5, rec.PositionEntryCount);
        }

        [Fact]
        public void Sample_CorpseOnChange_SerializedAsD()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);

            rec.Sample(0.0, new[] { Player(1), Corpse(2, 3f) });
            Assert.Equal(2, rec.PositionEntryCount);

            rec.Sample(0.25, new[] { Player(1), Corpse(2, 3.05f) });
            Assert.Equal(3, rec.PositionEntryCount);

            rec.Sample(0.5, new[] { Player(1), Corpse(2, 3.2f) });
            Assert.Equal(5, rec.PositionEntryCount);

            string[] posLines = rec.ToJsonLines().Where(l => l.Contains("\"k\":\"p\"")).ToArray();
            Assert.Contains("\"D\":[[2,3,0,0]]", posLines[0]);
            Assert.DoesNotContain("\"D\"", posLines[1]);
            Assert.Contains("\"D\":[[2,3.2,0,0]]", posLines[2]);
        }

        [Fact]
        public void Sample_ValuableDisappearance_EmitsValGone()
        {
            var vals = new List<ReplayValuableInfo>
            {
                new ReplayValuableInfo { Id = 10, Name = "Vase", Dollars = 1000 },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals), 0.0);

            rec.Sample(0.0, new[] { Player(1), Valuable(10, 0f) });
            Assert.Equal(0, rec.EventCount);

            rec.Sample(0.25, new[] { Player(1) });
            Assert.Equal(1, rec.EventCount);
            string gone = rec.ToJsonLines().Single(l => l.Contains("\"val_gone\""));
            Assert.Contains("\"v\":10", gone);
        }

        [Fact]
        public void Sample_ItemDisappearance_EmitsItemGone()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);

            rec.Sample(0.0, new[] { Player(1), Item(30, 0f) });
            Assert.Equal(0, rec.EventCount);

            rec.Sample(0.25, new[] { Player(1), Item(30, 0.05f) });
            Assert.Equal(0, rec.EventCount);

            rec.Sample(0.5, new[] { Player(1) });
            Assert.Equal(1, rec.EventCount);
            string gone = rec.ToJsonLines().Single(l => l.Contains("\"item_gone\""));
            Assert.Contains("\"i\":30", gone);

            rec.Sample(0.75, new[] { Player(1) });
            Assert.Equal(1, rec.EventCount);
        }

        [Fact]
        public void Sample_ItemPresence_DoesNotLeakAcrossSegments()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(0.0, new[] { Item(30, 0f) });
            rec.BeginSegment(Header(), 100.0);
            rec.Sample(100.0, System.Array.Empty<ReplayEntitySample>());
            Assert.Equal(0, rec.EventCount);
        }

        [Fact]
        public void NoteValuableValue_DedupsAgainstHeaderSeedAndPreviousValue()
        {
            var vals = new List<ReplayValuableInfo>
            {
                new ReplayValuableInfo { Id = 10, Name = "Vase", Dollars = 1000 },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals), 0.0);

            rec.NoteValuableValue(0.25, 10, 1000);
            Assert.Equal(0, rec.EventCount);

            rec.NoteValuableValue(0.5, 10, 700);
            rec.NoteValuableValue(0.75, 10, 700);
            Assert.Equal(1, rec.EventCount);

            rec.NoteValuableValue(1.0, 99, 250);
            Assert.Equal(2, rec.EventCount);
        }

        [Fact]
        public void NoteEpState_DedupsAgainstHeaderSeed()
        {
            var eps = new List<ReplayEpInfo>
            {
                new ReplayEpInfo { Id = 5, State = 0, StateName = "Idle" },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(eps: eps), 0.0);

            rec.NoteEpState(0.25, 5, 0, "Idle");
            Assert.Equal(0, rec.EventCount);

            rec.NoteEpState(0.5, 5, 3, "Complete");
            rec.NoteEpState(0.75, 5, 3, "Complete");
            Assert.Equal(1, rec.EventCount);
            string line = rec.ToJsonLines().Single(l => l.Contains("\"ep_state\""));
            Assert.Contains("\"ep\":5", line);
            Assert.Contains("\"n\":\"Complete\"", line);
            Assert.Single(Regex.Matches(line, "\"e\":"));
        }

        [Fact]
        public void ToJsonLines_MapMesh_EmittedAfterHeader_OmittedWhenAbsent()
        {
            var rec = new ReplayRecorder();
            ReplaySegmentHeader header = Header();
            header.Map = new ReplayMapMesh
            {
                Vertices = new[] { 0f, 1.234f, -2.5f, 10f, 4f, 0f, 0f, 0f, 10f },
                Triangles = new[] { 0, 1, 2 },
            };
            rec.BeginSegment(header, 0.0);
            rec.EndSegment(1.0);
            string[] lines = rec.ToJsonLines().ToArray();
            Assert.Equal(4, lines.Length);
            Assert.StartsWith("{\"k\":\"seg\"", lines[0]);
            Assert.Equal(
                "{\"k\":\"map\",\"verts\":[0,1.23,-2.5,10,4,0,0,0,10],\"tris\":[0,1,2]}",
                lines[1]);
            Assert.Equal("{\"k\":\"ledger\",\"n\":[]}", lines[2]);

            rec.Reset();
            rec.BeginSegment(Header(), 0.0);
            rec.EndSegment(1.0);
            Assert.DoesNotContain(rec.ToJsonLines(), l => l.Contains("\"k\":\"map\""));
        }

        [Fact]
        public void AttachMapImage_AttachesToLastSegment_AndEmitsMapImgLine()
        {
            var rec = new ReplayRecorder();
            rec.AttachMapImage(new ReplayMapImage { Png = new byte[] { 1 } });

            rec.BeginSegment(Header(), 0.0);
            rec.EndSegment(10.0);
            rec.BeginSegment(Header(), 100.0);
            rec.EndSegment(150.0);
            rec.AttachMapImage(new ReplayMapImage
            {
                Width = 1600,
                Height = 960,
                MinX = -42.5f,
                MaxX = 42.5f,
                MinZ = -25.62f,
                MaxZ = 25.62f,
                Png = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            });

            var lines = rec.ToJsonLines().ToList();
            var imgs = lines.Where(l => l.Contains("\"k\":\"mapimg\"")).ToList();
            Assert.Single(imgs);
            Assert.Equal(
                "{\"k\":\"mapimg\",\"w\":1600,\"h\":960,\"x0\":-42.5,\"x1\":42.5,"
                + "\"z0\":-25.62,\"z1\":25.62,\"png\":\"iVBORw==\"}",
                imgs[0]);
            int seg2 = lines.FindLastIndex(l => l.StartsWith("{\"k\":\"seg\""));
            Assert.Equal(seg2 + 1, lines.IndexOf(imgs[0]));
        }

        [Fact]
        public void ToJsonLines_MapImage_OmittedWhenPngEmpty()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.EndSegment(1.0);
            rec.AttachMapImage(new ReplayMapImage());
            Assert.DoesNotContain(rec.ToJsonLines(), l => l.Contains("\"k\":\"mapimg\""));
        }

        [Fact]
        public void NoteEntity_DedupsByKindAndId()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEntity(0.0, ReplayEntityKind.Enemy, 7, "Gnome");
            rec.NoteEntity(0.25, ReplayEntityKind.Enemy, 7, "Gnome");
            rec.NoteEntity(0.5, ReplayEntityKind.Item, 7, "Flashlight");
            Assert.Equal(2, rec.ToJsonLines().Count(l => l.Contains("\"k\":\"ent\"")));
        }

        [Fact]
        public void NoteEvent_AfterEnd_WithinGrace_AppendsToLastSegment()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.EndSegment(10.0);

            rec.NoteEvent(12.0, "gameover", ("team", 1));
            Assert.Equal(1, rec.EventCount);

            rec.NoteEvent(16.0, "late", ("x", 1));
            Assert.Equal(1, rec.EventCount);

            var lines = rec.ToJsonLines().ToList();
            int over = lines.FindIndex(l => l.Contains("\"gameover\""));
            int end = lines.FindIndex(l => l.Contains("\"segend\""));
            Assert.True(over >= 0 && end > over);
        }

        [Fact]
        public void NoteEvent_OutsideAnySegment_IsDropped()
        {
            var rec = new ReplayRecorder();
            rec.NoteEvent(0.0, "death", ("a", 1));
            Assert.Equal(0, rec.EventCount);
            Assert.Empty(rec.ToJsonLines());
        }

        [Fact]
        public void CapacityDegrade_DoublesIntervalThenStopsSampling()
        {
            var rec = new ReplayRecorder(degradeEntries1: 2, degradeEntries2: 4, hardCapEntries: 6);
            rec.BeginSegment(Header(), 0.0);
            Assert.Equal(0.25, rec.CurrentSampleIntervalSec);

            rec.Sample(0.0, new[] { Player(1), Player(2) });
            Assert.Equal(0.5, rec.CurrentSampleIntervalSec);
            Assert.False(rec.ShouldSample(0.3));
            Assert.True(rec.ShouldSample(0.5));

            rec.Sample(0.5, new[] { Player(1), Player(2) });
            Assert.Equal(1.0, rec.CurrentSampleIntervalSec);

            rec.Sample(1.5, new[] { Player(1), Player(2) });
            Assert.False(rec.ShouldSample(100.0));

            rec.NoteEvent(2.0, "death", ("a", 1));
            Assert.Equal(1, rec.EventCount);
        }

        [Fact]
        public void BeginSegment_WhileOpen_EndsPreviousFirst()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.BeginSegment(Header(), 100.0);
            Assert.Equal(2, rec.SegmentCount);
            Assert.True(rec.SegmentOpen);
            Assert.Equal(1, rec.ToJsonLines().Count(l => l.Contains("\"segend\"")));
        }

        [Fact]
        public void SegmentTimestamps_AreRelativeToSegmentStart()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 1000.0);
            rec.Sample(1012.345, new[] { Player(1) });
            string pos = rec.ToJsonLines().Single(l => l.Contains("\"k\":\"p\""));
            Assert.Contains("\"t\":12.35", pos);
        }

        [Fact]
        public void Reset_DropsEverything()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(0.0, new[] { Player(1) });
            rec.NoteEvent(1.0, "death", ("a", 1));
            rec.Reset();

            Assert.Equal(0, rec.SegmentCount);
            Assert.False(rec.SegmentOpen);
            Assert.Equal(0, rec.PositionEntryCount);
            Assert.Equal(0, rec.EventCount);
            Assert.Empty(rec.ToJsonLines());
        }

        [Fact]
        public void NoteLoss_EmitsEventWithVidAndDestroyed_AndAccumulatesLedger()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteLoss(1.0, vid: 501, dollars: 300, isOrb: false, destroyed: false);
            rec.NoteLoss(2.0, vid: 501, dollars: 700, isOrb: false, destroyed: true);
            rec.NoteLoss(3.0, vid: 0, dollars: 120, isOrb: false, destroyed: false);

            string[] losses = rec.ToJsonLines().Where(l => l.Contains("\"loss\"")).ToArray();
            Assert.Equal(3, losses.Length);
            Assert.Contains("\"$\":300", losses[0]);
            Assert.Contains("\"vid\":501", losses[0]);
            Assert.Contains("\"d\":false", losses[0]);
            Assert.Contains("\"d\":true", losses[1]);
            Assert.Contains("\"vid\":0", losses[2]);

            rec.EndSegment(10.0);
            string ledger = rec.ToJsonLines().Single(l => l.Contains("\"k\":\"ledger\""));
            Assert.Equal(
                "{\"k\":\"ledger\",\"n\":[[501,300,0,0],[501,700,0,1],[0,120,0,0]]}",
                ledger);
        }

        [Fact]
        public void NoteLoss_AfterEnd_WithinGrace_LandsInLastSegmentLedger()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.EndSegment(10.0);
            rec.NoteLoss(12.0, vid: 7, dollars: 100, isOrb: true, destroyed: true);
            rec.NoteLoss(16.1, vid: 8, dollars: 200, isOrb: false, destroyed: true);

            string ledger = rec.ToJsonLines().Single(l => l.Contains("\"k\":\"ledger\""));
            Assert.Equal("{\"k\":\"ledger\",\"n\":[[7,100,1,1]]}", ledger);
        }

        [Fact]
        public void HostLedgerLine_OmittedWhileSegmentOpen()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteLoss(1.0, vid: 5, dollars: 100, isOrb: false, destroyed: true);
            Assert.DoesNotContain(rec.ToJsonLines(), l => l.Contains("\"k\":\"ledger\""));
        }

        [Fact]
        public void NoteHaulIds_EmitsOnChangeOnly_WithAddAndDel()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);

            rec.NoteHaulIds(0.25, new[] { 10, 11 });
            rec.NoteHaulIds(0.5, new[] { 10, 11 });
            rec.NoteHaulIds(0.75, new[] { 11, 12 });
            rec.NoteHaulIds(1.0, System.Array.Empty<int>());

            string[] hauls = rec.ToJsonLines().Where(l => l.Contains("\"haul\"")).ToArray();
            Assert.Equal(3, hauls.Length);
            Assert.Contains("\"add\":[10,11]", hauls[0]);
            Assert.Contains("\"del\":[]", hauls[0]);
            Assert.Contains("\"add\":[12]", hauls[1]);
            Assert.Contains("\"del\":[10]", hauls[1]);
            Assert.Contains("\"add\":[]", hauls[2]);
            Assert.Contains("\"del\":[11,12]", hauls[2]);
        }

        [Fact]
        public void NoteHaulIds_MembershipDoesNotLeakAcrossSegments()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteHaulIds(0.25, new[] { 10 });
            rec.BeginSegment(Header(), 100.0);
            rec.NoteHaulIds(100.25, new[] { 10 });
            Assert.Equal(2, rec.ToJsonLines().Count(l => l.Contains("\"add\":[10]")));
        }

        [Fact]
        public void LossLedgerWire_RoundTrip_GuestEmitsLedgerLinesWithCoverage()
        {
            var host = new ReplayRecorder();
            host.BeginSegment(Header(), 0.0);
            host.NoteLoss(1.0, vid: 501, dollars: 300, isOrb: false, destroyed: true);
            host.BeginSegment(Header(), 100.0);
            object[] wire = host.BuildLossLedgerWire();
            Assert.NotNull(wire);
            Assert.Equal(2, (int)wire[0]);

            var guest = new ReplayRecorder();
            ReplaySegmentHeader h1 = Header(); h1.IsHost = false;
            ReplaySegmentHeader h2 = Header(); h2.IsHost = false;
            guest.BeginSegment(h1, 0.0);
            guest.BeginSegment(h2, 100.0);
            guest.EndSegment(200.0);
            Assert.True(guest.ApplyLossLedgerWire(wire));

            string[] ledgers = guest.ToJsonLines().Where(l => l.Contains("\"k\":\"ledger\"")).ToArray();
            Assert.Equal(2, ledgers.Length);
            Assert.Equal("{\"k\":\"ledger\",\"n\":[[501,300,0,1]]}", ledgers[0]);
            Assert.Equal("{\"k\":\"ledger\",\"n\":[]}", ledgers[1]);
        }

        [Fact]
        public void ApplyLossLedgerWire_RejectsMalformedPayloads()
        {
            var rec = new ReplayRecorder();
            Assert.False(rec.ApplyLossLedgerWire(null));
            Assert.False(rec.ApplyLossLedgerWire(new object[] { 1, new int[1], new int[1], new int[1] }));
            Assert.False(rec.ApplyLossLedgerWire(new object[] { 0, new int[0], new int[0], new int[0], new byte[0] }));
            Assert.False(rec.ApplyLossLedgerWire(new object[] { 1, new[] { 0, 1 }, new[] { 5 }, new[] { 100 }, new byte[] { 0 } }));
            Assert.False(rec.ApplyLossLedgerWire(new object[] { 1, new[] { 1 }, new[] { 5 }, new[] { 100 }, new byte[] { 0 } }));
            Assert.Equal(0, rec.HostLedgerSegmentCount);
        }

        [Fact]
        public void BuildLossLedgerWire_NoSegments_ReturnsNull()
        {
            Assert.Null(new ReplayRecorder().BuildLossLedgerWire());
        }

        [Fact]
        public void Reset_DropsHaulAndHostLedger()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteHaulIds(0.25, new[] { 10 });
            rec.EndSegment(1.0);
            Assert.True(rec.ApplyLossLedgerWire(
                new object[] { 1, new int[0], new int[0], new int[0], new byte[0] }));
            rec.Reset();
            Assert.Equal(0, rec.HostLedgerSegmentCount);
            Assert.Empty(rec.ToJsonLines());
        }

        [Fact]
        public void NoteEntity_And_Header_CarryVid()
        {
            var vals = new List<ReplayValuableInfo>
            {
                new ReplayValuableInfo { Id = 10, Name = "Vase", Dollars = 1000, Vid = 9001 },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals), 0.0);
            rec.NoteEntity(0.5, ReplayEntityKind.Valuable, 20, "Orb", vid: 9002);
            rec.NoteEntity(0.5, ReplayEntityKind.Enemy, 7, "Gnome");

            var lines = rec.ToJsonLines().ToList();
            Assert.Contains("\"vid\":9001", lines.Single(l => l.StartsWith("{\"k\":\"seg\"")));
            Assert.Contains("\"vid\":9002", lines.Single(l => l.Contains("\"id\":20")));
            Assert.DoesNotContain("\"vid\"", lines.Single(l => l.Contains("\"id\":7")));
        }

        [Fact]
        public void ToJsonLines_HeaderFirst_RecordsInAppendOrder()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEntity(0.0, ReplayEntityKind.Enemy, 7, "Gnome");
            rec.Sample(0.0, new[] { Player(1) });
            rec.NoteEvent(1.0, "death", ("a", 1));
            rec.EndSegment(2.0);

            var lines = rec.ToJsonLines().ToList();
            Assert.Equal(6, lines.Count);
            Assert.StartsWith("{\"k\":\"seg\"", lines[0]);
            Assert.Contains("\"k\":\"ledger\"", lines[1]);
            Assert.Contains("\"k\":\"ent\"", lines[2]);
            Assert.Contains("\"k\":\"p\"", lines[3]);
            Assert.Contains("\"k\":\"ev\"", lines[4]);
            Assert.StartsWith("{\"k\":\"segend\"", lines[5]);
        }
    }
}
