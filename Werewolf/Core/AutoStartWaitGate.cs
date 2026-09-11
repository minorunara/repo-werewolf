namespace Werewolf.Core
{
    public sealed class AutoStartWaitGate
    {
        private bool _armed;

        public AutoStartWaitGate() { }

        public bool Armed => _armed;

        public void Arm()
        {
            if (Armed) return;
            _armed = true;
        }

        public void Disarm()
        {
            _armed = false;
        }

        public bool ShouldFire(bool allPlayersLoaded)
        {
            if (!Armed || !allPlayersLoaded) return false;
            Disarm();
            return true;
        }
    }
}
