using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class PreplayEnemySpawnHold
    {
        private static readonly Dictionary<EnemyParent, long> FirstAttempts =
            new Dictionary<EnemyParent, long>();

        private static bool _active;
        private static long _armedAtUnixMs;

        internal static void Begin(long nowUnixMs)
        {
            if (_active) return;
            FirstAttempts.Clear();
            _armedAtUnixMs = nowUnixMs;
            _active = true;
        }

        internal static bool ShouldBlock(EnemyParent enemy, long nowUnixMs)
        {
            if (!_active) return false;
            if (enemy != null && !FirstAttempts.ContainsKey(enemy))
            {
                FirstAttempts[enemy] = nowUnixMs;
            }
            return true;
        }

        internal static void Release()
        {
            if (!_active) return;

            foreach (var pair in FirstAttempts)
            {
                EnemyParent enemy = pair.Key;
                if (enemy == null) continue;
                try
                {
                    float delaySec = Math.Max(0f, (pair.Value - _armedAtUnixMs) / 1000f);
                    enemy.DespawnedTimer = delaySec;
                }
                catch (Exception e)
                {
                    WLog.Line("preplay_enemy_restore_error", secret: false, ("err", e.Message));
                }
            }

            _active = false;
            FirstAttempts.Clear();
            _armedAtUnixMs = 0;
        }

        internal static void Cancel()
        {
            if (!_active && FirstAttempts.Count == 0) return;
            _active = false;
            FirstAttempts.Clear();
            _armedAtUnixMs = 0;
        }
    }
}
