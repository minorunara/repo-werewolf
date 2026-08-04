namespace Werewolf.Core
{
    public enum ValuableMapMode : byte
    {
        Realtime = 0,

        MeetingSync = 1,

        Hidden = 2,
    }

    public static class ValuableMapGate
    {
        public static bool ShouldSuppressAdd(ValuableMapMode mode, bool roundActive)
            => roundActive && (mode == ValuableMapMode.Hidden || mode == ValuableMapMode.MeetingSync);

        public static bool ShouldSnapshotOnDiscover(ValuableMapMode mode, bool roundActive)
            => roundActive && mode == ValuableMapMode.MeetingSync;

        public static bool ShouldRefreshSnapshotAtInventoryPoint(ValuableMapMode mode, bool roundActive)
            => roundActive && mode == ValuableMapMode.MeetingSync;

        public static bool ShouldRestoreValuablesOnEnd(ValuableMapMode mode)
            => mode == ValuableMapMode.Hidden || mode == ValuableMapMode.MeetingSync;
    }
}
