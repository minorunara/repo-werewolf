using UnityEngine;

namespace Werewolf.Vanilla
{
    public readonly struct PlayerHandle
    {
        internal readonly PlayerAvatar Avatar;

        public int Actor { get; }

        internal PlayerHandle(int actor, PlayerAvatar avatar)
        {
            Actor = actor;
            Avatar = avatar;
        }

        public static PlayerHandle None => new PlayerHandle(-1, null);

        public bool IsValid => Avatar != null;

        public bool TryGetPosition(out Vector3 position)
        {
            if (Avatar == null || Avatar.transform == null)
            {
                position = default;
                return false;
            }
            position = Avatar.transform.position;
            return true;
        }

        public Vector3? Position => TryGetPosition(out Vector3 p) ? p : (Vector3?)null;
    }
}
