using HarmonyLib;

namespace PEAK_AutoMic.Patches;


internal class VoiceVolumePatches
{
    [HarmonyPatch(typeof(AudioLevelSlider), "Init")]
    [HarmonyPostfix]
    static void InitPostfix(AudioLevelSlider __instance)
    {
        if (__instance.player == null)
            return;

        Plugin.Log.LogInfo($"Player Joined: {__instance.player.NickName}");

    }

    [HarmonyPatch(typeof(CharacterVoiceHandler), nameof(CharacterVoiceHandler.Update))]
    [HarmonyPostfix]
    static void Postfix(CharacterVoiceHandler __instance)
    {
        if (__instance.m_character == null || __instance.m_character.photonView.Owner.IsLocal)
            return;

        string userId = __instance.m_character.photonView.Owner.UserId;

        if(!Plugin.PlayerVoices.ContainsKey(userId))
        {
            Plugin.PlayerVoices.Add(userId, new PlayerVoiceInfo(1024));
        }

        if( Plugin.PlayerVoices.TryGetValue(userId, out var info))
        {
            __instance.m_source.GetOutputData(info.sampleCache, 0);
            Plugin.Log.LogInfo($"VOICETEST LENGTH: {info.sampleCache.Length}");

            if (info.sampleCache.Length > 0)
            {
                var sampledump = string.Join(", ", info.sampleCache);
                Plugin.Log.LogInfo($"VOICETEST SAMPLES: {sampledump}");
                info.ProcessSamples(info.sampleCache);
                float lufs = info.GetShortTermLUFS();
                info.RecordLUFS(lufs);

                AudioLevels.SetPlayerLevel(userId, info.GetOutputLevel());
            }
        }
    }
}
