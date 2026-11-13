#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class BombIconPresenter : IClientPanel
    {
        public string LayerName => "Hud";

        private const float RadialSize = 96f;
        private const float RadialOutlineSize = 108f;
        private const float BombIconSize = 144f;
        private const float HeadOffsetY = 1.7f;
        private const float BombIconVerticalOffsetPx = -52.8f;
        private const float PlantFlightSeconds = 3f;
        private const float PlantArcHeightPx = 260f;
        private const float PlantThrowOriginScreenX = 0.56f;
        private const float PlantThrowOriginScreenY = 0.12f;
        private static readonly Color RadialChargingColor = new Color(1f, 0.85f, 0.15f, 0.65f);
        private static readonly Color RadialFullColor = new Color(0.35f, 0.95f, 0.35f, 0.65f);
        private static readonly Color RadialOutlineColor = new Color(1f, 1f, 1f, 0.95f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root)
            {
                Root = root;
                RadialOutlines = new Dictionary<int, Image>();
                Radials = new Dictionary<int, Image>();
                BombIcons = new Dictionary<int, Image>();
            }

            public GameObject Root { get; }
            public Dictionary<int, Image> RadialOutlines { get; }
            public Dictionary<int, Image> Radials { get; }
            public Dictionary<int, Image> BombIcons { get; }
            public Image? PlantFlightIcon { get; set; }
            public int PlantFlightTarget { get; set; } = -1;
            public float PlantFlightStartedAt { get; set; }
        }

        private ViewRefs? _view;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);
                root = new GameObject("WW_BombIconPresenter", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);
                root.SetActive(false);
                _view = new ViewRefs(root);
            }
            catch
            {
                _view = null;
                if (root != null)
                {
                    root.SetActive(false);
                    UnityEngine.Object.Destroy(root);
                }
                throw;
            }
        }

        public void Tick(bool visible,
            IReadOnlyDictionary<int, float>? radialTargets,
            IReadOnlyCollection<int>? bombIconActors,
            Func<int, Vector3?>? resolveWorldPos)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                if (view.Root.activeSelf != visible) view.Root.SetActive(visible);
                if (!visible)
                {
                    HideAll(view);
                    return;
                }

                Camera cam = Camera.main;
                if (cam == null)
                {
                    HideAll(view);
                    return;
                }

                if (radialTargets != null)
                {
                    foreach (var kv in radialTargets)
                    {
                        Vector3? anchor = resolveWorldPos != null ? resolveWorldPos(kv.Key) : null;
                        Image img = EnsureRadial(view, kv.Key);
                        if (anchor != null && !OverheadVision.BodyVisibleFromCamera(anchor.Value, cam))
                            img.enabled = false;
                        else
                            PlaceOrHide(view, img, anchor, cam);
                        if (img.enabled)
                        {
                            float ratio = Mathf.Clamp01(kv.Value);
                            img.fillAmount = ratio;
                            img.color = ratio >= 1f ? RadialFullColor : RadialChargingColor;
                            Image outline = EnsureRadialOutline(view, kv.Key, img);
                            outline.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;
                            outline.fillAmount = ratio;
                            outline.enabled = true;
                        }
                        else if (view.RadialOutlines.TryGetValue(kv.Key, out Image hiddenOutline)
                            && hiddenOutline != null)
                        {
                            hiddenOutline.enabled = false;
                        }
                    }
                    var toRemove = new List<int>();
                    foreach (var actor in view.Radials.Keys)
                    {
                        if (!radialTargets.ContainsKey(actor)) toRemove.Add(actor);
                    }
                    foreach (int actor in toRemove)
                    {
                        if (view.Radials[actor] != null) view.Radials[actor].enabled = false;
                        if (view.RadialOutlines.TryGetValue(actor, out Image outline) && outline != null)
                            outline.enabled = false;
                    }
                }
                else
                {
                    foreach (var img in view.Radials.Values) if (img != null) img.enabled = false;
                    foreach (var img in view.RadialOutlines.Values) if (img != null) img.enabled = false;
                }

                if (bombIconActors != null && bombIconActors.Count > 0)
                {
                    foreach (int actor in bombIconActors)
                    {
                        Vector3? anchor = resolveWorldPos != null ? resolveWorldPos(actor) : null;
                        Image img = EnsureBombIcon(view, actor);
                        if (actor == view.PlantFlightTarget)
                            img.enabled = false;
                        else if (anchor != null && !OverheadVision.BodyVisibleFromCamera(anchor.Value, cam))
                            img.enabled = false;
                        else
                            PlaceOrHide(view, img, anchor, cam,
                                verticalOffsetPx: BombIconVerticalOffsetPx);
                    }
                    var toRemove = new List<int>();
                    foreach (var actor in view.BombIcons.Keys)
                    {
                        if (!ContainsActor(bombIconActors, actor)) toRemove.Add(actor);
                    }
                    foreach (int actor in toRemove)
                    {
                        if (view.BombIcons[actor] != null) view.BombIcons[actor].enabled = false;
                    }
                }
                else
                {
                    foreach (var img in view.BombIcons.Values) if (img != null) img.enabled = false;
                }

                TickPlantFlight(view, cam, resolveWorldPos);
            }
            catch (Exception e)
            {
                WLog.Line("bomb_icon_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (view.Root.activeSelf) view.Root.SetActive(false);
            EndPlantFlight(view);
            HideAll(view);
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view == null) return;
            ResetViewState(view);
            if (view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        public void BeginPlantFlight(int targetActor)
        {
            if (targetActor == -1) return;
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            view.PlantFlightTarget = targetActor;
            view.PlantFlightStartedAt = Time.unscaledTime;
            if (view.PlantFlightIcon != null) view.PlantFlightIcon.enabled = true;
        }

        private static bool ContainsActor(IReadOnlyCollection<int> set, int actor)
        {
            foreach (int a in set) if (a == actor) return true;
            return false;
        }

        private static Image EnsureRadial(ViewRefs view, int actor)
        {
            if (view.Radials.TryGetValue(actor, out Image cached) && cached != null)
            {
                if (!cached.enabled) cached.enabled = true;
                return cached;
            }
            Image img = UiKit.CreateRadialImage(view.Root.transform, "Radial_" + actor,
                Vector2.zero, new Vector2(RadialSize, RadialSize),
                RadialChargingColor);
            view.Radials[actor] = img;
            return img;
        }

        private static Image EnsureRadialOutline(ViewRefs view, int actor, Image coloredRadial)
        {
            if (view.RadialOutlines.TryGetValue(actor, out Image cached) && cached != null)
            {
                PlaceOutlineDirectlyBehind(cached, coloredRadial);
                return cached;
            }

            Image img = UiKit.CreateRadialImage(view.Root.transform, "RadialOutline_" + actor,
                Vector2.zero, new Vector2(RadialOutlineSize, RadialOutlineSize),
                RadialOutlineColor);
            PlaceOutlineDirectlyBehind(img, coloredRadial);
            view.RadialOutlines[actor] = img;
            return img;
        }

        private static void PlaceOutlineDirectlyBehind(Image outline, Image coloredRadial)
        {
            if (outline == null || coloredRadial == null) return;
            int outlineIndex = outline.transform.GetSiblingIndex();
            int coloredIndex = coloredRadial.transform.GetSiblingIndex();
            if (outlineIndex > coloredIndex)
            {
                outline.transform.SetSiblingIndex(coloredIndex);
            }
            else if (outlineIndex < coloredIndex - 1)
            {
                outline.transform.SetSiblingIndex(coloredIndex - 1);
            }
        }

        private static Image EnsureBombIcon(ViewRefs view, int actor)
        {
            if (view.BombIcons.TryGetValue(actor, out Image cached) && cached != null)
            {
                if (!cached.enabled) cached.enabled = true;
                return cached;
            }
            Image img = UiKit.CreateImage(view.Root.transform, "BombIcon_" + actor,
                Vector2.zero, new Vector2(BombIconSize, BombIconSize), Color.white);
            Sprite? sprite = AssetCatalog.GetBombIcon();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            view.BombIcons[actor] = img;
            return img;
        }

        private static Image EnsurePlantFlightIcon(ViewRefs view)
        {
            if (view.PlantFlightIcon != null) return view.PlantFlightIcon;
            Image icon = UiKit.CreateImage(view.Root.transform, "BombPlantFlight",
                Vector2.zero, new Vector2(BombIconSize, BombIconSize), Color.white);
            Sprite? sprite = AssetCatalog.GetBombIcon();
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }
            view.PlantFlightIcon = icon;
            return icon;
        }

        private static void TickPlantFlight(
            ViewRefs view, Camera cam, Func<int, Vector3?>? resolveWorldPos)
        {
            if (view.PlantFlightTarget == -1)
            {
                if (view.PlantFlightIcon != null) view.PlantFlightIcon.enabled = false;
                return;
            }

            float t = Mathf.Clamp01(
                (Time.unscaledTime - view.PlantFlightStartedAt) / PlantFlightSeconds);
            Vector3? target = resolveWorldPos != null
                ? resolveWorldPos(view.PlantFlightTarget)
                : null;
            if (target == null)
            {
                EndPlantFlight(view);
                return;
            }

            RectTransform rootRect = (RectTransform)view.Root.transform;
            Vector2 startUi = OverheadProjection.ViewportToUi(
                new Vector2(PlantThrowOriginScreenX, PlantThrowOriginScreenY), rootRect);
            Vector3 endViewport = cam.WorldToViewportPoint(target.Value + Vector3.up * HeadOffsetY);
            if (endViewport.z <= 0f)
            {
                EndPlantFlight(view);
                return;
            }

            Vector2 uiPos = Vector2.Lerp(
                startUi, OverheadProjection.ViewportToUi(endViewport, rootRect), t);
            uiPos.y += BombIconVerticalOffsetPx * t;
            uiPos.y += PlantArcHeightPx * 4f * t * (1f - t);
            Image icon = EnsurePlantFlightIcon(view);
            icon.enabled = true;
            icon.rectTransform.anchoredPosition = uiPos;
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -360f * t);

            if (t >= 1f) EndPlantFlight(view);
        }

        private static void EndPlantFlight(ViewRefs view)
        {
            view.PlantFlightTarget = -1;
            view.PlantFlightStartedAt = 0f;
            if (view.PlantFlightIcon != null)
            {
                view.PlantFlightIcon.enabled = false;
                view.PlantFlightIcon.rectTransform.localRotation = Quaternion.identity;
            }
        }

        private static void PlaceOrHide(
            ViewRefs view, Image img, Vector3? anchorPos, Camera cam, float verticalOffsetPx = 0f)
        {
            if (img == null) return;
            if (anchorPos == null)
            {
                img.enabled = false;
                return;
            }
            Vector3 world = anchorPos.Value + Vector3.up * HeadOffsetY;
            if (!OverheadProjection.TryProject(
                    cam, world, (RectTransform)view.Root.transform, out Vector2 uiPos))
            {
                img.enabled = false;
                return;
            }
            if (!img.enabled) img.enabled = true;
            uiPos.y += verticalOffsetPx;
            img.rectTransform.anchoredPosition = uiPos;
        }

        private static void HideAll(ViewRefs view)
        {
            foreach (var img in view.Radials.Values) if (img != null) img.enabled = false;
            foreach (var img in view.RadialOutlines.Values) if (img != null) img.enabled = false;
            foreach (var img in view.BombIcons.Values) if (img != null) img.enabled = false;
            if (view.PlantFlightIcon != null) view.PlantFlightIcon.enabled = false;
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            ResetViewState(view);
            return null;
        }

        private static void ResetViewState(ViewRefs view)
        {
            view.Radials.Clear();
            view.RadialOutlines.Clear();
            view.BombIcons.Clear();
            view.PlantFlightIcon = null;
            view.PlantFlightTarget = -1;
            view.PlantFlightStartedAt = 0f;
        }
    }
}
