using UnityEngine;
using Werewolf.Core;
using Werewolf.Scenarios.Vanilla;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Game
{
    internal sealed partial class ScenarioController
    {
        private GameObject _quotaBag;
        private int _quotaBagIssues;
        private long _quotaBagNextCheckAtMs;
        private bool _quotaBagWatched;

        private const long QuotaBagCheckIntervalMs = 25000L;

        private void OpenQuotaBagWindow(long now)
        {
            _quotaBagWatched = true;
            _quotaBagNextCheckAtMs = now;
        }

        private void TickQuotaBagWatch(long now)
        {
            if (!ScriptRunning || !_quotaBagWatched) return;

            ScenarioSignal? until = _definition.Watch.QuotaBagUntil;
            if (until.HasValue && _flow.IsLatched(until.Value))
            {
                _quotaBagWatched = false;
                return;
            }

            if (now < _quotaBagNextCheckAtMs) return;
            _quotaBagNextCheckAtMs = now + QuotaBagCheckIntervalMs;

            if (!ScenarioQuota.TryReadState(out int haulGoal, out int points, out int currentHaul, out int surplus))
            {
                WLog.Line("scenario_quota_bag_skipped", secret: false, ("reason", "no_round"));
                return;
            }

            if (!TryReadReachableBagDollars(out int reachable))
            {
                WLog.Line("scenario_quota_bag_skipped", secret: false, ("reason", "bag_unreadable"));
                return;
            }

            int dollars = ScenarioQuotaBag.ResolveBagDollars(haulGoal, points, currentHaul,
                surplus, reachable);
            if (dollars <= 0) return;

            int max = _definition.Watch.MaxQuotaBagReissues;
            if (max > 0 && _quotaBagIssues > max)
            {
                Break(ScenarioBreakKind.QuotaBagLost, now);
                return;
            }

            _quotaBagIssues++;
            bool spawned = _d.SpawnMoneyBagInFront(dollars, out _quotaBag, out string reason);
            WLog.Line("scenario_quota_bag", secret: false, ("dollars", dollars),
                ("spawned", spawned), ("reason", reason ?? ""),
                ("issues", _quotaBagIssues), ("reachable", reachable),
                ("haulGoal", haulGoal), ("points", points), ("currentHaul", currentHaul),
                ("surplus", surplus));
        }

        private bool TryReadReachableBagDollars(out int dollars)
        {
            dollars = 0;
            if (_quotaBag == null) return true;

            float leash = _definition.Environment.TruckLeashMeters;
            if (leash > 0f)
            {
                if (!TruckWarper.TryNearestTruckSpawn(_quotaBag.transform.position, out _, out _,
                        out float sqr))
                {
                    return false;
                }
                if (sqr > leash * leash) return true;
            }

            return ScenarioQuota.TryReadBagDollars(_quotaBag, out dollars);
        }

        private void ResetQuotaBag()
        {
            _quotaBag = null;
            _quotaBagIssues = 0;
            _quotaBagNextCheckAtMs = 0;
            _quotaBagWatched = false;
        }
    }
}
