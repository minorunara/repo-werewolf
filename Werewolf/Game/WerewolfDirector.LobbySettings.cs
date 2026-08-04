using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private void TickLobbySettings(long now)
        {
            if (now - _lobbyTickAtMs < 2000L) return;
            _lobbyTickAtMs = now;

            if (!Photon.Pun.PhotonNetwork.InRoom) return;
            if (!(SemiFunc.RunIsLobby() || SemiFunc.RunIsLobbyMenu())) return;

            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                if (Plugin.Bindings == null) return;

                GameConfig snapshot = Plugin.Bindings.Snapshot();
                string blob = SettingsCatalog.EncodeBlob(snapshot);
                if (string.Equals(blob, _lastPublishedBlob, StringComparison.Ordinal)) return;

                _roomState.PublishSharedSettings(blob);
                _lastPublishedBlob = blob;
            }
            else if (string.IsNullOrEmpty(_lobbyBlobMirror))
            {
                if (_roomState.TryReadSharedSettings(out string recovered) && !string.IsNullOrEmpty(recovered))
                {
                    UpdateLobbyBlobMirror(recovered);
                }
            }

            UpdateLobbySettingsPanelRows();
        }

        private string ResolveLobbySettingsBlob()
        {
            if (!string.IsNullOrEmpty(_debugInjectedBlob)) return _debugInjectedBlob;
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                if (Plugin.Bindings == null) return null;
                GameConfig snapshot = Plugin.Bindings.Snapshot();
                return SettingsCatalog.EncodeBlob(snapshot);
            }
            return _lobbyBlobMirror;
        }

        private void UpdateLobbySettingsPanelRows()
        {
            string blob = ResolveLobbySettingsBlob();
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
                if (!_uiManager.EnsureCreated(gameObject)) return;
                if (!_lobbySettingsPanel.Exists)
                {
                    Transform layer = _uiManager.GetLayerRoot(WerewolfUIManager.LobbyLayer);
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

        private void TickLobbySettingsPanelVisibility()
        {
            if (!_lobbySettingsPanel.Exists) return;
            bool inLobby = SemiFunc.RunIsLobby() || SemiFunc.RunIsLobbyMenu();

            if (inLobby && InputGate.KeysFree && Plugin.LobbySettingsPanelKey != null
                && Input.GetKeyDown(Plugin.LobbySettingsPanelKey.Value))
            {
                _lobbyPanelUserHidden = !_lobbyPanelUserHidden;
                WLog.Line("lobby_settings_panel_toggle", secret: false,
                    ("userHidden", _lobbyPanelUserHidden));
            }

            bool baseShow = inLobby
                            && !string.IsNullOrEmpty(_lastPanelBlob)
                            && _lastPanelModeEnabled;
            _lobbySettingsPanel.SetVisibility(
                panelVisible: baseShow && !_lobbyPanelUserHidden,
                hintVisible: baseShow && _lobbyPanelUserHidden);
        }

        private void UpdateLobbyBlobMirror(string blob)
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
    }
}
