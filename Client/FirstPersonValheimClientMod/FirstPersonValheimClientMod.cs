using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Loki.Mods
{
    
    
    [BepInPlugin("com.loki.clientmods.valheim.firstperson", "First Person Client Mod", "1.0.0.0")]
    public class FirstPersonValheimClientMod : BaseUnityPlugin
    {
        private static bool IsFirstPerson = false;
        private static Action<bool> _setVisible;
        private static MethodInfo _setVisibleInfo;
        
        void Awake(){
            _setVisibleInfo = typeof(Character).GetMethod("SetVisible",BindingFlags.NonPublic | BindingFlags.Instance);
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

            if (IsFirstPerson) {
                pos = Player.m_localPlayer.m_eye.transform.position;
            }
        }
        
        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        [HarmonyPostfix]
        static void ForceCharacterModelVisible(Player __instance, ZNetView ___m_nview) {

            // See Player code aborting on desync issues
            if (!___m_nview.IsOwner() || Player.m_localPlayer != __instance) return;
            
            // Late bind to instance
            if (_setVisible == null) {
                _setVisible = (Action<bool>) Delegate.CreateDelegate(typeof(Action<bool>), __instance, _setVisibleInfo);
            }
            
            if (IsFirstPerson) {
                _setVisible(true);
            }
        }
        
    }
}