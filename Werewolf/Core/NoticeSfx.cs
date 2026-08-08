namespace Werewolf.Core
{
    public static class NoticeSfx
    {
        public const string DefaultClipKey = "sfx_toast";

        public const string ConveneStartedClipKey = "sfx_notice_convene";

        public const string CorpseReportClipKey = "sfx_corpse_report";

        public const string BomberProximityWarningClipKey = "sfx_bomb_proximity_warning";

        public const string PlayerDisconnectedClipKey = "sfx_player_disconnected";

        public static string Resolve(SessionNotice notice)
        {
            if (notice == null) return DefaultClipKey;

            switch (notice.Kind)
            {
                case NoticeKind.ConveneStarted:
                    return ConveneStartedClipKey;

                case NoticeKind.CorpseReportStarted:
                    return CorpseReportClipKey;

                case NoticeKind.ScatterGuardTripped:
                    return CorpseReportClipKey;

                case NoticeKind.PlayerDisconnected:
                    return PlayerDisconnectedClipKey;

                default:
                    return DefaultClipKey;
            }
        }
    }
}
