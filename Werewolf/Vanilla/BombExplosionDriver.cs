#nullable enable
using System.Collections;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    public static class BombExplosionDriver
    {
        private const int LethalDamage = 9999;

        private const float HurtColliderBaseRadiusMeters = 1.5f;

        private static readonly string[] PresetDonorPrefabPaths =
        {
            "Items/Item Grenade Explosive",
            "Items/Item Mine Explosive",
            "Items/Item Grenade Duct Taped",
        };

        private sealed class ExplosionSourceState
        {
            public ExplosionSourceState(GameObject root, ParticleScriptExplosion source)
            {
                Root = root;
                Source = source;
            }

            public GameObject Root { get; }
            public ParticleScriptExplosion Source { get; }
            public WarmupCoroutineHost? WarmupHost { get; set; }
        }

        private static ExplosionSourceState? _sourceState;
        private static ExplosionPreset? _fallback;

        private static bool _warmedThisRound;

        public static void Detonate(PlayerHandle epicenter, bool localIsBomber,
            int playerDamage, int enemyDamage, float blastRadiusMeters)
        {
            PlayerAvatar? avatar = epicenter.Avatar;
            if (avatar == null)
            {
                WLog.Line("bomb_explode_fail", secret: false, ("reason", "epicenter_null"));
                return;
            }
            DetonateAt(avatar.transform.position + Vector3.up * 0.5f, avatar,
                localIsBomber, playerDamage, enemyDamage, blastRadiusMeters);
        }

        public static void DetonateAt(Vector3 pos, bool localIsBomber,
            int playerDamage, int enemyDamage, float blastRadiusMeters)
            => DetonateAt(pos, null, localIsBomber, playerDamage, enemyDamage, blastRadiusMeters);

        private static void DetonateAt(Vector3 pos, PlayerAvatar? ignorePlayer, bool localIsBomber,
            int playerDamage, int enemyDamage, float blastRadiusMeters)
        {
            ParticleScriptExplosion? source = EnsureSource();
            if (source == null) return;

            source.transform.position = pos;

            float size = Mathf.Clamp(
                blastRadiusMeters / HurtColliderBaseRadiusMeters, 0.5f, 20f);
            int localPlayerDamage = localIsBomber ? LethalDamage : playerDamage;

            ParticlePrefabExplosion explosion;
            try
            {
                explosion = source.Spawn(pos, size, localPlayerDamage, enemyDamage, 1f, false, false, 1f);
            }
            catch (System.Exception e)
            {
                WLog.Line("bomb_explode_fail", secret: false,
                    ("reason", "spawn_exception"), ("err", e.GetType().Name));
                return;
            }
            if (explosion == null)
            {
                WLog.Line("bomb_explode_fail", secret: false, ("reason", "spawn_null"));
                return;
            }

            explosion.HurtColliderSecondSetup = false;

            HurtCollider hurt = explosion.HurtCollider;
            float hurtBaseRadius = -1f;
            float blastRadiusWorld = -1f;
            if (hurt != null)
            {
                hurt.playerDamageCooldown = 1f;
                if (ignorePlayer != null)
                {
                    hurt.ignorePlayers.Add(ignorePlayer);
                }
                SphereCollider sphere = hurt.GetComponent<SphereCollider>();
                if (sphere != null)
                {
                    float parentScale = hurt.transform.parent != null
                        ? hurt.transform.parent.lossyScale.x
                        : 1f;
                    hurtBaseRadius = sphere.radius;
                    blastRadiusWorld = sphere.radius * parentScale * size;
                }
            }

            int targetDamage = 0;
            if (ignorePlayer != null && ignorePlayer.playerHealth != null)
            {
                int currentHealth = ignorePlayer.playerHealth.health;
                targetDamage = BombDamageRules.TargetDamage(
                    playerDamage, currentHealth);
                if (targetDamage > 0)
                {
                    ignorePlayer.playerHealth.Hurt(targetDamage, false, -1, false);
                }
            }

            WLog.Line("bomb_explode", secret: false,
                ("local_bomber", localIsBomber),
                ("size", size),
                ("player_dmg", localPlayerDamage),
                ("target_dmg", targetDamage),
                ("enemy_dmg", enemyDamage),
                ("cfg_r_m", blastRadiusMeters),
                ("hurt_r", hurtBaseRadius),
                ("blast_r_m", blastRadiusWorld));
        }

        private static ParticleScriptExplosion? EnsureSource()
        {
            ExplosionSourceState? existing = GetLiveSourceStateOrNull();
            if (existing != null)
            {
                UpgradeFallbackPreset();
                return existing.Source;
            }

            ExplosionPreset? preset = ResolvePreset(out string presetSource);
            if (preset == null)
            {
                WLog.Line("bomb_explode_fail", secret: false, ("reason", "preset_not_found"));
                return null;
            }
            GameObject? prefab = Resources.Load<GameObject>("Effects/Part Prefab Explosion");
            if (prefab == null)
            {
                WLog.Line("bomb_explode_fail", secret: false, ("reason", "prefab_not_found"));
                return null;
            }

            GameObject? partialRoot = null;
            try
            {
                var go = new GameObject("WW_BombExplosionSource");
                partialRoot = go;
                Object.DontDestroyOnLoad(go);
                ParticleScriptExplosion source = go.AddComponent<ParticleScriptExplosion>();
                source.explosionPreset = preset;
                source.explosionPrefab = prefab;
                _sourceState = new ExplosionSourceState(go, source);
                partialRoot = null;
                WLog.Line("bomb_explode_source_ready", secret: false,
                    ("preset", preset.name), ("src", presetSource));
                return source;
            }
            catch (System.Exception e)
            {
                if (partialRoot != null) Object.Destroy(partialRoot);
                _sourceState = null;
                WLog.Line("bomb_explode_fail", secret: false,
                    ("reason", "source_build_exception"), ("err", e.GetType().Name));
                return null;
            }
        }

        public static void WarmupOnce()
        {
            if (_warmedThisRound) return;
            ParticleScriptExplosion? source = EnsureSource();
            if (source == null) return;

            ExplosionSourceState? state = GetLiveSourceStateOrNull();
            if (state == null) return;
            if (state.WarmupHost == null)
            {
                state.WarmupHost = source.gameObject.AddComponent<WarmupCoroutineHost>();
            }
            state.WarmupHost.Run(source);
            _warmedThisRound = true;
        }

        public static void ResetWarmup()
        {
            _warmedThisRound = false;
        }

        private sealed class WarmupCoroutineHost : MonoBehaviour
        {
            public void Run(ParticleScriptExplosion source)
            {
                StartCoroutine(RunCoroutine(source));
            }

            private static IEnumerator RunCoroutine(ParticleScriptExplosion source)
            {
                Vector3 far = new Vector3(0f, -5000f, 0f);
                try
                {
                    source.transform.position = far;
                    source.Spawn(far, 0.01f, 0, 0, 0f, false, false, 0f);
                    WLog.Line("bomb_warmup", secret: false);
                }
                catch (System.Exception e)
                {
                    WLog.Line("bomb_warmup_fail", secret: false, ("err", e.GetType().Name));
                }
                yield break;
            }
        }

        private static ExplosionPreset ResolvePreset(out string source)
        {
            ExplosionPreset? preset = FindPreset();
            if (preset != null)
            {
                source = "loaded";
                return preset;
            }

            preset = LoadPreset();
            if (preset != null)
            {
                source = "item_prefab";
                return preset;
            }

            source = "builtin";
            return BuildPreset();
        }

        private static void UpgradeFallbackPreset()
        {
            ExplosionSourceState? state = GetLiveSourceStateOrNull();
            if (_fallback == null || state == null || state.Source.explosionPreset != _fallback) return;
            ExplosionPreset? real = FindPreset() ?? LoadPreset();
            if (real == null) return;
            state.Source.explosionPreset = real;
            WLog.Line("bomb_explode_source_upgraded", secret: false, ("preset", real.name));
        }

        private static ExplosionSourceState? GetLiveSourceStateOrNull()
        {
            ExplosionSourceState? state = _sourceState;
            if (state == null) return null;
            if (state.Root != null && state.Source != null) return state;
            _sourceState = null;
            if (state.Root != null) Object.Destroy(state.Root);
            return null;
        }

        private static ExplosionPreset? FindPreset()
        {
            ExplosionPreset[] loaded = Resources.FindObjectsOfTypeAll<ExplosionPreset>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && loaded[i] != _fallback) return loaded[i];
            }
            return null;
        }

        private static ExplosionPreset? LoadPreset()
        {
            for (int i = 0; i < PresetDonorPrefabPaths.Length; i++)
            {
                GameObject? donor = Resources.Load<GameObject>(PresetDonorPrefabPaths[i]);
                if (donor == null) continue;
                ParticleScriptExplosion? script = donor.GetComponentInChildren<ParticleScriptExplosion>(true);
                if (script != null && script.explosionPreset != null) return script.explosionPreset;
            }
            return null;
        }

        private static ExplosionPreset BuildPreset()
        {
            var preset = ScriptableObject.CreateInstance<ExplosionPreset>();
            preset.name = "WW_ExplosionFallback";
            preset.explosionForceMultiplier = 1f;
            preset.explosionColors = Ramp(
                new Color(1f, 0.85f, 0.35f), new Color(1f, 0.35f, 0.05f), new Color(0.25f, 0.1f, 0.05f));
            preset.smokeColors = Ramp(
                new Color(0.35f, 0.33f, 0.3f), new Color(0.22f, 0.21f, 0.2f), new Color(0.12f, 0.12f, 0.12f));
            preset.lightColor = Ramp(
                new Color(1f, 0.9f, 0.6f), new Color(1f, 0.5f, 0.15f), Color.black);
            preset.explosionSoundSmall = Silent();
            preset.explosionSoundSmallGlobal = Silent();
            preset.explosionSoundMedium = Silent();
            preset.explosionSoundMediumGlobal = Silent();
            preset.explosionSoundBig = Silent();
            preset.explosionSoundBigGlobal = Silent();
            Object.DontDestroyOnLoad(preset);
            _fallback = preset;
            return preset;
        }

        private static Gradient Ramp(Color start, Color middle, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(middle, 0.5f),
                    new GradientColorKey(end, 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Sound Silent() => new Sound { Sounds = new AudioClip[0] };
    }
}
