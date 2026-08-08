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
            Assert.Equal(WWEventCodes.AssignRole, MessageCodes.AssignRole);
            Assert.Equal(WWEventCodes.RevealSelfRole, MessageCodes.RevealSelfRole);
            Assert.Equal(WWEventCodes.RevealTeammates, MessageCodes.RevealTeammates);
            Assert.Equal(WWEventCodes.PlayerDied, MessageCodes.PlayerDied);
            Assert.Equal(WWEventCodes.GameOver, MessageCodes.GameOver);
            Assert.Equal(WWEventCodes.GameStart, MessageCodes.GameStart);
            Assert.Equal(WWEventCodes.PhaseChanged, MessageCodes.PhaseChanged);
        }

        [Fact]
        public void ImplementedCodes_HaveExactValues()
        {
            Assert.Equal(160, MessageCodes.AssignRole);
            Assert.Equal(161, MessageCodes.RevealSelfRole);
            Assert.Equal(162, MessageCodes.RevealTeammates);
            Assert.Equal(168, MessageCodes.PlayerDied);
            Assert.Equal(169, MessageCodes.GameOver);
            Assert.Equal(170, MessageCodes.GameStart);
            Assert.Equal(172, MessageCodes.PhaseChanged);
        }

        [Fact]
        public void ReservedCodes_AreDefinedWithinRange()
        {
            foreach (byte code in new byte[]
                     {
                         MessageCodes.StartMeeting, MessageCodes.CastVote, MessageCodes.VoteProgress,
                         MessageCodes.MeetingResult, MessageCodes.RequestMeeting, MessageCodes.BeaconAudit,
                         MessageCodes.SyncPerkGauge, MessageCodes.RoleAction, MessageCodes.RoleState,
                     })
            {
                Assert.InRange(code, MessageCodes.MinCode, MessageCodes.MaxCode);
            }
        }

        [Fact]
        public void RolesCodes_AliasWWRolesCodes_SingleSource()
        {
            Assert.Equal(WWRolesCodes.BeaconAudit, MessageCodes.BeaconAudit);
            Assert.Equal(WWRolesCodes.SyncPerkGauge, MessageCodes.SyncPerkGauge);
            Assert.Equal(WWRolesCodes.RoleAction, MessageCodes.RoleAction);
            Assert.Equal(WWRolesCodes.RoleState, MessageCodes.RoleState);
            Assert.Equal(167, MessageCodes.BeaconAudit);
            Assert.Equal(171, MessageCodes.SyncPerkGauge);
            Assert.Equal(174, MessageCodes.RoleAction);
            Assert.Equal(175, MessageCodes.RoleState);
        }

        [Fact]
        public void MeetingCodes_HaveExactValues()
        {
            Assert.Equal(163, MessageCodes.StartMeeting);
            Assert.Equal(164, MessageCodes.CastVote);
            Assert.Equal(165, MessageCodes.VoteProgress);
            Assert.Equal(166, MessageCodes.MeetingResult);
            Assert.Equal(173, MessageCodes.RequestMeeting);
        }

        [Theory]
        [InlineData(160, true)]
        [InlineData(175, true)]
        [InlineData(168, true)]
        [InlineData(176, true)]
        [InlineData(180, true)]
        [InlineData(189, true)]
        [InlineData(190, true)]
        [InlineData(159, false)]
        [InlineData(191, false)]
        [InlineData(0, false)]
        [InlineData(200, false)]
        public void IsInRange_MatchesReservedBand(int code, bool expected)
        {
            Assert.Equal(expected, MessageCodes.IsInRange((byte)code));
        }

        [Fact]
        public void ConveneDenied_HasExactValue()
        {
            Assert.Equal(176, MessageCodes.ConveneDenied);
        }

        [Fact]
        public void ConveneDenied_IsInReservedRange()
        {
            Assert.InRange(MessageCodes.ConveneDenied, MessageCodes.MinCode, MessageCodes.MaxCode);
        }

        [Fact]
        public void ConveneDenied_NotClassifiedAsTargetOnlySecret()
        {
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ConveneDenied));
        }

        [Fact]
        public void ConveneDenied_NotClassifiedAsMasterInbound()
        {
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ConveneDenied));
        }

        [Fact]
        public void ConveneDenied_IsNotSecret()
        {
            Assert.False(MessageCodes.IsSecret(MessageCodes.ConveneDenied));
        }

        [Fact]
        public void ConveneDenied_Schema_IsByteReason()
        {
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.ConveneDenied));
        }

        [Fact]
        public void MaxCode_ExtendedTo190()
        {
            Assert.Equal(190, MessageCodes.MaxCode);
        }

        [Fact]
        public void CosmeticGrant_HasExactValue()
        {
            Assert.Equal(177, MessageCodes.CosmeticGrant);
        }

        [Fact]
        public void CosmeticGrant_IsInReservedRange()
        {
            Assert.True(MessageCodes.IsInRange(MessageCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_NotClassifiedAsTargetOnly()
        {
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_NotClassifiedAsMasterInbound()
        {
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_IsNotSecret()
        {
            Assert.False(MessageCodes.IsSecret(MessageCodes.CosmeticGrant));
        }

        [Fact]
        public void CosmeticGrant_Schema_IsActorsAndRarities()
        {
            Assert.Equal(new[] { typeof(int[]), typeof(byte[]) }, MessageCodes.Schema(MessageCodes.CosmeticGrant));
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
            Assert.Equal(expected, MessageCodes.IsTargetOnly((byte)code));
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
            Assert.Equal(expected, MessageCodes.IsMasterInbound((byte)code));
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
            Assert.Equal(expected, MessageCodes.IsSecret((byte)code));
        }

        [Fact]
        public void Schema_ImplementedCodes_MatchEventContract()
        {
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.AssignRole));
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.RevealSelfRole));
            Assert.Equal(new[] { typeof(int[]), typeof(byte[]) }, MessageCodes.Schema(MessageCodes.RevealTeammates));
            Assert.Equal(new[] { typeof(int), typeof(byte) }, MessageCodes.Schema(MessageCodes.PlayerDied));
            Assert.Equal(new[] { typeof(byte), typeof(int[]), typeof(byte[]) }, MessageCodes.Schema(MessageCodes.GameOver));
            Assert.Equal(
                new[] { typeof(long), typeof(int), typeof(byte), typeof(byte), typeof(int), typeof(byte), typeof(int[]) },
                MessageCodes.Schema(MessageCodes.GameStart));
            Assert.Equal(new[] { typeof(byte), typeof(long), typeof(long) }, MessageCodes.Schema(MessageCodes.PhaseChanged));
        }

        [Fact]
        public void Schema_MeetingCodes_MatchEventContract()
        {
            Assert.Equal(new[] { typeof(int), typeof(long), typeof(long), typeof(byte) }, MessageCodes.Schema(MessageCodes.StartMeeting));
            Assert.Equal(new[] { typeof(int) }, MessageCodes.Schema(MessageCodes.CastVote));
            Assert.Equal(new[] { typeof(int[]), typeof(long) }, MessageCodes.Schema(MessageCodes.VoteProgress));
            Assert.Equal(new[] { typeof(int), typeof(int[]), typeof(int[]) }, MessageCodes.Schema(MessageCodes.MeetingResult));
        }

        [Fact]
        public void Schema_RequestMeeting_CarriesConveneKind()
        {
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.RequestMeeting));
        }

        [Fact]
        public void Schema_UnknownCode_IsNull()
        {
            Assert.Null(MessageCodes.Schema(199));
            Assert.Null(MessageCodes.Schema(159));
        }

        [Fact]
        public void Schema_RolesCodes_MatchEventContract()
        {
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.BeaconAudit));

            Assert.Equal(
                new[] { typeof(int), typeof(byte), typeof(byte), typeof(byte), typeof(long), typeof(int[]) },
                MessageCodes.Schema(MessageCodes.SyncPerkGauge));

            Assert.Equal(
                new[] { typeof(byte), typeof(int), typeof(byte) },
                MessageCodes.Schema(MessageCodes.RoleAction));

            Assert.Equal(
                new[] { typeof(byte), typeof(int[]), typeof(long) },
                MessageCodes.Schema(MessageCodes.RoleState));
        }

        [Theory]
        [InlineData(171, true)]
        [InlineData(167, false)]
        [InlineData(175, false)]
        public void IsTargetOnly_ClassifiesRolesCodes(int code, bool expected)
        {
            Assert.Equal(expected, MessageCodes.IsTargetOnly((byte)code));
        }

        [Theory]
        [InlineData(174, true)]
        [InlineData(171, false)]
        [InlineData(175, false)]
        public void IsMasterInbound_ClassifiesRolesCodes(int code, bool expected)
        {
            Assert.Equal(expected, MessageCodes.IsMasterInbound((byte)code));
        }

        [Theory]
        [InlineData(171, true)]
        [InlineData(174, true)]
        [InlineData(167, false)]
        [InlineData(175, false)]
        public void IsSecret_ClassifiesRolesCodes(int code, bool expected)
        {
            Assert.Equal(expected, MessageCodes.IsSecret((byte)code));
        }

        [Fact]
        public void BombDetonation_HasExactValue()
        {
            Assert.Equal(180, MessageCodes.BombDetonation);
        }

        [Fact]
        public void BomberState_HasExactValue()
        {
            Assert.Equal(181, MessageCodes.BomberState);
        }

        [Fact]
        public void BombDetonation_IsInReservedRange()
        {
            Assert.True(MessageCodes.IsInRange(MessageCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsInReservedRange()
        {
            Assert.True(MessageCodes.IsInRange(MessageCodes.BomberState));
        }

        [Fact]
        public void BombDetonation_IsNotTargetOnly_BroadcastToAll()
        {
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsTargetOnly()
        {
            Assert.True(MessageCodes.IsTargetOnly(MessageCodes.BomberState));
        }

        [Fact]
        public void BombDetonation_IsSecret_ForLogRedaction()
        {
            Assert.True(MessageCodes.IsSecret(MessageCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsSecret()
        {
            Assert.True(MessageCodes.IsSecret(MessageCodes.BomberState));
        }

        [Fact]
        public void BombDetonation_IsNotMasterInbound()
        {
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_IsNotMasterInbound()
        {
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.BomberState));
        }

        [Fact]
        public void Schema_BombDetonation_TargetActorAndDetonateAtUnixMs()
        {
            Assert.Equal(new[] { typeof(int), typeof(long) }, MessageCodes.Schema(MessageCodes.BombDetonation));
        }

        [Fact]
        public void Schema_BomberState_FiveFields()
        {
            Assert.Equal(
                new[] { typeof(int), typeof(byte), typeof(byte), typeof(long), typeof(long) },
                MessageCodes.Schema(MessageCodes.BomberState));
        }

        [Fact]
        public void ScatterGroups_Is189_PublicBroadcast()
        {
            Assert.Equal(189, MessageCodes.ScatterGroups);
            Assert.True(MessageCodes.IsInRange(MessageCodes.ScatterGroups));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ScatterGroups));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ScatterGroups));
            Assert.False(MessageCodes.IsSecret(MessageCodes.ScatterGroups));
        }

        [Fact]
        public void ScatterGroups_Schema_IsParallelArrays()
        {
            Assert.Equal(new[] { typeof(int[]), typeof(byte[]) },
                MessageCodes.Schema(MessageCodes.ScatterGroups));
        }

        [Fact]
        public void ScatterGuardWindow_Is190_PublicBroadcast()
        {
            Assert.Equal(190, MessageCodes.ScatterGuardWindow);
            Assert.True(MessageCodes.IsInRange(MessageCodes.ScatterGuardWindow));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ScatterGuardWindow));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ScatterGuardWindow));
            Assert.False(MessageCodes.IsSecret(MessageCodes.ScatterGuardWindow));
        }

        [Fact]
        public void ScatterGuardWindow_Schema_IsGuardSeconds()
        {
            Assert.Equal(new[] { typeof(int) }, MessageCodes.Schema(MessageCodes.ScatterGuardWindow));
        }

        [Fact]
        public void ResultDigest_Is188_PublicBroadcast()
        {
            Assert.Equal(188, MessageCodes.ResultDigest);
            Assert.True(MessageCodes.IsInRange(MessageCodes.ResultDigest));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ResultDigest));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ResultDigest));
            Assert.False(MessageCodes.IsSecret(MessageCodes.ResultDigest));
        }

        [Fact]
        public void ResultDigest_Schema_IsFiveParallelArrays()
        {
            Assert.Equal(
                new[] { typeof(byte[]), typeof(int[]), typeof(int[]), typeof(int[]), typeof(int[]) },
                MessageCodes.Schema(MessageCodes.ResultDigest));
        }

        [Fact]
        public void ModIntegrityCodes_HaveExactContracts()
        {
            Assert.Equal(182, MessageCodes.ModManifestRequest);
            Assert.Equal(183, MessageCodes.ModManifestReport);
            Assert.Equal(184, MessageCodes.ModIntegritySnapshot);
            Assert.Equal(185, MessageCodes.ModIntegrityDetailRequest);
            Assert.Equal(186, MessageCodes.ModIntegrityDetailResponse);

            Assert.Equal(new[] { typeof(int), typeof(byte), typeof(string) },
                MessageCodes.Schema(MessageCodes.ModManifestRequest));
            Assert.Equal(new[]
            {
                typeof(int), typeof(byte), typeof(int), typeof(int), typeof(string),
                typeof(string[]), typeof(string[]), typeof(string[]), typeof(string[]),
            }, MessageCodes.Schema(MessageCodes.ModManifestReport));
            Assert.Equal(new[]
            {
                typeof(int), typeof(int), typeof(int), typeof(int[]), typeof(byte[]),
                typeof(byte[]), typeof(int[]),
            }, MessageCodes.Schema(MessageCodes.ModIntegritySnapshot));
            Assert.Equal(new[] { typeof(int), typeof(int) },
                MessageCodes.Schema(MessageCodes.ModIntegrityDetailRequest));
            Assert.Equal(new[]
            {
                typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                typeof(byte[]), typeof(string[]), typeof(string[]), typeof(string[]), typeof(string[]),
            }, MessageCodes.Schema(MessageCodes.ModIntegrityDetailResponse));
        }

        [Fact]
        public void ModIntegrityCodes_ArePublicAndOnlyReportAndDetailRequestAreMasterInbound()
        {
            for (byte code = 182; code <= 186; code++)
            {
                Assert.False(MessageCodes.IsSecret(code));
                Assert.False(MessageCodes.IsTargetOnly(code));
            }
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ModManifestRequest));
            Assert.True(MessageCodes.IsMasterInbound(MessageCodes.ModManifestReport));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ModIntegritySnapshot));
            Assert.True(MessageCodes.IsMasterInbound(MessageCodes.ModIntegrityDetailRequest));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ModIntegrityDetailResponse));
        }
    }
}
