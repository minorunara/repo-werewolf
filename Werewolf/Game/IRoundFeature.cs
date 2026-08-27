namespace Werewolf.Game
{
    internal interface IRoundFeature
    {
        void Tick(long now);

        void Reset();

        void DestroyUi();
    }
}
