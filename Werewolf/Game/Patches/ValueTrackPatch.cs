using System;
using System.Collections.Generic;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class ValueTrackPatch
    {
        private static readonly AccessTools.FieldRef<ValuableObject, float> DollarValueCurrent =
            GameRefs.ValuableObject_dollarValueCurrent;

        private static readonly AccessTools.FieldRef<ValuableObject, float> DollarValueOriginal =
            GameRefs.ValuableObject_dollarValueOriginal;

        private static readonly AccessTools.FieldRef<PhysGrabObjectImpactDetector, ValuableObject> ValuableRef =
            GameRefs.PhysGrabObjectImpactDetector_valuableObject;

        private static readonly AccessTools.FieldRef<PhysGrabObjectImpactDetector, PhysGrabObject> PhysGrabObjectRef =
            GameRefs.PhysGrabObjectImpactDetector_physGrabObject;

        private static readonly HashSet<int> _breakAccountedIds = new HashSet<int>();

        internal static void MarkBreakAccounted(int instanceId) => _breakAccountedIds.Add(instanceId);
        internal static bool ConsumeBreakAccounted(int instanceId) => _breakAccountedIds.Remove(instanceId);

        private static readonly HashSet<int> _boxAccountedIds = new HashSet<int>();

        internal static void MarkBoxAccounted(int instanceId) => _boxAccountedIds.Add(instanceId);
        internal static bool ConsumeBoxAccounted(int instanceId) => _boxAccountedIds.Remove(instanceId);

        internal static ValuableObject ResolveValuable(PhysGrabObjectImpactDetector detector)
            => ValuableRef(detector);

        internal static PhysGrabObject ResolvePhysGrabObject(PhysGrabObjectImpactDetector detector)
            => PhysGrabObjectRef(detector);

        internal static float ReadCurrent(ValuableObject valuable) => DollarValueCurrent(valuable);

        internal static float ReadOriginal(ValuableObject valuable) => DollarValueOriginal(valuable);

        internal static void ScanAndFreezeBase()
        {
            CloseHaulFreeze();

            WerewolfDirector dir = WerewolfDirector.Instance;
            if (dir == null || !dir.IsHostSessionActive) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!SemiFunc.RunIsLevel()) return;

            float total = 0f;
            foreach (ValuableObject valuable in UnityEngine.Object.FindObjectsOfType<ValuableObject>())
            {
                total += DollarValueCurrent(valuable);
            }
            foreach (ItemValuableBox box in UnityEngine.Object.FindObjectsOfType<ItemValuableBox>())
            {
                total += box.CurrentValue;
            }
            dir.HostFreezeGaugeBase(total);
            dir.HostRequestCheckmateScan();
        }

        internal static float ComputeObtainableDollars()
        {
            float total = 0f;
            foreach (ValuableObject valuable in UnityEngine.Object.FindObjectsOfType<ValuableObject>())
            {
                total += DollarValueCurrent(valuable);
            }
            foreach (ItemValuableBox box in UnityEngine.Object.FindObjectsOfType<ItemValuableBox>(true))
            {
                total += box.CurrentValue;
            }
            RoundDirector rd = RoundDirector.instance;
            if (rd != null)
            {
                total += GameRefs.RoundDirector_extractionPointSurplus(rd);
            }
            return total;
        }

        private static readonly HaulFreeze _haulFreeze = new HaulFreeze();

        internal static void NoteHaulSuck()
            => _haulFreeze.NoteSuck(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        internal static void CloseHaulFreeze() => _haulFreeze.Close();

        internal static bool InHaulSuckWindow(long nowUnixMs)
            => _haulFreeze.IsHolding(nowUnixMs);
    }

    [HarmonyPatch(typeof(ExtractionPoint), "DestroyTheFirstPhysObjectsInHaulList")]
    internal static class HaulSuckPatch
    {
        private static void Postfix()
        {
            try
            {
                ValueTrackPatch.NoteHaulSuck();
            }
            catch (Exception e)
            {
                WLog.Line("patch_haulsuck_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(RoundDirector), "ExtractionCompleted")]
    internal static class CheckmateExtractionPatch
    {
        private static void Postfix()
        {
            try
            {
                ValueTrackPatch.CloseHaulFreeze();
                WerewolfDirector.Instance?.HostRecordExtractionDone();
                WerewolfDirector.Instance?.HostRequestCheckmateScan();
            }
            catch (Exception e)
            {
                WLog.Line("patch_checkmate_extract_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
    internal static class LevelGenerateDonePatch
    {
        private static void Postfix()
        {
            try
            {
                ValueTrackPatch.ScanAndFreezeBase();
            }
            catch (Exception e)
            {
                WLog.Line("patch_gaugebase_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "BreakRPC")]
    internal static class ValuableBreakPatch
    {
        private static void Prefix(
            PhysGrabObjectImpactDetector __instance, float valueLost, bool _loseValue)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                if (!SemiFunc.RunIsLevel()) return;
                if (!_loseValue) return;

                ValuableObject valuable = ValueTrackPatch.ResolveValuable(__instance);
                if (valuable == null) return;

                float before = ValueTrackPatch.ReadCurrent(valuable);
                float original = ValueTrackPatch.ReadOriginal(valuable);
                float realLoss = PerkGauge.ComputeRealLoss(before, valueLost, original);
                if (realLoss <= 0f) return;

                bool isOrb = __instance.GetComponent<EnemyValuable>() != null;
                dir.HostAddValueLoss(realLoss, isOrb);

                bool fatal = (before - valueLost) < original * 0.15f;
                if (fatal) ValueTrackPatch.MarkBreakAccounted(valuable.GetInstanceID());
            }
            catch (Exception e)
            {
                WLog.Line("patch_break_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "DestroyObject")]
    internal static class ValuableDestroyPatch
    {
        private static void Prefix(PhysGrabObjectImpactDetector __instance)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                if (!SemiFunc.RunIsLevel()) return;
                if (__instance.destroyDisable) return;

                PhysGrabObject physGrab = ValueTrackPatch.ResolvePhysGrabObject(__instance);
                if (physGrab != null && physGrab.dead) return;

                ValuableObject valuable = ValueTrackPatch.ResolveValuable(__instance);
                if (valuable == null)
                {
                    AccountValuableBoxResidual(__instance, dir);
                    return;
                }

                if (ValueTrackPatch.ConsumeBreakAccounted(valuable.GetInstanceID())) return;

                float residual = ValueTrackPatch.ReadCurrent(valuable);
                if (residual <= 0f) return;

                bool isOrb = __instance.GetComponent<EnemyValuable>() != null;
                dir.HostAddValueLoss(residual, isOrb);
            }
            catch (Exception e)
            {
                WLog.Line("patch_destroy_error", secret: false, ("err", e.Message));
            }
        }

        private static void AccountValuableBoxResidual(
            PhysGrabObjectImpactDetector __instance, WerewolfDirector dir)
        {
            ItemValuableBox box = __instance.GetComponentInChildren<ItemValuableBox>(true);
            if (box == null) return;

            float residual = box.CurrentValue;
            if (residual <= 0f) return;

            dir.HostAddValueLoss(residual, isOrb: false);
            ValueTrackPatch.MarkBoxAccounted(box.GetInstanceID());
        }
    }

    [HarmonyPatch(typeof(ItemValuableBox), "ExplodeValuableBox")]
    internal static class ValuableBoxExplodePatch
    {
        private static void Prefix(ItemValuableBox __instance)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                if (!SemiFunc.RunIsLevel()) return;

                float residual = __instance.CurrentValue;
                if (ValueTrackPatch.ConsumeBoxAccounted(__instance.GetInstanceID())) return;
                if (residual <= 0f) return;

                dir.HostAddValueLoss(residual, isOrb: false);
            }
            catch (Exception e)
            {
                WLog.Line("patch_boxexplode_error", secret: false, ("err", e.Message));
            }
        }
    }
}
