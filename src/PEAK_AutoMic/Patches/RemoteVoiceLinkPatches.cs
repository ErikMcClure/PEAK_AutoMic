using HarmonyLib;
using Photon.Pun;
using Photon.Pun.Demo.Cockpit;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static Zorro.ControllerSupport.Rumble.RumbleClip;

namespace PEAK_AutoMic.Patches
{
    internal class RemoteVoiceLinkPatches
    {
        [HarmonyPatch(typeof(VoiceConnection), MethodType.Constructor)]
        public static void Postfix(VoiceConnection __instance)
        {
            if (__instance != null)
            {
                Plugin.Log.LogInfo($"Found VoiceConnection instance: {__instance.name}");
                __instance.RemoteVoiceAdded += __instance_RemoteVoiceAdded;
            }
            else
            {
                Plugin.Log.LogError($"VoiceConnection constructor returned NULL????????");
            }
        }

        private static void __instance_RemoteVoiceAdded(RemoteVoiceLink link)
        {
            if (link == null)
            {
                Plugin.Log.LogError($"RemoteVoiceLink was NULL????????");
                return;
            }

            Plugin.Log.LogInfo($"Found RemoteVoiceLink for player #{link.PlayerId}");

            // If this player already exists, delete it and recreate it

            lock (PhotonNetwork.PlayerList)
            {
                Photon.Realtime.Player[] playerList = PhotonNetwork.PlayerList;
                if (playerList != null)
                {
                    for (int j = 0; j < playerList.Length; j++)
                    {
                        if (playerList[j] != null && playerList[j].ActorNumber == link.PlayerId)
                        {
                            var userID = playerList[j].UserId;

                            if (userID != null)
                            {
                                Plugin.PlayerVoices.TryRemove(userID, out var value);
                            }
                        }
                    }
                }

            }

            link.FloatFrameDecoded += (frame) =>
        {
            string? userID = null;
            lock (PhotonNetwork.PlayerList)
            {
                Photon.Realtime.Player[] playerList = PhotonNetwork.PlayerList;
                if (playerList != null)
                {
                    for (int j = 0; j < playerList.Length; j++)
                    {
                        if (playerList[j] != null && playerList[j].ActorNumber == link.PlayerId)
                        {
                            userID = playerList[j].UserId;
                            //Plugin.Log.LogInfo($"Found player #{playerList[j].ActorNumber} for {playerList[j].UserId} on #{link.PlayerId} ");

                            if (userID != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (userID != null && frame != null && frame.Buf != null)
            {
                PlayerVoiceInfo info;
                if (!Plugin.PlayerVoices.TryGetValue(userID, out info))
                {
                    //Plugin.Log.LogInfo($"Adding player #{link.PlayerId} with sampling rate {link.VoiceInfo.SamplingRate} for ID {userID}");
                    info = new PlayerVoiceInfo(link.VoiceInfo.SamplingRate, link.VoiceId);
                    Plugin.PlayerVoices.TryAdd(userID, info);
                }

                info.ProcessSamples(frame.Buf);
                float lufs = info.GetShortTermLUFS();
                info.RecordLUFS(lufs);
                var level = info.GetOutputLevel() * 0.5f;
                var prev = AudioLevels.GetPlayerLevel(userID);

                if (Math.Abs(level - prev) > 0.01f)
                {
                    AudioLevels.SetPlayerLevel(userID, Math.Clamp(level, 0.0f, 2.0f));
                    if (Plugin.LevelsReference != null)
                    {
                        lock (Plugin.LevelsReference)
                        {
                            Plugin.LevelsReference._dirty = true;
                        }
                    }
                    //Plugin.Log.LogInfo($"Link Set level for #{link.PlayerId} to {level}");
                }
            }
        };
        }

        // Hook the audiolevels instance so we can mark it as dirty whenever the levels change
        [HarmonyPatch(typeof(AudioLevels), nameof(AudioLevels.InitNavigation))]
        [HarmonyPostfix]
        static void Postfix(AudioLevels __instance)
        {
            if (!__instance.mainPage)
                return;

            Plugin.LevelsReference = __instance;
        }
    }
}
