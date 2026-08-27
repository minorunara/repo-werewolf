using System.Collections.Generic;
using UnityEngine;

namespace Werewolf.Vanilla
{
    internal static class MapGeometry
    {
        public static bool TryReadLevelGrid(out int levelWidth, out int levelHeight, out float mapScale, out Vector3 overlayOrigin)
        {
            LevelGenerator lg = LevelGenerator.Instance;
            Map map = Map.Instance;
            if (lg == null || map == null || map.OverLayerParent == null)
            {
                levelWidth = 0;
                levelHeight = 0;
                mapScale = 0f;
                overlayOrigin = default;
                return false;
            }
            levelWidth = lg.LevelWidth;
            levelHeight = lg.LevelHeight;
            mapScale = map.Scale;
            overlayOrigin = map.OverLayerParent.position;
            return true;
        }

        public static List<(float X, float Z)> ModuleCentersXZ()
        {
            Module[] modules = Object.FindObjectsOfType<Module>();
            var centers = new List<(float X, float Z)>(modules != null ? modules.Length : 0);
            if (modules == null) return centers;
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == null) continue;
                Vector3 p = modules[i].transform.position;
                centers.Add((p.x, p.z));
            }
            return centers;
        }
    }
}
