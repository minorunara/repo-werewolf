namespace Werewolf.Core
{
    public sealed class ChatUnread
    {
        public bool HasUnread { get; private set; }

        public void OnMessageAppended(int actor, int localActor, bool panelOpen)
        {
            if (panelOpen || actor == localActor) return;
            HasUnread = true;
        }

        public void Clear() => HasUnread = false;
    }
}
