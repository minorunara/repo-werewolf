namespace Werewolf.Vanilla
{
    internal static class MoonPhaseUi
    {
        public static bool IsPlaying
        {
            get
            {
                MoonUI moon = MoonUI.instance;
                return moon != null && moon.objectActive != null && moon.objectActive.activeSelf;
            }
        }
    }
}
