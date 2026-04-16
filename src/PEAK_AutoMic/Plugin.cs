using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PEAK_AutoMic.Patches;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace PEAK_AutoMic;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static Harmony Harmony = null!;
    internal static Dictionary<string, PlayerVoiceInfo> PlayerVoices { get; private set; } = new Dictionary<string, PlayerVoiceInfo>();
    public static float TargetLUFS = 1.0f;

    private void Awake()
    {
        Log = Logger;
        Harmony = new Harmony(Id);

        var version = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(VoiceVolumePatches)).Location).ProductVersion;
        Logger.LogInfo($"Plugin {Id} v.{version} patching volume controls!");
        Harmony.PatchAll(typeof(VoiceVolumePatches));

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}
