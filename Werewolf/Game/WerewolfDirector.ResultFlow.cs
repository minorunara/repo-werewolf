using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        internal bool IsResultChatActiveClient
            => ResultChatGate.IsOpen(ClientPhase, _resultScreen.Visible, MeetingChatController.LogEnabled);

        internal void TickReplayViewer()
        {
            bool window = _resultScreen.Visible || _replayViewer.DemoActive;
            if (window && !_replayViewer.Exists)
            {
                EnsurePanelBuilt(_replayViewer);
                _chatFeature.EnsureResultPanelTopSibling();
            }
            if (!_replayViewer.Exists) return;

            _replayViewer.Tick(
                _resultScreen.Visible,
                ReplaySampler.Recorder,
                Plugin.Bindings != null ? Plugin.Bindings.MeetingMapOrthoSize.Value : 15f,
                Plugin.Bindings != null ? Plugin.Bindings.MeetingMapResolution.Value : 1,
                Plugin.ReplayViewerKey != null ? Plugin.ReplayViewerKey.Value : KeyCode.R,
                InputGate.KeysFree,
                p => IsResultChatActiveClient && _chatFeature.IsPointerOverResultPanel(p),
                ReplaySampler.ExportForUser);
        }

        internal void DebugReplayDemo(int count)
        {
            EnsurePanelBuilt(_replayViewer);
            _chatFeature.EnsureResultPanelTopSibling();
            if (_replayViewer.Exists) _replayViewer.SetDemo(count);
        }
    }
}
