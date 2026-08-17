namespace Werewolf.Core.Replay
{
    public static class ReplayEpStateText
    {
        public static string Label(string stateName)
        {
            switch (stateName)
            {
                case "None": return Texts.Get(TextId.ReplayEpStateNone);
                case "Idle": return Texts.Get(TextId.ReplayEpStateIdle);
                case "Active": return Texts.Get(TextId.ReplayEpStateActive);
                case "Success": return Texts.Get(TextId.ReplayEpStateSuccess);
                case "Warning": return Texts.Get(TextId.ReplayEpStateWarning);
                case "Cancel": return Texts.Get(TextId.ReplayEpStateCancel);
                case "Extracting": return Texts.Get(TextId.ReplayEpStateExtracting);
                case "Complete": return Texts.Get(TextId.ReplayEpStateComplete);
                case "Surplus": return Texts.Get(TextId.ReplayEpStateSurplus);
                case "TaxReturn": return Texts.Get(TextId.ReplayEpStateTaxReturn);
                default: return stateName ?? "";
            }
        }
    }
}
