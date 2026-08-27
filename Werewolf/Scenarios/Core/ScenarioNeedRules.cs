using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public readonly struct ScenarioNeedSample
    {
        public ScenarioNeedSample(bool meetingUiVisible, bool chatLogOpen, bool meetingMapOpen,
                                  bool mapToolOpen, bool tranceActive, bool senseCoolingDown,
                                  bool stormActive, bool corpseNear, bool bombTargetNear = false,
                                  bool bombOnTarget = false)
        {
            MeetingUiVisible = meetingUiVisible;
            ChatLogOpen = chatLogOpen;
            MeetingMapOpen = meetingMapOpen;
            MapToolOpen = mapToolOpen;
            TranceActive = tranceActive;
            SenseCoolingDown = senseCoolingDown;
            StormActive = stormActive;
            CorpseNear = corpseNear;
            BombTargetNear = bombTargetNear;
            BombOnTarget = bombOnTarget;
        }

        public bool MeetingUiVisible { get; }

        public bool ChatLogOpen { get; }

        public bool MeetingMapOpen { get; }

        public bool MapToolOpen { get; }

        public bool TranceActive { get; }

        public bool SenseCoolingDown { get; }

        public bool StormActive { get; }

        public bool CorpseNear { get; }

        public bool BombTargetNear { get; }

        public bool BombOnTarget { get; }
    }

    public readonly struct ScenarioNeedNag
    {
        public ScenarioNeedNag(TextId text, ScenarioAnchor anchor, ScenarioHighlight highlight,
                               bool clearedByUiOperation)
        {
            Text = text;
            Anchor = anchor;
            Highlight = highlight;
            ClearedByUiOperation = clearedByUiOperation;
        }

        public TextId Text { get; }

        public ScenarioAnchor Anchor { get; }

        public ScenarioHighlight Highlight { get; }

        public bool ClearedByUiOperation { get; }
    }

    public static class ScenarioNeedRules
    {
        public static ScenarioNeed Unmet(ScenarioNeed need, ScenarioNeedSample sample)
        {
            switch (need)
            {
                case ScenarioNeed.MapToolOpen:
                    return sample.MapToolOpen ? ScenarioNeed.None : need;
                case ScenarioNeed.ShamanTranceActive:
                    if (sample.SenseCoolingDown) return ScenarioNeed.ShamanSenseReady;
                    return sample.TranceActive ? ScenarioNeed.None : need;
                case ScenarioNeed.ShamanSenseReady:
                    return sample.SenseCoolingDown ? need : ScenarioNeed.None;
                case ScenarioNeed.ShamanStormActive:
                    return sample.StormActive ? ScenarioNeed.None : need;
                case ScenarioNeed.CorpseNear:
                    return sample.CorpseNear ? ScenarioNeed.None : need;
                case ScenarioNeed.BombTargetNear:
                    return sample.BombTargetNear ? ScenarioNeed.None : need;
                case ScenarioNeed.BombOnTarget:
                    return sample.BombOnTarget ? ScenarioNeed.None : need;
            }

            if (!sample.MeetingUiVisible) return ScenarioNeed.None;

            switch (need)
            {
                case ScenarioNeed.ChatLogOpen:
                    return sample.ChatLogOpen ? ScenarioNeed.None : need;
                case ScenarioNeed.MapClosed:
                    return sample.MeetingMapOpen ? need : ScenarioNeed.None;
                case ScenarioNeed.MapOpen:
                    if (sample.MeetingMapOpen) return ScenarioNeed.None;
                    return sample.MapToolOpen ? ScenarioNeed.MapToolClosed : need;
                default:
                    return ScenarioNeed.None;
            }
        }

        public static bool TryGetNag(ScenarioNeed need, out ScenarioNeedNag nag)
        {
            switch (need)
            {
                case ScenarioNeed.ChatLogOpen:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedOpenChatLogNag,
                        ScenarioAnchor.ChatLogButton, ScenarioHighlight.ChatLogButton, true);
                    return true;
                case ScenarioNeed.MapClosed:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedCloseMapNag,
                        ScenarioAnchor.MapButton, ScenarioHighlight.MapButton, true);
                    return true;
                case ScenarioNeed.MapOpen:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedOpenMapNag,
                        ScenarioAnchor.MapButton, ScenarioHighlight.MapButton, true);
                    return true;
                case ScenarioNeed.MapToolOpen:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedOpenMapToolNag,
                        ScenarioAnchor.PlayHud, ScenarioHighlight.None, true);
                    return true;
                case ScenarioNeed.MapToolClosed:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedCloseMapToolNag,
                        ScenarioAnchor.BottomLeft, ScenarioHighlight.None, true);
                    return true;
                case ScenarioNeed.ShamanTranceActive:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedShamanTranceNag,
                        ScenarioAnchor.PlayHud, ScenarioHighlight.None, false);
                    return true;
                case ScenarioNeed.ShamanSenseReady:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedShamanSenseReadyNag,
                        ScenarioAnchor.PlayHud, ScenarioHighlight.None, false);
                    return true;
                case ScenarioNeed.ShamanStormActive:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedShamanStormNag,
                        ScenarioAnchor.PlayHud, ScenarioHighlight.None, false);
                    return true;
                case ScenarioNeed.CorpseNear:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedCorpseNearNag,
                        ScenarioAnchor.PlayHud, ScenarioHighlight.None, false);
                    return true;
                case ScenarioNeed.BombTargetNear:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedBombTargetNearNag,
                        ScenarioAnchor.PlayHud, ScenarioHighlight.None, false);
                    return true;
                case ScenarioNeed.BombOnTarget:
                    nag = new ScenarioNeedNag(TextId.ScenarioNeedBombOnTargetNag,
                        ScenarioAnchor.PlayHud, ScenarioHighlight.None, false);
                    return true;
                default:
                    nag = default;
                    return false;
            }
        }

        public static bool ClearedByUiOperation(ScenarioNeed need)
            => TryGetNag(need, out ScenarioNeedNag nag) && nag.ClearedByUiOperation;
    }
}
