using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Game
{
    internal sealed class ClientSettingsMirror
    {
        internal bool MinimapHideEnabled;

        internal bool CatPossible;

        internal bool DebugSession;

        internal ValuableMapMode ValuableMapMode = ValuableMapMode.MeetingSync;

        internal NecroVoiceMode NecroVoiceMode = NecroVoiceMode.NonWerewolfDead;

        internal int? ExtraJumpCount;
        internal int? ConveneSuppressStartSec;
        internal int? ConveneSuppressAfterSec;
        internal int? HealIntervalSec;
        internal bool? OutfitChangeAllowed;

        internal int[] BombPack;

        internal int[] ShamanPack;

        internal void Reset()
        {
            MinimapHideEnabled = false;
            CatPossible = false;
            DebugSession = false;
            ValuableMapMode = ValuableMapMode.MeetingSync;
            NecroVoiceMode = NecroVoiceMode.NonWerewolfDead;
            ExtraJumpCount = null;
            ConveneSuppressStartSec = null;
            ConveneSuppressAfterSec = null;
            HealIntervalSec = null;
            OutfitChangeAllowed = null;
            BombPack = null;
            ShamanPack = null;
        }
    }

    internal sealed class ClientRoundMirror
    {
        internal Role? LocalRole;

        internal int[] KnownWerewolves;

        internal byte[] KnownTeammateRoles;

        internal readonly Dictionary<int, DeathCause> DeathMirror = new Dictionary<int, DeathCause>();

        internal Dictionary<int, string> DisplayNameCache;

        internal long RoundEndUnixMs;

        internal int WerewolfCount;

        internal int RevealDelaySec;

        internal long GameStartUnixMs;

        internal long LastMeetingEndUnixMs;

        internal void Reset()
        {
            LocalRole = null;
            KnownWerewolves = null;
            KnownTeammateRoles = null;
            DeathMirror.Clear();
            DisplayNameCache = null;
            RoundEndUnixMs = 0;
            WerewolfCount = 0;
            RevealDelaySec = 0;
            GameStartUnixMs = 0;
            LastMeetingEndUnixMs = 0;
        }
    }
}
