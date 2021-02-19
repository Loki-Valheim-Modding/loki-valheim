using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Loki.Mods
{
    
    
    [BepInPlugin("com.loki.clientmods.valheim.firstperson", "First Person Client Mod", "1.0.0.0")]
    public class FirstPersonValheimClientMod : BaseUnityPlugin
    {
        private static bool IsFirstPerson = false;
        
        void Awake(){
            Harmony.CreateAndPatchAll(typeof(FirstPersonValheimClientMod));
        }
        
        //private Vector3 GameCamera.GetCameraOffset(Player player)
        //private void GetCameraPosition(float dt, out Vector3 pos, out Quaternion rot)
        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        [HarmonyPostfix]
        static void SetCameraPositionToEyeOnFPS(GameCamera __instance, float dt, ref Vector3 pos, ref Quaternion rot) {

            // Toggle FPS on H
            if (Input.GetKeyDown(KeyCode.H))
                IsFirstPerson = !IsFirstPerson;
            
            if(IsFirstPerson)
                pos = Player.m_localPlayer.m_eye.transform.position;
        }
        
    }
}