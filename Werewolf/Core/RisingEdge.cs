namespace Werewolf.Core
{
    public sealed class RisingEdge
    {
        private bool _previous;

        public bool Observe(bool current)
        {
            bool rose = current && !_previous;
            _previous = current;
            return rose;
        }
    }
}
