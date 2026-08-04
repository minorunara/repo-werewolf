using System;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class NetEventCodesTests
    {

        [Fact]
        public void ImplementedCodes_AliasWWEventCodes_SingleSource()
        {
            Assert.Equal(WWEventCodes.AssignRole, EventCodes.AssignRole);
            Assert.Equal(WWEventCodes.RevealSelfRole, EventCodes.RevealSelfRole);
            Assert.Equal(WWEventCodes.RevealTeammates, EventCodes.RevealTeammates);
            Assert.Equal(WWEventCodes.PlayerDied, EventCodes.PlayerDied);
            Assert.Equal(WWEventCodes.GameOver, EventCodes.GameOver);
            Assert.Equal(WWEventCodes.GameStart, EventCodes.GameStart);
            Assert.Equal(WWEventCodes.PhaseChanged, EventCodes.PhaseChanged);
        }

        [Fact]
        public void ImplementedCodes_HaveExactValues()
        {
            Assert.Equal(160, EventCodes.AssignRole);
            Assert.Equal(161, EventCodes.RevealSelfRole);
            Assert.Equal(162, EventCodes.RevealTeammates);
            Assert.Equal(168, EventCodes.PlayerDied);
            Assert.Equal(169, EventCodes.GameOver);
            Assert.Equal(170, EventCodes.GameStart);
            Assert.Equal(172, EventCodes.PhaseChanged);
        }

        [Fact]
        public void ReservedCodes_AreDefinedWithinRange()
        {
            foreach (byte code in new byte[]
                     {
                         EventCodes.StartMeeting, EventCodes.CastVote, EventCodes.VoteProgress,
                         EventCodes.MeetingResult, EventCodes.RequestMeeting, EventCodes.BeaconAudit,
                         EventCodes.SyncPerkGauge, EventCodes.RoleAction, EventCodes.RoleState,
                     })
            {
                Assert.InRange(code, EventCodes.MinCode, EventCodes.MaxCode);
            }
        }

        [Fact]
        public void RolesCodes_AliasWWRolesCodes_SingleSource()
        {
            Assert.Equal(WWRolesCodes.BeaconAudit, EventCodes.BeaconAudit);
            Assert.Equal(WWRolesCodes.SyncPerkGauge, EventCodes.SyncPerkGauge);
            Assert.Equal(WWRolesCodes.RoleAction, EventCodes.RoleAction);
            Assert.Equal(WWRolesCodes.RoleState, EventCodes.RoleState);
            Assert.Equal(167, EventCodes.BeaconAudit);
            Assert.Equal(171, EventCodes.SyncPerkGauge);
            Assert.Equal(174, EventCodes.RoleAction);
            Assert.Equal(175, EventCodes.RoleState);
        }

        [Fact]
        public void MeetingCodes_HaveExactValues()
        {
            Assert.Equal(163, EventCodes.StartMeeting);
            Assert.Equal(164, EventCodes.CastVote);
            Assert.Equal(165, EventCodes.VoteProgress);
            Assert.Equal(166, EventCodes.MeetingResult);
            Assert.Equal(173, EventCodes.RequestMeeting);
        }

        [Theory]
        [InlineData(160, true)]
        [InlineData(175, true)]
        [InlineData(168, true)]
        [InlineData(176, true)]
        [InlineData(180, true)]
        [InlineData(189, true)]
        [InlineData(159, false)]
        [InlineData(190, false)]
        [InlineData(0, false)]
        [InlineData(200, false)]
        public void IsInRange_MatchesReservedBand(int code, bool expected)
        {
            Assert.Equal(expected, EventCodes.IsInRange((byte)code));
        }

        [Fact]
        public void ConveneDenied_HasExactValue()
        {
            Assert.Equal(176, EventCodes.ConveneDenied);
        }

        [Fact]
        public void ConveneDenied_IsInReservedRange()
        {
            Assert.InRange(EventCodes.ConveneDenied, EventCodes.MinCode, EventCodes.MaxCode);
        }

        [Fact]
        public void ConveneDenied_NotClassifiedAsTargetOnlySecret()
        {
            Assert.False(EventCodes.IsTargetOnly(EventCodes.ConveneDenied));
        }

        [Fact]
        public void ConveneDenied_NotClassifiedAsMasterInbound()
        {
            Assert.False(EventCodes.IsMasterInbound(EventCodes.ConveneDenied));
        }

        [Fact]
        public void ConveneDenied_IsNotSecret()
        {
            Assert.False(EventCodes.IsSecret(EventCodes.ConveneDenied));
        }

        [Fact]
        public void ConveneDenied_Schema_IsByteReason()
        {
            Assert.Equal(new[] { typeof(byte) }, EventCodes.Schema(EventCodes.ConveneDenied));
        }

        [Fact]
        public void MaxCode_ExtendedTo189()
        {
            Assert.Equal(189, EventCodes.MaxCode);
        }

        [Fact]
        public void CosmeticGrant_HasExactValue()
        {
            Assert.Equal(177, EventCodes.CosmeticGrant);
        }

        [Fact]
        public void CosmeticGrant_IsInReservedRange()
        {
            Assert.True(EventCodes.IsInRange(EventCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_NotClassifiedAsTargetOnly()
        {
            Assert.False(EventCodes.IsTargetOnly(EventCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_NotClassifiedAsMasterInbound()
        {
            Assert.False(EventCodes.IsMasterInbound(EventCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_IsNotSecret()
        {
            Assert.False(EventCodes.IsSecret(EventCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_Schema_IsActorsAndRarities()
        {
            Assert.Equal(new[] { typeof(int[]), typeof(byte[]) }, EventCodes.Schema(EventCodes.CosmeticGrant));
        }

        [Theory]
        [InlineData(160, true)]
        [InlineData(161, true)]
        [InlineData(162, true)]
        [InlineData(168, false)]
        [InlineData(169, false)]
        [InlineData(170, false)]
        [InlineData(172, false)]
        public void IsTargetOnly_ClassifiesSecrecy(int code, bool expected)
        {
            Assert.Equal(expected, EventCodes.IsTargetOnly((byte)code));
        }

        [Theory]
        [InlineData(164, true)]
        [InlineData(173, true)]
        [InlineData(163, false)]
        [InlineData(165, false)]
        [InlineData(166, false)]
        [InlineData(160, false)]
        [InlineData(172, false)]
        public void IsMasterInbound_ClassifiesMasterOnlyCodes(int code, bool expected)
        {
            Assert.Equal(expected, EventCodes.IsMasterInbound((byte)code));
        }

        [Theory]
        [InlineData(160, true)]
        [InlineData(161, true)]
        [InlineData(162, true)]
        [InlineData(164, true)]
        [InlineData(163, false)]
        [InlineData(165, false)]
        [InlineData(166, false)]
        [InlineData(173, false)]
        [InlineData(168, false)]
        public void IsSecret_ClassifiesSecretLoggableCodes(int code, bool expected)
        {
            Assert.Equal(expected, EventCodes.IsSecret((byte)code));
        }

        [Fact]
        public void Schema_ImplementedCodes_MatchEventContract()
        {
            Assert.Equal(new[] { typeof(byte) }, EventCodes.Schema(EventCodes.AssignRole));
            Assert.Equal(new[] { typeof(byte) }, EventCodes.Schema(EventCodes.RevealSelfRole));
            Assert.Equal(new[] { typeof(int[]), typeof(byte[]) }, EventCodes.Schema(EventCodes.RevealTeammates));
            Assert.Equal(new[] { typeof(int), typeof(byte) }, EventCodes.Schema(EventCodes.PlayerDied));
            Assert.Equal(new[] { typeof(byte), typeof(int[]), typeof(byte[]) }, EventCodes.Schema(EventCodes.GameOver));
            Assert.Equal(
                new[] { typeof(long), typeof(int), typeof(byte), typeof(byte), typeof(int), typeof(byte) },
                EventCodes.Schema(EventCodes.GameStart));
            Assert.Equal(new[] { typeof(byte), typeof(long), typeof(long) }, EventCodes.Schema(EventCodes.PhaseChanged));
        }

        [Fact]
        public void Schema_MeetingCodes_MatchEventContract()
        {
            Assert.Equal(new[] { typeof(int), typeof(long), typeof(long), typeof(byte) }, EventCodes.Schema(EventCodes.StartMeeting));
            Assert.Equal(new[] { typeof(int) }, EventCodes.Schema(EventCodes.CastVote));
            Assert.Equal(new[] { typeof(int[]), typeof(long) }, EventCodes.Schema(EventCodes.VoteProgress));
            Assert.Equal(new[] { typeof(int), typeof(int[]), typeof(int[]) }, EventCodes.Schema(EventCodes.MeetingResult));
        }

        [Fact]
        public void Schema_RequestMeeting_CarriesConveneKind()
        {
            Assert.Equal(new[] { typeof(byte) }, EventCodes.Schema(EventCodes.RequestMeeting));
        }

        [Fact]
        public void Schema_UnknownCode_IsNull()
        {
            Assert.Null(EventCodes.Schema(199));
            Assert.Null(EventCodes.Schema(159));
        }

        [Fact]
        public void Schema_RolesCodes_MatchEventContract()
        {
            Assert.Equal(new[] { typeof(byte) }, EventCodes.Schema(EventCodes.BeaconAudit));

            Assert.Equal(
                new[] { typeof(int), typeof(byte), typeof(byte), typeof(byte), typeof(long), typeof(int[]) },
                EventCodes.Schema(EventCodes.SyncPerkGauge));

            Assert.Equal(
                new[] { typeof(byte), typeof(int), typeof(byte) },
                EventCodes.Schema(EventCodes.RoleAction));

            Assert.Equal(
                new[] { typeof(byte), typeof(int[]), typeof(long) },
                EventCodes.Schema(EventCodes.RoleState));
        }

        [Theory]
        [InlineData(171, true)]
        [InlineData(167, false)]
        [InlineData(175, false)]
        public void IsTargetOnly_ClassifiesRolesCodes(int code, bool expected)
        {
            Assert.Equal(expected, EventCodes.IsTargetOnly((byte)code));
        }

        [Theory]
        [InlineData(174, true)]
        [InlineData(171, false)]
        [InlineData(175, false)]
        public void IsMasterInbound_ClassifiesRolesCodes(int code, bool expected)
        {
            Assert.Equal(expected, EventCodes.IsMasterInbound((byte)code));
        }

        [Theory]
        [InlineData(171, true)]
        [InlineData(174, true)]
        [InlineData(167, false)]
        [InlineData(175, false)]
        public void IsSecret_ClassifiesRolesCodes(int code, bool expected)
        {
            Assert.Equal(expected, EventCodes.IsSecret((byte)code));
        }

        [Fact]
        public void BombDetonation_HasExactValue()
        {
            Assert.Equal(180, EventCodes.BombDetonation);
        }

        [Fact]
        public void BomberState_HasExactValue()
        {
            Assert.Equal(181, EventCodes.BomberState);
        }

        [Fact]
        public void BombDetonation_IsInReservedRange()
        {
            Assert.True(EventCodes.IsInRange(EventCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsInReservedRange()
        {
            Assert.True(EventCodes.IsInRange(EventCodes.BomberState));
        }

        [Fact]
        public void BombDetonation_IsNotTargetOnly_BroadcastToAll()
        {
            Assert.False(EventCodes.IsTargetOnly(EventCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsTargetOnly()
        {
            Assert.True(EventCodes.IsTargetOnly(EventCodes.BomberState));
        }

        [Fact]
        public void BombDetonation_IsSecret_ForLogRedaction()
        {
            Assert.True(EventCodes.IsSecret(EventCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsSecret()
        {
            Assert.True(EventCodes.IsSecret(EventCodes.BomberState));
        }

        [Fact]
        public void BombDetonation_IsNotMasterInbound()
        {
            Assert.False(EventCodes.IsMasterInbound(EventCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsNotMasterInbound()
        {
            Assert.False(EventCodes.IsMasterInbound(EventCodes.BomberState));
        }

        [Fact]
        public void Schema_BombDetonation_TargetActorAndDetonateAtUnixMs()
        {
            Assert.Equal(new[] { typeof(int), typeof(long) }, EventCodes.Schema(EventCodes.BombDetonation));
        }

        [Fact]
        public void Schema_BomberState_FiveFields()
        {
            Assert.Equal(
                new[] { typeof(int), typeof(byte), typeof(byte), typeof(long), typeof(long) },
                EventCodes.Schema(EventCodes.BomberState));
        }

        [Fact]
        public void Schema_ReservedFutureCodes_ReturnNull()
        {
            Assert.Null(EventCodes.Schema(189));
        }

        [Fact]
        public void ResultDigest_Is188_PublicBroadcast()
        {
            Assert.Equal(188, EventCodes.ResultDigest);
            Assert.True(EventCodes.IsInRange(EventCodes.ResultDigest));
            Assert.False(EventCodes.IsTargetOnly(EventCodes.ResultDigest));
            Assert.False(EventCodes.IsMasterInbound(EventCodes.ResultDigest));
            Assert.False(EventCodes.IsSecret(EventCodes.ResultDigest));
        }

        [Fact]
        public void ResultDigest_Schema_IsFiveParallelArrays()
        {
            Assert.Equal(
                new[] { typeof(byte[]), typeof(int[]), typeof(int[]), typeof(int[]), typeof(int[]) },
                EventCodes.Schema(EventCodes.ResultDigest));
        }

        [Fact]
        public void ModIntegrityCodes_HaveExactContracts()
        {
            Assert.Equal(182, EventCodes.ModManifestRequest);
            Assert.Equal(183, EventCodes.ModManifestReport);
            Assert.Equal(184, EventCodes.ModIntegritySnapshot);
            Assert.Equal(185, EventCodes.ModIntegrityDetailRequest);
            Assert.Equal(186, EventCodes.ModIntegrityDetailResponse);

            Assert.Equal(new[] { typeof(int), typeof(byte), typeof(string) },
                EventCodes.Schema(EventCodes.ModManifestRequest));
            Assert.Equal(new[]
            {
                typeof(int), typeof(byte), typeof(int), typeof(int), typeof(string),
                typeof(string[]), typeof(string[]), typeof(string[]), typeof(string[]),
            }, EventCodes.Schema(EventCodes.ModManifestReport));
            Assert.Equal(new[]
            {
                typeof(int), typeof(int), typeof(int), typeof(int[]), typeof(byte[]),
                typeof(byte[]), typeof(int[]),
            }, EventCodes.Schema(EventCodes.ModIntegritySnapshot));
            Assert.Equal(new[] { typeof(int), typeof(int) },
                EventCodes.Schema(EventCodes.ModIntegrityDetailRequest));
            Assert.Equal(new[]
            {
                typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                typeof(byte[]), typeof(string[]), typeof(string[]), typeof(string[]), typeof(string[]),
            }, EventCodes.Schema(EventCodes.ModIntegrityDetailResponse));
        }

        [Fact]
        public void ModIntegrityCodes_ArePublicAndOnlyReportAndDetailRequestAreMasterInbound()
        {
            for (byte code = 182; code <= 186; code++)
            {
                Assert.False(EventCodes.IsSecret(code));
                Assert.False(EventCodes.IsTargetOnly(code));
            }
            Assert.False(EventCodes.IsMasterInbound(EventCodes.ModManifestRequest));
            Assert.True(EventCodes.IsMasterInbound(EventCodes.ModManifestReport));
            Assert.False(EventCodes.IsMasterInbound(EventCodes.ModIntegritySnapshot));
            Assert.True(EventCodes.IsMasterInbound(EventCodes.ModIntegrityDetailRequest));
            Assert.False(EventCodes.IsMasterInbound(EventCodes.ModIntegrityDetailResponse));
        }
    }
}
