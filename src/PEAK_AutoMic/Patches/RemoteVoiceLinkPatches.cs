using HarmonyLib;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static Zorro.ControllerSupport.Rumble.RumbleClip;

namespace PEAK_AutoMic.Patches
{
    internal class RemoteVoiceLinkPatches
    {
        /*[HarmonyPatch(typeof(Room), MethodType.Constructor, new Type[] { typeof(string), typeof(RoomOptions), typeof(bool) })]
        public static void Postfix(Room __instance, string roomName, RoomOptions options, bool isOffline)
        {
            Plugin.Log.LogInfo($"Found Room instance: {__instance.Name}");

            Plugin.RoomReference = __instance;
        }*/

        [HarmonyPatch(typeof(VoiceConnection), MethodType.Constructor)]
        public static void Postfix(VoiceConnection __instance)
        {
            if (__instance != null)
            {
                Plugin.Log.LogInfo($"Found VoiceConnection instance: {__instance.name}");
                __instance.RemoteVoiceAdded += __instance_RemoteVoiceAdded;
            } else
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

            link.FloatFrameDecoded += (frame) =>
            {
                Photon.Realtime.Player[] playerList = PhotonNetwork.PlayerList;
                string? userID = null;
                if (playerList != null) {
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

                if (userID != null && frame != null && frame.Buf != null)
                {
                    PlayerVoiceInfo info;
                    if (!Plugin.PlayerVoices.TryGetValue(userID, out info)) 
                    {
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
                        //Plugin.Log.LogInfo($"Link Set level for #{link.PlayerId} to {level}");
                    }
                }
            };
        }
    }
}
