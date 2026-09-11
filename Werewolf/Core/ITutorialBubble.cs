namespace Werewolf.Core
{
    public interface ITutorialBubble
    {
        bool Exists { get; }

        bool Visible { get; }

        bool Idle { get; }

        bool Tick();

        void ShowMessage(string message, float fontScale);

        void Hide();
    }
}
