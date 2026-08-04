using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Voice.Unity;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class VoiceMixerDriver
    {
        private static AccessTools.FieldRef<PlayerAvatar, PlayerVoiceChat> s_voiceChatRef;

        private static AccessTools.FieldRef<PlayerVoiceChat, AudioSource> s_audioSourceRef;

        private static AccessTools.FieldRef<PlayerVoiceChat, Recorder> s_recorderRef;

        private static AccessTools.FieldRef<RunManager, List<PlayerVoiceChat>> s_voiceChatsRef;
        private static bool s_fieldRefResolved;

        private const float SpatialKeepAliveSeconds = 0.1f;

        private const float DeadCueMuteKeepAliveSeconds = 0.1f;

        private readonly PlayerRegistry _registry;

        private VoicePlanKind _lastKind = VoicePlanKind.None;

        private bool _deadCueMuteWasActive;

        private readonly HashSet<int> _touched = new HashSet<int>();

        private readonly HashSet<int> _frameSeenTargets = new HashSet<int>();

        private readonly Dictionary<int, FilterSet> _filters = new Dictionary<int, FilterSet>();

        private readonly HashSet<int> _filterEnabledActors = new HashSet<int>();

        private readonly Dictionary<int, PausedLowPassLogic> _pausedActorLowPassLogics
            = new Dictionary<int, PausedLowPassLogic>();

        private struct PausedLowPassLogic
        {
            public AudioLowPassLogic Logic;
            public bool OriginalEnabled;
        }

        private bool _debugSelfEchoOn;

        private FilterSet _debugSelfEchoFilters;

        private AudioLowPassLogic _pausedLowPassLogic;
        private bool _pausedLowPassLogicOriginalEnabled;

        private struct FilterSet
        {
            public AudioLowPassFilter LowPass;
            public AudioEchoFilter Echo;
            public AudioReverbFilter Reverb;

            public bool IsAlive()
            {
                return LowPass != null && Echo != null && Reverb != null;
            }
        }

        public VoiceMixerDriver(PlayerRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Tick(VoicePlanKind kind,
            Func<int, bool> isEavesdropTarget, Func<int, bool> isDeadActor,
            bool deadCueMuteActive)
        {
            try
            {
                if (!ResolveFieldRef())
                {
                    return;
                }

                if (_debugSelfEchoOn)
                {
                    DebugSelfEchoKeepAlive();
                }

                if (kind != _lastKind)
                {
                    RestoreTouched(isDeadActor);
                    WLog.Line("voice_plan", secret: false,
                        ("from", _lastKind), ("to", kind));
                    _lastKind = kind;
                }

                switch (kind)
                {
                    case VoicePlanKind.None:
                        break;
                    case VoicePlanKind.ResultAll:
                        ApplyResultAll();
                        break;
                    case VoicePlanKind.Eavesdrop:
                        ApplyEavesdrop(isEavesdropTarget, isDeadActor);
                        break;
                }

                ApplyDeadCueMute(deadCueMuteActive, kind, isEavesdropTarget, isDeadActor);
            }
            catch (Exception e)
            {
                WLog.Line("voice_tick_error", secret: false,
                    ("kind", kind), ("err", e.Message));
                ForceRestore("tick_error");
            }
        }

        public void ForceRestore(string reason)
        {
            int touchedCount = _touched.Count;
            if (_debugSelfEchoOn)
            {
                SetDebugSelfEcho(false);
            }
            try
            {
                if (ResolveFieldRef())
                {
                    RestoreTouched(isDeadActor: null);
                }
                else
                {
                    DisableAllFilters(reason);
                    _touched.Clear();
                }
            }
            catch (Exception e)
            {
                _touched.Clear();
                _filterEnabledActors.Clear();
                foreach (var kv in _pausedActorLowPassLogics)
                {
                    if (kv.Value.Logic != null) kv.Value.Logic.enabled = kv.Value.OriginalEnabled;
                }
                _pausedActorLowPassLogics.Clear();
                WLog.Line("voice_restore_error", secret: false,
                    ("reason", reason), ("err", e.Message));
            }
            finally
            {
                _lastKind = VoicePlanKind.None;
                _deadCueMuteWasActive = false;
                WLog.Line("voice_restore", secret: false,
                    ("reason", reason), ("touched", touchedCount));
            }
        }

        public void SanitizeAtRoundStart()
        {
            try
            {
                if (!ResolveFieldRef()) return;

                RunManager rm = RunManager.instance;
                List<PlayerVoiceChat> voices = rm != null ? s_voiceChatsRef(rm) : null;
                if (voices == null)
                {
                    WLog.Line("voice_sanitize", secret: false, ("status", "no_voicechats"));
                    ClearInternalTracking();
                    return;
                }

                int localCount = 0;
                int remoteCount = 0;
                foreach (PlayerVoiceChat vc in voices)
                {
                    if (vc == null) continue;
                    if (s_audioSourceRef(vc) == null) continue;

                    if (vc == PlayerVoiceChat.instance)
                    {
                        vc.ToggleMixer(false, false);
                        localCount++;
                        continue;
                    }

                    vc.ToggleMixer(false, false);
                    GameObject go = ResolveVoiceGameObject(vc);
                    if (go != null)
                    {
                        AudioLowPassLogic logic = go.GetComponent<AudioLowPassLogic>();
                        if (logic != null) logic.enabled = true;
                        AudioEchoFilter echo = go.GetComponent<AudioEchoFilter>();
                        if (echo != null) echo.enabled = false;
                        AudioReverbFilter reverb = go.GetComponent<AudioReverbFilter>();
                        if (reverb != null) reverb.enabled = false;
                    }
                    remoteCount++;
                }

                ClearInternalTracking();
                WLog.Line("voice_sanitize", secret: false,
                    ("remote", remoteCount), ("local", localCount), ("listed", voices.Count));
            }
            catch (Exception e)
            {
                WLog.Line("voice_sanitize_error", secret: false, ("err", e.Message));
            }
        }

        private void ClearInternalTracking()
        {
            _touched.Clear();
            _filterEnabledActors.Clear();
            _pausedActorLowPassLogics.Clear();
            _filters.Clear();
            _lastKind = VoicePlanKind.None;
            _deadCueMuteWasActive = false;
        }

        private static GameObject ResolveVoiceGameObject(PlayerVoiceChat vc)
        {
            AudioSource audioSrc = s_audioSourceRef(vc);
            return audioSrc != null ? audioSrc.gameObject : vc.gameObject;
        }

        public bool IsDebugSelfEchoOn => _debugSelfEchoOn;

        public void SetDebugSelfEcho(bool enabled)
        {
            try
            {
                if (!ResolveFieldRef())
                {
                    WLog.Line("voice_debug_echo_rejected", secret: false,
                        ("enabled", enabled ? 1 : 0), ("reason", "fieldref_failed"));
                    return;
                }

                PlayerVoiceChat vc = PlayerVoiceChat.instance;
                if (vc == null)
                {
                    WLog.Line("voice_debug_echo_rejected", secret: false,
                        ("enabled", enabled ? 1 : 0), ("reason", "no_local_voice"));
                    return;
                }

                Recorder rec = s_recorderRef(vc);
                AudioSource audioSrc = s_audioSourceRef(vc);
                GameObject go = audioSrc != null ? audioSrc.gameObject : vc.gameObject;

                if (enabled)
                {
                    PauseVanillaLowPassLogic(go);
                    EnsureDebugSelfEchoFilters(go);
                    SetDebugSelfEchoFiltersEnabled(true);
                    if (rec != null)
                    {
                        rec.DebugEchoMode = true;
                        rec.TransmitEnabled = true;
                    }
                    if (audioSrc != null) audioSrc.volume = EavesdropVolume();
                    _debugSelfEchoOn = true;
                    WLog.Line("voice_debug_echo", secret: false,
                        ("enabled", 1), ("recorder", rec != null ? 1 : 0),
                        ("volume_set", audioSrc != null ? 1 : 0),
                        ("paused_lowpasslogic", _pausedLowPassLogic != null ? 1 : 0));
                }
                else
                {
                    SetDebugSelfEchoFiltersEnabled(false);
                    if (rec != null) rec.DebugEchoMode = false;
                    if (audioSrc != null) audioSrc.volume = 0f;
                    ResumeVanillaLowPassLogic();
                    _debugSelfEchoOn = false;
                    WLog.Line("voice_debug_echo", secret: false,
                        ("enabled", 0), ("recorder", rec != null ? 1 : 0),
                        ("volume_set", audioSrc != null ? 1 : 0));
                }
            }
            catch (Exception e)
            {
                _debugSelfEchoOn = false;
                WLog.Line("voice_debug_echo_error", secret: false,
                    ("enabled", enabled ? 1 : 0), ("err", e.Message));
            }
        }

        private void DebugSelfEchoKeepAlive()
        {
            PlayerVoiceChat vc = PlayerVoiceChat.instance;
            if (vc == null) return;
            Recorder rec = s_recorderRef(vc);
            AudioSource audioSrc = s_audioSourceRef(vc);
            if (rec != null && !rec.TransmitEnabled) rec.TransmitEnabled = true;
            if (audioSrc != null)
            {
                float volume = EavesdropVolume();
                if (audioSrc.volume != volume) audioSrc.volume = volume;
            }
        }

        private void EnsureDebugSelfEchoFilters(GameObject go)
        {
            if (_debugSelfEchoFilters.IsAlive())
            {
                ApplyFilterParams(_debugSelfEchoFilters);
                return;
            }
            if (go == null) return;

            AudioLowPassFilter lowPass = go.GetComponent<AudioLowPassFilter>();
            if (lowPass == null) lowPass = go.AddComponent<AudioLowPassFilter>();
            AudioEchoFilter echo = go.GetComponent<AudioEchoFilter>();
            if (echo == null) echo = go.AddComponent<AudioEchoFilter>();
            AudioReverbFilter reverb = go.GetComponent<AudioReverbFilter>();
            if (reverb == null) reverb = go.AddComponent<AudioReverbFilter>();

            echo.enabled = false;
            reverb.enabled = false;

            _debugSelfEchoFilters = new FilterSet { LowPass = lowPass, Echo = echo, Reverb = reverb };
            ApplyFilterParams(_debugSelfEchoFilters);
            WLog.Line("voice_debug_echo_filter_ensure", secret: false,
                ("lowpass_existed", lowPass != null && !(echo != null && !echo.enabled) ? 1 : 0));
        }

        private void PauseVanillaLowPassLogic(GameObject go)
        {
            if (go == null || _pausedLowPassLogic != null) return;
            AudioLowPassLogic logic = go.GetComponent<AudioLowPassLogic>();
            if (logic == null) return;
            _pausedLowPassLogic = logic;
            _pausedLowPassLogicOriginalEnabled = logic.enabled;
            logic.enabled = false;
        }

        private void ResumeVanillaLowPassLogic()
        {
            if (_pausedLowPassLogic != null)
            {
                _pausedLowPassLogic.enabled = _pausedLowPassLogicOriginalEnabled;
            }
            _pausedLowPassLogic = null;
            _pausedLowPassLogicOriginalEnabled = false;
        }

        private void SetDebugSelfEchoFiltersEnabled(bool enabled)
        {
            if (!_debugSelfEchoFilters.IsAlive()) return;
            _debugSelfEchoFilters.Echo.enabled = enabled;
            _debugSelfEchoFilters.Reverb.enabled = enabled;
        }

        private void ApplyResultAll()
        {
            GameDirector gd = GameDirector.instance;
            if (gd == null || gd.PlayerList == null) return;

            foreach (PlayerAvatar avatar in gd.PlayerList)
            {
                if (avatar == null) continue;
                int actor = _registry.ResolveActor(avatar);
                if (actor <= 0) continue;
                PlayerVoiceChat vc = s_voiceChatRef(avatar);
                if (vc == null) continue;

                vc.ToggleMixer(true, false);
                _touched.Add(actor);
            }
        }

        private void ApplyEavesdrop(Func<int, bool> isEavesdropTarget, Func<int, bool> isDeadActor)
        {
            GameDirector gd = GameDirector.instance;
            if (gd == null || gd.PlayerList == null) return;

            _frameSeenTargets.Clear();

            foreach (PlayerAvatar avatar in gd.PlayerList)
            {
                if (avatar == null) continue;
                int actor = _registry.ResolveActor(avatar);
                if (actor <= 0) continue;
                PlayerVoiceChat vc = s_voiceChatRef(avatar);
                if (vc == null) continue;

                bool dead = isDeadActor != null && isDeadActor(actor);
                bool isTarget = dead && isEavesdropTarget != null && isEavesdropTarget(actor);

                if (isTarget)
                {
                    _frameSeenTargets.Add(actor);
                    _touched.Add(actor);

                    AudioSource audioSrc = s_audioSourceRef(vc);
                    if (audioSrc != null && audioSrc.outputAudioMixerGroup != vc.mixerMicrophoneSound)
                    {
                        vc.ToggleMixer(false, false);
                    }

                    EnableFilters(actor, vc);

                    if (audioSrc != null) audioSrc.volume = EavesdropVolume();

                    vc.SpatialDisable(SpatialKeepAliveSeconds);
                }
                else if (_touched.Contains(actor))
                {
                    vc.ToggleMixer(dead, false);
                    DisableFilters(actor, "non_target");
                    _touched.Remove(actor);
                }
            }

            if (_touched.Count > _frameSeenTargets.Count)
            {
                _touched.RemoveWhere(a => !_frameSeenTargets.Contains(a));
            }
        }

        private void ApplyDeadCueMute(bool active, VoicePlanKind kind,
            Func<int, bool> isEavesdropTarget, Func<int, bool> isDeadActor)
        {
            if (!active)
            {
                if (_deadCueMuteWasActive)
                {
                    _deadCueMuteWasActive = false;
                    WLog.Line("voice_deadcue_mute", secret: false, ("active", 0));
                }
                return;
            }

            GameDirector gd = GameDirector.instance;
            if (gd == null || gd.PlayerList == null) return;

            int muted = 0;
            foreach (PlayerAvatar avatar in gd.PlayerList)
            {
                if (avatar == null) continue;
                int actor = _registry.ResolveActor(avatar);
                if (actor <= 0) continue;

                bool dead = isDeadActor != null && isDeadActor(actor);
                bool eavesdropAudible = kind == VoicePlanKind.Eavesdrop
                    && isEavesdropTarget != null && isEavesdropTarget(actor);
                if (!VoiceRules.IsDeadCueMuteTarget(dead, eavesdropAudible)) continue;

                PlayerVoiceChat vc = s_voiceChatRef(avatar);
                if (vc == null) continue;
                vc.OverrideMute(DeadCueMuteKeepAliveSeconds);
                muted++;
            }

            if (!_deadCueMuteWasActive)
            {
                _deadCueMuteWasActive = true;
                WLog.Line("voice_deadcue_mute", secret: false, ("active", 1), ("muted", muted));
            }
        }

        private static float EavesdropVolume()
        {
            GameConfig cfg = Plugin.GameConfig;
            return cfg != null ? Mathf.Clamp01(cfg.NecroVoiceVolume) : 0.05f;
        }

        private void RestoreTouched(Func<int, bool> isDeadActor)
        {
            if (_touched.Count == 0 && _filterEnabledActors.Count == 0) return;

            GameDirector gd = GameDirector.instance;
            if (gd != null && gd.PlayerList != null)
            {
                foreach (PlayerAvatar avatar in gd.PlayerList)
                {
                    if (avatar == null) continue;
                    int actor = _registry.ResolveActor(avatar);
                    if (actor <= 0) continue;
                    if (!_touched.Contains(actor)) continue;
                    PlayerVoiceChat vc = s_voiceChatRef(avatar);
                    if (vc == null) continue;

                    bool dead = isDeadActor != null
                        ? isDeadActor(actor)
                        : _registry.IsDeadSet(avatar);

                    vc.ToggleMixer(dead, false);
                    DisableFilters(actor, "restore");
                }
            }

            _touched.Clear();
            DisableAllFilters("restore_stale");
        }

        private void EnsureFilters(int actor, PlayerVoiceChat vc)
        {
            if (_filters.TryGetValue(actor, out FilterSet set) && set.IsAlive())
            {
                ApplyFilterParams(set);
                return;
            }

            AudioSource audioSrc = s_audioSourceRef(vc);
            GameObject go = audioSrc != null ? audioSrc.gameObject : vc.gameObject;
            if (go == null) return;

            AudioLowPassFilter lowPass = go.GetComponent<AudioLowPassFilter>();
            if (lowPass == null) lowPass = go.AddComponent<AudioLowPassFilter>();
            AudioEchoFilter echo = go.GetComponent<AudioEchoFilter>();
            if (echo == null) echo = go.AddComponent<AudioEchoFilter>();
            AudioReverbFilter reverb = go.GetComponent<AudioReverbFilter>();
            if (reverb == null) reverb = go.AddComponent<AudioReverbFilter>();

            echo.enabled = false;
            reverb.enabled = false;

            set = new FilterSet { LowPass = lowPass, Echo = echo, Reverb = reverb };
            _filters[actor] = set;

            ApplyFilterParams(set);
            WLog.Line("voice_filter_ensure", secret: false, ("actor", actor));
        }

        private void PauseActorLowPassLogic(int actor, GameObject go)
        {
            if (go == null) return;
            if (_pausedActorLowPassLogics.ContainsKey(actor)) return;
            AudioLowPassLogic logic = go.GetComponent<AudioLowPassLogic>();
            if (logic == null) return;
            _pausedActorLowPassLogics[actor] = new PausedLowPassLogic
            {
                Logic = logic,
                OriginalEnabled = logic.enabled,
            };
            logic.enabled = false;
        }

        private void ResumeActorLowPassLogic(int actor)
        {
            if (!_pausedActorLowPassLogics.TryGetValue(actor, out PausedLowPassLogic p)) return;
            if (p.Logic != null) p.Logic.enabled = p.OriginalEnabled;
            _pausedActorLowPassLogics.Remove(actor);
        }

        private static void ApplyFilterParams(FilterSet set)
        {
            if (!set.IsAlive()) return;
            GameConfig cfg = Plugin.GameConfig;
            if (cfg == null) return;

            set.LowPass.cutoffFrequency = cfg.NecroVoiceLowPassCutoffHz;

            set.Echo.delay = cfg.NecroVoiceEchoDelayMs;
            set.Echo.decayRatio = cfg.NecroVoiceEchoDecay;
            set.Echo.wetMix = cfg.NecroVoiceEchoDecay;
            set.Echo.dryMix = 1f;

            set.Reverb.reverbPreset = AudioReverbPreset.User;
            set.Reverb.dryLevel = 0f;
            set.Reverb.room = cfg.NecroVoiceReverbRoom;
            set.Reverb.roomHF = cfg.NecroVoiceReverbRoomHF;
            set.Reverb.decayTime = cfg.NecroVoiceReverbDecayTime;
            set.Reverb.decayHFRatio = cfg.NecroVoiceReverbDecayHFRatio;
            set.Reverb.reflectionsLevel = cfg.NecroVoiceReverbReflections;
            set.Reverb.reflectionsDelay = cfg.NecroVoiceReverbReflectionsDelay;
            set.Reverb.reverbLevel = cfg.NecroVoiceReverbLevel;
            set.Reverb.reverbDelay = cfg.NecroVoiceReverbDelay;
            set.Reverb.diffusion = cfg.NecroVoiceReverbDiffusion;
            set.Reverb.density = cfg.NecroVoiceReverbDensity;
            set.Reverb.hfReference = cfg.NecroVoiceReverbHFReference;
        }

        private void EnableFilters(int actor, PlayerVoiceChat vc)
        {
            EnsureFilters(actor, vc);
            if (!_filters.TryGetValue(actor, out FilterSet set) || !set.IsAlive()) return;

            AudioSource audioSrc = s_audioSourceRef(vc);
            GameObject go = audioSrc != null ? audioSrc.gameObject : vc.gameObject;
            PauseActorLowPassLogic(actor, go);

            set.Echo.enabled = true;
            set.Reverb.enabled = true;

            if (_filterEnabledActors.Add(actor))
            {
                WLog.Line("voice_filter_apply", secret: false, ("actor", actor));
            }
        }

        private void DisableFilters(int actor, string reason)
        {
            if (_filters.TryGetValue(actor, out FilterSet set) && set.IsAlive())
            {
                set.Echo.enabled = false;
                set.Reverb.enabled = false;
            }
            ResumeActorLowPassLogic(actor);
            if (_filterEnabledActors.Remove(actor))
            {
                WLog.Line("voice_filter_disable", secret: false,
                    ("actor", actor), ("reason", reason));
            }
        }

        private void DisableAllFilters(string reason)
        {
            if (_filterEnabledActors.Count == 0) return;
            int[] actors = new int[_filterEnabledActors.Count];
            _filterEnabledActors.CopyTo(actors);
            foreach (int a in actors)
            {
                DisableFilters(a, reason);
            }
        }

        private static bool ResolveFieldRef()
        {
            if (s_fieldRefResolved) return true;
            s_voiceChatRef = GameRefs.PlayerAvatar_voiceChat;
            s_audioSourceRef = GameRefs.PlayerVoiceChat_audioSource;
            s_recorderRef = GameRefs.PlayerVoiceChat_recorder;
            s_voiceChatsRef = GameRefs.RunManager_voiceChats;
            s_fieldRefResolved = true;
            return true;
        }
    }
}
