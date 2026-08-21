using System;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class BombWarningPresenter : IClientPanel
    {
        public string LayerName => "Hud";

        private const float CenterImageSize = 512f;
        private const float PulseSpeed = 6f;

        private GameObject _root;
        private Image _centerImage;
        private AudioSource _selfAudio;
        private GameObject _spatialAudioGo;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_BombWarningPresenter", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;

            _centerImage = UiKit.CreateImage(rect, "Center", Vector2.zero,
                new Vector2(CenterImageSize, CenterImageSize),
                new Color(1f, 0.15f, 0.05f, 0.85f));
            Sprite sp = AssetCatalog.GetBombIcon();
            if (sp != null)
            {
                _centerImage.sprite = sp;
                _centerImage.preserveAspect = true;
            }
            _centerImage.enabled = false;

            _selfAudio = _root.AddComponent<AudioSource>();
            _selfAudio.spatialBlend = 0f;
            _selfAudio.loop = true;
            _selfAudio.playOnAwake = false;

            _root.SetActive(false);
        }

        public void Show(bool localIsTarget, PlayerAvatar targetAvatar, float heartbeatRadiusMeters,
            Vector3? spatialFallbackPos = null)
        {
            if (_root == null) return;
            try
            {
                if (!_root.activeSelf) _root.SetActive(true);
                AudioClip clip = AssetCatalog.GetHeartbeatClip();

                if (localIsTarget)
                {
                    if (_centerImage != null) _centerImage.enabled = true;
                    if (_selfAudio != null && clip != null)
                    {
                        _selfAudio.clip = clip;
                        _selfAudio.volume = 1f;
                        if (!_selfAudio.isPlaying) _selfAudio.Play();
                    }
                }
                else
                {
                    if (_centerImage != null) _centerImage.enabled = false;
                    SpawnSpatial(targetAvatar, spatialFallbackPos, clip, heartbeatRadiusMeters);
                }
            }
            catch (Exception e)
            {
                WLog.Line("bomb_warning_show_error", secret: false, ("err", e.Message));
            }
        }

        public void Tick()
        {
            if (_root == null || _centerImage == null || !_centerImage.enabled) return;
            float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * PulseSpeed));
            Color c = _centerImage.color;
            c.a = a;
            _centerImage.color = c;
        }

        public void Hide()
        {
            try
            {
                if (_centerImage != null) _centerImage.enabled = false;
                if (_selfAudio != null && _selfAudio.isPlaying) _selfAudio.Stop();
                if (_spatialAudioGo != null)
                {
                    UnityEngine.Object.Destroy(_spatialAudioGo);
                    _spatialAudioGo = null;
                }
                if (_root != null && _root.activeSelf) _root.SetActive(false);
            }
            catch (Exception e)
            {
                WLog.Line("bomb_warning_hide_error", secret: false, ("err", e.Message));
            }
        }

        public void Destroy()
        {
            Hide();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _centerImage = null;
            _selfAudio = null;
        }

        private void SpawnSpatial(PlayerAvatar targetAvatar, Vector3? fallbackPos,
            AudioClip clip, float maxDistance)
        {
            Vector3 pos;
            if (targetAvatar != null && targetAvatar.transform != null) pos = targetAvatar.transform.position;
            else if (fallbackPos != null) pos = fallbackPos.Value;
            else return;
            if (clip == null) return;
            _spatialAudioGo = new GameObject("WW_BombHeartbeat3D");
            _spatialAudioGo.transform.position = pos;
            AudioSource src = _spatialAudioGo.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = maxDistance > 0f ? maxDistance : 2f;
            src.minDistance = 0.5f;
            src.loop = true;
            src.playOnAwake = false;
            src.Play();
        }
    }
}
