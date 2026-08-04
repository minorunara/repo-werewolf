using System;
using System.Collections.Generic;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(InventorySpot), "AttemptEquipItem")]
    internal static class InventorySpotEquipContestPatch
    {
        private static bool Prefix()
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return true;

                PhysGrabber grabber = PhysGrabber.instance;
                if (grabber == null || !grabber.grabbed) return true;

                PhysGrabObject grabbed = GameRefs.PhysGrabber_grabbedPhysGrabObject(grabber);
                if (grabbed == null) return true;

                bool grabbedByOther = IsGrabbedByOther(grabbed, grabber);
                if (!CombatRules.ShouldBlockEquipContestedItem(dir.ClientPhase, grabbedByOther)) return true;

                dir.MaybeShowTutorial(TutorialId.EquipBlockedByOtherGrabber);
                WLog.Line("equip_blocked_contested", secret: false, ("phase", dir.ClientPhase));
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_inventory_equip_error", secret: false, ("err", e.Message));
                return true;
            }
        }

        private static bool IsGrabbedByOther(PhysGrabObject grabbed, PhysGrabber self)
        {
            List<PhysGrabber> grabbing = grabbed.playerGrabbing;
            if (grabbing == null) return false;

            foreach (PhysGrabber other in grabbing)
            {
                if (other != null && other != self) return true;
            }

            return false;
        }
    }
}
