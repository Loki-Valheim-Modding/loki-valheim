using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Loki.Mods
{
    
    
    [BepInPlugin("com.loki.clientmods.valheim.firstperson", "First Person Client Mod", "1.0.0.0")]
    public class FirstPersonValheimClientMod : BaseUnityPlugin {

        // Statics done because injection requires static methods
        private static bool IsFirstPerson = false;
        private static Action<bool> _setVisible;
        private static MethodInfo _setVisibleInfo;
        
        private static float _oldNearPlane;
        private static ConfigEntry<float> _fspNearPlane, _eyeOffset;
        
        void Awake() {
            _fspNearPlane = Config.Bind("Camera", "FPSNearPlane", 0.001f, "The Near Plane of the camera during FP mode");
            _eyeOffset = Config.Bind("Camera", "FPSOffset", -0.1f, "The offset from the eye position the camera is placed");
            
            _setVisibleInfo = typeof(Character).GetMethod("SetVisible",BindingFlags.NonPublic | BindingFlags.Instance);
            Harmony.CreateAndPatchAll(typeof(FirstPersonValheimClientMod));
        }
        
        // private void GetCameraPosition(float dt, out Vector3 pos, out Quaternion rot)
        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        [HarmonyPostfix]
        static void SetCameraPositionToEyeOnFPS(GameCamera __instance, float dt, ref Vector3 pos, ref Quaternion rot) {

            // Toggle FPS on H
            if (Input.GetKeyDown(KeyCode.H)) {
                IsFirstPerson = !IsFirstPerson;
                
                // Setup near plane etc
                var cam = __instance.GetComponent<Camera>();

                if (IsFirstPerson) {
                    _oldNearPlane = cam.nearClipPlane;
                    cam.nearClipPlane = _fspNearPlane.Value;
                } else {
                    cam.nearClipPlane = _oldNearPlane;
                }
            }

            if (!IsFirstPerson) return;
            
            var mEye = Player.m_localPlayer.m_eye.transform;
            pos = mEye.position + mEye.forward * _eyeOffset.Value;
            
        }
        
        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        [HarmonyPostfix]
        static void ForceCharacterModelVisible(Player __instance, ZNetView ___m_nview) {

            // See Player code aborting on desync issues
            if (___m_nview == null || ___m_nview.GetZDO() == null || !___m_nview.IsOwner() || Player.m_localPlayer != __instance)
                return;
            
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