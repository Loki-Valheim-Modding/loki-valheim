using System;
using System.Collections;
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
        private static ConfigEntry<bool> _meleeAimFix;
        private static ConfigEntry<bool> _defaultState;

        private static Transform _helmetAttach;
        private static Transform _jaw;
        private static Vector3 _originalHeadScale;
        private static Vector3 _originalJawScale;
        private static Transform _head;
        private static Transform _spine;
        private static bool _isAimingBow;

        private static bool _transitionOutOfAttack;

        private static string[] attacks = new string[]
        {
            "swing_sword 1",
            //"Aim bow",
            //"bow_fire",
            //"spear_throw",
            "bow_fire",
            //"swing_pickaxe",
            "Unarmed punch 1",
            "One handed Blocking",
            "Unarmed Blocking",
            "Shield blocking",
            "knife slash 1",
            "knife slash 2",
            "knife slash 3",
            "swing_sword 0",
            "swing_sword 2",
            //"swing_hammer",
            //"swing_hoe",
            "Two handed blocking",
            "swing_sledge",
            "sword_secondary",
            "atgeir_attack 0",
            "atgeir_attack 1",
            "atgeir_attack 2",
            "battleaxe_attack 0",
            "battleaxe_attack 1",
            "battleaxe_attack 2",
            "swing_axe 0",
            "swing_axe 1",
            "swing_axe 2",
            "Unarmed punch 2",
            "Unarmed kick",
            "knife secondary",
            "atgeir_secondary",
            "spear_poke",
            "battleaxe_powerattack",
            "mace_secondary",
        };

        private static FirstPersonValheimClientMod _thisMod;
        private static int _currentTransitionAnim;

        void Awake()
        {
            _thisMod = this;
            _fspNearPlane = Config.Bind("Camera", "FPSNearPlane", 0.05f, "The Near Plane of the camera during FP mode");
            _hotkey = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.H), "Hotkey used to cycle between first person modes");
            _showBodyWhenAiming = Config.Bind("Body", "ShowBodyWhenAiming", false, "Whether to show your body while aiming your bow. The bow obscures the center of your screen so you might want to disable it");
            _showBodyWhenBlocking = Config.Bind("Body", "ShowBodyWhenBlocking", false, "Whether to show your body while blocking. Some shields might obscure your vision, but that's what shields are for!");
            _jawFix = Config.Bind("Body", "JawFix", false, "[Experimental] Tries to fix the visible jaw when helmet is set to be shown (even when not wearing a helmet). Might cause other artifacts for certain helmets.");
            _meleeAimFix = Config.Bind("Body", "MeleeAimFix", false, "[Experimental] Changes the default melee attack direction (which is always straight forward from your body) into a direction based on your head camera.");
            _defaultState = Config.Bind("Controls", "StartsEnabled", true);

            _setVisibleInfo = typeof(Character).GetMethod("SetVisible", BindingFlags.NonPublic | BindingFlags.Instance);
            Harmony.CreateAndPatchAll(typeof(FirstPersonValheimClientMod));
        }

        [HarmonyPatch(typeof(Player), "OnSpawned")]
        [HarmonyPostfix]
        static void OnSpawnedPost(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                _thisMod.StartCoroutine(SmallSpawndelay());
            }
        }


        [HarmonyPatch(typeof(Attack), "Clone")]
        [HarmonyPostfix]
        static void AttackAwakePost(Attack __instance, ref Attack __result)
        {
            Console.instance.Print("New instance of attack has been cloned");

            if (_currentFPMode == FirstPersonModes.ThirdPerson)
                return;

            __result.m_attackHeight = 0;
        }

        [HarmonyPatch(typeof(Attack), "GetMeleeAttackDir")]
        [HarmonyPostfix]
        static void OverrideGetMeleeAttackDir(Attack __instance, ref Transform originJoint, ref Vector3 attackDir)
        {
            if (_currentFPMode == FirstPersonModes.ThirdPerson || !_meleeAimFix.Value)
                return;

            var m_character = (Humanoid)AccessTools.Field(typeof(Attack), "m_character").GetValue(__instance);
            if (Player.m_localPlayer != m_character)
                return;

            //originJoint = _spine.transform;
            originJoint = GameCamera.instance.transform;
            //originJoint = _head.transform;
            attackDir = originJoint.forward;
            //attackDir = Vector3.RotateTowards(m_character.transform.forward, aimDir, 0.017453292f * __instance.m_maxYAngle, 10f);
        }

        [HarmonyPatch(typeof(Player), "LateUpdate")]
        [HarmonyPostfix]
        static void LateUpdate(Player __instance)
        {
            if (_currentFPMode == FirstPersonModes.ThirdPerson || !_meleeAimFix.Value ||  Player.m_localPlayer != __instance)
                return;

            var animator = __instance.GetComponentInChildren<Animator>();
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            var nextState = animator.GetNextAnimatorStateInfo(0);

            foreach (var attack in attacks)
            {
                if (currentState.IsName(attack))
                {
                    //Console.instance.Print("In attack state");
                    _spine.rotation = GameCamera.instance.transform.rotation;
                    _transitionOutOfAttack = true;
                    _currentTransitionAnim = nextState.shortNameHash;

                }
                else if (nextState.IsName(attack))
                {
                    _transitionOutOfAttack = false;
                    _spine.rotation = Quaternion.RotateTowards(_spine.rotation, GameCamera.instance.transform.rotation, 360 * nextState.normalizedTime);
                    //Console.instance.Print("Transitioning to attack state, time " + nextState.normalizedTime);
                }
                //TODO bugged
                //else if (_transitionOutOfAttack)
                //{
                //    if (_currentTransitionAnim == currentState.shortNameHash)
                //    {
                //        _spine.rotation = Quaternion.RotateTowards(GameCamera.instance.transform.rotation, _spine.rotation, 360 * nextState.normalizedTime);
                //    }
                //}
            }

            //_helmetAttach.rotation = GameCamera.instance.transform.rotation;
        }

        [HarmonyPatch(typeof(Player), "OnDeath")]
        [HarmonyPostfix]
        static void OnDeathPost(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                if (_currentFPMode != FirstPersonModes.ThirdPerson)
                {
                    _currentFPMode = FirstPersonModes.ThirdPerson;
                    SetFirstPerson(GameCamera.instance, false);
                    ChangeMode(GameCamera.instance);
                }
            }
        }

        public static IEnumerator SmallSpawndelay()
        {
            yield return new WaitForSeconds(0.1f);
            _setVisible = null;

            if (_defaultState.Value && _currentFPMode == FirstPersonModes.ThirdPerson)
            {
                _currentFPMode = FirstPersonModes.FirstPersonHelmet;
                SetFirstPerson(GameCamera.instance, true);
                ChangeMode(GameCamera.instance);
            }
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
                _spine = FindTransform(Player.m_localPlayer.transform, "Visual", "Armature", "Hips", "Spine", "Spine1");
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
            
            foreach (var renderer in GetComponentsInGrandChildren<Renderer>(beardGO)) {
                renderer.enabled = _currentFPMode == FirstPersonModes.ThirdPerson;
            }
            foreach (var renderer in GetComponentsInGrandChildren<Renderer>(hairGO)) {
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