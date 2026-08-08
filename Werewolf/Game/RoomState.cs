using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class RoomState
    {
        public const string KeyPhase = "WW_Phase";

        public const string KeyRoundEndTime = "WW_RoundEndTime";

        public const string KeyIsAlive = "WW_IsAlive";

        public const string KeyMeetingCaller = "WW_MeetingCaller";

        public const string KeyMeetingEndTime = "WW_MeetingEndTime";

        public const string KeyMinimapHide = RoomStateKeys.CfgMinimapHide;

        public const string KeyCatPossible = RoomStateKeys.CfgCatPossible;

        public const string KeyValuableMapMode = RoomStateKeys.CfgValuableMapMode;

        public const string KeyRights = RoomStateKeys.Rights;

        public const string KeyCfgShared = RoomStateKeys.CfgShared;

        public const string KeyNecroVoiceMode = RoomStateKeys.CfgNecroVoiceMode;

        public const string KeyExtraJump = RoomStateKeys.CfgExtraJump;

        public const string KeyConveneSuppressStart = RoomStateKeys.CfgConveneSuppressStart;

        public const string KeyConveneSuppressAfter = RoomStateKeys.CfgConveneSuppressAfter;

        public const string KeyHealInterval = RoomStateKeys.CfgHealInterval;

        public const string KeyOutfitChange = RoomStateKeys.CfgOutfitChange;

        public const string KeyBomb = RoomStateKeys.CfgBomb;

        public const string KeyShaman = RoomStateKeys.CfgShaman;

        public void PublishPhase(GamePhase phase, long roundEndUnixMs)
        {
            SemiFunc.SetCurrentRoomProperty(KeyPhase, (int)phase);
            SemiFunc.SetCurrentRoomProperty(KeyRoundEndTime, roundEndUnixMs);
            WLog.Line("roomstate_publish", secret: false,
                ("key", "phase"), ("phase", phase), ("roundEnd", roundEndUnixMs));
        }

        public void PublishAlive(int actorNumber, bool alive)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            Room room = PhotonNetwork.CurrentRoom;
            Player player = room?.GetPlayer(actorNumber);
            if (player != null)
            {
                var props = new Hashtable { [KeyIsAlive] = (byte)(alive ? 1 : 0) };
                player.SetCustomProperties(props);
            }
            WLog.Line("roomstate_publish", secret: false,
                ("key", "isalive"), ("actor", actorNumber), ("alive", alive ? 1 : 0));
        }

        public void PublishMeeting(int callerActor, long meetingEndUnixMs)
        {
            SemiFunc.SetCurrentRoomProperty(KeyMeetingCaller, callerActor);
            SemiFunc.SetCurrentRoomProperty(KeyMeetingEndTime, meetingEndUnixMs);
            WLog.Line("roomstate_publish", secret: false,
                ("key", "meeting"), ("caller", callerActor), ("meetingEnd", meetingEndUnixMs));
        }

        public bool TryReadMeeting(out int callerActor, out long meetingEndUnixMs)
        {
            callerActor = -1;
            meetingEndUnixMs = 0;
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return false;

            if (room.CustomProperties.TryGetValue(KeyMeetingCaller, out object callerValue) && callerValue is int caller &&
                room.CustomProperties.TryGetValue(KeyMeetingEndTime, out object endValue) && endValue is long end)
            {
                callerActor = caller;
                meetingEndUnixMs = end;
                return true;
            }
            return false;
        }

        public bool TryReadPhase(out GamePhase phase)
        {
            phase = GamePhase.Lobby;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyPhase, out object value) && value is int i)
            {
                phase = (GamePhase)(byte)i;
                return true;
            }
            return false;
        }

        public bool TryReadRoundEndTime(out long roundEndUnixMs)
        {
            roundEndUnixMs = 0;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyRoundEndTime, out object value) && value is long l)
            {
                roundEndUnixMs = l;
                return true;
            }
            return false;
        }

        public bool TryReadAlive(int actorNumber, out bool alive)
        {
            alive = true;
            Player player = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
            if (player != null && player.CustomProperties.TryGetValue(KeyIsAlive, out object value) && value is byte b)
            {
                alive = b != 0;
                return true;
            }
            return false;
        }

        public void PublishSettings(GameConfig config, int playerCount)
        {
            SemiFunc.SetCurrentRoomProperty(KeyMinimapHide, RoomStateKeys.EncodeBool(config.MinimapHideEnabled));
            SemiFunc.SetCurrentRoomProperty(KeyCatPossible, RoomStateKeys.EncodeBool(config.BlackCatPossible(playerCount)));
            SemiFunc.SetCurrentRoomProperty(KeyValuableMapMode, RoomStateKeys.EncodeValuableMapMode(config.ValuableMapMode));
            SemiFunc.SetCurrentRoomProperty(KeyNecroVoiceMode, RoomStateKeys.EncodeNecroVoiceMode(config.NecroVoiceMode));
            SemiFunc.SetCurrentRoomProperty(KeyExtraJump, RoomStateKeys.EncodeExtraJump(config.ExtraJumpCount));
            SemiFunc.SetCurrentRoomProperty(KeyConveneSuppressStart, config.ConveneSuppressStartSec);
            SemiFunc.SetCurrentRoomProperty(KeyConveneSuppressAfter, config.ConveneSuppressAfterSec);
            SemiFunc.SetCurrentRoomProperty(KeyHealInterval, config.HealIntervalSec);
            SemiFunc.SetCurrentRoomProperty(KeyOutfitChange, RoomStateKeys.EncodeBool(config.OutfitChangeAllowed));
            SemiFunc.SetCurrentRoomProperty(KeyBomb, RoomStateKeys.EncodeBomb(config, playerCount));
            SemiFunc.SetCurrentRoomProperty(KeyShaman, RoomStateKeys.EncodeShaman(config));
            WLog.Line("roomstate_publish", secret: false,
                ("key", "settings"),
                ("minimapHide", config.MinimapHideEnabled ? 1 : 0),
                ("catPossible", config.BlackCatPossible(playerCount) ? 1 : 0),
                ("valuableMode", (int)config.ValuableMapMode),
                ("necroVoiceMode", (int)config.NecroVoiceMode),
                ("extraJump", config.ExtraJumpCount),
                ("conveneSupStart", config.ConveneSuppressStartSec),
                ("conveneSupAfter", config.ConveneSuppressAfterSec),
                ("healInterval", config.HealIntervalSec),
                ("outfitChange", config.OutfitChangeAllowed ? 1 : 0),
                ("bomberPossible", config.BomberPossible(playerCount) ? 1 : 0));
        }

        public bool TryReadBombPack(out int[] packed)
        {
            packed = null;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyBomb, out object value)
                && value is int[] arr && arr.Length == RoomStateKeys.BombIndex.Length)
            {
                packed = arr;
                return true;
            }
            return false;
        }

        public bool TryReadShamanPack(out int[] packed)
        {
            packed = null;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyShaman, out object value)
                && value is int[] arr && arr.Length == RoomStateKeys.ShamanIndex.Length)
            {
                packed = arr;
                return true;
            }
            return false;
        }

        public bool TryReadMinimapHide(out bool minimapHideEnabled)
        {
            minimapHideEnabled = false;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyMinimapHide, out object value) && value is byte b)
            {
                minimapHideEnabled = RoomStateKeys.DecodeBool(b);
                return true;
            }
            return false;
        }

        public bool TryReadCatPossible(out bool catPossible)
        {
            catPossible = false;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyCatPossible, out object value) && value is byte b)
            {
                catPossible = RoomStateKeys.DecodeBool(b);
                return true;
            }
            return false;
        }

        public bool TryReadValuableMapMode(out ValuableMapMode mode)
        {
            mode = ValuableMapMode.MeetingSync;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyValuableMapMode, out object value) && value is byte b)
            {
                mode = RoomStateKeys.DecodeValuableMapMode(b);
                return true;
            }
            return false;
        }

        public bool TryReadNecroVoiceMode(out NecroVoiceMode mode)
        {
            mode = NecroVoiceMode.Off;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyNecroVoiceMode, out object value) && value is byte b)
            {
                mode = RoomStateKeys.DecodeNecroVoiceMode(b);
                return true;
            }
            return false;
        }

        public bool TryReadExtraJump(out int extraJumpCount)
        {
            extraJumpCount = 0;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyExtraJump, out object value) && value is byte b)
            {
                extraJumpCount = RoomStateKeys.DecodeExtraJump(b);
                return true;
            }
            return false;
        }

        public bool TryReadConveneSuppressStart(out int seconds)
        {
            seconds = 0;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyConveneSuppressStart, out object value) && value is int i)
            {
                seconds = i;
                return true;
            }
            return false;
        }

        public bool TryReadConveneSuppressAfter(out int seconds)
        {
            seconds = 0;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyConveneSuppressAfter, out object value) && value is int i)
            {
                seconds = i;
                return true;
            }
            return false;
        }

        public bool TryReadHealInterval(out int seconds)
        {
            seconds = 0;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyHealInterval, out object value) && value is int i)
            {
                seconds = i;
                return true;
            }
            return false;
        }

        public bool TryReadOutfitChange(out bool allowed)
        {
            allowed = false;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyOutfitChange, out object value) && value is byte b)
            {
                allowed = RoomStateKeys.DecodeBool(b);
                return true;
            }
            return false;
        }

        public void PublishRights(int actorNumber, int remaining)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            Room room = PhotonNetwork.CurrentRoom;
            Player player = room?.GetPlayer(actorNumber);
            if (player != null)
            {
                var props = new Hashtable { [KeyRights] = RoomStateKeys.EncodeRights(remaining) };
                player.SetCustomProperties(props);
            }
            WLog.Line("roomstate_publish", secret: false,
                ("key", "rights"), ("actor", actorNumber), ("remaining", remaining));
        }

        public bool TryReadRights(int actorNumber, out int remaining)
        {
            remaining = 0;
            Player player = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
            if (player != null && player.CustomProperties.TryGetValue(KeyRights, out object value) && value is byte b)
            {
                remaining = RoomStateKeys.DecodeRights(b);
                return true;
            }
            return false;
        }

        public void PublishSharedSettings(string blob)
        {
            SemiFunc.SetCurrentRoomProperty(KeyCfgShared, blob);
            WLog.Line("roomstate_publish", secret: false,
                ("key", "cfgshared"), ("blobLen", blob?.Length ?? 0));
        }

        public bool TryReadSharedSettings(out string blob)
        {
            blob = null;
            Room room = PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue(KeyCfgShared, out object value) && value is string s)
            {
                blob = s;
                return true;
            }
            return false;
        }
    }
}
