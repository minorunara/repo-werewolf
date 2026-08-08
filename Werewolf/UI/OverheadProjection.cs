using UnityEngine;

namespace Werewolf.UI
{
    internal static class OverheadProjection
    {
        public static bool TryProject(Camera cam, Vector3 world, RectTransform canvasRect, out Vector2 uiPos)
        {
            Vector3 viewport = cam.WorldToViewportPoint(world);
            if (viewport.z <= 0f
                || viewport.x < 0f || viewport.x > 1f
                || viewport.y < 0f || viewport.y > 1f)
            {
                uiPos = Vector2.zero;
                return false;
            }
            uiPos = ViewportToUi(viewport, canvasRect);
            return true;
        }

        public static Vector2 ViewportToUi(Vector2 viewport, RectTransform canvasRect)
        {
            return new Vector2(
                (viewport.x - 0.5f) * canvasRect.rect.width,
                (viewport.y - 0.5f) * canvasRect.rect.height);
        }
    }
}
