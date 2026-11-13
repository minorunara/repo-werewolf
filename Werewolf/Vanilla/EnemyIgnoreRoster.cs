#nullable enable
using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    public sealed class EnemyIgnoreRoster
    {
        private readonly HashSet<string> _added = new HashSet<string>();

        public void SetIgnored(string steamId, bool ignored)
        {
            if (string.IsNullOrEmpty(steamId)) return;
            var director = EnemyDirector.instance;
            if (director == null)
            {
                WLog.Line("enemy_ignore_skipped", secret: true, ("reason", "no_enemy_director"));
                return;
            }

            List<string> list = director.debugNoVision;
            if (ignored)
            {
                if (!list.Contains(steamId)) list.Add(steamId);
                _added.Add(steamId);
            }
            else
            {
                list.Remove(steamId);
                _added.Remove(steamId);
            }
            WLog.Line("enemy_ignore", secret: true, ("ignored", ignored), ("count", list.Count));
        }

        public void ClearAll()
        {
            if (_added.Count == 0) return;
            var director = EnemyDirector.instance;
            if (director != null)
            {
                List<string> list = director.debugNoVision;
                foreach (string steamId in _added)
                {
                    list.Remove(steamId);
                }
            }
            _added.Clear();
            WLog.Line("enemy_ignore_cleared", secret: true);
        }
    }
}
