namespace Werewolf.Net
{
    public static class InboundAuthority
    {
        public static bool IsAcceptable(byte code, int senderActor, int masterActor)
        {
            if (MessageCodes.IsMasterInbound(code)) return true;
            if (masterActor <= 0) return true;
            return senderActor == masterActor;
        }
    }

    public sealed class InboundDropThrottle
    {
        public const long WindowMs = 10000L;

        private long _nextLogAtUnixMs;
        private int _suppressed;

        public bool TryTake(long nowUnixMs, out int suppressedSinceLastLog)
        {
            if (nowUnixMs < _nextLogAtUnixMs)
            {
                _suppressed++;
                suppressedSinceLastLog = 0;
                return false;
            }

            suppressedSinceLastLog = _suppressed;
            _suppressed = 0;
            _nextLogAtUnixMs = nowUnixMs + WindowMs;
            return true;
        }

        public void Reset()
        {
            _nextLogAtUnixMs = 0L;
            _suppressed = 0;
        }
    }
}
