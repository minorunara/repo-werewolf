using UnityEngine;

namespace Werewolf.Game
{
    internal static class VisionProbe
    {
        private const float ProbeHeight = 1.2f;
        private static int _obstructMask = -1;

        public static bool BodyToBodyClear(Vector3 fromBodyPos, Vector3 toBodyPos)
        {
            Vector3 from = fromBodyPos + Vector3.up * ProbeHeight;
            Vector3 dir = toBodyPos + Vector3.up * ProbeHeight - from;
            float dist = dir.magnitude;
            if (dist <= 0.001f) return true;
            if (_obstructMask == -1)
                _obstructMask = LayerMask.GetMask("Default", "StaticGrabObject");
            return !Physics.Raycast(from, dir, dist, _obstructMask, QueryTriggerInteraction.Ignore);
        }
    }
}
