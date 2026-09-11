using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class ChatStampCooldown
    {
        public const long CooldownMs = 3000L;

        private readonly Dictionary<int, long> _lastTakenAtMs = new Dictionary<int, long>();

        public bool TryTake(int actor, long nowMs)
        {
            if (_lastTakenAtMs.TryGetValue(actor, out long last) && nowMs - last < CooldownMs)
            {
                return false;
            }
            _lastTakenAtMs[actor] = nowMs;
            return true;
        }

        public bool IsReady(int actor, long nowMs)
            => !_lastTakenAtMs.TryGetValue(actor, out long last) || nowMs - last >= CooldownMs;
    }
}
