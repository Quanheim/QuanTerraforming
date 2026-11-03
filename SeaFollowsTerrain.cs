using System;
using HarmonyLib;

namespace QuanTerraforming
{
    // Vanilla: a zone's sea (WaterVolume) copies its heightmap's four corner depths once, in Start, and scales its
    // waves by them. That happens straight after the heightmap is built from the world generator, before the zone's
    // TerrainComp (the players' terrain edits) loads, so the waves always follow the original seabed.
    //
    // Fix: whenever the heightmap recalculates its corner depths (every full regenerate: on load, when the edits
    // arrive, and after each new edit), have that zone's sea read them again.
    [HarmonyPatch(typeof(Heightmap), "UpdateCornerDepths")]
    internal static class SeaFollowsTerrain
    {
        private static readonly AccessTools.FieldRef<WaterVolume, float> OneDepth =
            AccessTools.FieldRefAccess<WaterVolume, float>("m_oneDepth");

        private static readonly Action<WaterVolume> DetectWaterDepth =
            AccessTools.MethodDelegate<Action<WaterVolume>>(AccessTools.Method(typeof(WaterVolume), "DetectWaterDepth"));

        private static readonly Action<WaterVolume> SetupMaterial =
            AccessTools.MethodDelegate<Action<WaterVolume>>(AccessTools.Method(typeof(WaterVolume), "SetupMaterial"));

        private static void Postfix(Heightmap __instance)
        {
            if (!Plugin.SeaFollowsTerrain.Value) return;

            foreach (WaterVolume water in WaterVolume.Instances)
            {
                if (water == null || water.m_heightmap != __instance || water.m_waterSurface == null) continue;

                // DetectWaterDepth only sets m_oneDepth when all four corners match, and never clears it. Clear it
                // first, or a zone that loaded as uniform open sea would keep full waves after its corners differ.
                OneDepth(water) = -1f;
                DetectWaterDepth(water);
                SetupMaterial(water); // pushes the new depths to the shader, which draws the waves
            }
        }
    }
}
