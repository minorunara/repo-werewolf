using System;
using Werewolf.Core;

namespace Werewolf.Net
{
    public static class MessageCodes
    {
        public const byte MinCode = 160;

        public const byte MaxCode = 190;

        public const byte AssignRole = WWEventCodes.AssignRole;

        public const byte RevealSelfRole = WWEventCodes.RevealSelfRole;

        public const byte RevealTeammates = WWEventCodes.RevealTeammates;

        public const byte PlayerDied = WWEventCodes.PlayerDied;

        public const byte GameOver = WWEventCodes.GameOver;

        public const byte GameStart = WWEventCodes.GameStart;

        public const byte PhaseChanged = WWEventCodes.PhaseChanged;

        public const byte StartMeeting = 163;

        public const byte CastVote = 164;

        public const byte VoteProgress = 165;

        public const byte MeetingResult = 166;

        public const byte BeaconAudit = WWRolesCodes.BeaconAudit;

        public const byte SyncPerkGauge = WWRolesCodes.SyncPerkGauge;

        public const byte RequestMeeting = 173;

        public const byte RoleAction = WWRolesCodes.RoleAction;

        public const byte RoleState = WWRolesCodes.RoleState;

        public const byte ConveneDenied = 176;

        public const byte CurseCandidates = WWRolesCodes.CurseCandidates;

        public const byte MeetingCancelled = Core.WWMeetingCodes.MeetingCancelled;

        public const byte CosmeticGrant = 177;

        public const byte BombDetonation = 180;

        public const byte BomberState = 181;

        public const byte CheckmateReveal = WWCheckmateCodes.CheckmateReveal;

        public const byte ResultDigest = 188;

        public const byte ScatterGroups = 189;

        public const byte ScatterGuardWindow = 190;

        public const byte ModManifestRequest = 182;

        public const byte ModManifestReport = 183;

        public const byte ModIntegritySnapshot = 184;

        public const byte ModIntegrityDetailRequest = 185;

        public const byte ModIntegrityDetailResponse = 186;

        public static bool IsInRange(byte code) => code >= MinCode && code <= MaxCode;

        public static bool IsTargetOnly(byte code) =>
            code == AssignRole || code == RevealSelfRole || code == RevealTeammates ||
            code == SyncPerkGauge || code == CurseCandidates || code == BomberState;

        public static bool IsMasterInbound(byte code) =>
            code == CastVote || code == RequestMeeting || code == RoleAction ||
            code == ModManifestReport || code == ModIntegrityDetailRequest;

        public static bool IsSecret(byte code) =>
            IsTargetOnly(code) || code == CastVote || code == RoleAction || code == BombDetonation;

        public static Type[] Schema(byte code)
        {
            switch (code)
            {
                case AssignRole:      return new[] { typeof(byte) };
                case RevealSelfRole:  return new[] { typeof(byte) };
                case RevealTeammates: return new[] { typeof(int[]), typeof(byte[]) };
                case PlayerDied:      return new[] { typeof(int), typeof(byte) };
                case GameOver:        return new[] { typeof(byte), typeof(int[]), typeof(byte[]) };
                case GameStart:       return new[] { typeof(long), typeof(int), typeof(byte), typeof(byte), typeof(int), typeof(byte), typeof(int[]) };
                case PhaseChanged:    return new[] { typeof(byte), typeof(long), typeof(long) };
                case StartMeeting:    return new[] { typeof(int), typeof(long), typeof(long), typeof(byte) };
                case CastVote:        return new[] { typeof(int) };
                case VoteProgress:    return new[] { typeof(int[]), typeof(long) };
                case MeetingResult:   return new[] { typeof(int), typeof(int[]), typeof(int[]) };
                case RequestMeeting:  return new[] { typeof(byte) };
                case MeetingCancelled: return new[] { typeof(byte) };
                case BeaconAudit:     return new[] { typeof(byte) };
                case SyncPerkGauge:   return new[] { typeof(int), typeof(byte), typeof(byte), typeof(byte), typeof(long), typeof(int[]) };
                case RoleAction:      return new[] { typeof(byte), typeof(int), typeof(byte) };
                case RoleState:       return new[] { typeof(byte), typeof(int[]), typeof(long) };
                case CurseCandidates: return new[] { typeof(int[]) };
                case ConveneDenied:   return new[] { typeof(byte) };
                case CosmeticGrant:   return new[] { typeof(int[]), typeof(byte[]) };
                case BombDetonation:  return new[] { typeof(int), typeof(long) };
                case BomberState:     return new[] { typeof(int), typeof(byte), typeof(byte), typeof(long), typeof(long) };
                case CheckmateReveal: return new[] { typeof(int[]), typeof(long) };
                case ResultDigest:
                    return new[] { typeof(byte[]), typeof(int[]), typeof(int[]), typeof(int[]), typeof(int[]) };
                case ScatterGroups:   return new[] { typeof(int[]), typeof(byte[]) };
                case ScatterGuardWindow: return new[] { typeof(int) };
                case ModManifestRequest:
                    return new[] { typeof(int), typeof(byte), typeof(string) };
                case ModManifestReport:
                    return new[] { typeof(int), typeof(byte), typeof(int), typeof(int), typeof(string),
                        typeof(string[]), typeof(string[]), typeof(string[]), typeof(string[]) };
                case ModIntegritySnapshot:
                    return new[] { typeof(int), typeof(int), typeof(int), typeof(int[]), typeof(byte[]),
                        typeof(byte[]), typeof(int[]) };
                case ModIntegrityDetailRequest:
                    return new[] { typeof(int), typeof(int) };
                case ModIntegrityDetailResponse:
                    return new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                        typeof(byte[]), typeof(string[]), typeof(string[]), typeof(string[]), typeof(string[]) };
                default:              return null;
            }
        }
    }
}
