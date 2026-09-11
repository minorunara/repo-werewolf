namespace Werewolf.Vanilla
{
    internal interface IReplayDirectorView
    {
        bool IsRoundActiveClient { get; }

        int LocalActor { get; }

        PlayerRegistry Registry { get; }

        Core.IdRosterClient IdRoster { get; }

        string DisplayNameForActor(int actorNumber);
    }
}
