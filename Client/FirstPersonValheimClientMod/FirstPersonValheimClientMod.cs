using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private static MethodInfo _setVisibleFieldInfo;
        private static FieldInfo _characterFieldInfo;
        private static FirstPersonModes _currentFPMode = FirstPersonModes.ThirdPerson;

        private static float _oldNearPlane;
        private static ConfigEntry<float> _fspNearPlane;
        private static ConfigEntry<KeyboardShortcut> _hotkey;
        private static ConfigEntry<string> _configStatesAllowed;
        private static ConfigEntry<bool> _showBodyWhenAiming;
        private static ConfigEntry<bool> _showBodyWhenBlocking;
        private static ConfigEntry<bool> _jawFix;
        private static ConfigEntry<bool> _meleeAimFix;
        private static ConfigEntry<bool> _overrideFoV;
        private static ConfigEntry<int> _configFoVThirdPerson;
        private static ConfigEntry<int> _configFoVFirstPerson;
        private static Transform _helmetAttach;
        private static Transform _jaw;
        private static Vector3 _originalHeadScale;
        private static Vector3 _originalJawScale;
        private static Transform _head;
        private static Transform _spine;
        private static bool _isAimingBow;
        private static FirstPersonValheimClientMod _thisMod;
        private static List<FirstPersonModes> _statesAllowed;

        void Awake()
        {
            _thisMod = this;
            _fspNearPlane = Config.Bind("Camera", "FPSNearPlane", 0.05f, "The Near Plane of the camera during FP mode");
            _hotkey = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.H), "Hotkey used to cycle between first person. You can also add a modifier, e.g. H + LeftControl");
            _showBodyWhenAiming = Config.Bind("Body", "ShowBodyWhenAiming", false, "Whether to show your body while aiming your bow. The bow obscures the center of your screen so you might want to disable it");
            _showBodyWhenBlocking = Config.Bind("Body", "ShowBodyWhenBlocking", false, "Whether to show your body while blocking. Some shields might obscure your vision, but that's what shields are for!");
            _jawFix = Config.Bind("Body", "JawFix", false, "[Experimental] Tries to fix the visible jaw when helmet is set to be shown (even when not wearing a helmet). Might cause other artifacts for certain helmets.");
            _meleeAimFix = Config.Bind("Body", "MeleeAimFix", true, "[Experimental] Changes the default melee attack direction (which is always straight forward from your body) into a direction based on your head camera.");
            _configStatesAllowed = Config.Bind("Body", "Modes", String.Join(",", (new FirstPersonModes[] { FirstPersonModes.FirstPersonNoHelmet, FirstPersonModes.ThirdPerson }).Select(x => x.ToString())), "The list of modes that you want to be able to cycle through when the hotkey is pressed. The first entry is the mode used when the game starts. Current functional options: ThirdPerson, FirstPersonHelmet, FirstPersonNoHelmet, FirstPersonNoBody");

            _overrideFoV = Config.Bind("Camera", "OverrideFoV", false, "Override the game's default FoV of 65 with your own setting");
            _configFoVThirdPerson = Config.Bind("Camera", "FovThirdPerson", 90, "The FoV used when in third person");
            _configFoVFirstPerson = Config.Bind("Camera", "FovFirstPerson", 90, "The FoV used when in first person");

            _setVisibleFieldInfo = typeof(Character).GetMethod("SetVisible", BindingFlags.NonPublic | BindingFlags.Instance);
            _characterFieldInfo = AccessTools.Field(typeof(Attack), "m_character");

            if (_configStatesAllowed.Value == null || !_configStatesAllowed.Value.Any())
            {
                _statesAllowed = new List<FirstPersonModes>() { FirstPersonModes.FirstPersonNoHelmet };
            }
            else
            {
                _statesAllowed = new List<FirstPersonModes>(_configStatesAllowed.Value.Split(',').Select(x => (FirstPersonModes)Enum.Parse(typeof(FirstPersonModes), x.Trim())));
            }

            Harmony.CreateAndPatchAll(typeof(FirstPersonValheimClientMod));

            if (_meleeAimFix.Value)
            {
                StartCoroutine(PlayerFixedUpdate());
            }
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

        [HarmonyPatch(typeof(Attack), "Start")]
        [HarmonyPrefix]
        static void AttackStartPre(Attack __instance, Humanoid character)
        {
            if (IsThirdPerson(CurrentFPMode) || !_meleeAimFix.Value)
                return;

            if (Player.m_localPlayer != character)
                return;

            if (__instance.m_attackType == Attack.AttackType.Projectile)
            {
                // Compensate ranged attacks with extra height since they originate from the feet.
                // Would be better to just fire the arrow directly from the camera forward
                __instance.m_attackHeight = 1.2f;
            }
            else
            {
                // Add no extra height when doing a melee attack since we originate from our eyes instead of feet
                __instance.m_attackHeight = 0;
            }
        }

        [HarmonyPatch(typeof(Attack), "GetMeleeAttackDir")]
        [HarmonyPostfix]
        static void OverrideGetMeleeAttackDir(Attack __instance, ref Transform originJoint, ref Vector3 attackDir)
        {
            if (IsThirdPerson(CurrentFPMode) || !_meleeAimFix.Value || CurrentFPMode == FirstPersonModes.FirstPersonOnlyWeapons)
                return;

            var m_character = (Humanoid)_characterFieldInfo.GetValue(__instance);
            if (Player.m_localPlayer != m_character)
                return;

            originJoint = GameCamera.instance.transform;
            attackDir = originJoint.forward;
        }

        public IEnumerator PlayerFixedUpdate()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();

                if (IsThirdPerson(CurrentFPMode))
                    continue;

                if (CurrentFPMode == FirstPersonModes.FirstPersonNoBody || CurrentFPMode == FirstPersonModes.FirstPersonOnlyWeapons)
                    continue;

                try
                {
                    var __instance = Player.m_localPlayer;
                    var cam = GameCamera.instance.transform;
                    var camRot = new Vector3(cam.localEulerAngles.x, 0, 0);
                    var camDir = new Vector3(0, cam.eulerAngles.y, 0);
                    _spine.transform.Rotate(-camDir, Space.World);
                    _spine.transform.Rotate(camRot, Space.World);
                    _spine.transform.Rotate(camDir, Space.World);
                }
                catch
                {

                }
            }
        }

        [HarmonyPatch(typeof(CharacterAnimEvent), "UpdateLookat")]
        [HarmonyPrefix]
        static bool PreUpdateLookat(CharacterAnimEvent __instance)
        {
            if (!IsThirdPerson(CurrentFPMode) && _meleeAimFix.Value && __instance.GetComponentInParent<Character>() == Player.m_localPlayer)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterAnimEvent), "UpdateHeadRotation")]
        [HarmonyPrefix]
        static bool PreUpdateHeadRotation(CharacterAnimEvent __instance)
        {
            if (!IsThirdPerson(CurrentFPMode) && _meleeAimFix.Value && __instance.GetComponentInParent<Character>() == Player.m_localPlayer)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Player), "OnDeath")]
        [HarmonyPostfix]
        static void OnDeathPost(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                CurrentFPMode = FirstPersonModes.ThirdPerson;
            }
        }

        public static IEnumerator SmallSpawndelay()
        {
            yield return new WaitForSeconds(0.1f);
            _setVisible = null;

            CurrentFPMode = _statesAllowed.First();
        }

        // private void GetCameraPosition(float dt, out Vector3 pos, out Quaternion rot)
        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        [HarmonyPostfix]
        static void SetCameraPositionToEyeOnFPS(GameCamera __instance, float dt, ref Vector3 pos, ref Quaternion rot)
        {
            // Toggle FPS on H
            if (IsDown(_hotkey.Value) && Player.m_localPlayer != null && !Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible())
            {
                if (_statesAllowed.Contains(CurrentFPMode))
                {
                    var index = _statesAllowed.IndexOf(CurrentFPMode) + 1;
                    if (index >= _statesAllowed.Count)
                        index = 0;
                    CurrentFPMode = _statesAllowed[index];
                }
                else
                {
                    CurrentFPMode = _statesAllowed.First();
                }
            }

            if (CurrentFPMode == FirstPersonModes.FirstPersonHelmet || CurrentFPMode == FirstPersonModes.FirstPersonNoBody || CurrentFPMode == FirstPersonModes.FirstPersonOnlyWeapons)
            {
                pos = _helmetAttach.position;
            }
            else if (CurrentFPMode == FirstPersonModes.FirstPersonNoHelmet )
            {
                _head.localScale = _originalHeadScale;
                pos = _helmetAttach.position;
                _head.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
            }
        }

        private static bool IsDown(KeyboardShortcut value)
        {
            if (Input.GetKeyDown(value.MainKey))
            {
                if (value.Modifiers != null)
                {
                    foreach (var mod in value.Modifiers)
                    {
                        if (!Input.GetKey(mod))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
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

                if (_overrideFoV.Value)
                    __instance.m_fov = _configFoVFirstPerson.Value;
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

                if (_overrideFoV.Value)
                    __instance.m_fov = _configFoVThirdPerson.Value;
            }
        }

        static void ChangeMode(GameCamera __instance)
        {
            var visEqu = Player.m_localPlayer.GetComponentInChildren<VisEquipment>();
            var beardGO = (GameObject)AccessTools.Field(typeof(VisEquipment), "m_beardItemInstance").GetValue(visEqu);
            var hairGO = (GameObject)AccessTools.Field(typeof(VisEquipment), "m_hairItemInstance").GetValue(visEqu);

            foreach (var renderer in GetComponentsInGrandChildren<Renderer>(beardGO))
            {
                renderer.enabled = IsThirdPerson(CurrentFPMode);
            }
            foreach (var renderer in GetComponentsInGrandChildren<Renderer>(hairGO))
            {
                renderer.enabled = IsThirdPerson(CurrentFPMode);
            }

            Player.m_localPlayer.GetVisual().transform.Find("body").GetComponent<SkinnedMeshRenderer>().enabled = CurrentFPMode != FirstPersonModes.FirstPersonOnlyWeapons;

            if (_jawFix.Value && CurrentFPMode == FirstPersonModes.FirstPersonHelmet)
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
                _setVisible = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), __instance, _setVisibleFieldInfo);
            }

            if (!IsThirdPerson(CurrentFPMode))
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

                // hidden character model messes up the spine rotation for some reason, so force it to true
                if (_meleeAimFix.Value)
                {
                    visible = true;
                }

                // but override it to false if we don't want a body!
                if (CurrentFPMode == FirstPersonModes.FirstPersonNoBody)
                {
                    visible = false;
                }

                _setVisible(visible);
            }
        }

        private static FirstPersonModes CurrentFPMode
        {
            get
            {
                return _currentFPMode;
            }
            set
            {
                if (_currentFPMode == value)
                    return;

                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Changing camera to " + value);

                if (IsThirdPerson(_currentFPMode) && !IsThirdPerson(value))
                {
                    _currentFPMode = value;
                    SetFirstPerson(GameCamera.instance, true);
                }
                else if (!IsThirdPerson(_currentFPMode) && IsThirdPerson(value))
                {
                    _currentFPMode = value;
                    SetFirstPerson(GameCamera.instance, false);
                }
                else
                {
                    _currentFPMode = value;
                }

                ChangeMode(GameCamera.instance);
            }
        }

        private static bool IsThirdPerson(FirstPersonModes mode)
        {
            return mode == FirstPersonModes.ThirdPerson;
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
        FirstPersonOnlyWeapons,
        FirstPersonNoBody,
    }
}