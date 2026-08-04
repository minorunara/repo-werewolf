using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
{
    public static class BombExplosionDriver
    {
        private const int LethalDamage = 9999;

        private const float HurtColliderBaseRadiusMeters = 1.5f;

        private static readonly FieldInfo ExplosionPrefabField =
            GameRefs.ParticleScriptExplosion_explosionPrefab;

        private static ParticleScriptExplosion _source;

        private static bool _warmedThisRound;
        private static WarmupCoroutineHost _warmupHost;

        public static void Detonate(PlayerAvatar epicenter, bool localIsBomber,
            int playerDamage, int enemyDamage, float blastRadiusMeters)
        {
            if (epicenter == null)
            {
                WLog.Line("bomb_explode_fail", secret: false, ("reason", "epicenter_null"));
                return;
            }
            DetonateAt(epicenter.transform.position + Vector3.up * 0.5f, epicenter,
                localIsBomber, playerDamage, enemyDamage, blastRadiusMeters);
        }

        public static void DetonateAt(Vector3 pos, PlayerAvatar ignorePlayer, bool localIsBomber,
            int playerDamage, int enemyDamage, float blastRadiusMeters)
        {
            ParticleScriptExplosion source = EnsureSource();
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

            GameRefs.ParticlePrefabExplosion_HurtColliderSecondSetup?.SetValue(explosion, false);

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
                int currentHealth = GameRefs.PlayerHealth_health(ignorePlayer.playerHealth);
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

        private static ParticleScriptExplosion EnsureSource()
        {
            if (_source != null) return _source;

            ExplosionPreset preset = FindPreset();
            if (preset == null)
            {
                WLog.Line("bomb_explode_fail", secret: false, ("reason", "preset_not_found"));
                return null;
            }
            GameObject prefab = Resources.Load<GameObject>("Effects/Part Prefab Explosion");
            if (prefab == null)
            {
                WLog.Line("bomb_explode_fail", secret: false, ("reason", "prefab_not_found"));
                return null;
            }

            var go = new GameObject("WW_BombExplosionSource");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<ParticleScriptExplosion>();
            _source.explosionPreset = preset;
            ExplosionPrefabField?.SetValue(_source, prefab);
            WLog.Line("bomb_explode_source_ready", secret: false, ("preset", preset.name));
            return _source;
        }

        public static void WarmupOnce()
        {
            if (_warmedThisRound) return;
            ParticleScriptExplosion source = EnsureSource();
            if (source == null) return;
            _warmedThisRound = true;

            if (_warmupHost == null)
            {
                _warmupHost = source.gameObject.AddComponent<WarmupCoroutineHost>();
            }
            _warmupHost.Run(source);
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

        private static ExplosionPreset FindPreset()
        {
            ExplosionPreset[] loaded = Resources.FindObjectsOfTypeAll<ExplosionPreset>();
            if (loaded.Length > 0) return loaded[0];
            ExplosionPreset[] fromResources = Resources.LoadAll<ExplosionPreset>("");
            return fromResources.Length > 0 ? fromResources[0] : null;
        }
    }
}
