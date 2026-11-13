using System;
using System.IO;
using Xunit;

namespace Werewolf.Tests
{
    public sealed class TeammateRoleBoundsAuditTests
    {
        [Fact]
        public void FellowWerewolfLookup_GuardsRoleArrayIndex()
        {
            string path = Path.Combine(
                AuditEnvironment.Meta("Werewolf.SourceDir"), "Game", "WerewolfDirector.cs");
            string source = File.ReadAllText(path);
            int start = source.IndexOf(
                "public bool IsFellowWerewolfActor", StringComparison.Ordinal);
            int end = source.IndexOf(
                "public bool IsDeadActorClient", start, StringComparison.Ordinal);

            Assert.True(start >= 0 && end > start,
                "IsFellowWerewolfActor のソース範囲を解決できない");
            string method = source.Substring(start, end - start);
            Assert.Contains("roles != null && i < roles.Length", method, StringComparison.Ordinal);
        }

        [Fact]
        public void RevealTeammates_NormalizesMismatchedParallelArrays()
        {
            string path = Path.Combine(
                AuditEnvironment.Meta("Werewolf.SourceDir"), "Game", "WerewolfDirector.Inbound.cs");
            string source = File.ReadAllText(path);

            Assert.Contains(
                "rr.Length == _roundMirror.KnownWerewolves.Length", source, StringComparison.Ordinal);
        }
    }
}
