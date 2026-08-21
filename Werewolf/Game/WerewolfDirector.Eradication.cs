using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private long _eradicationConfirmAtUnixMs;

        private readonly EradicationRevealPanel _eradicationReveal = new EradicationRevealPanel();
        private Coroutine _eradicationRevealCoroutine;

        private void HostBeginEradicationCeremony()
        {
            _eradicationConfirmAtUnixMs = NowUnixMs() + EradicationCeremony.CeremonyMs;
            WLog.Line("eradication_ceremony_start", secret: false,
                ("confirmAtUnixMs", _eradicationConfirmAtUnixMs));
        }

        private void TickEradicationHost(long now)
        {
            if (_eradicationConfirmAtUnixMs == 0 || now < _eradicationConfirmAtUnixMs) return;
            _eradicationConfirmAtUnixMs = 0;
            _session?.ConfirmPendingWin(now);
        }

        private void HandleEradicationReveal(object[] p)
        {
            EradicationRevealData data = EradicationRevealWire.FromWire(p);
            if (data == null)
            {
                WLog.Line("eradication_reveal_drop", secret: false, ("reason", "invalid_payload"));
                return;
            }

            _winCeremonyActive = true;

            EnsurePanelBuilt(_eradicationReveal);
            if (!_eradicationReveal.Exists) return;

            _eradicationReveal.Show(
                ResolveEradicationVictim(data), ResolveAvatar, data.WinningTeam, data.Vanished);

            EnsureSfxBuilt();
            if (_eradicationRevealCoroutine != null) StopCoroutine(_eradicationRevealCoroutine);
            _eradicationRevealCoroutine = StartCoroutine(_eradicationReveal.Play(
                onStamp: () => _sfxPlayer.Play("sfx_death_stamp")));
            WLog.Line("recv_eradication_reveal", secret: false,
                ("actor", data.ActorNumber), ("team", data.WinningTeam), ("vanished", data.Vanished));
        }

        private WPlayer ResolveEradicationVictim(EradicationRevealData data)
        {
            IReadOnlyList<WPlayer> roster = _session != null
                ? _session.Players
                : Registry.BuildRealPlayers();
            if (roster != null)
            {
                foreach (WPlayer player in roster)
                {
                    if (player != null && player.ActorNumber == data.ActorNumber) return player;
                }
            }
            return new WPlayer
            {
                ActorNumber = data.ActorNumber,
                Name = string.IsNullOrEmpty(data.Name) ? $"#{data.ActorNumber}" : data.Name,
                IsBot = data.ActorNumber < 0,
            };
        }

        private void TickEradicationClient()
        {
            _eradicationReveal.Tick();
            if (_eradicationReveal.Visible && _resultScreen.Visible)
            {
                HideEradicationReveal();
            }
        }

        private void HideEradicationReveal()
        {
            _winCeremonyActive = false;
            if (_eradicationRevealCoroutine != null)
            {
                StopCoroutine(_eradicationRevealCoroutine);
                _eradicationRevealCoroutine = null;
            }
            if (_eradicationReveal.Exists) _eradicationReveal.HideNow();
        }
    }
}
