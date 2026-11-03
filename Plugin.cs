using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;

namespace QuanTerraforming
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // Soft: makes Heightmap Unlimited load first when present, so TerrainLimits can see it and stand aside.
    [BepInDependency(TerrainLimits.HeightmapUnlimitedGuid, BepInDependency.DependencyFlags.SoftDependency)]
    // The sea fix changes wave height, which drives boat and float physics on whichever client owns the object,
    // so everyone needs the same behaviour.
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "quandru.QuanTerraforming";
        public const string PluginName = "QuanTerraforming";
        public const string PluginVersion = PluginInfo.Version; // generated from <Version> in the .csproj

        internal static ConfigEntry<bool> SeaFollowsTerrain;
        internal static ConfigEntry<float> MaxRaise;
        internal static ConfigEntry<float> MaxDig;
        internal static ConfigEntry<bool> ShowOnStart;
        internal static ConfigEntry<KeyboardShortcut> ToggleKey;
        internal static ConfigEntry<int> Radius;

        private void Awake()
        {
            const string sea = "1 - Sea";
            SeaFollowsTerrain = Config.Bind(sea, "Waves follow terrain edits", true,
                new ConfigDescription(
                    "Raising or lowering terrain changes how big the sea's waves are there, as the ground's height " +
                    "already suggests. Off is vanilla: waves keep the seabed as the world was generated, whatever " +
                    "you build. Applies as zones load or are edited. Synced from the server; admins only.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            const string limits = "2 - Terrain limits";
            MaxRaise = Config.Bind(limits, "Max raise", 200f,
                new ConfigDescription(
                    "How far terrain can be raised above the ground as the world generated it, in metres. Vanilla " +
                    "is 8. Lowering this below existing edits shows them cut down, and the next edit there saves " +
                    "the cut. Synced from the server; admins only.",
                    new AcceptableValueRange<float>(1f, 200f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            MaxDig = Config.Bind(limits, "Max dig", 200f,
                new ConfigDescription(
                    "How far terrain can be dug below the ground as the world generated it, in metres. Vanilla is 8. " +
                    "Same caution as Max raise. Synced from the server; admins only.",
                    new AcceptableValueRange<float>(1f, 200f), new ConfigurationManagerAttributes { IsAdminOnly = true }));

            const string overlay = "3 - Zone grid (personal)";
            ShowOnStart = Config.Bind(overlay, "Show on start", false,
                "Show the zone grid as soon as you enter a world. Otherwise press the toggle key. Personal.");
            ToggleKey = Config.Bind(overlay, "Toggle key", new KeyboardShortcut(KeyCode.G, KeyCode.LeftAlt),
                "Shows or hides the zone grid. Not F7: Magic Mike's Mod Settings opens on that. Personal.");
            Radius = Config.Bind(overlay, "Radius", 3,
                new ConfigDescription("How many zones to draw on each side of the one you're standing in. Personal.",
                    new AcceptableValueRange<int>(1, 8)));

            new Harmony(PluginGuid).PatchAll(typeof(Plugin).Assembly);
            if (TerrainLimits.OtherModHandlesLimits)
                Logger.LogWarning("Heightmap Unlimited JvL is installed, so it sets the terrain limits and this mod's " +
                                  "\"2 - Terrain limits\" settings do nothing. Remove it once this mod is on every client.");

            var grid = gameObject.AddComponent<ZoneGridOverlay>();
            grid.Visible = ShowOnStart.Value;

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
