using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class LobbySettingsController
    {
        private readonly WerewolfDirector _d;

        private readonly LobbySettingsPanel _lobbySettingsPanel = new LobbySettingsPanel();

        private string _lastPublishedBlob;
        private long _panelTickAtMs;

        private string _lastPanelBlob;
        private bool _lastPanelModeEnabled;

        private bool _lobbyPanelUserHidden;

        private string _debugInjectedBlob;

        private string _lobbyBlobMirror;

        private string _matchBlob;

        private bool _matchPanelUserHidden;

        internal LobbySettingsController(WerewolfDirector director)
        {
            _d = director;
        }

        internal string BlobMirror => _lobbyBlobMirror;

        internal bool IsPanelVisible => _lobbySettingsPanel.Visible;

        internal bool PanelModeEnabled => _lastPanelModeEnabled;

        internal void Tick(long now)
        {
            bool matchScope = SettingsPanelGate.IsMatchScope(_d.ClientPhase, Session.InRunLevel);

            if (now - _panelTickAtMs >= 2000L)
            {
                _panelTickAtMs = now;
                if (matchScope)
                {
                    if (!string.IsNullOrEmpty(_matchBlob)) UpdatePanelRows(_matchBlob);
                }
                else
                {
                    TickShare();
                }
            }

            TickPanelVisibility(matchScope);
        }

        private void TickShare()
        {
            if (!Photon.Pun.PhotonNetwork.InRoom) return;
            if (!(Session.InLobby || Session.InLobbyMenu)) return;

            if (Session.IsHost)
            {
                if (Plugin.Bindings == null) return;

                GameConfig snapshot = Plugin.Bindings.Snapshot();
                string blob = SettingsCatalog.EncodeBlob(snapshot);
                if (string.Equals(blob, _lastPublishedBlob, StringComparison.Ordinal)) return;

                _d.RoomStateView.PublishSharedSettings(blob);
                _lastPublishedBlob = blob;
            }
            else if (string.IsNullOrEmpty(_lobbyBlobMirror))
            {
                if (_d.RoomStateView.TryReadSharedSettings(out string recovered) && !string.IsNullOrEmpty(recovered))
                {
                    UpdateBlobMirror(recovered);
                }
            }

            UpdatePanelRows(ResolveLobbySettingsBlob());
        }

        internal string ResolveLobbySettingsBlob()
        {
            if (!string.IsNullOrEmpty(_debugInjectedBlob)) return _debugInjectedBlob;
            if (Session.IsHost)
            {
                if (Plugin.Bindings == null) return null;
                GameConfig snapshot = Plugin.Bindings.Snapshot();
                return SettingsCatalog.EncodeBlob(snapshot);
            }
            return _lobbyBlobMirror;
        }

        private void UpdatePanelRows(string blob)
        {
            if (string.Equals(blob, _lastPanelBlob, StringComparison.Ordinal)) return;

            _lastPanelBlob = blob;
            _lastPanelModeEnabled = false;

            if (string.IsNullOrEmpty(blob)) return;
            if (!SettingsCatalog.TryDecodeBlob(blob, out var values))
            {
                WLog.Line("cfgshared_decode_failed", secret: false,
                    ("source", _debugInjectedBlob != null ? "injected" : "panel"), ("blobLen", blob.Length));
                return;
            }

            _lastPanelModeEnabled = values.TryGetValue("WerewolfModeEnabled", out string modeRaw)
                                     && string.Equals(modeRaw, "1", StringComparison.Ordinal);

            try
            {
                if (!_d.EnsureUiCreated()) return;
                if (!_lobbySettingsPanel.Exists)
                {
                    Transform layer = _d.GetUiLayerRoot(WerewolfUIManager.LobbyLayer);
                    if (layer == null) return;
                    string keyName = Plugin.LobbySettingsPanelKey != null
                        ? Plugin.LobbySettingsPanelKey.Value.ToString()
                        : "F7";
                    _lobbySettingsPanel.Build(layer, keyName);
                }
                _lobbySettingsPanel.SetRows(SettingsCatalog.BuildRows(values));
            }
            catch (Exception e)
            {
                WLog.Line("lobby_settings_panel_error", secret: false,
                    ("reason", "update_rows_failed"), ("err", e.Message));
            }
        }

        private void TickPanelVisibility(bool matchScope)
        {
            if (!_lobbySettingsPanel.Exists) return;
            bool inLobby = !matchScope && (Session.InLobby || Session.InLobbyMenu);
            bool escOpen = matchScope && Session.IsEscMenuOpen;

            if (InputGate.KeysFree && Plugin.LobbySettingsPanelKey != null
                && Input.GetKeyDown(Plugin.LobbySettingsPanelKey.Value))
            {
                if (escOpen)
                {
                    _matchPanelUserHidden = !_matchPanelUserHidden;
                    WLog.Line("lobby_settings_panel_toggle", secret: false,
                        ("scope", "match"), ("userHidden", _matchPanelUserHidden));
                }
                else if (inLobby)
                {
                    _lobbyPanelUserHidden = !_lobbyPanelUserHidden;
                    WLog.Line("lobby_settings_panel_toggle", secret: false,
                        ("scope", "lobby"), ("userHidden", _lobbyPanelUserHidden));
                }
            }

            SettingsPanelView view;
            if (matchScope)
            {
                bool matchRowsReady = !string.IsNullOrEmpty(_matchBlob)
                                      && string.Equals(_lastPanelBlob, _matchBlob, StringComparison.Ordinal);
                view = SettingsPanelGate.Match(
                    escMenuOpen: escOpen,
                    hasData: matchRowsReady,
                    manualOpen: _d.ManualOverlayView.IsOpen,
                    userHidden: _matchPanelUserHidden);
            }
            else
            {
                view = SettingsPanelGate.Lobby(
                    inLobby: inLobby,
                    hasData: !string.IsNullOrEmpty(_lastPanelBlob),
                    modeEnabled: _lastPanelModeEnabled,
                    userHidden: _lobbyPanelUserHidden);
            }
            _lobbySettingsPanel.SetVisibility(view);
        }

        internal void UpdateBlobMirror(string blob)
        {
            if (SettingsCatalog.TryDecodeBlob(blob, out _))
            {
                _lobbyBlobMirror = blob;
                WLog.Line("room_props_applied", secret: false,
                    ("key", "cfgshared"), ("blobLen", blob?.Length ?? 0));
            }
            else
            {
                _lobbyBlobMirror = null;
                WLog.Line("cfgshared_decode_failed", secret: false, ("blobLen", blob?.Length ?? 0));
            }
        }

        internal void NotePublishedBlob(string blob)
        {
            _lastPublishedBlob = blob;
        }

        internal void NoteMatchBlob(string blob)
        {
            if (string.IsNullOrEmpty(blob)) return;
            _matchBlob = blob;
            _matchPanelUserHidden = false;
        }

        internal void DebugInjectBlob(string blob)
        {
            _debugInjectedBlob = blob;
        }

        internal void DebugClearInjectedBlob()
        {
            _debugInjectedBlob = null;
        }

        internal void ResetForLobbyReturn()
        {
            _matchBlob = null;
            _matchPanelUserHidden = false;
        }

        internal void ResetForRoomLeave()
        {
            _lastPublishedBlob = null;
            _lobbyBlobMirror = null;
            _lobbySettingsPanel.SetVisibility(SettingsPanelView.Hidden);
            _lastPanelBlob = null;
            _lastPanelModeEnabled = false;
            _lobbyPanelUserHidden = false;
            _debugInjectedBlob = null;
            ResetForLobbyReturn();
        }
    }
}
