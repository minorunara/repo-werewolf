using UnityEngine;

namespace Werewolf.Vanilla
{
    internal static class LocalPlayer
    {
        public static bool TryGetPosition(out Vector3 position)
        {
            PlayerAvatar local = PlayerAvatar.instance;
            if (local == null || local.transform == null)
            {
                position = default;
                return false;
            }
            position = local.transform.position;
            return true;
        }

        public static bool TryGetPositionAndForward(out Vector3 position, out Vector3 forward)
        {
            PlayerAvatar local = PlayerAvatar.instance;
            if (local == null || local.transform == null)
            {
                position = default;
                forward = Vector3.forward;
                return false;
            }
            position = local.transform.position;
            forward = local.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            return true;
        }

        public static bool IsTumbling => TruckWarper.IsTumbling(PlayerAvatar.instance);

        public static void SetInvincible(float seconds)
        {
            PlayerAvatar local = PlayerAvatar.instance;
            if (local == null) return;
            PlayerHealth ph = local.playerHealth;
            if (ph == null) return;
            ph.InvincibleSet(seconds);
        }

        public static void DisableGrab(float seconds)
        {
            PhysGrabber grabber = PhysGrabber.instance;
            if (grabber != null) grabber.OverrideGrabDisable(seconds);
        }
    }
}
