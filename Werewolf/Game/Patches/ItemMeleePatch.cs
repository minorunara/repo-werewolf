using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class ItemMeleeFriendlyFire
    {
        private static readonly AccessTools.FieldRef<ItemMelee, HurtCollider> HurtColliderRef =
            GameRefs.ItemMelee_hurtCollider;

        internal static void Apply(ItemMelee melee)
        {
            try
            {
                if (melee == null || melee.swingLogicOnly) return;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return;

                HurtCollider hurtCollider = HurtColliderRef(melee);
                if (hurtCollider == null) return;

                int overridden = CombatRules.OverrideMeleePlayerDamage(
                    dir.ClientPhase, hurtCollider.playerDamage, hurtCollider.enemyDamage);

                if (overridden == hurtCollider.playerDamage) return;

                hurtCollider.playerDamage = overridden;
            }
            catch (Exception e)
            {
                WLog.Line("patch_itemmelee_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(ItemMelee), "Start")]
    internal static class ItemMeleeStartPatch
    {
        private static void Postfix(ItemMelee __instance)
        {
            ItemMeleeFriendlyFire.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemMelee), "StateSwinging")]
    internal static class ItemMeleeStateSwingingPatch
    {
        private static void Postfix(ItemMelee __instance)
        {
            ItemMeleeFriendlyFire.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemMelee), nameof(ItemMelee.EnemyOrPVPSwingHitRPC))]
    internal static class ItemMeleePvpDurabilityPatch
    {
        private static void Prefix(ref bool _playerHit)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return;

                bool arenaOrShop = SemiFunc.RunIsArena() || SemiFunc.RunIsShop();
                bool overridden = CombatRules.OverrideMeleePvpDurabilityHit(
                    dir.ClientPhase, _playerHit, arenaOrShop);
                if (overridden == _playerHit) return;

                _playerHit = overridden;
                WLog.Line("melee_pvp_durability", secret: false, ("phase", dir.ClientPhase));
            }
            catch (Exception e)
            {
                WLog.Line("patch_itemmelee_durability_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(ItemMelee), nameof(ItemMelee.PlayerSwingHit))]
    internal static class ItemMeleeDisarmPatch
    {
        private const float DisarmGrabDisableSeconds = 0.5f;

        private static void Postfix(ItemMelee __instance)
        {
            try
            {
                if (__instance == null || __instance.swingLogicOnly) return;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return;

                PlayerAvatar victim = SemiFunc.PlayerAvatarLocal();
                if (victim == null) return;

                if (IsGrabbing(victim, __instance)) return;

                DisarmGrabbed(dir, victim);
                SpillInventory(dir, victim);
            }
            catch (Exception e)
            {
                WLog.Line("patch_itemmelee_disarm_error", secret: false, ("err", e.Message));
            }
        }

        private static bool IsGrabbing(PlayerAvatar victim, ItemMelee melee)
        {
            PhysGrabber grabber = victim.physGrabber;
            if (grabber == null || !grabber.grabbed) return false;

            PhysGrabObject grabbed = GameRefs.PhysGrabber_grabbedPhysGrabObject(grabber);
            return grabbed != null && grabbed.gameObject == melee.gameObject;
        }

        private static void DisarmGrabbed(WerewolfDirector dir, PlayerAvatar victim)
        {
            PhysGrabber grabber = victim.physGrabber;
            if (grabber == null || !grabber.grabbed) return;

            PhysGrabObject grabbed = GameRefs.PhysGrabber_grabbedPhysGrabObject(grabber);
            if (grabbed == null) return;

            bool grabbedIsMelee = GameRefs.PhysGrabObject_isMelee(grabbed);
            if (!CombatRules.ShouldDisarmGrabbedOnMeleeHit(dir.ClientPhase, true, grabbedIsMelee)) return;

            PhotonView view = grabbed.GetComponent<PhotonView>();
            grabber.OverrideGrabRelease(view != null ? view.ViewID : -1, DisarmGrabDisableSeconds);
            grabber.OverrideGrabDisable(DisarmGrabDisableSeconds);
            WLog.Line("melee_disarm_grabbed", secret: false, ("phase", dir.ClientPhase));
        }

        private static void SpillInventory(WerewolfDirector dir, PlayerAvatar victim)
        {
            if (!CombatRules.ShouldSpillInventoryOnMeleeHit(dir.ClientPhase)) return;

            Inventory inventory = Inventory.instance;
            if (inventory == null) return;

            List<ItemEquippable> occupied = new List<ItemEquippable>();
            foreach (InventorySpot spot in inventory.GetAllSpots())
            {
                if (spot == null || !spot.IsOccupied()) continue;
                if (spot.CurrentItem != null) occupied.Add(spot.CurrentItem);
            }

            int pick = CombatRules.PickSpillSlotIndex(occupied.Count, UnityEngine.Random.value);
            if (pick < 0) return;

            PhysGrabber grabber = victim.physGrabber;
            int grabberViewID = (SemiFunc.IsMultiplayer() && grabber != null) ? grabber.photonView.ViewID : -1;

            occupied[pick].ForceUnequip(victim.transform.position, grabberViewID);
            WLog.Line("melee_disarm_inventory", secret: false,
                ("phase", dir.ClientPhase), ("slot", pick), ("occupied", occupied.Count));
        }
    }
}
