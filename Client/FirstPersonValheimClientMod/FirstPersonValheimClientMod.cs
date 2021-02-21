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
        private static Action<bool> _setVisible;
        private static MethodInfo _setVisibleInfo;
        private static FirstPersonModes _currentFPMode = FirstPersonModes.ThirdPerson;

        private static float _oldNearPlane;
        private static ConfigEntry<float> _fspNearPlane;
        private static ConfigEntry<KeyboardShortcut> _hotkey;
        private static ConfigEntry<bool> _showBodyWhenAiming;
        private static ConfigEntry<bool> _showBodyWhenBlocking;
        private static ConfigEntry<bool> _jawFix;

        //private static ConfigEntry<bool> _showBodyWhenHoldingWeapons;
        private static Transform _helmetAttach;
        private static Transform _jaw;
        private static Vector3 _originalHeadScale;
        private static Vector3 _originalJawScale;
        private static Transform _head;
        private static bool _isAimingBow;

        void Awake()
        {
            _fspNearPlane = Config.Bind("Camera", "FPSNearPlane", 0.05f, "The Near Plane of the camera during FP mode");
            //_eyeOffset = Config.Bind("Camera", "FPSOffset", -0.1f, "The offset from the eye position the camera is placed");
            _hotkey = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.H), "Hotkey used to cycle between first person modes");
            _showBodyWhenAiming = Config.Bind("Body", "ShowBodyWhenAiming", false, "Whether to show your body while aiming your bow. The bow obscures the center of your screen so you might want to disable it");
            _showBodyWhenBlocking = Config.Bind("Body", "ShowBodyWhenBlocking", false, "Whether to show your body while blocking. Some shields might obscure your vision, but that's what shields are for!");
            //_showBodyWhenHoldingWeapons = Config.Bind("Body", "ShowBodyHoldingWeapons", false, "Whether to show your body while you are actively holding weapons (e.g. when in combat)");
            _jawFix = Config.Bind("Body", "JawFix", false, "Tries to fix the visible jaw when helmet is set to be shown (even when not wearing a helmet). Might cause other artifacts for certain helmets.");

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
                switch (_currentFPMode)
                {
                    case FirstPersonModes.ThirdPerson:
                        _currentFPMode = FirstPersonModes.FirstPersonHelmet; SetFirstPerson(__instance, true);
                        break;
                    case FirstPersonModes.FirstPersonHelmet:
                        _currentFPMode = FirstPersonModes.FirstPersonNoHelmet;
                        break;
                    case FirstPersonModes.FirstPersonNoHelmet:
                        _currentFPMode = FirstPersonModes.ThirdPerson; SetFirstPerson(__instance, false);
                        break;
                    default:
                        break;
                }

                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Changing camera to " + _currentFPMode);

                ChangeMode(__instance);
            }

            if (_currentFPMode == FirstPersonModes.FirstPersonHelmet)
            {
                pos = _helmetAttach.position;
            }
            else if (_currentFPMode == FirstPersonModes.FirstPersonNoHelmet)
            {
                _head.localScale = _originalHeadScale;
                pos = _helmetAttach.position;
                _head.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
            }
        }

        private static void SetFirstPerson(GameCamera __instance, bool toFirstPerson)
        {
            if (toFirstPerson)
            {
                var cam = __instance.GetComponent<Camera>();

                // Setup near plane etc
                _oldNearPlane = cam.nearClipPlane;
                cam.nearClipPlane = _fspNearPlane.Value;
                __instance.m_nearClipPlaneMin = _fspNearPlane.Value;
                __instance.m_nearClipPlaneMax = _fspNearPlane.Value;
                _head = FindTransform(Player.m_localPlayer.transform, "Visual", "Armature", "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head");
                _helmetAttach = FindTransform(_head, "Helmet_attach");
                _jaw = FindTransform(_head, "Jaw");
                _originalHeadScale = _head.localScale;
                _originalJawScale = _jaw.localScale;
            }
            else
            {
                // reset original values
                var cam = __instance.GetComponent<Camera>();
                cam.nearClipPlane = _oldNearPlane;
                __instance.m_nearClipPlaneMin = _oldNearPlane;
                __instance.m_nearClipPlaneMax = _oldNearPlane;

                if (_head)
                {
                    _head.localScale = _originalHeadScale;
                    _jaw.localScale = _originalJawScale;
                }
            }
        }

        static void ChangeMode(GameCamera __instance)
        {
            var visEqu = Player.m_localPlayer.GetComponentInChildren<VisEquipment>();
            var beardGO = (GameObject)AccessTools.Field(typeof(VisEquipment), "m_beardItemInstance").GetValue(visEqu);
            var hairGO = (GameObject)AccessTools.Field(typeof(VisEquipment), "m_hairItemInstance").GetValue(visEqu);
            var helmetGO = (GameObject)AccessTools.Field(typeof(VisEquipment), "m_helmetItemInstance").GetValue(visEqu);

            //foreach (var renderer in GetComponentsInGrandChildren<Renderer>(visEqu.m_helmet))
            //{
            //    renderer.enabled = false;
            //}
            //foreach (var renderer in GetComponentsInGrandChildren<Renderer>(helmetGO))
            //{
            //    renderer.enabled = !IsFirstPerson;
            //}
            foreach (var renderer in GetComponentsInGrandChildren<Renderer>(beardGO))
            {
                renderer.enabled = _currentFPMode == FirstPersonModes.ThirdPerson;
            }
            foreach (var renderer in GetComponentsInGrandChildren<Renderer>(hairGO))
            {
                renderer.enabled = _currentFPMode == FirstPersonModes.ThirdPerson;
            }

            if (_jawFix.Value && _currentFPMode == FirstPersonModes.FirstPersonHelmet)
            {
                _jaw.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
            }
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

            if (_currentFPMode != FirstPersonModes.ThirdPerson)
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
        static void SetControls(Player __instance, bool attackHold, bool block)
        {
            _isAimingBow = attackHold && __instance.GetCurrentWeapon().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
        }

        private static IEnumerable<T> GetComponentsInGrandChildren<T>(GameObject root)
        {

            if (root == null) yield break;

            foreach (var comp in GetComponentsInGrandChildren<T>(root.transform))
            {
                yield return comp;
            }
        }

        private static IEnumerable<T> GetComponentsInGrandChildren<T>(Transform root)
        {

            if (root == null) yield break;

            foreach (var component in root.gameObject.GetComponents<T>())
            {
                Debug.Log("yielding on: " + root.name);
                yield return component;
            }

            foreach (Transform child in root)
            {
                foreach (var componentsInGrandChild in GetComponentsInGrandChildren<T>(child))
                {
                    yield return componentsInGrandChild;
                }
            }
        }

        private static Transform FindTransform(Transform root, params string[] path)
        {

            Transform output = root;
            for (int i = 0; i < path.Length; i++)
            {
                output = output.Find(path[i]);

                if (output == null) break;
            }

            return output;
        }

        private static void DumpHierarchy(Transform t, int depth)
        {
            Debug.Log(depth + ": " + t.name);
            foreach (Transform child in t)
            {
                DumpHierarchy(child, depth + 1);
            }
        }

        private static void DumpComponents(Transform foundHead)
        {
            Debug.Log("Components on " + foundHead.name);
            foreach (var component in foundHead.GetComponents<Component>())
            {
                Debug.Log("Found component " + component.GetType().Name);
            }

            foreach (Transform t in foundHead)
            {
                DumpComponents(t);
            }
        }
    }

    public enum FirstPersonModes
    {
        ThirdPerson,
        FirstPersonHelmet,
        FirstPersonNoHelmet,
    }
}