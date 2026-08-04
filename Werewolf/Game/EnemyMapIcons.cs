using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    internal static class EnemyMapIcons
    {
        private static readonly AccessTools.FieldRef<EnemyParent, Enemy> EnemyRef = GameRefs.EnemyParent_Enemy;
        private static readonly AccessTools.FieldRef<EnemyParent, bool> SpawnedRef = GameRefs.EnemyParent_Spawned;
        private static readonly AccessTools.FieldRef<Enemy, EnemyRigidbody> RigidbodyRef = GameRefs.Enemy_Rigidbody;
        private static readonly AccessTools.FieldRef<Enemy, bool> HasRigidbodyRef = GameRefs.Enemy_HasRigidbody;
        private static readonly AccessTools.FieldRef<MapCustom, MapCustomEntity> MapEntityRef = GameRefs.MapCustom_mapCustomEntity;

        private static Sprite _spriteLevel1;
        private static Sprite _spriteLevel2;
        private static Sprite _spriteLevel3;

        private static readonly List<MapCustom> _added = new List<MapCustom>();
        private static bool _active;

        private const float VeilLiftY = 0.2f;
        private const int VeilSortingOrder = 100;

        private static bool GateOpen()
        {
            WerewolfDirector dir = WerewolfDirector.Instance;
            return dir != null && dir.IsRoundActiveClient && dir.LocalRoleClient == Role.Werewolf;
        }

        internal static void Tick()
        {
            bool gate = GateOpen();
            if (gate == _active) return;
            _active = gate;
            if (gate) AttachAll();
            else ClearAll();
        }

        internal static void LateTick()
        {
            if (!_active) return;
            foreach (MapCustom custom in _added)
            {
                if (custom == null) continue;
                MapCustomEntity entity = MapEntityRef(custom);
                if (entity == null || entity.spriteRenderer == null) continue;
                SpriteRenderer renderer = entity.spriteRenderer;
                Vector3 local = renderer.transform.localPosition;
                if (local.y != VeilLiftY)
                {
                    local.y = VeilLiftY;
                    renderer.transform.localPosition = local;
                }
                if (renderer.sortingOrder < VeilSortingOrder)
                {
                    renderer.sortingOrder = VeilSortingOrder;
                }
            }
        }

        internal static void OnSpawn(EnemyParent parent)
        {
            if (!GateOpen()) return;
            Attach(parent);
        }

        internal static void OnDespawn(EnemyParent parent)
        {
            Enemy enemy = EnemyRef(parent);
            if (enemy == null) return;
            MapCustom custom = ResolveIconObject(enemy).GetComponent<MapCustom>();
            if (custom == null) return;
            int index = _added.IndexOf(custom);
            if (index < 0) return;
            _added.RemoveAt(index);
            DestroyIcon(custom);
        }

        internal static void ClearAll()
        {
            int count = 0;
            foreach (MapCustom custom in _added)
            {
                if (custom == null) continue;
                DestroyIcon(custom);
                count++;
            }
            _added.Clear();
            WLog.Line("enemy_map_off", secret: true, ("destroyed", count));
        }

        private static void AttachAll()
        {
            int count = 0;
            foreach (EnemyParent parent in UnityEngine.Object.FindObjectsOfType<EnemyParent>())
            {
                if (!SpawnedRef(parent)) continue;
                if (Attach(parent)) count++;
            }
            WLog.Line("enemy_map_on", secret: true, ("icons", count));
        }

        private static bool Attach(EnemyParent parent)
        {
            Enemy enemy = EnemyRef(parent);
            if (enemy == null) return false;

            GameObject target = ResolveIconObject(enemy);
            if (target.GetComponent<MapCustom>() != null) return false;

            MapCustom custom = target.AddComponent<MapCustom>();
            custom.autoAdd = false;
            custom.sprite = SpriteFor(parent.difficulty);
            custom.color = IsMonsterSprite(custom.sprite) ? Color.white : Color.red;
            custom.Add();
            _added.Add(custom);
            return true;
        }

        private static GameObject ResolveIconObject(Enemy enemy)
        {
            EnemyRigidbody rigidbody = HasRigidbodyRef(enemy) ? RigidbodyRef(enemy) : null;
            return rigidbody != null ? rigidbody.gameObject : enemy.gameObject;
        }

        private static void DestroyIcon(MapCustom custom)
        {
            MapCustomEntity entity = MapEntityRef(custom);
            if (entity != null)
            {
                UnityEngine.Object.Destroy(entity.gameObject);
            }
            UnityEngine.Object.Destroy(custom);
        }

        private static Sprite SpriteFor(EnemyParent.Difficulty difficulty)
        {
            EnsureSprites();
            switch (difficulty)
            {
                case EnemyParent.Difficulty.Difficulty2: return _spriteLevel2;
                case EnemyParent.Difficulty.Difficulty3: return _spriteLevel3;
                default: return _spriteLevel1;
            }
        }

        private static bool IsMonsterSprite(Sprite sprite)
        {
            return sprite != null &&
                   (sprite == _spriteLevel1 || sprite == _spriteLevel2 || sprite == _spriteLevel3) &&
                   sprite.texture != null && sprite.texture.width == 48 && sprite.texture.height == 48;
        }

        private const float MonsterPixelsPerUnit = 480f;

        private static void EnsureSprites()
        {
            if (_spriteLevel1 != null && _spriteLevel2 != null && _spriteLevel3 != null) return;
            _spriteLevel1 = CreateMonsterSprite("map_enemy_level1") ?? CreateSquareSprite();
            _spriteLevel2 = CreateMonsterSprite("map_enemy_level2") ?? CreateCircleSprite();
            _spriteLevel3 = CreateMonsterSprite("map_enemy_level3") ?? CreateTriangleSprite();
        }

        private static Sprite CreateMonsterSprite(string assetKey)
        {
            Texture2D texture = AssetCatalog.GetTexture(assetKey);
            if (texture == null) return null;
            return Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), MonsterPixelsPerUnit);
        }

        private static Sprite CreateSquareSprite()
        {
            const int size = 5;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 10;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    pixels[y * size + x] = dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateTriangleSprite()
        {
            const int size = 10;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var pixels = new Color[size * size];
            var a = new Vector2(size / 2f, size - 1);
            var b = new Vector2(1f, 1f);
            var c = new Vector2(size - 2, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] =
                        IsPointInTriangle(new Vector2(x, y), a, b, c) ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !hasNegative || !hasPositive;
        }
    }

    [HarmonyPatch(typeof(EnemyParent), "SpawnRPC")]
    internal static class EnemyMapSpawnPatch
    {
        private static void Postfix(EnemyParent __instance)
        {
            try
            {
                EnemyMapIcons.OnSpawn(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_enemymap_error", secret: false, ("via", "spawn"), ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(EnemyParent), "DespawnRPC")]
    internal static class EnemyMapDespawnPatch
    {
        private static void Postfix(EnemyParent __instance)
        {
            try
            {
                EnemyMapIcons.OnDespawn(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_enemymap_error", secret: false, ("via", "despawn"), ("err", e.Message));
            }
        }
    }
}
