using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class MeetingButtonAssetResolver
    {
        internal const string KnownModuleName = "Module - Shop - N - Center Extract";

        private static bool _resolved;
        private static bool _adopted;
        private static GameObject _template;
        private static AnimationCurve _pressCurve;

        internal static GameObject Template => _template;
        internal static AnimationCurve PressCurve => _pressCurve;

        internal static bool TryWarmup()
        {
            if (_resolved) return _template != null;
            RunManager rm = RunManager.instance;
            if (rm == null) return false;
            List<Level> shopLevels = rm.levelShop;
            if (shopLevels == null || shopLevels.Count == 0) return false;
            return TryResolve();
        }

        internal static bool TryResolve()
        {
            if (_adopted && _template == null)
            {
                _adopted = false;
                _resolved = false;
            }
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

            if (TryResolveByName(shopLevels)) return true;
            if (TryResolveByScan(shopLevels)) return true;

            WLog.Line("mb_resolver_not_found", secret: false);
            return false;
        }

        private static bool TryResolveByName(List<Level> shopLevels)
        {
            foreach (Level lvl in shopLevels)
            {
                if (lvl == null) continue;
                foreach (KeyValuePair<string, List<PrefabRef>> entry in ListsOf(lvl))
                {
                    List<PrefabRef> list = entry.Value;
                    if (list == null) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        PrefabRef pref = list[i];
                        if (pref == null || !pref.IsValid() || pref.PrefabName != KnownModuleName) continue;
                        if (TryAdopt(LevelName(lvl), entry.Key, pref)) return true;
                    }
                }
            }
            return false;
        }

        private static bool TryResolveByScan(List<Level> shopLevels)
        {
            foreach (Level lvl in shopLevels)
            {
                if (lvl == null) continue;
                foreach (KeyValuePair<string, List<PrefabRef>> entry in ListsOf(lvl))
                {
                    List<PrefabRef> list = entry.Value;
                    if (list == null) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        PrefabRef pref = list[i];
                        if (pref == null || !pref.IsValid()) continue;
                        if (TryAdopt(LevelName(lvl), entry.Key, pref)) return true;
                    }
                }
            }
            return false;
        }

        private static string LevelName(Level lvl) => lvl.NarrativeName ?? lvl.name ?? "?";

        private static IEnumerable<KeyValuePair<string, List<PrefabRef>>> ListsOf(Level lvl)
        {
            yield return new KeyValuePair<string, List<PrefabRef>>("StartRooms", lvl.StartRooms);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesSpecial", lvl.ModulesSpecial);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesNormal1", lvl.ModulesNormal1);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesNormal2", lvl.ModulesNormal2);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesNormal3", lvl.ModulesNormal3);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesPassage1", lvl.ModulesPassage1);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesPassage2", lvl.ModulesPassage2);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesPassage3", lvl.ModulesPassage3);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesDeadEnd1", lvl.ModulesDeadEnd1);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesDeadEnd2", lvl.ModulesDeadEnd2);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesDeadEnd3", lvl.ModulesDeadEnd3);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesExtraction1", lvl.ModulesExtraction1);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesExtraction2", lvl.ModulesExtraction2);
            yield return new KeyValuePair<string, List<PrefabRef>>("ModulesExtraction3", lvl.ModulesExtraction3);
        }

        private static bool TryAdopt(string lvlName, string listName, PrefabRef pref)
        {
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
                return false;
            }
            if (prefabGo == null) return false;

            ExtractionPoint[] eps = prefabGo.GetComponentsInChildren<ExtractionPoint>(true);
            if (eps == null || eps.Length == 0) return false;
            foreach (ExtractionPoint ep in eps)
            {
                if (ep.shopButton == null) continue;
                _template = ep.shopButton.gameObject;
                _pressCurve = ep.buttonPressAnimationCurve;
                _adopted = true;
                return true;
            }
            return false;
        }
    }
}
