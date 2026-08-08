using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class IdRosterClientTests : IDisposable
    {
        private readonly List<string> _log = new List<string>();

        public IdRosterClientTests()
        {
            WLog.Sink = (line, secret) => _log.Add(line);
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void Apply_MapsActorsToIndexPlusOne()
        {
            var roster = new IdRosterClient();

            roster.Apply(new[] { 3, 7, -101 });

            Assert.True(roster.HasRoster);
            Assert.Equal(1, roster.IdOf(3));
            Assert.Equal(2, roster.IdOf(7));
            Assert.Equal(3, roster.IdOf(-101));
        }

        [Fact]
        public void IdOf_UnknownActorOrBeforeApply_ReturnsZero()
        {
            var roster = new IdRosterClient();

            Assert.False(roster.HasRoster);
            Assert.Equal(0, roster.IdOf(1));

            roster.Apply(new[] { 1, 2 });
            Assert.Equal(0, roster.IdOf(99));
        }

        [Fact]
        public void Apply_ReplacesEntireRoster()
        {
            var roster = new IdRosterClient();
            roster.Apply(new[] { 1, 2, 3 });

            roster.Apply(new[] { 5, 2 });

            Assert.Equal(0, roster.IdOf(1));
            Assert.Equal(0, roster.IdOf(3));
            Assert.Equal(1, roster.IdOf(5));
            Assert.Equal(2, roster.IdOf(2));
        }

        [Fact]
        public void Apply_NullOrEmpty_RejectedAndKeepsExisting()
        {
            var roster = new IdRosterClient();
            roster.Apply(new[] { 1, 2 });

            roster.Apply(null);
            roster.Apply(new int[0]);

            Assert.Equal(1, roster.IdOf(1));
            Assert.Equal(2, roster.IdOf(2));
            Assert.Contains(_log, line => line.Contains("id_roster_rejected"));
        }

        [Fact]
        public void Apply_DuplicateActor_RejectedAndKeepsExisting()
        {
            var roster = new IdRosterClient();
            roster.Apply(new[] { 1, 2 });

            roster.Apply(new[] { 3, 4, 3 });

            Assert.Equal(1, roster.IdOf(1));
            Assert.Equal(0, roster.IdOf(3));
            Assert.Contains(_log, line => line.Contains("id_roster_rejected"));
        }

        [Fact]
        public void Reset_ClearsRoster()
        {
            var roster = new IdRosterClient();
            roster.Apply(new[] { 1, 2 });

            roster.Reset();

            Assert.False(roster.HasRoster);
            Assert.Equal(0, roster.IdOf(1));
        }
    }
}
