using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace DeathAnnouncerValheimMod
{
    
    [BepInPlugin("com.freecode.mods.valheim.deathannouncermod", "Death Announcer Mod", "1.0.0.0")]
    public class DeathAnnouncerValheimMod : BaseUnityPlugin {

        void Awake(){
            Harmony.CreateAndPatchAll(typeof(DeathAnnouncerValheimMod));
        }
        
        [HarmonyPatch(typeof(Player), "OnDeath")]
        [HarmonyPrefix]
        static void NotifyOtherPlayers(Player __instance){
            Chat.instance.SendPing(__instance.transform.position);
            Chat.instance.SendText(Talker.Type.Shout, __instance.GetPlayerName() + " died a horrible death!");
        }
        
    }
    
}