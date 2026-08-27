namespace Werewolf.Vanilla
{
    public sealed class LobbyStartPage
    {
        private readonly MenuPageLobby _page;

        internal LobbyStartPage(MenuPageLobby page)
        {
            _page = page;
        }

        public int InstanceId => _page.GetInstanceID();

        public void ButtonStart() => _page.ButtonStart();
    }
}
