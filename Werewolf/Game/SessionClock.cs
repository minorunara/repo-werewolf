using System.Diagnostics;
using Photon.Pun;
using Werewolf.Core;

namespace Werewolf.Game
{
    internal static class SessionClock
    {
        private static readonly Stopwatch Local = Stopwatch.StartNew();
        private static readonly ServerClock Server = new ServerClock();

        internal static long NowMs()
        {
            int raw = PhotonNetwork.InRoom ? PhotonNetwork.ServerTimestamp : 0;
            return Server.Read(raw, Local.ElapsedMilliseconds);
        }

        internal static bool IsServerBacked => Server.ServerBacked;

        internal static void RequestResync()
        {
            if (PhotonNetwork.InRoom) PhotonNetwork.FetchServerTimestamp();
        }
    }
}
