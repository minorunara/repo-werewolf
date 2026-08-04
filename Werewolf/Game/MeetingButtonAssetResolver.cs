using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
{
    internal static class MeetingButtonAssetResolver
    {
        private static bool _resolved;
        private static GameObject _template;
        private static AnimationCurve _pressCurve;

        internal static GameObject Template => _template;
        internal static AnimationCurve PressCurve => _pressCurve;

        internal static bool TryResolve()
        {
            if (_resolved) return _template != null;
            _resolved = true;

            RunManager rm = RunManager.instance;
            if (rm == null)
            {
                WLog.Line("mb_resolver_no_runmanager", secret: false);
                return false;
            }
            List<Level> shopLevels = rm.levelShop;
            if (shopLevels == null || shopLevels.Count == 0)
            {
                WLog.Line("mb_resolver_no_shop_levels", secret: false,
                    ("null", shopLevels == null));
                return false;
            }

            foreach (Level lvl in shopLevels)
            {
                if (lvl == null) continue;
                string lvlName = lvl.NarrativeName ?? lvl.name ?? "?";

                TryScan(lvlName, "StartRooms", lvl.StartRooms);
                if (_template != null) return true;

                TryScan(lvlName, "ModulesSpecial", lvl.ModulesSpecial);
                if (_template != null) return true;

                TryScan(lvlName, "ModulesNormal1", lvl.ModulesNormal1);
                TryScan(lvlName, "ModulesNormal2", lvl.ModulesNormal2);
                TryScan(lvlName, "ModulesNormal3", lvl.ModulesNormal3);
                TryScan(lvlName, "ModulesPassage1", lvl.ModulesPassage1);
                TryScan(lvlName, "ModulesPassage2", lvl.ModulesPassage2);
                TryScan(lvlName, "ModulesPassage3", lvl.ModulesPassage3);
                TryScan(lvlName, "ModulesDeadEnd1", lvl.ModulesDeadEnd1);
                TryScan(lvlName, "ModulesDeadEnd2", lvl.ModulesDeadEnd2);
                TryScan(lvlName, "ModulesDeadEnd3", lvl.ModulesDeadEnd3);
                TryScan(lvlName, "ModulesExtraction1", lvl.ModulesExtraction1);
                TryScan(lvlName, "ModulesExtraction2", lvl.ModulesExtraction2);
                TryScan(lvlName, "ModulesExtraction3", lvl.ModulesExtraction3);
                if (_template != null) return true;
            }

            WLog.Line("mb_resolver_not_found", secret: false);
            return false;
        }

        private static void TryScan(string lvlName, string listName, List<PrefabRef> list)
        {
            if (list == null || list.Count == 0) return;
            for (int i = 0; i < list.Count; i++)
            {
                PrefabRef pref = list[i];
                if (pref == null || !pref.IsValid()) continue;
                GameObject prefabGo;
                try
                {
                    prefabGo = pref.Prefab;
                }
                catch (System.Exception e)
                {
                    WLog.Line("mb_resolver_prefab_error", secret: false,
                        ("lvl", lvlName), ("list", listName), ("name", pref.PrefabName),
                        ("err", e.Message));
                    continue;
                }
                if (prefabGo == null) continue;

                ExtractionPoint[] eps = prefabGo.GetComponentsInChildren<ExtractionPoint>(true);
                if (eps == null || eps.Length == 0) continue;
                foreach (ExtractionPoint ep in eps)
                {
                    if (ep.shopButton == null) continue;
                    _template = ep.shopButton.gameObject;
                    _pressCurve = ep.buttonPressAnimationCurve;
                    WLog.Line("mb_resolver_ok", secret: false,
                        ("lvl", lvlName), ("list", listName), ("name", pref.PrefabName),
                        ("btnName", _template.name),
                        ("curve", _pressCurve != null));
                    return;
                }
            }
        }
    }
}
