using System;
using System.Linq;
using System.Reflection;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class NetEventCodesTests
    {
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
        public void RolesCodes_HaveExactValues()
        {
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

        [Fact]
        public void IsInRange_AcceptsEveryDefinedCode_AndRejectsOutsideTheBand()
        {
            var codes = typeof(MessageCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(byte))
                .Select(f => new { f.Name, Value = (byte)f.GetRawConstantValue() })
                .ToArray();

            Assert.NotEmpty(codes);
            foreach (var c in codes)
            {
                Assert.True(MessageCodes.IsInRange(c.Value), c.Name + " が帯域外");
            }

            Assert.False(MessageCodes.IsInRange((byte)(MessageCodes.MinCode - 1)));
            Assert.False(MessageCodes.IsInRange((byte)(MessageCodes.MaxCode + 1)));
            Assert.False(MessageCodes.IsInRange(0));
            Assert.False(MessageCodes.IsInRange(255));
        }

        [Fact]
        public void ConveneDenied_Is176_PublicTargetDelivery()
        {
            Assert.Equal(176, MessageCodes.ConveneDenied);
            Assert.True(MessageCodes.IsInRange(MessageCodes.ConveneDenied));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ConveneDenied));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ConveneDenied));
            Assert.False(MessageCodes.IsSecret(MessageCodes.ConveneDenied));
        }

        [Fact]
        public void ConveneDenied_Schema_IsByteReason()
        {
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.ConveneDenied));
        }

        [Fact]
        public void ChatStampRequest_Is193_MasterInbound()
        {
            Assert.Equal(193, MessageCodes.ChatStampRequest);
            Assert.True(MessageCodes.IsInRange(MessageCodes.ChatStampRequest));
            Assert.True(MessageCodes.IsMasterInbound(MessageCodes.ChatStampRequest));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ChatStampRequest));
            Assert.False(MessageCodes.IsSecret(MessageCodes.ChatStampRequest));
        }

        [Fact]
        public void ChatStamp_Is194_PublicBroadcast()
        {
            Assert.Equal(194, MessageCodes.ChatStamp);
            Assert.True(MessageCodes.IsInRange(MessageCodes.ChatStamp));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ChatStamp));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ChatStamp));
            Assert.False(MessageCodes.IsSecret(MessageCodes.ChatStamp));
        }

        [Fact]
        public void ChatStampCodes_Schemas_CarryIdOnly()
        {
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.ChatStampRequest));
            Assert.Equal(new[] { typeof(int), typeof(byte) }, MessageCodes.Schema(MessageCodes.ChatStamp));
        }

        [Fact]
        public void CosmeticGrant_Is177_PublicBroadcast()
        {
            Assert.Equal(177, MessageCodes.CosmeticGrant);
            Assert.True(MessageCodes.IsInRange(MessageCodes.CosmeticGrant));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.CosmeticGrant));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.CosmeticGrant));
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
        public void BombDetonation_Is180_BroadcastButRedactedInLogs()
        {
            Assert.Equal(180, MessageCodes.BombDetonation);
            Assert.True(MessageCodes.IsInRange(MessageCodes.BombDetonation));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.BombDetonation));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.BombDetonation));
            Assert.True(MessageCodes.IsSecret(MessageCodes.BombDetonation));
        }

        [Fact]
        public void BomberState_Is181_TargetOnlySecret()
        {
            Assert.Equal(181, MessageCodes.BomberState);
            Assert.True(MessageCodes.IsInRange(MessageCodes.BomberState));
            Assert.True(MessageCodes.IsTargetOnly(MessageCodes.BomberState));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.BomberState));
            Assert.True(MessageCodes.IsSecret(MessageCodes.BomberState));
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
        public void ReplayLossLedger_Is191_PublicBroadcast()
        {
            Assert.Equal(191, MessageCodes.ReplayLossLedger);
            Assert.True(MessageCodes.IsInRange(MessageCodes.ReplayLossLedger));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.ReplayLossLedger));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.ReplayLossLedger));
            Assert.False(MessageCodes.IsSecret(MessageCodes.ReplayLossLedger));
        }

        [Fact]
        public void ReplayLossLedger_Schema_IsSegCountAndFourParallelArrays()
        {
            Assert.Equal(
                new[] { typeof(int), typeof(int[]), typeof(int[]), typeof(int[]), typeof(byte[]) },
                MessageCodes.Schema(MessageCodes.ReplayLossLedger));
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
