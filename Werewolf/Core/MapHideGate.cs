namespace Werewolf.Core
{
    public static class MapHideGate
    {
        public static bool ShouldSuppress(bool roundActive, bool minimapHideEnabled)
            => roundActive && minimapHideEnabled;
    }
}
