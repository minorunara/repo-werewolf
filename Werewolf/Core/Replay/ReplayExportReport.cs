namespace Werewolf.Core.Replay
{
    public enum ReplayExportOutcome : byte
    {
        Saved = 0,

        AlreadyExists = 1,

        Empty = 2,

        Failed = 3,
    }

    public struct ReplayExportReport
    {
        public ReplayExportOutcome Outcome;

        public string FileName;

        public bool ToDownloads;
    }
}
