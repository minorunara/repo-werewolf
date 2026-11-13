#nullable enable
using System;
using UnityEngine;
using UnityEngine.Audio;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class SfxPlayer
    {
        private sealed class AudioRefs
        {
            public AudioRefs(AudioSource oneShot, AudioSource stoppable,
                AudioSource loop, AudioSource conveneCharge)
            {
                OneShot = oneShot;
                Stoppable = stoppable;
                Loop = loop;
                ConveneCharge = conveneCharge;
                All = new[] { oneShot, stoppable, loop, conveneCharge };
            }

            public AudioSource OneShot { get; }
            public AudioSource Stoppable { get; }
            public AudioSource Loop { get; }
            public AudioSource ConveneCharge { get; }
            public AudioSource[] All { get; }
        }

        private AudioRefs? _audio;

        private string? _stoppableClipKey;

        private string? _loopClipKey;

        private bool _mixerResolved;

        public bool Exists => GetLiveAudioOrNull() != null;

        public bool CanPlay
        {
            get
            {
                AudioRefs? audio = GetLiveAudioOrNull();
                return audio != null && audio.OneShot.outputAudioMixerGroup != null;
            }
        }

        public void Build(GameObject host)
        {
            if (GetLiveAudioOrNull() != null || host == null) return;

            var created = new System.Collections.Generic.List<AudioSource>(4);
            try
            {
                AudioSource oneShot = CreateSource(host, loop: false); created.Add(oneShot);
                AudioSource stoppable = CreateSource(host, loop: false); created.Add(stoppable);
                AudioSource loop = CreateSource(host, loop: true); created.Add(loop);
                AudioSource conveneCharge = CreateSource(host, loop: true); created.Add(conveneCharge);
                _audio = new AudioRefs(oneShot, stoppable, loop, conveneCharge);
                TryResolveMixerGroup();
            }
            catch (Exception e)
            {
                foreach (AudioSource source in created)
                {
                    if (source != null) UnityEngine.Object.Destroy(source);
                }
                _audio = null;
                WLog.Line("sfx_build_error", secret: false, ("err", e.GetType().Name));
            }
        }

        public void Play(string clipKey, float volumeScale = 1f)
        {
            AudioRefs? audio = GetLiveAudioOrNull();
            if (audio == null || !MixerReady(audio.OneShot)) return;
            AudioClip? clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                audio.OneShot.PlayOneShot(clip, volumeScale);
            }
            catch (Exception e)
            {
                LogPlayError(clipKey, e);
            }
        }

        public void PlayStoppable(string clipKey, float volumeScale = 1f)
        {
            AudioRefs? audio = GetLiveAudioOrNull();
            if (audio == null || !MixerReady(audio.Stoppable)) return;
            AudioClip? clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                audio.Stoppable.Stop();
                _stoppableClipKey = clipKey;
                audio.Stoppable.clip = clip;
                audio.Stoppable.volume = volumeScale;
                audio.Stoppable.Play();
            }
            catch (Exception e)
            {
                LogPlayError(clipKey, e);
            }
        }

        public void PlayLoop(string clipKey, float volume)
        {
            AudioRefs? audio = GetLiveAudioOrNull();
            if (audio == null || !MixerReady(audio.Loop)) return;
            AudioClip? clip = AssetCatalog.GetClip(clipKey);
            if (clip == null) return;

            try
            {
                _loopClipKey = clipKey;
                audio.Loop.volume = Mathf.Clamp01(volume);
                KeepLooping(audio.Loop, clip);
            }
            catch (Exception e)
            {
                LogPlayError(clipKey, e);
            }
        }

        public void StopLoop(string clipKey)
        {
            if (clipKey != _loopClipKey) return;
            StopSource(GetLiveAudioOrNull()?.Loop, clipKey);
        }

        public void PlayConveneCharge(float ratio)
        {
            AudioRefs? audio = GetLiveAudioOrNull();
            if (audio == null || !MixerReady(audio.ConveneCharge)) return;
            AudioClip? clip = AssetCatalog.GetConveneChargeClip();
            if (clip == null) return;

            try
            {
                audio.ConveneCharge.pitch = ConveneChargeTone.PitchFor(ratio);
                audio.ConveneCharge.volume = ConveneChargeTone.VolumeFor(ratio);
                KeepLooping(audio.ConveneCharge, clip);
            }
            catch (Exception e)
            {
                LogPlayError("convene_charge", e);
            }
        }

        public void StopConveneCharge()
        {
            AudioRefs? audio = GetLiveAudioOrNull();
            if (audio == null) return;
            StopSource(audio.ConveneCharge, "convene_charge");
            audio.ConveneCharge.pitch = 1f;
            audio.ConveneCharge.volume = 0f;
        }

        public void StopStoppable(string? clipKey = null)
        {
            if (clipKey != null && clipKey != _stoppableClipKey) return;
            StopSource(GetLiveAudioOrNull()?.Stoppable, clipKey);
        }

        public void Destroy()
        {
            AudioRefs? audio = _audio;
            _audio = null;
            if (audio != null)
            {
                foreach (AudioSource source in audio.All)
                {
                    if (source != null) UnityEngine.Object.Destroy(source);
                }
            }
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

        private static void StopSource(AudioSource? source, string? key)
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
            AudioSource? source = null;
            try
            {
                source = host.AddComponent<AudioSource>();
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
            catch
            {
                if (source != null) UnityEngine.Object.Destroy(source);
                throw;
            }
        }

        private void TryResolveMixerGroup()
        {
            AudioRefs? audio = GetLiveAudioOrNull();
            if (audio == null) return;

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
                    if (src == null || Array.IndexOf(audio.All, src) >= 0) continue;
                    AudioMixerGroup group = src.outputAudioMixerGroup;
                    if (group == null) continue;

                    if (IsVoiceLikeGroup(group.name)) continue;

                    foreach (AudioSource own in audio.All) own.outputAudioMixerGroup = group;
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

        private AudioRefs? GetLiveAudioOrNull()
        {
            AudioRefs? audio = _audio;
            if (audio == null) return null;
            bool alive = true;
            foreach (AudioSource source in audio.All)
            {
                if (source == null) { alive = false; break; }
            }
            if (alive) return audio;

            _audio = null;
            foreach (AudioSource source in audio.All)
            {
                if (source != null) UnityEngine.Object.Destroy(source);
            }
            _stoppableClipKey = null;
            _loopClipKey = null;
            _mixerResolved = false;
            return null;
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
