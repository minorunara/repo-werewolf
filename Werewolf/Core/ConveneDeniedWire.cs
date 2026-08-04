namespace Werewolf.Core
{
    public static class ConveneDeniedWire
    {
        public static byte ToWire(ConveneRejectReason reason)
        {
            switch (reason)
            {
                case ConveneRejectReason.NoRight:    return 1;
                case ConveneRejectReason.Suppressed: return 2;
                case ConveneRejectReason.WrongPhase: return 3;
                case ConveneRejectReason.CorpseReportLastRun: return 4;
                case ConveneRejectReason.NoCorpse:   return 5;
                default: return 0;
            }
        }

        public static ConveneRejectReason FromWire(byte wire)
        {
            switch (wire)
            {
                case 1: return ConveneRejectReason.NoRight;
                case 2: return ConveneRejectReason.Suppressed;
                case 3: return ConveneRejectReason.WrongPhase;
                case 4: return ConveneRejectReason.CorpseReportLastRun;
                case 5: return ConveneRejectReason.NoCorpse;
                default: return ConveneRejectReason.CallerDead;
            }
        }
    }
}
