using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Loki.Mods
{
    
    [BepInPlugin("com.loki.clientmods.valheim.deathannouncer", "Death Announcer Client Mod", "1.0.0.0")]
    public class DeathAnnouncerValheimClientMod : BaseUnityPlugin {

        void Awake(){
            Harmony.CreateAndPatchAll(typeof(DeathAnnouncerValheimClientMod));
        }
        
        [HarmonyPatch(typeof(Player), "OnDeath")]
        [HarmonyPrefix]
        static void NotifyOtherPlayers(Player __instance){
            Chat.instance.SendPing(__instance.transform.position);
            Chat.instance.SendText(Talker.Type.Shout, __instance.GetPlayerName() + " died a horrible death!");
        }
        
    }
    
}