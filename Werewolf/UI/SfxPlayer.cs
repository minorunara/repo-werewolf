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

        private AudioSource _loopSource;

        private bool _mixerResolved;

        public bool Exists => _source != null;

        public bool CanPlay => _source != null && _source.outputAudioMixerGroup != null;

        public void Build(GameObject host)
        {
            if (_source != null || host == null) return;

            _source = CreateSource(host);
            _stoppableSource = CreateSource(host);
            _loopSource = CreateSource(host);
            _loopSource.loop = true;

            TryResolveMixerGroup();

            WLog.Line("sfx_built", secret: false,
                ("mixer", _source.outputAudioMixerGroup != null ? _source.outputAudioMixerGroup.name : "none"));
        }

        public void Play(string clipKey, float volumeScale = 1f)
        {
            if (_source == null) return;

            if (_source.outputAudioMixerGroup == null && !_mixerResolved)
            {
                TryResolveMixerGroup();
            }
            if (_source.outputAudioMixerGroup == null) return;

            AudioClip clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                _source.PlayOneShot(clip, volumeScale);
            }
            catch (Exception e)
            {
                WLog.Line("sfx_play_error", secret: false, ("key", clipKey ?? ""), ("err", e.GetType().Name));
            }
        }

        public void PlayStoppable(string clipKey, float volumeScale = 1f)
        {
            if (_stoppableSource == null) return;

            if (_stoppableSource.outputAudioMixerGroup == null && !_mixerResolved)
            {
                TryResolveMixerGroup();
            }
            if (_stoppableSource.outputAudioMixerGroup == null) return;

            AudioClip clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                _stoppableSource.Stop();
                _stoppableSource.clip = clip;
                _stoppableSource.volume = volumeScale;
                _stoppableSource.Play();
            }
            catch (Exception e)
            {
                WLog.Line("sfx_play_error", secret: false, ("key", clipKey ?? ""), ("err", e.GetType().Name));
            }
        }

        public void PlayLoop(string clipKey, float volume)
        {
            if (_loopSource == null) return;

            if (_loopSource.outputAudioMixerGroup == null && !_mixerResolved)
            {
                TryResolveMixerGroup();
            }
            if (_loopSource.outputAudioMixerGroup == null) return;

            AudioClip clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                _loopSource.volume = Mathf.Clamp01(volume);
                if (_loopSource.clip != clip)
                {
                    _loopSource.Stop();
                    _loopSource.clip = clip;
                }
                if (!_loopSource.isPlaying) _loopSource.Play();
            }
            catch (Exception e)
            {
                WLog.Line("sfx_play_error", secret: false, ("key", clipKey ?? ""), ("err", e.GetType().Name));
            }
        }

        public void StopLoop()
        {
            if (_loopSource == null) return;
            try
            {
                if (_loopSource.isPlaying) _loopSource.Stop();
            }
            catch (Exception e)
            {
                WLog.Line("sfx_stop_error", secret: false, ("err", e.GetType().Name));
            }
        }

        public void StopStoppable()
        {
            if (_stoppableSource == null) return;
            try
            {
                if (_stoppableSource.isPlaying) _stoppableSource.Stop();
            }
            catch (Exception e)
            {
                WLog.Line("sfx_stop_error", secret: false, ("err", e.GetType().Name));
            }
        }

        public void Destroy()
        {
            if (_source != null)
            {
                UnityEngine.Object.Destroy(_source);
                _source = null;
            }
            if (_stoppableSource != null)
            {
                UnityEngine.Object.Destroy(_stoppableSource);
                _stoppableSource = null;
            }
            if (_loopSource != null)
            {
                UnityEngine.Object.Destroy(_loopSource);
                _loopSource = null;
            }
            _mixerResolved = false;
        }

        private static AudioSource CreateSource(GameObject host)
        {
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
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
            if (_source == null) return;

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
                    if (src == null || src == _source || src == _stoppableSource || src == _loopSource) continue;
                    AudioMixerGroup group = src.outputAudioMixerGroup;
                    if (group == null) continue;

                    if (IsVoiceLikeGroup(group.name)) continue;

                    _source.outputAudioMixerGroup = group;
                    if (_stoppableSource != null) _stoppableSource.outputAudioMixerGroup = group;
                    if (_loopSource != null) _loopSource.outputAudioMixerGroup = group;
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
