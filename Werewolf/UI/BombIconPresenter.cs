using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

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

        private GameObject _root;
        private readonly Dictionary<int, Image> _radialOutlines = new Dictionary<int, Image>();
        private readonly Dictionary<int, Image> _radials = new Dictionary<int, Image>();
        private readonly Dictionary<int, Image> _bombIcons = new Dictionary<int, Image>();
        private Image _plantFlightIcon;
        private int _plantFlightTarget = -1;
        private float _plantFlightStartedAt;

        public bool Exists => _root != null;

        private RectTransform RootRect => (RectTransform)_root.transform;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);
            var go = new GameObject("WW_BombIconPresenter", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;
            _root.SetActive(false);
        }

        public void Tick(bool visible,
            IReadOnlyDictionary<int, float> radialTargets,
            IReadOnlyCollection<int> bombIconActors,
            Func<int, Vector3?> resolveWorldPos)
        {
            if (_root == null) return;
            try
            {
                if (_root.activeSelf != visible) _root.SetActive(visible);
                if (!visible)
                {
                    HideAll();
                    return;
                }

                Camera cam = Camera.main;
                if (cam == null)
                {
                    HideAll();
                    return;
                }

                if (radialTargets != null)
                {
                    foreach (var kv in radialTargets)
                    {
                        Vector3? anchor = resolveWorldPos != null ? resolveWorldPos(kv.Key) : null;
                        Image img = EnsureRadial(kv.Key);
                        if (anchor != null && !OverheadVision.BodyVisibleFromCamera(anchor.Value, cam))
                            img.enabled = false;
                        else
                            PlaceOrHide(img, anchor, cam);
                        if (img.enabled)
                        {
                            float ratio = Mathf.Clamp01(kv.Value);
                            img.fillAmount = ratio;
                            img.color = ratio >= 1f ? RadialFullColor : RadialChargingColor;
                            Image outline = EnsureRadialOutline(kv.Key, img);
                            outline.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;
                            outline.fillAmount = ratio;
                            outline.enabled = true;
                        }
                        else if (_radialOutlines.TryGetValue(kv.Key, out Image hiddenOutline)
                            && hiddenOutline != null)
                        {
                            hiddenOutline.enabled = false;
                        }
                    }
                    var toRemove = new List<int>();
                    foreach (var actor in _radials.Keys)
                    {
                        if (!radialTargets.ContainsKey(actor)) toRemove.Add(actor);
                    }
                    foreach (int actor in toRemove)
                    {
                        if (_radials[actor] != null) _radials[actor].enabled = false;
                        if (_radialOutlines.TryGetValue(actor, out Image outline) && outline != null)
                            outline.enabled = false;
                    }
                }
                else
                {
                    foreach (var img in _radials.Values) if (img != null) img.enabled = false;
                    foreach (var img in _radialOutlines.Values) if (img != null) img.enabled = false;
                }

                if (bombIconActors != null && bombIconActors.Count > 0)
                {
                    foreach (int actor in bombIconActors)
                    {
                        Vector3? anchor = resolveWorldPos != null ? resolveWorldPos(actor) : null;
                        Image img = EnsureBombIcon(actor);
                        if (actor == _plantFlightTarget)
                            img.enabled = false;
                        else if (anchor != null && !OverheadVision.BodyVisibleFromCamera(anchor.Value, cam))
                            img.enabled = false;
                        else
                            PlaceOrHide(img, anchor, cam, verticalOffsetPx: BombIconVerticalOffsetPx);
                    }
                    var toRemove = new List<int>();
                    foreach (var actor in _bombIcons.Keys)
                    {
                        if (!ContainsActor(bombIconActors, actor)) toRemove.Add(actor);
                    }
                    foreach (int actor in toRemove)
                    {
                        if (_bombIcons[actor] != null) _bombIcons[actor].enabled = false;
                    }
                }
                else
                {
                    foreach (var img in _bombIcons.Values) if (img != null) img.enabled = false;
                }

                TickPlantFlight(cam, resolveWorldPos);
            }
            catch (Exception e)
            {
                WLog.Line("bomb_icon_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
            EndPlantFlight();
            HideAll();
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _radials.Clear();
            _radialOutlines.Clear();
            _bombIcons.Clear();
            _plantFlightIcon = null;
            _plantFlightTarget = -1;
        }

        public void BeginPlantFlight(int targetActor)
        {
            if (targetActor == -1) return;
            _plantFlightTarget = targetActor;
            _plantFlightStartedAt = Time.unscaledTime;
            if (_plantFlightIcon != null) _plantFlightIcon.enabled = true;
        }

        private static bool ContainsActor(IReadOnlyCollection<int> set, int actor)
        {
            foreach (int a in set) if (a == actor) return true;
            return false;
        }

        private Image EnsureRadial(int actor)
        {
            if (_radials.TryGetValue(actor, out Image cached) && cached != null)
            {
                if (!cached.enabled) cached.enabled = true;
                return cached;
            }
            Image img = UiKit.CreateRadialImage(_root.transform, "Radial_" + actor,
                Vector2.zero, new Vector2(RadialSize, RadialSize),
                RadialChargingColor);
            _radials[actor] = img;
            return img;
        }

        private Image EnsureRadialOutline(int actor, Image coloredRadial)
        {
            if (_radialOutlines.TryGetValue(actor, out Image cached) && cached != null)
            {
                PlaceOutlineDirectlyBehind(cached, coloredRadial);
                return cached;
            }

            Image img = UiKit.CreateRadialImage(_root.transform, "RadialOutline_" + actor,
                Vector2.zero, new Vector2(RadialOutlineSize, RadialOutlineSize),
                RadialOutlineColor);
            PlaceOutlineDirectlyBehind(img, coloredRadial);
            _radialOutlines[actor] = img;
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

        private Image EnsureBombIcon(int actor)
        {
            if (_bombIcons.TryGetValue(actor, out Image cached) && cached != null)
            {
                if (!cached.enabled) cached.enabled = true;
                return cached;
            }
            Image img = UiKit.CreateImage(_root.transform, "BombIcon_" + actor,
                Vector2.zero, new Vector2(BombIconSize, BombIconSize), Color.white);
            Sprite sprite = AssetCatalog.GetBombIcon();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            _bombIcons[actor] = img;
            return img;
        }

        private Image EnsurePlantFlightIcon()
        {
            if (_plantFlightIcon != null) return _plantFlightIcon;
            _plantFlightIcon = UiKit.CreateImage(_root.transform, "BombPlantFlight",
                Vector2.zero, new Vector2(BombIconSize, BombIconSize), Color.white);
            Sprite sprite = AssetCatalog.GetBombIcon();
            if (sprite != null)
            {
                _plantFlightIcon.sprite = sprite;
                _plantFlightIcon.preserveAspect = true;
            }
            return _plantFlightIcon;
        }

        private void TickPlantFlight(Camera cam, Func<int, Vector3?> resolveWorldPos)
        {
            if (_plantFlightTarget == -1)
            {
                if (_plantFlightIcon != null) _plantFlightIcon.enabled = false;
                return;
            }

            float t = Mathf.Clamp01((Time.unscaledTime - _plantFlightStartedAt) / PlantFlightSeconds);
            Vector3? target = resolveWorldPos != null ? resolveWorldPos(_plantFlightTarget) : null;
            if (target == null)
            {
                EndPlantFlight();
                return;
            }

            Vector2 startUi = OverheadProjection.ViewportToUi(
                new Vector2(PlantThrowOriginScreenX, PlantThrowOriginScreenY), RootRect);
            Vector3 endViewport = cam.WorldToViewportPoint(target.Value + Vector3.up * HeadOffsetY);
            if (endViewport.z <= 0f)
            {
                EndPlantFlight();
                return;
            }

            Vector2 uiPos = Vector2.Lerp(startUi, OverheadProjection.ViewportToUi(endViewport, RootRect), t);
            uiPos.y += BombIconVerticalOffsetPx * t;
            uiPos.y += PlantArcHeightPx * 4f * t * (1f - t);
            Image icon = EnsurePlantFlightIcon();
            icon.enabled = true;
            icon.rectTransform.anchoredPosition = uiPos;
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -360f * t);

            if (t >= 1f) EndPlantFlight();
        }

        private void EndPlantFlight()
        {
            _plantFlightTarget = -1;
            if (_plantFlightIcon != null)
            {
                _plantFlightIcon.enabled = false;
                _plantFlightIcon.rectTransform.localRotation = Quaternion.identity;
            }
        }

        private void PlaceOrHide(Image img, Vector3? anchorPos, Camera cam, float verticalOffsetPx = 0f)
        {
            if (img == null) return;
            if (anchorPos == null)
            {
                img.enabled = false;
                return;
            }
            Vector3 world = anchorPos.Value + Vector3.up * HeadOffsetY;
            if (!OverheadProjection.TryProject(cam, world, RootRect, out Vector2 uiPos))
            {
                img.enabled = false;
                return;
            }
            if (!img.enabled) img.enabled = true;
            uiPos.y += verticalOffsetPx;
            img.rectTransform.anchoredPosition = uiPos;
        }

        private void HideAll()
        {
            foreach (var img in _radials.Values) if (img != null) img.enabled = false;
            foreach (var img in _radialOutlines.Values) if (img != null) img.enabled = false;
            foreach (var img in _bombIcons.Values) if (img != null) img.enabled = false;
            if (_plantFlightIcon != null) _plantFlightIcon.enabled = false;
        }
    }
}
