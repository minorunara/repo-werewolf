#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class BombWarningPresenter : IClientPanel
    {
        public string LayerName => "Hud";

        private const float CenterImageSize = 512f;
        private const float PulseSpeed = 6f;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, Image centerImage, AudioSource selfAudio)
            {
                Root = root;
                CenterImage = centerImage;
                SelfAudio = selfAudio;
            }

            public GameObject Root { get; }
            public Image CenterImage { get; }
            public AudioSource SelfAudio { get; }
        }

        private ViewRefs? _view;
        private GameObject? _spatialAudioGo;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                root = new GameObject("WW_BombWarningPresenter", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);

                Image centerImage = UiKit.CreateImage(rect, "Center", Vector2.zero,
                    new Vector2(CenterImageSize, CenterImageSize),
                    new Color(1f, 0.15f, 0.05f, 0.85f));
                Sprite? sp = AssetCatalog.GetBombIcon();
                if (sp != null)
                {
                    centerImage.sprite = sp;
                    centerImage.preserveAspect = true;
                }
                centerImage.enabled = false;

                AudioSource selfAudio = root.AddComponent<AudioSource>();
                selfAudio.spatialBlend = 0f;
                selfAudio.loop = true;
                selfAudio.playOnAwake = false;

                root.SetActive(false);
                _view = new ViewRefs(root, centerImage, selfAudio);
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

        public void Show(bool localIsTarget, PlayerHandle targetAvatar, float heartbeatRadiusMeters,
            Vector3? spatialFallbackPos = null)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                ResetPresentation(view);
                if (!view.Root.activeSelf) view.Root.SetActive(true);
                AudioClip? clip = AssetCatalog.GetHeartbeatClip();

                if (localIsTarget)
                {
                    view.CenterImage.enabled = true;
                    if (clip != null)
                    {
                        view.SelfAudio.clip = clip;
                        view.SelfAudio.volume = 1f;
                        if (!view.SelfAudio.isPlaying) view.SelfAudio.Play();
                    }
                }
                else
                {
                    SpawnSpatial(targetAvatar, spatialFallbackPos, clip, heartbeatRadiusMeters);
                }
            }
            catch (Exception e)
            {
                try
                {
                    ResetPresentation(view);
                    if (view.Root != null && view.Root.activeSelf) view.Root.SetActive(false);
                }
                catch
                {
                }
                WLog.Line("bomb_warning_show_error", secret: false, ("err", e.Message));
            }
        }

        public void Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.CenterImage.enabled) return;
            float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * PulseSpeed));
            Color c = view.CenterImage.color;
            c.a = a;
            view.CenterImage.color = c;
        }

        public void Hide()
        {
            try
            {
                ViewRefs? view = GetLiveViewOrNull();
                if (view != null)
                {
                    ResetPresentation(view);
                    if (view.Root.activeSelf) view.Root.SetActive(false);
                }
                else
                {
                    DestroySpatialAudio();
                }
            }
            catch (Exception e)
            {
                WLog.Line("bomb_warning_hide_error", secret: false, ("err", e.Message));
            }
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            try
            {
                if (view != null) ResetPresentation(view);
                else DestroySpatialAudio();
            }
            catch (Exception e)
            {
                WLog.Line("bomb_warning_hide_error", secret: false, ("err", e.Message));
            }
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private void SpawnSpatial(PlayerHandle targetAvatar, Vector3? fallbackPos,
            AudioClip? clip, float maxDistance)
        {
            Vector3 pos;
            if (targetAvatar.TryGetPosition(out Vector3 avatarPos)) pos = avatarPos;
            else if (fallbackPos != null) pos = fallbackPos.Value;
            else return;
            if (clip == null) return;
            GameObject? spatialAudioGo = null;
            try
            {
                spatialAudioGo = new GameObject("WW_BombHeartbeat3D");
                spatialAudioGo.transform.position = pos;
                AudioSource src = spatialAudioGo.AddComponent<AudioSource>();
                src.clip = clip;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.maxDistance = maxDistance > 0f ? maxDistance : 2f;
                src.minDistance = 0.5f;
                src.loop = true;
                src.playOnAwake = false;
                src.Play();
                _spatialAudioGo = spatialAudioGo;
                spatialAudioGo = null;
            }
            finally
            {
                if (spatialAudioGo != null) UnityEngine.Object.Destroy(spatialAudioGo);
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            DestroySpatialAudio();
            return null;
        }

        private void ResetPresentation(ViewRefs view)
        {
            try
            {
                if (view.CenterImage != null) view.CenterImage.enabled = false;
                if (view.SelfAudio != null && view.SelfAudio.isPlaying) view.SelfAudio.Stop();
            }
            finally
            {
                DestroySpatialAudio();
            }
        }

        private void DestroySpatialAudio()
        {
            GameObject? spatialAudioGo = _spatialAudioGo;
            _spatialAudioGo = null;
            if (spatialAudioGo != null) UnityEngine.Object.Destroy(spatialAudioGo);
        }
    }
}
