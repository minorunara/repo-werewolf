using System;
using UnityEngine;
using UnityEngine.Audio;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class SfxPlayer
    {
        private AudioSource _source;

        private AudioSource _stoppableSource;
        private string _stoppableClipKey;

        private AudioSource _loopSource;

        private AudioSource _conveneChargeSource;

        private AudioSource[] _sources = Array.Empty<AudioSource>();

        private string _loopClipKey;

        private bool _mixerResolved;

        public bool Exists => _source != null;

        public bool CanPlay => _source != null && _source.outputAudioMixerGroup != null;

        public void Build(GameObject host)
        {
            if (_source != null || host == null) return;

            _source = CreateSource(host, loop: false);
            _stoppableSource = CreateSource(host, loop: false);
            _loopSource = CreateSource(host, loop: true);
            _conveneChargeSource = CreateSource(host, loop: true);
            _sources = new[] { _source, _stoppableSource, _loopSource, _conveneChargeSource };

            TryResolveMixerGroup();
        }

        public void Play(string clipKey, float volumeScale = 1f)
        {
            if (!MixerReady(_source)) return;
            AudioClip clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                _source.PlayOneShot(clip, volumeScale);
            }
            catch (Exception e)
            {
                LogPlayError(clipKey, e);
            }
        }

        public void PlayStoppable(string clipKey, float volumeScale = 1f)
        {
            if (!MixerReady(_stoppableSource)) return;
            AudioClip clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                _stoppableSource.Stop();
                _stoppableClipKey = clipKey;
                _stoppableSource.clip = clip;
                _stoppableSource.volume = volumeScale;
                _stoppableSource.Play();
            }
            catch (Exception e)
            {
                LogPlayError(clipKey, e);
            }
        }

        public void PlayLoop(string clipKey, float volume)
        {
            if (!MixerReady(_loopSource)) return;
            AudioClip clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                _loopClipKey = clipKey;
                _loopSource.volume = Mathf.Clamp01(volume);
                KeepLooping(_loopSource, clip);
            }
            catch (Exception e)
            {
                LogPlayError(clipKey, e);
            }
        }

        public void StopLoop(string clipKey)
        {
            if (clipKey != _loopClipKey) return;
            StopSource(_loopSource, clipKey);
        }

        public void PlayConveneCharge(float ratio)
        {
            if (!MixerReady(_conveneChargeSource)) return;
            AudioClip clip = AssetCatalog.GetConveneChargeClip();
            if (clip == null) return;

            try
            {
                _conveneChargeSource.pitch = ConveneChargeTone.PitchFor(ratio);
                _conveneChargeSource.volume = ConveneChargeTone.VolumeFor(ratio);
                KeepLooping(_conveneChargeSource, clip);
            }
            catch (Exception e)
            {
                LogPlayError("convene_charge", e);
            }
        }

        public void StopConveneCharge()
        {
            if (_conveneChargeSource == null) return;
            StopSource(_conveneChargeSource, "convene_charge");
            _conveneChargeSource.pitch = 1f;
            _conveneChargeSource.volume = 0f;
        }

        public void StopStoppable(string clipKey = null)
        {
            if (clipKey != null && clipKey != _stoppableClipKey) return;
            StopSource(_stoppableSource, clipKey);
        }

        public void Destroy()
        {
            foreach (AudioSource source in _sources)
            {
                if (source != null) UnityEngine.Object.Destroy(source);
            }
            _sources = Array.Empty<AudioSource>();
            _source = null;
            _stoppableSource = null;
            _loopSource = null;
            _conveneChargeSource = null;
            _stoppableClipKey = null;
            _loopClipKey = null;
            _mixerResolved = false;
        }

        private bool MixerReady(AudioSource source)
        {
            if (source == null) return false;
            if (source.outputAudioMixerGroup == null && !_mixerResolved)
            {
                TryResolveMixerGroup();
            }
            return source.outputAudioMixerGroup != null;
        }

        private static void KeepLooping(AudioSource source, AudioClip clip)
        {
            if (source.clip != clip)
            {
                source.Stop();
                source.clip = clip;
            }
            if (!source.isPlaying) source.Play();
        }

        private static void StopSource(AudioSource source, string key)
        {
            if (source == null) return;
            try
            {
                if (source.isPlaying) source.Stop();
            }
            catch (Exception e)
            {
                WLog.Line("sfx_stop_error", secret: false, ("key", key ?? ""), ("err", e.GetType().Name));
            }
        }

        private static void LogPlayError(string key, Exception e)
            => WLog.Line("sfx_play_error", secret: false, ("key", key ?? ""), ("err", e.GetType().Name));

        private static AudioSource CreateSource(GameObject host, bool loop)
        {
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.bypassEffects = false;
            source.bypassListenerEffects = false;
            source.bypassReverbZones = false;
            source.volume = 1f;
            return source;
        }

        private void TryResolveMixerGroup()
        {
            if (_sources.Length == 0) return;

            try
            {
                AudioSource[] sources = Resources.FindObjectsOfTypeAll<AudioSource>();
                if (sources == null || sources.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < sources.Length; i++)
                {
                    AudioSource src = sources[i];
                    if (src == null || Array.IndexOf(_sources, src) >= 0) continue;
                    AudioMixerGroup group = src.outputAudioMixerGroup;
                    if (group == null) continue;

                    if (IsVoiceLikeGroup(group.name)) continue;

                    foreach (AudioSource own in _sources) own.outputAudioMixerGroup = group;
                    _mixerResolved = true;
                    return;
                }

                _mixerResolved = true;
            }
            catch (Exception e)
            {
                WLog.Line("sfx_mixer_resolve_error", secret: false, ("err", e.GetType().Name));
                _mixerResolved = true;
            }
        }

        private static bool IsVoiceLikeGroup(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string lower = name.ToLowerInvariant();
            return lower.Contains("microphone")
                || lower.Contains("voice")
                || lower.Contains("tts")
                || lower.Contains("music");
        }
    }
}
