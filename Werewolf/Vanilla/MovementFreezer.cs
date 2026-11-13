#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    public sealed class MovementFreezer
    {
        private const float KeepAliveSeconds = 0.1f;

        private bool _active;

        public bool Active => _active;

        public void Begin()
        {
            _active = true;
            Game.Patches.PlayerSpawnPatch.MeetingActive = true;
            ReleaseGrabNow();
            WLog.Line("freeze_begin", secret: false);
        }

        public void End()
        {
            if (!_active) return;
            _active = false;
            Game.Patches.PlayerSpawnPatch.MeetingActive = false;
            WLog.Line("freeze_end", secret: false);
        }

        public void Tick(bool freezeMovement)
        {
            if (!_active) return;
            try
            {
                if (freezeMovement)
                {
                    SemiFunc.InputDisableMovement();
                    PhysGrabber grabber = PhysGrabber.instance;
                    if (grabber != null) grabber.OverrideGrabDisable(KeepAliveSeconds);
                    PollToggleMute();
                }
                DisableSpatialAll();
            }
            catch (Exception e)
            {
                WLog.Line("freeze_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void ReleaseGrabNow()
        {
            PhysGrabber grabber = PhysGrabber.instance;
            if (grabber != null) grabber.OverrideGrabRelease(-1, KeepAliveSeconds);
        }

        private void DisableSpatialAll()
        {
            RunManager rm = RunManager.instance;
            if (rm == null) return;
            List<PlayerVoiceChat> chats = rm.voiceChats;
            if (chats == null) return;
            foreach (PlayerVoiceChat vc in chats)
            {
                if (vc != null) vc.SpatialDisable(KeepAliveSeconds);
            }
        }

        private static void PollToggleMute()
        {
            if (!SemiFunc.IsMultiplayer()) return;
            if (!InputGate.KeysFree) return;
            InputManager im = InputManager.instance;
            DataDirector dd = DataDirector.instance;
            if (im == null || dd == null) return;
            Dictionary<InputKey, InputAction> actions = im.inputActions;
            if (actions == null) return;
            if (!actions.TryGetValue(InputKey.ToggleMute, out InputAction action) || action == null) return;
            if (!action.WasPressedThisFrame()) return;
            ref bool mute = ref dd.toggleMute;
            mute = !mute;
        }
    }
}
