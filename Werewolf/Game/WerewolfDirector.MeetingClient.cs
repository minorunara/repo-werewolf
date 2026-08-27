using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        public void SendConveneRequest() => SendConveneRequest(ConveneKind.Button);

        public void SendConveneRequest(ConveneKind kind)
        {
            if (_bus == null)
            {
                WLog.Line("convene_send_fail", secret: false, ("reason", "no_bus"));
                return;
            }
            _bus.SendToMaster(MessageCodes.RequestMeeting, new object[] { (byte)kind });
        }

        public void SendVote(int targetActor)
        {
            if (_bus == null)
            {
                WLog.Line("vote_send_fail", secret: false, ("reason", "no_bus"));
                return;
            }
            _bus.SendToMaster(MessageCodes.CastVote, new object[] { targetActor });
        }

        internal int LocalActor => _bus != null ? _bus.LocalActorNumber : 1;

        internal bool IsLocalAlive()
            => _meetingClient.GetRowStatus(LocalActor) == RowStatus.Alive;

        internal bool IsSessionAliveActor(int actor)
        {
            if (_session == null) return false;
            foreach (var p in _session.Players)
            {
                if (p.ActorNumber == actor) return p.Alive;
            }
            return false;
        }

        internal bool LocalIsNearUnannouncedCorpse =>
            _meetingClientFeature != null && _meetingClientFeature.LocalIsNearUnannouncedCorpse;

        internal bool CorpseReportConsumedPress =>
            _meetingClientFeature != null && _meetingClientFeature.CorpseReportConsumedPress;
    }
}
