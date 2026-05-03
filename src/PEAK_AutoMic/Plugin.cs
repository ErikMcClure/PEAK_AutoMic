using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PEAK_AutoMic.Patches;
using System.Collections.Concurrent;

namespace PEAK_AutoMic;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static Harmony Harmony = null!;
    internal static ConcurrentDictionary<string, PlayerVoiceInfo> PlayerVoices { get; private set; } = new ConcurrentDictionary<string, PlayerVoiceInfo>();
    //internal static Photon.Realtime.Room? RoomReference = null;
    internal static AudioLevels? LevelsReference = null;

    internal static bool _patched = false;
    private void Awake()
    {
        Log = Logger;
        Harmony = new Harmony(Id);
        //var version = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(RemoteVoiceLinkPatches)).Location).ProductVersion;

        Harmony.PatchAll(typeof(RemoteVoiceLinkPatches));

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}
