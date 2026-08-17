using System.Collections.Generic;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayPlaybackTests
    {
        private static ReplaySegmentHeader Header(
            List<ReplayPlayerInfo> players = null,
            List<ReplayValuableInfo> vals = null,
            List<ReplayEpInfo> eps = null,
            int localActor = 1)
        {
            return new ReplaySegmentHeader
            {
                LevelName = "Level - Test",
                StartedAtIso = "2026-08-11T00:00:00+09:00",
                IsHost = true,
                LocalActor = localActor,
                Players = players ?? new List<ReplayPlayerInfo>
                {
                    new ReplayPlayerInfo { Actor = 1, ParticipantId = 1, Name = "Alice" },
                    new ReplayPlayerInfo { Actor = 2, ParticipantId = 2, Name = "Bob" },
                },
                Valuables = vals ?? new List<ReplayValuableInfo>(),
                ExtractionPoints = eps ?? new List<ReplayEpInfo>(),
            };
        }

        private static ReplayEntitySample P(int actor, float x, float y = 0f, float z = 0f)
            => new ReplayEntitySample(ReplayEntityKind.Player, actor, x, y, z);

        [Fact]
        public void FromRecorder_NoSegments_ReturnsNull()
        {
            Assert.Null(ReplayPlayback.FromRecorder(new ReplayRecorder()));
            Assert.Null(ReplayPlayback.FromRecorder(null));
        }

        [Fact]
        public void PlayerTrack_InterpolatesBetweenSamples()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 100.0);
            rec.Sample(100.0, new[] { P(1, 0f, 0f, 0f) });
            rec.Sample(100.5, new[] { P(1, 10f, 2f, -4f) });
            rec.EndSegment(101.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayPlayerEntry alice = pb.Players.Find(p => p.Actor == 1);
            Assert.Equal("Alice", alice.Name);

            Assert.True(pb.TryGetPos(alice.Track, 0.25, out float x, out float y, out float z));
            Assert.Equal(5f, x, 3);
            Assert.Equal(1f, y, 3);
            Assert.Equal(-2f, z, 3);
        }

        [Fact]
        public void PlayerTrack_GapBeyondThreshold_IsAbsent()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(0.0, new[] { P(1, 0f) });
            rec.Sample(10.0, new[] { P(1, 5f) });
            rec.EndSegment(12.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayPlayerEntry alice = pb.Players.Find(p => p.Actor == 1);

            Assert.False(pb.TryGetPos(alice.Track, 5.0, out _, out _, out _));
            Assert.True(pb.TryGetPos(alice.Track, 0.0, out _, out _, out _));
            Assert.True(pb.TryGetPos(alice.Track, 10.0, out _, out _, out _));
            Assert.True(pb.TryGetPos(alice.Track, 11.5, out _, out _, out _));
            Assert.False(pb.TryGetPos(alice.Track, 12.1 + ReplayPlayback.PresenceGapSec, out _, out _, out _));
        }

        [Fact]
        public void Valuable_HoldsHeaderPosition_AndDisappearsAtValGone()
        {
            var vals = new List<ReplayValuableInfo>
            {
                new ReplayValuableInfo { Id = 10, Name = "Vase", Dollars = 1000, X = 3f, Y = 0f, Z = 7f },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals: vals), 0.0);
            rec.Sample(0.25, new[] { P(1, 0f) });
            rec.EndSegment(1.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayEntityTrack vase = pb.Valuables.Find(v => v.Id == 10);
            Assert.Equal("Vase", vase.Name);
            Assert.Equal(0.25, vase.GoneAtT, 3);

            Assert.True(pb.TryGetPos(vase, 0.1, out float x, out _, out float z));
            Assert.Equal(3f, x, 3);
            Assert.Equal(7f, z, 3);
            Assert.False(pb.TryGetPos(vase, 0.3, out _, out _, out _));
        }

        [Fact]
        public void CorpseTrack_BindsToPlayer_AndHoldsPosition()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(0.0, new[] { P(1, 0f), P(2, 1f) });
            rec.NoteEvent(0.5, "death", ("a", 2), ("cause", "Weapon"));
            rec.Sample(0.75, new[]
            {
                P(1, 0f),
                P(2, 1f),
                new ReplayEntitySample(ReplayEntityKind.Corpse, 2, 5f, 0f, -3f),
            });
            rec.Sample(1.0, new[]
            {
                P(1, 0f),
                P(2, 1f),
                new ReplayEntitySample(ReplayEntityKind.Corpse, 2, 8f, 0f, -6f),
            });
            rec.EndSegment(2.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayPlayerEntry bob = pb.Players.Find(p => p.Actor == 2);
            Assert.NotNull(bob.CorpseTrack);
            Assert.Equal(ReplayEntityKind.Corpse, bob.CorpseTrack.Kind);
            Assert.Equal(0.5, bob.DeathT, 3);

            Assert.False(pb.TryGetPos(bob.CorpseTrack, 0.2, out _, out _, out _));

            Assert.True(pb.TryGetPos(bob.CorpseTrack, 0.9, out float x, out _, out float z));
            Assert.Equal(5f, x, 3);
            Assert.Equal(-3f, z, 3);

            Assert.True(pb.TryGetPos(bob.CorpseTrack, 60.0, out x, out _, out z));
            Assert.Equal(8f, x, 3);
            Assert.Equal(-6f, z, 3);

            Assert.Null(pb.Players.Find(p => p.Actor == 1).CorpseTrack);
        }

        [Fact]
        public void CorpseTrack_LeadingOffMapSpawnSamples_ArePruned()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(0.0, new[] { P(2, 1f, 0f, 0f) });
            rec.NoteEvent(0.5, "death", ("a", 2), ("cause", "Weapon"));
            rec.Sample(0.75, new[]
            {
                P(2, 1f, 0f, 0f),
                new ReplayEntitySample(ReplayEntityKind.Corpse, 2, 0f, 3000f, 0f),
            });
            rec.Sample(1.0, new[]
            {
                P(2, 1f, 0f, 0f),
                new ReplayEntitySample(ReplayEntityKind.Corpse, 2, 1.2f, 0f, 0.3f),
            });
            rec.EndSegment(2.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayPlayerEntry bob = pb.Players.Find(p => p.Actor == 2);
            Assert.Equal(1, bob.CorpseTrack.Count);
            Assert.False(pb.TryGetPos(bob.CorpseTrack, 0.8, out _, out _, out _));
            Assert.True(pb.TryGetPos(bob.CorpseTrack, 1.0, out float x, out _, out float z));
            Assert.Equal(1.2f, x, 3);
            Assert.Equal(0.3f, z, 3);
        }

        [Fact]
        public void AnnouncedEvent_DepartsPlayerFromBoard()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(0.0, new[] { P(1, 0f), P(2, 1f) });
            rec.NoteEvent(0.5, "death", ("a", 2), ("cause", "Weapon"));
            rec.Sample(0.75, new[]
            {
                P(1, 0f),
                P(2, 1f),
                new ReplayEntitySample(ReplayEntityKind.Corpse, 2, 5f, 0f, -3f),
            });
            rec.NoteEvent(10.0, "announced", ("actors", new[] { 2 }));
            rec.EndSegment(20.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayPlayerEntry bob = pb.Players.Find(p => p.Actor == 2);
            Assert.Equal(10.0, bob.AnnouncedT, 3);
            Assert.False(bob.IsDepartedAt(9.9));
            Assert.True(bob.IsDepartedAt(10.0));

            ReplayPlayerEntry alice = pb.Players.Find(p => p.Actor == 1);
            Assert.Equal(double.PositiveInfinity, alice.AnnouncedT);
            Assert.False(alice.IsDepartedAt(double.MaxValue));

            Assert.True(pb.TryGetPos(bob.CorpseTrack, 15.0, out float x, out _, out _));
            Assert.Equal(5f, x, 3);
        }

        [Fact]
        public void DeathMarks_WindowFromDeathToMeetingEnd_PositionFromCorpse()
        {
            var players = new List<ReplayPlayerInfo>
            {
                new ReplayPlayerInfo { Actor = 1, ParticipantId = 1, Name = "A" },
                new ReplayPlayerInfo { Actor = 2, ParticipantId = 2, Name = "B" },
                new ReplayPlayerInfo { Actor = 3, ParticipantId = 3, Name = "C" },
                new ReplayPlayerInfo { Actor = 4, ParticipantId = 4, Name = "D" },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(players: players), 0.0);
            rec.Sample(1.0, new[] { P(2, 2f, 0f, 3f), P(4, 7f, 0f, 8f) });
            rec.NoteEvent(5.0, "death", ("a", 2), ("cause", "Weapon"));
            rec.Sample(5.25, new[]
            {
                P(2, 2.5f, 0f, 3.5f),
                new ReplayEntitySample(ReplayEntityKind.Corpse, 2, 999f, 3000f, 999f),
            });
            rec.Sample(5.75, new[] { P(2, 50f, 0f, 50f) });
            rec.NoteEvent(10.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(12.0, "meet_warp");
            rec.NoteEvent(15.0, "death", ("a", 3), ("cause", "Vote"));
            rec.NoteEvent(20.0, "phase", ("to", "Play"));
            rec.NoteEvent(25.0, "death", ("a", 4), ("cause", "Weapon"));
            rec.EndSegment(30.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Equal(2, pb.DeathMarks.Count);

            ReplayDeathMark m0 = pb.DeathMarks[0];
            Assert.Equal(2, m0.Actor);
            Assert.Equal(5.0, m0.T, 3);
            Assert.Equal(20.0, m0.HideT, 3);
            Assert.Equal(2.5f, m0.X, 3);
            Assert.Equal(3.5f, m0.Z, 3);
            Assert.False(m0.IsVisibleAt(4.9));
            Assert.True(m0.IsVisibleAt(5.0));
            Assert.True(m0.IsVisibleAt(15.0));
            Assert.False(m0.IsVisibleAt(20.0));

            ReplayDeathMark m1 = pb.DeathMarks[1];
            Assert.Equal(4, m1.Actor);
            Assert.Equal(25.0, m1.T, 3);
            Assert.Equal(double.PositiveInfinity, m1.HideT);
            Assert.Equal(7f, m1.X, 3);
            Assert.Equal(8f, m1.Z, 3);
            Assert.True(m1.IsVisibleAt(29.9));
        }

        [Fact]
        public void DeathMarks_UnclosedMeeting_HidesAtDuration()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(1.0, new[] { P(2, 2f, 0f, 3f) });
            rec.NoteEvent(5.0, "death", ("a", 2), ("cause", "Weapon"));
            rec.NoteEvent(10.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(12.0, "meet_warp");
            rec.EndSegment(30.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.DeathMarks);
            Assert.Equal(30.0, pb.DeathMarks[0].HideT, 3);
        }

        [Fact]
        public void Item_HoldsLastPosition_AndDisappearsAtItemGone()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEntity(0.0, ReplayEntityKind.Item, 30, "Item Grenade");
            rec.Sample(0.0, new[]
            {
                P(1, 0f),
                new ReplayEntitySample(ReplayEntityKind.Item, 30, 4f, 0f, -2f),
            });
            rec.Sample(0.25, new[] { P(1, 0f) });
            rec.EndSegment(60.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayEntityTrack grenade = pb.Items.Find(i => i.Id == 30);
            Assert.Equal(0.25, grenade.GoneAtT, 3);

            Assert.True(pb.TryGetPos(grenade, 0.1, out float x, out _, out float z));
            Assert.Equal(4f, x, 3);
            Assert.Equal(-2f, z, 3);
            Assert.False(pb.TryGetPos(grenade, 0.25, out _, out _, out _));
            Assert.False(pb.TryGetPos(grenade, 50.0, out _, out _, out _));
        }

        [Fact]
        public void Meetings_StartAtWarp_ClosedAndUnclosed()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEvent(10.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(13.0, "meet_warp");
            rec.NoteEvent(40.0, "phase", ("to", "Play"));
            rec.NoteEvent(80.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(83.0, "meet_warp");
            rec.EndSegment(90.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Equal(2, pb.Meetings.Count);
            Assert.Equal((13.0, 40.0), pb.Meetings[0]);
            Assert.Equal((83.0, 90.0), pb.Meetings[1]);
        }

        [Fact]
        public void Meetings_WithoutWarp_ProduceNoRange()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEvent(10.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(12.0, "meet_cancel", ("reason", 0));
            rec.NoteEvent(13.0, "phase", ("to", "Play"));
            rec.NoteEvent(50.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(53.0, "meet_warp");
            rec.NoteEvent(70.0, "phase", ("to", "Play"));
            rec.EndSegment(90.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.Meetings);
            Assert.Equal((53.0, 70.0), pb.Meetings[0]);
        }

        [Fact]
        public void Meetings_DuplicateWarp_KeepsFirst()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEvent(10.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(13.0, "meet_warp");
            rec.NoteEvent(15.0, "meet_warp");
            rec.NoteEvent(40.0, "phase", ("to", "Play"));
            rec.EndSegment(90.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.Meetings);
            Assert.Equal((13.0, 40.0), pb.Meetings[0]);
        }

        [Fact]
        public void DeathMarks_DuringConveneCountdown_AreKept()
        {
            var players = new List<ReplayPlayerInfo>
            {
                new ReplayPlayerInfo { Actor = 2, ParticipantId = 2, Name = "Bob" },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(players: players), 0.0);
            rec.Sample(1.0, new[] { P(2, 2f, 0f, 3f) });
            rec.NoteEvent(10.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(11.0, "death", ("a", 2), ("cause", "Weapon"));
            rec.Sample(11.25, new[] { P(2, 4f, 0f, 5f) });
            rec.NoteEvent(13.0, "meet_warp");
            rec.NoteEvent(40.0, "phase", ("to", "Play"));
            rec.EndSegment(50.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.DeathMarks);
            Assert.Equal(11.0, pb.DeathMarks[0].T);
            Assert.Equal(40.0, pb.DeathMarks[0].HideT);
            Assert.Equal(4f, pb.DeathMarks[0].X);
            Assert.Equal(5f, pb.DeathMarks[0].Z);
        }

        [Fact]
        public void DeathsAndGameOverRoles_AreApplied()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEvent(30.0, "death", ("a", 2), ("cause", "WolfKill"));
            rec.NoteEvent(60.0, "gameover",
                ("team", 1),
                ("actors", new[] { 1, 2 }),
                ("roles", new[] { 0, 1 }));
            rec.EndSegment(61.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Equal((byte)1, pb.WinnerTeam);
            Assert.Single(pb.Deaths);
            Assert.Equal(30.0, pb.Deaths[0].T, 3);

            ReplayPlayerEntry bob = pb.Players.Find(p => p.Actor == 2);
            Assert.True(bob.IsAliveAt(29.9));
            Assert.False(bob.IsAliveAt(30.0));
            Assert.True(bob.IsWerewolfSide);
            ReplayPlayerEntry alice = pb.Players.Find(p => p.Actor == 1);
            Assert.False(alice.IsWerewolfSide);
        }

        [Fact]
        public void EpTimeline_SeededByHeader_TracksStateChanges()
        {
            var eps = new List<ReplayEpInfo>
            {
                new ReplayEpInfo { Id = 5, State = 0, StateName = "Idle", X = 1f, Y = 0f, Z = 2f },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(eps: eps), 0.0);
            rec.NoteEpState(20.0, 5, 2, "Active");
            rec.NoteEpState(50.0, 5, 5, "Complete");
            rec.EndSegment(60.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayEpEntry ep = pb.Eps[0];
            Assert.Equal(1, ep.Number);
            Assert.Equal("Idle", ep.StateAt(10.0).Name);
            Assert.Equal("Active", ep.StateAt(20.0).Name);
            Assert.Equal("Complete", ep.StateAt(59.0).Name);
        }

        [Fact]
        public void CartDuplicateItem_IsHidden_ByNameAndInitialDistance()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.NoteEntity(0.0, ReplayEntityKind.Cart, 100, "Item Cart Medium");
            rec.NoteEntity(0.0, ReplayEntityKind.Item, 200, "Item Cart Medium");
            rec.NoteEntity(0.0, ReplayEntityKind.Item, 201, "Item Cart Medium");
            rec.NoteEntity(0.0, ReplayEntityKind.Item, 202, "Item Drone");
            rec.Sample(0.0, new[]
            {
                new ReplayEntitySample(ReplayEntityKind.Cart, 100, 0f, 0f, 0f),
                new ReplayEntitySample(ReplayEntityKind.Item, 200, 0.5f, 0f, 0f),
                new ReplayEntitySample(ReplayEntityKind.Item, 201, 30f, 0f, 0f),
                new ReplayEntitySample(ReplayEntityKind.Item, 202, 0.2f, 0f, 0f),
            });
            rec.EndSegment(1.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.True(pb.Items.Find(i => i.Id == 200).HiddenDuplicate);
            Assert.False(pb.Items.Find(i => i.Id == 201).HiddenDuplicate);
            Assert.False(pb.Items.Find(i => i.Id == 202).HiddenDuplicate);
        }

        [Fact]
        public void TrailInto_ReturnsRangeInclusive()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            for (int i = 0; i <= 10; i++)
            {
                rec.Sample(i * 1.0, new[] { P(1, i) });
            }
            rec.EndSegment(11.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            ReplayPlayerEntry alice = pb.Players.Find(p => p.Actor == 1);
            var points = new List<ReplayTrailPoint>();
            pb.TrailInto(alice.Track, 3.0, 7.0, points);
            Assert.Equal(5, points.Count);
            Assert.Equal(3f, points[0].X, 3);
            Assert.Equal(7f, points[points.Count - 1].X, 3);
        }

        private static List<ReplayValuableInfo> Vals(params (int Id, int Vid, int Dollars, float X)[] items)
        {
            var list = new List<ReplayValuableInfo>();
            foreach ((int id, int vid, int dollars, float x) in items)
            {
                list.Add(new ReplayValuableInfo
                {
                    Id = id, Vid = vid, Name = "Vase" + id, Dollars = dollars, X = x, Y = 0f, Z = 0f,
                });
            }
            return list;
        }

        private static List<ReplayEpInfo> Ep(int id, float x)
            => new List<ReplayEpInfo>
            {
                new ReplayEpInfo { Id = id, State = 1, StateName = "Idle", X = x, Y = 0f, Z = 0f },
            };

        [Fact]
        public void BaseDollars_SumsHeaderValuables()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals: Vals((10, 910, 1000, 0f), (11, 911, 2500, 20f))), 0.0);
            rec.EndSegment(1.0);

            Assert.Equal(3500, ReplayPlayback.FromRecorder(rec).BaseDollars);
        }

        [Fact]
        public void ValueDecrease_ProducesDamagePopup()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals: Vals((10, 910, 1000, 3f))), 0.0);
            rec.Sample(0.0, new[] { new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 3f, 0f, 0f) });
            rec.NoteValuableValue(5.0, 10, 600);
            rec.Sample(5.0, new[] { new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 3f, 0f, 0f) });
            rec.EndSegment(10.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.Popups);
            Assert.Equal(ReplayValueEventKind.Damage, pb.Popups[0].Kind);
            Assert.Equal(400, pb.Popups[0].Amount);
            Assert.Equal(5.0, pb.Popups[0].T, 3);
            Assert.Equal(3f, pb.Popups[0].X, 3);
        }

        [Fact]
        public void ValGone_WithDestroyedLoss_IsLostWithLedgerAmount()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals: Vals((10, 910, 1000, 3f))), 0.0);
            rec.Sample(0.0, new[] { new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 3f, 0f, 0f) });
            rec.NoteLoss(4.0, 910, 1000, isOrb: false, destroyed: true);
            rec.Sample(4.0, new ReplayEntitySample[0]);
            rec.EndSegment(10.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.Popups);
            Assert.Equal(ReplayValueEventKind.Lost, pb.Popups[0].Kind);
            Assert.Equal(1000, pb.Popups[0].Amount);
        }

        [Fact]
        public void ValGone_InHaulWhileEpPressing_IsDeliver()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals: Vals((10, 910, 1000, 3f)), eps: Ep(5, 3f)), 0.0);
            rec.Sample(0.0, new[] { new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 3f, 0f, 0f) });
            rec.NoteHaulIds(1.0, new[] { 10 });
            rec.NoteEpState(2.0, 5, 6, "Extracting");
            rec.Sample(3.0, new ReplayEntitySample[0]);
            rec.EndSegment(10.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.Popups);
            Assert.Equal(ReplayValueEventKind.Deliver, pb.Popups[0].Kind);
            Assert.Equal(1000, pb.Popups[0].Amount);
            Assert.Equal(1000, pb.DeliveredDollarsAt(3.0));
            Assert.Equal(0, pb.DeliveredDollarsAt(2.9));
        }

        [Fact]
        public void ValGone_Absorbed_ProducesNoPopup()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals: Vals((10, 910, 1000, 3f))), 0.0);
            rec.Sample(0.0, new[] { new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 3f, 0f, 0f) });
            rec.Sample(4.0, new ReplayEntitySample[0]);
            rec.EndSegment(10.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Empty(pb.Popups);
        }

        [Fact]
        public void ValGone_WithoutDestroyConfirmation_ProducesNoPopup()
        {
            ReplaySegmentHeader header = Header(
                vals: Vals((10, 0, 1000, 1f), (11, 0, 700, 30f)), eps: Ep(5, 0f));
            header.IsHost = false;
            var rec = new ReplayRecorder();
            rec.BeginSegment(header, 0.0);
            rec.Sample(0.0, new[]
            {
                new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 1f, 0f, 0f),
                new ReplayEntitySample(ReplayEntityKind.Valuable, 11, 30f, 0f, 0f),
            });
            rec.NoteEpState(1.0, 5, 6, "Extracting");
            rec.Sample(2.0, new ReplayEntitySample[0]);
            rec.EndSegment(10.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Empty(pb.Popups);
        }

        [Fact]
        public void LostDollarsAt_UsesHostLossEvents()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(vals: Vals((10, 910, 5000, 3f))), 0.0);
            rec.NoteLoss(10.0, 910, 300, isOrb: false, destroyed: false);
            rec.NoteLoss(20.0, 0, 200, isOrb: false, destroyed: false);
            rec.EndSegment(30.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Equal(0, pb.LostDollarsAt(9.9));
            Assert.Equal(300, pb.LostDollarsAt(10.0));
            Assert.Equal(500, pb.LostDollarsAt(25.0));
        }

        [Fact]
        public void LostDollarsAt_WithoutLossEvents_DerivesFromPopups()
        {
            ReplaySegmentHeader header = Header(vals: Vals((10, 910, 1000, 30f)), eps: Ep(5, 0f));
            header.IsHost = false;
            var rec = new ReplayRecorder();
            rec.BeginSegment(header, 0.0);
            rec.Sample(0.0, new[] { new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 30f, 0f, 0f) });
            rec.NoteValuableValue(5.0, 10, 600);
            rec.Sample(5.0, new[] { new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 30f, 0f, 0f) });
            rec.Sample(8.0, new ReplayEntitySample[0]);
            rec.EndSegment(10.0);
            Assert.True(rec.ApplyLossLedgerWire(new object[]
            {
                1, new[] { 0 }, new[] { 910 }, new[] { 600 }, new byte[] { 2 },
            }));

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Equal(0, pb.LostDollarsAt(4.9));
            Assert.Equal(400, pb.LostDollarsAt(5.0));
            Assert.Equal(1000, pb.LostDollarsAt(8.0));
            Assert.Equal(0, pb.DeliveredDollarsAt(10.0));
        }

        [Fact]
        public void Duration_UsesEndTick_WhenClosed()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.Sample(0.0, new[] { P(1, 0f) });
            rec.EndSegment(123.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Equal(123.0, pb.Duration, 3);
        }

        [Fact]
        public void FromRecorder_UsesLastSegment()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(Header(), 0.0);
            rec.EndSegment(10.0);
            var second = Header();
            second.LevelName = "Level - Second";
            rec.BeginSegment(second, 100.0);
            rec.EndSegment(150.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Equal("Level - Second", pb.Header.LevelName);
            Assert.Equal(50.0, pb.Duration, 3);
        }
    }
}
