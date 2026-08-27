namespace Werewolf.Core
{
    public sealed class ServerClock
    {
        private long _lastServerMs;
        private long _localAtLastMs;

        public bool ServerBacked { get; private set; }

        public long Read(int rawServerMs, long localMonoMs)
        {
            if (rawServerMs != 0)
            {
                _lastServerMs = (long)(uint)rawServerMs;
                _localAtLastMs = localMonoMs;
                ServerBacked = true;
                return _lastServerMs;
            }

            ServerBacked = false;
            return _lastServerMs + (localMonoMs - _localAtLastMs);
        }
    }
}
