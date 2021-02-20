using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Loki.Mods
{


    [BepInPlugin("com.loki.clientmods.valheim.firstperson", "First Person Client Mod", "1.0.0.0")]
    public class FirstPersonValheimClientMod : BaseUnityPlugin
    {

        // Statics done because injection requires static methods
        private static bool IsFirstPerson = false;
        
        private static Action<bool> _setVisible;
        private static MethodInfo _setVisibleInfo;

        private static float _oldNearPlane;
        private static ConfigEntry<float> _fspNearPlane;
        private static ConfigEntry<KeyboardShortcut> _hotkey;
        private static ConfigEntry<bool> _showBodyWhenAiming;
        private static ConfigEntry<bool> _showBodyWhenBlocking;
        
        //private static ConfigEntry<bool> _showBodyWhenHoldingWeapons;
        private static Transform _foundHead;
        private static bool _isAimingBow;

        void Awake()
        {
            _fspNearPlane = Config.Bind("Camera", "FPSNearPlane", 0.05f, "The Near Plane of the camera during FP mode");
            //_eyeOffset = Config.Bind("Camera", "FPSOffset", -0.1f, "The offset from the eye position the camera is placed");
            _hotkey = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.H));
            _showBodyWhenAiming = Config.Bind("Body", "ShowBodyWhenAiming", false, "Whether to show your body while aiming your bow. The bow obscures the center of your screen so you might want to disable it");
            _showBodyWhenBlocking = Config.Bind("Body", "ShowBodyWhenBlocking", false, "Whether to show your body while blocking. Some shields might obscure your vision, but that's what shields are for!");
            //_showBodyWhenHoldingWeapons = Config.Bind("Body", "ShowBodyHoldingWeapons", false, "Whether to show your body while you are actively holding weapons (e.g. when in combat)");

            _setVisibleInfo = typeof(Character).GetMethod("SetVisible", BindingFlags.NonPublic | BindingFlags.Instance);
            Harmony.CreateAndPatchAll(typeof(FirstPersonValheimClientMod));
        }

        // private void GetCameraPosition(float dt, out Vector3 pos, out Quaternion rot)
        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        [HarmonyPostfix]
        static void SetCameraPositionToEyeOnFPS(GameCamera __instance, float dt, ref Vector3 pos, ref Quaternion rot)
        {

            // Toggle FPS on H
            if (_hotkey.Value.IsDown())
            {
                IsFirstPerson = !IsFirstPerson;

                // Setup near plane etc
                var cam = __instance.GetComponent<Camera>();

                if (IsFirstPerson)
                {
                    _oldNearPlane = cam.nearClipPlane;
                    cam.nearClipPlane = _fspNearPlane.Value;
                    __instance.m_nearClipPlaneMin = _fspNearPlane.Value;
                    __instance.m_nearClipPlaneMax = _fspNearPlane.Value;
                    
                } else {
                    _foundHead = null;
                    cam.nearClipPlane = _oldNearPlane;
                    __instance.m_nearClipPlaneMin = _oldNearPlane;
                    __instance.m_nearClipPlaneMax = _oldNearPlane;
                }
                
                var visEqu = Player.m_localPlayer.GetComponentInChildren<VisEquipment>();
                var beardGO = (GameObject) AccessTools.Field(typeof(VisEquipment), "m_beardItemInstance").GetValue(visEqu);
                var hairGO = (GameObject) AccessTools.Field(typeof(VisEquipment), "m_hairItemInstance").GetValue(visEqu);

                foreach (var renderer in GetComponentsInGrandChildren<Renderer>(visEqu.m_helmet)) {
                    renderer.enabled = false;
                }
                foreach (var renderer in GetComponentsInGrandChildren<Renderer>(beardGO)) {
                    renderer.enabled = false;
                }
                foreach (var renderer in GetComponentsInGrandChildren<Renderer>(hairGO)) {
                    renderer.enabled = false;
                }
            }

            if (!IsFirstPerson) return;

            if (!_foundHead) {
                _foundHead = FindTransform(Player.m_localPlayer.transform, "Visual", "Armature", "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "Helmet_attach");
            }
            
            pos = _foundHead.position;

        }
        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        [HarmonyPostfix]
        static void ForceCharacterModelVisible(Player __instance, ZNetView ___m_nview)
        {

            // See Player code aborting on desync issues
            if (___m_nview == null || ___m_nview.GetZDO() == null || !___m_nview.IsOwner() || Player.m_localPlayer != __instance)
                return;

            // Late bind to instance
            if (_setVisible == null)
            {
                _setVisible = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), __instance, _setVisibleInfo);
            }

            if (IsFirstPerson)
            {
                __instance.FaceLookDirection();

                bool visible = true;

                if (_isAimingBow)
                {
                    visible &= _showBodyWhenAiming.Value;
                }
                if (__instance.IsBlocking())
                {
                    visible &= _showBodyWhenBlocking.Value;
                }

                _setVisible(visible);
            }
        }


        [HarmonyPatch(typeof(Player), "SetControls")]
        [HarmonyPostfix]
        static void SetControls(Player __instance, bool attackHold, bool block) {
            _isAimingBow = attackHold && __instance.GetCurrentWeapon().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
        }

        private static IEnumerable<T> GetComponentsInGrandChildren<T>(GameObject root) {
            
            if(root == null) yield break;
            
            foreach (var comp  in GetComponentsInGrandChildren<T>(root.transform)) {
                yield return comp;
            }
        }

        private static IEnumerable<T> GetComponentsInGrandChildren<T>(Transform root) {
            
            if(root == null) yield break;

            foreach (var component in root.gameObject.GetComponents<T>()) {
                Debug.Log("yielding on: " + root.name);
                yield return component;
            }
            
            foreach (Transform child in root) {
                foreach (var componentsInGrandChild in GetComponentsInGrandChildren<T>(child)) {
                    yield return componentsInGrandChild;
                }
            }
        }

        private static Transform FindTransform(Transform root, params string[] path) {

            Transform output = root;
            for (int i = 0; i < path.Length; i++) {
                output = output.Find(path[i]);

                if (output == null) break;
            }

            return output;
        }
        
        private static void DumpHierarchy(Transform t, int depth) {
            Debug.Log(depth + ": " + t.name);
            foreach (Transform child in t)
            {
                DumpHierarchy(child, depth + 1);
            }
        }

    }
}