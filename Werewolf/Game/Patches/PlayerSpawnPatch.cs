using System;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(PlayerAvatar), "SpawnRPC")]
    internal static class PlayerSpawnPatch
    {
        internal static volatile bool MeetingActive;

        private static long s_warpGraceUntilMs;

        internal static void ArmWarpGrace(long durationMs = 10_000L) =>
            s_warpGraceUntilMs = SessionClock.NowMs() + durationMs;

        private static bool WindowActive => MeetingActive || SessionClock.NowMs() <= s_warpGraceUntilMs;

        private static void Prefix(PlayerAvatar __instance)
        {
            try
            {
                if (!WindowActive) return;
                if (PlayerAvatar.instance == null || __instance != PlayerAvatar.instance) return;

                PhysGrabber grabber = PhysGrabber.instance;
                if (grabber != null) grabber.OverrideGrabRelease(-1, 0.1f);
            }
            catch (Exception e)
            {
                WLog.Line("patch_playerspawn_error", secret: false, ("err", e.Message));
            }
        }

        private static void Postfix(PlayerAvatar __instance)
        {
            try
            {
                if (!WindowActive) return;
                if (PlayerAvatar.instance == null || __instance != PlayerAvatar.instance) return;

                PlayerController pc = PlayerController.instance;
                if (pc == null || pc.rb == null) return;

                pc.rb.position = pc.transform.position;
                pc.rb.rotation = pc.transform.rotation;
                if (!pc.rb.isKinematic)
                {
                    pc.rb.velocity = Vector3.zero;
                    pc.rb.angularVelocity = Vector3.zero;
                }
            }
            catch (Exception e)
            {
                WLog.Line("patch_playerspawn_postfix_error", secret: false, ("err", e.Message));
            }
        }
    }
}
