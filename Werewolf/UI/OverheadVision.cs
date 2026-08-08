using UnityEngine;

namespace Werewolf.UI
{
    internal static class OverheadVision
    {
        public const float MaxDistance = 15f;

        public const float ProbeHeight = 1.2f;

        public static bool BodyVisibleFromCamera(Vector3 bodyPos, Camera cam)
        {
            Vector3 probe = bodyPos + Vector3.up * ProbeHeight;
            Vector3 toCam = cam.transform.position - probe;
            float dist = toCam.magnitude;
            if (dist > MaxDistance) return false;
            int mask = (int)SemiFunc.LayerMaskGetVisionObstruct() & ~LayerMask.GetMask("Player");
            return !Physics.Raycast(probe, toCam, dist, mask, QueryTriggerInteraction.Collide);
        }
    }
}
