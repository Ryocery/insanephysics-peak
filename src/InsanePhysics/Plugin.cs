using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Crab.Network;
using HarmonyLib;
using InsanePhysics.Features.HostileItems;
using UnityEngine;

namespace InsanePhysics;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin {
    private static ManualLogSource Log { get; set; } = null!;
    private Harmony _harmony = null!;

    public static ConfigEntry<float> PlayerForceMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> HostileObjectPower { get; private set; } = null!;
    public static ConfigEntry<bool> EnableHostileObjects { get; private set; } = null!;
    
    private void Awake() {
        Log = Logger;

        PlayerForceMultiplier = Config.Bind("Physics", "ForceMultiplier", 1.0f, "Multiplier for all physics forces applied to characters.");

        EnableHostileObjects = Config.Bind("Chaos", "EnableHostileObjects", true, "If true, items occasionally launch themselves at players.");
        HostileObjectPower = Config.Bind("Chaos", "HostileObjectPower", 300.0f, "Sets the force of hostile items flying towards players.");


        _harmony = new Harmony(Id);
        _harmony.PatchAll();

        if (EnableHostileObjects.Value) {
            GameObject managerGo = new("InsanePhysics_HostileManager");
            DontDestroyOnLoad(managerGo);
            managerGo.AddComponent<HostileItems>();
            Log.LogInfo("Hostile Physics Manager Initialized.");
        }

        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}