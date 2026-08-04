using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class EnemyFreezer
    {
        private const float AliveShortRespawnSeconds = 1.5f;

        private bool _active;

        public bool Active => _active;

        public void Begin()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("enemy_freeze_skip", secret: false, ("reason", "not_host"));
                return;
            }

            Patches.EnemySpawnPatch.SuppressSpawns = true;
            _active = true;

            int despawned = 0;
            int preserved = 0;
            List<EnemyParent> enemies = EnemyDirector.instance != null ? EnemyDirector.instance.enemiesSpawned : null;
            if (enemies != null)
            {
                foreach (EnemyParent ep in enemies.ToArray())
                {
                    if (ep == null) continue;
                    try
                    {
                        bool alive = ep.EnableObject != null && ep.EnableObject.activeSelf;
                        if (alive)
                        {
                            ep.Despawn();
                            ep.DespawnedTimerSet(AliveShortRespawnSeconds, true);
                            despawned++;
                        }
                        else
                        {
                            preserved++;
                        }
                    }
                    catch (Exception e)
                    {
                        WLog.Line("enemy_despawn_error", secret: false, ("err", e.Message));
                    }
                }
            }

            WLog.Line("enemy_freeze_begin", secret: false, ("despawned", despawned), ("preserved", preserved));
        }

        public void End()
        {
            if (!_active) return;
            _active = false;
            Patches.EnemySpawnPatch.SuppressSpawns = false;

            WLog.Line("enemy_freeze_end", secret: false);
        }
    }
}
