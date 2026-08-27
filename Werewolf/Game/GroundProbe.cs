using UnityEngine;
using UnityEngine.AI;

namespace Werewolf.Game
{
    internal static class GroundProbe
    {
        private const float FloorRayLength = 5f;

        private static int _floorMask = -1;

        private static int FloorMask => _floorMask != -1 ? _floorMask : _floorMask = LayerMask.GetMask("Default");

        internal static bool TryStandableAt(Vector3 position, float sampleMaxDistance,
                                            out Vector3 navPoint, out RaycastHit floor)
        {
            navPoint = default;
            floor = default;
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, sampleMaxDistance, -1)) return false;
            if (!Physics.Raycast(hit.position, Vector3.down, out floor, FloorRayLength, FloorMask)) return false;
            navPoint = hit.position;
            return true;
        }
    }
}
