using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class MovementFreezer
    {
        private static readonly AccessTools.FieldRef<RunManager, List<PlayerVoiceChat>> VoiceChatsRef =
            GameRefs.RunManager_voiceChats;

        private static readonly AccessTools.FieldRef<InputManager, Dictionary<InputKey, InputAction>> InputActionsRef =
            GameRefs.InputManager_inputActions;
        private static readonly AccessTools.FieldRef<DataDirector, bool> ToggleMuteRef =
            GameRefs.DataDirector_toggleMute;

        private const float KeepAliveSeconds = 0.1f;

        private bool _active;

        public bool Active => _active;

        public void Begin()
        {
            _active = true;
            Patches.PlayerSpawnPatch.MeetingActive = true;
            ReleaseGrabNow();
            WLog.Line("freeze_begin", secret: false);
        }

        public void End()
        {
            if (!_active) return;
            _active = false;
            Patches.PlayerSpawnPatch.MeetingActive = false;
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
            List<PlayerVoiceChat> chats = VoiceChatsRef(rm);
            if (chats == null) return;
            foreach (PlayerVoiceChat vc in chats)
            {
                if (vc != null) vc.SpatialDisable(KeepAliveSeconds);
            }
        }

        private static void PollToggleMute()
        {
            if (InputActionsRef == null || ToggleMuteRef == null) return;
            if (!SemiFunc.IsMultiplayer()) return;
            if (!InputGate.KeysFree) return;
            InputManager im = InputManager.instance;
            DataDirector dd = DataDirector.instance;
            if (im == null || dd == null) return;
            Dictionary<InputKey, InputAction> actions = InputActionsRef(im);
            if (actions == null) return;
            if (!actions.TryGetValue(InputKey.ToggleMute, out InputAction action) || action == null) return;
            if (!action.WasPressedThisFrame()) return;
            ref bool mute = ref ToggleMuteRef(dd);
            mute = !mute;
        }
    }
}
