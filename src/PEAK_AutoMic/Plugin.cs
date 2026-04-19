using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PEAK_AutoMic.Patches;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static DG.Tweening.DOTweenAnimation;

namespace PEAK_AutoMic;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static Harmony Harmony = null!;
    internal static Dictionary<string, PlayerVoiceInfo> PlayerVoices { get; private set; } = new Dictionary<string, PlayerVoiceInfo>();
    //internal static Photon.Realtime.Room? RoomReference = null;

    internal static bool _patched = false;
    private void Awake()
    {
        Log = Logger;
        Harmony = new Harmony(Id);
        //var version = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(RemoteVoiceLinkPatches)).Location).ProductVersion;

        /*AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
        {
            if (_patched) return;

            var targetType = args.LoadedAssembly.GetType("Photon.Voice.Unity.RemoteVoiceLink");
            if (targetType != null)
            {
                Log.LogInfo($"Plugin {Id} v.{version} patching volume controls!");
                ManualPatch(targetType);
            }
        };

        var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetType("Photon.Voice.Unity.RemoteVoiceLink") != null);
        if (loadedAssembly != null)
        {
            var targetType = loadedAssembly.GetType("Photon.Voice.Unity.RemoteVoiceLink");
            if (targetType != null)
            {
                Log.LogInfo($"Plugin {Id} v.{version} patching volume controls from already loaded assembly!");
                ManualPatch(targetType);
            } else
            {
                Log.LogError("Assembly already loaded but Photon.Voice.Unity.RemoteVoiceLink not found???");
            }
        }*/

        Harmony.PatchAll(typeof(RemoteVoiceLinkPatches));

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static void ManualPatch(Type targetType)
    {
        var method = targetType.GetMethod("FloatFrameDecoded");
        if (method != null)
        {
            Harmony.Patch(method, new HarmonyMethod(typeof(RemoteVoiceLinkPatches).GetMethod(nameof(RemoteVoiceLinkPatches.Postfix), BindingFlags.Static | BindingFlags.NonPublic)));
            _patched = true;
        }
        else
        {
            Log.LogError($"Couldn't find FloatFrameDecoded!");
        }
    }
}
