using BepInEx;
using BepInEx.Configuration;
using Jotunn.Utils;
using UnityEngine;

namespace QuanZoneGrid
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // Client-only overlay: nothing to sync, so it doesn't need to match the server or other players.
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "quandru.QuanZoneGrid";
        public const string PluginName = "QuanZoneGrid";
        public const string PluginVersion = PluginInfo.Version; // generated from <Version> in the .csproj

        internal static ConfigEntry<bool> ShowOnStart;
        internal static ConfigEntry<KeyboardShortcut> ToggleKey;
        internal static ConfigEntry<int> Radius;

        private void Awake()
        {
            const string display = "1 - Display (personal)";
            ShowOnStart = Config.Bind(display, "Show on start", false,
                "Show the zone grid as soon as you enter a world. Otherwise press the toggle key. Personal.");
            ToggleKey = Config.Bind(display, "Toggle key", new KeyboardShortcut(KeyCode.G, KeyCode.LeftAlt),
                "Shows or hides the zone grid. Not F7: Magic Mike's Mod Settings opens on that. Personal.");
            Radius = Config.Bind(display, "Radius", 3,
                new ConfigDescription("How many zones to draw on each side of the one you're standing in. Personal.",
                    new AcceptableValueRange<int>(1, 8)));

            var overlay = gameObject.AddComponent<ZoneGridOverlay>();
            overlay.Visible = ShowOnStart.Value;

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
