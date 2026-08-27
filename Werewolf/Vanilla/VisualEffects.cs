using UnityEngine;

namespace Werewolf.Vanilla
{
    internal static class VisualEffects
    {
        public static void OverrideSaturation(float amount, float speedIn, float speedOut, float seconds, GameObject owner)
        {
            PostProcessing post = PostProcessing.Instance;
            if (post != null) post.SaturationOverride(amount, speedIn, speedOut, seconds, owner);
        }

        public static int VisionObstructMask => SemiFunc.LayerMaskGetVisionObstruct();
    }
}
