using System;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class PerkEffects
    {
        private bool _staminaApplied;
        private readonly NaturalHealClock _healClock = new NaturalHealClock();

        public void Tick(RolesClientState state, Role? localRole, Action<byte, int, byte> sendRoleAction,
                         Action<bool> onWolfModeChanged = null)
        {
            bool keysFree = InputGate.KeysFree;
            if (keysFree && Plugin.WolfModeKey != null && Input.GetKeyDown(Plugin.WolfModeKey.Value))
            {
                if (state.TryToggleWolfMode(localRole))
                {
                    sendRoleAction?.Invoke(
                        RoleActionSubtype.WolfModeSync, 0, (byte)(state.WolfMode ? 1 : 0));
                    onWolfModeChanged?.Invoke(state.WolfMode);
                }
            }

            if (keysFree && Plugin.BeaconKey != null && Input.GetKeyDown(Plugin.BeaconKey.Value)
                && localRole == Role.Werewolf)
            {
                sendRoleAction?.Invoke(RoleActionSubtype.BeaconUse, 0, 0);
            }

            ApplyStamina(state.StaminaActive);
        }

        public void TickHeal(bool active, long nowUnixMs, int intervalSec)
        {
            if (!_healClock.ShouldHeal(active, nowUnixMs, intervalSec)) return;

            PlayerAvatar avatar = PlayerAvatar.instance;
            PlayerHealth health = avatar != null ? avatar.playerHealth : null;
            if (health == null) return;
            health.Heal(1, false);
        }

        private void ApplyStamina(bool active)
        {
            PlayerController controller = PlayerController.instance;
            if (controller == null) return;

            if (active && !_staminaApplied)
            {
                controller.DebugEnergy = true;
                _staminaApplied = true;
            }
            else if (!active && _staminaApplied)
            {
                controller.DebugEnergy = false;
                _staminaApplied = false;
            }
        }

        public void ResetEffects()
        {
            ApplyStamina(false);
            _healClock.Reset();
        }
    }
}
