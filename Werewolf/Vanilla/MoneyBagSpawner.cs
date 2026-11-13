#nullable enable
using UnityEngine;
using System.Diagnostics.CodeAnalysis;

namespace Werewolf.Vanilla
{
    internal static class MoneyBagSpawner
    {
        public static bool TrySpawnInFrontOfLocal(int valueDollars,
            [NotNullWhen(true)] out GameObject? bag,
            [NotNullWhen(false)] out string? reason)
        {
            bag = null;
            reason = null;
            PlayerAvatar? local = PlayerAvatar.instance;
            GameObject? prefab = AssetManager.instance != null ? AssetManager.instance.surplusValuableSmall : null;
            if (local == null || local.transform == null || prefab == null)
            {
                reason = "no_avatar_or_prefab";
                return false;
            }

            Vector3 forward = local.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 pos = local.transform.position + forward * 1.5f + Vector3.up * 1f;

            GameObject? spawned = SemiFunc.IsMultiplayer()
                ? Photon.Pun.PhotonNetwork.InstantiateRoomObject("Valuables/" + prefab.name, pos, Quaternion.identity, 0)
                : UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            if (spawned == null)
            {
                reason = "spawn_failed";
                return false;
            }
            bag = spawned;
            if (valueDollars > 0)
            {
                ValuableObject? valuable = spawned.GetComponent<ValuableObject>();
                if (valuable != null) valuable.dollarValueOverride = valueDollars;
            }
            return true;
        }
    }
}
