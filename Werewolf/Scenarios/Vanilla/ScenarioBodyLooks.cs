using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioBodyLooks
    {
        private const string MenuAvatarResource = ScenarioVanillaNames.MenuAvatarResource;
        private const string DeathHeadResource = ScenarioVanillaNames.DeathHeadResource;
        private const string DeathHeadMeshChild = ScenarioVanillaNames.DeathHeadMeshChild;

        private const float MenuAvatarFallbackPivotHeight = 0.686f;

        private const float MinPivotHeight = 0.2f;
        private const float MaxPivotHeight = 2f;

        internal static FakeBodyVisual Semibot(int actor, Vector3 groundPos)
        {
            return Assemble("semibot", actor, Resources.Load<GameObject>(MenuAvatarResource), root =>
            {
                root.name = "WW_FakeBody_Semibot_" + actor;
                PlayerAvatarMenu menu = root.GetComponent<PlayerAvatarMenu>();
                if (menu == null) throw new InvalidOperationException("PlayerAvatarMenu missing");

                menu.worldAvatar = true;
                menu.iconMakerAvatar = false;
                menu.expressionAvatar = false;

                if (menu.cameraAndStuff != null)
                {
                    UnityEngine.Object.DestroyImmediate(menu.cameraAndStuff.gameObject);
                    menu.cameraAndStuff = null;
                }
                foreach (PlayerCrown crown in root.GetComponentsInChildren<PlayerCrown>(true))
                {
                    UnityEngine.Object.DestroyImmediate(crown.gameObject);
                }
                foreach (PlayerHealth health in root.GetComponents<PlayerHealth>())
                {
                    UnityEngine.Object.DestroyImmediate(health);
                }
                StripPhysics(root);

                PlayerAvatarVisuals visuals = root.GetComponentInChildren<PlayerAvatarVisuals>(true);
                float measured = visuals != null
                    ? root.transform.position.y - visuals.transform.position.y
                    : float.NaN;
                float pivotHeight = measured >= MinPivotHeight && measured <= MaxPivotHeight
                    ? measured
                    : MenuAvatarFallbackPivotHeight;

                root.transform.position = groundPos + Vector3.up * pivotHeight;
                root.SetActive(true);

                PlayerCosmetics cosmetics = root.GetComponentInChildren<PlayerCosmetics>();
                if (cosmetics != null) ApplySemibotColor(cosmetics, BotColors.IndexFor(actor));

                Animator animator = visuals != null ? visuals.GetComponent<Animator>() : null;
                return new FakeBodyVisual(root, pivotHeight, animator);
            });
        }

        internal static FakeBodyVisual DeathHead(int actor, Vector3 groundPos)
        {
            GameObject prefab = Resources.Load<GameObject>(DeathHeadResource);
            Transform mesh = prefab != null ? prefab.transform.Find(DeathHeadMeshChild) : null;
            return Assemble("death_head", actor, mesh != null ? mesh.gameObject : null, root =>
            {
                root.name = "WW_FakeBody_DeathHead_" + actor;
                foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!(mb is PlayerMaterial)) UnityEngine.Object.DestroyImmediate(mb);
                }
                StripPhysics(root);

                root.transform.position = groundPos;
                root.SetActive(true);
                ApplyDeathHeadColor(root, BotColors.IndexFor(actor));
                return new FakeBodyVisual(root, pivotHeight: 0f, animator: null);
            });
        }

        private static FakeBodyVisual Assemble(string look, int actor, GameObject source,
            Func<GameObject, FakeBodyVisual> build)
        {
            if (source == null) return Fallback(look, actor, "no_prefab");

            GameObject root = InstantiateInactive(source);
            string failure;
            try
            {
                FakeBodyVisual visual = build(root);
                if (!visual.IsEmpty) return visual;
                failure = "empty_visual";
            }
            catch (Exception e)
            {
                failure = e.Message;
            }
            UnityEngine.Object.Destroy(root);
            return Fallback(look, actor, failure);
        }

        private static GameObject InstantiateInactive(GameObject prefab)
        {
            bool wasActive = prefab.activeSelf;
            prefab.SetActive(false);
            try
            {
                return UnityEngine.Object.Instantiate(prefab);
            }
            finally
            {
                prefab.SetActive(wasActive);
            }
        }

        private static void StripPhysics(GameObject root)
        {
            foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                UnityEngine.Object.DestroyImmediate(rb);
            }
            foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(col);
            }
        }

        private static void ApplySemibotColor(PlayerCosmetics cosmetics, int colorIndex)
        {
            try
            {
                cosmetics.SetupCosmetics(_synced: false, _forced: true, new List<int>());
                int typeCount = Enum.GetValues(typeof(SemiFunc.CosmeticType)).Length;
                int[] colors = new int[typeCount];
                for (int i = 0; i < colors.Length; i++) colors[i] = colorIndex;
                cosmetics.SetupColors(_synced: false, colors);
            }
            catch (Exception e)
            {
                WLog.Line("scenario_body_color_failed", secret: false, ("look", "semibot"), ("err", e.Message));
            }
        }

        private static void ApplyDeathHeadColor(GameObject root, int colorIndex)
        {
            try
            {
                int albedo = Shader.PropertyToID("_AlbedoColor");
                int emission = Shader.PropertyToID("_EmissionColor");
                int fresnel = Shader.PropertyToID("_FresnelColor");
                foreach (PlayerMaterial material in root.GetComponentsInChildren<PlayerMaterial>(true))
                {
                    material.Setup();
                    material.ColorSet(albedo, emission, fresnel, colorIndex);
                }
            }
            catch (Exception e)
            {
                WLog.Line("scenario_body_color_failed", secret: false, ("look", "death_head"), ("err", e.Message));
            }
        }

        private static FakeBodyVisual Fallback(string look, int actor, string reason)
        {
            WLog.Line("scenario_body_look_fallback", secret: false,
                ("look", look), ("actor", actor), ("reason", reason));
            return default;
        }
    }
}
