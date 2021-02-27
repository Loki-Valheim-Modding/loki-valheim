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
        private static Animator _animator;
        private static MethodInfo _setVisibleFieldInfo;
        private static FieldInfo _characterFieldInfo;
        private static FieldInfo _currentZoomDistance;
        private static FirstPersonModes _currentFPMode = FirstPersonModes.ThirdPerson;

        private static float _oldNearPlane;
        private static ConfigEntry<float> _fspNearPlane;
        private static ConfigEntry<KeyboardShortcut> _hotkey;
        private static ConfigEntry<string> _configStatesAllowed;
        private static ConfigEntry<bool> _allowScrollToChangeState;
        private static ConfigEntry<bool> _showBodyWhenAiming;
        private static ConfigEntry<bool> _showBodyWhenBlocking;
        private static ConfigEntry<bool> _jawFix;
        private static ConfigEntry<bool> _meleeAimFix;
        private static ConfigEntry<bool> _overrideFoV;
        private static ConfigEntry<int> _configFoVThirdPerson;
        private static ConfigEntry<int> _configFoVFirstPerson;
        private static ConfigEntry<ForceBodyRotationMode> _configForceBodyRotationModeWhileStandingStill;
        private static ConfigEntry<bool> _configLimitCameraRotationWhenInIdleAnimation;
        private static ConfigEntry<bool> _configShowMessageOnSwitching;
        private static ConfigEntry<bool> _VPlusCompatibility;
        private static Transform _helmetAttach;
        private static Transform _jaw;
        private static Vector3 _originalHeadScale;
        private static Vector3 _originalJawScale;
        private static Transform _head;
        private static Transform _spine;
        private static bool _isAimingBow;
        private static FirstPersonValheimClientMod _thisMod;
        private static List<FirstPersonModes> _statesAllowed;

        private static Type _vplusTypeAem;
        private static Type _vplusTypeAbm;
        private static FieldInfo _inventoryAnimator;

        void Awake()
        {
            _thisMod = this;
            _fspNearPlane = Config.Bind("Camera", "FPSNearPlane", 0.05f, "The Near Plane of the camera during FP mode");
            _hotkey = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.H), "Hotkey used to cycle between first person. You can also add a modifier, e.g. H + LeftControl");
            _showBodyWhenAiming = Config.Bind("Body", "ShowBodyWhenAiming", false, "Whether to show your body while aiming your bow. The bow obscures the center of your screen so you might want to disable it");
            _showBodyWhenBlocking = Config.Bind("Body", "ShowBodyWhenBlocking", false, "Whether to show your body while blocking. Some shields might obscure your vision, but that's what shields are for!");
            _jawFix = Config.Bind("Body", "JawFix", false, "[Experimental] Tries to fix the visible jaw when helmet is set to be shown (even when not wearing a helmet). Might cause other artifacts for certain helmets.");
            _meleeAimFix = Config.Bind("Body", "MeleeAimFix", true, "[Experimental] Changes the default melee attack direction (which is always straight forward from your body) into a direction based on your head camera.");
            _configStatesAllowed = Config.Bind("Body", "Modes", String.Join(",", (new FirstPersonModes[] { FirstPersonModes.FirstPersonNoHelmet, FirstPersonModes.ThirdPerson }).Select(x => x.ToString())), "The list of modes that you want to be able to cycle through when the hotkey is pressed. The first entry is the mode used when the game starts. Currently functional options: ThirdPerson, FirstPersonHelmet, FirstPersonNoHelmet, FirstPersonNoBody, FirstPersonNoHelmetAlt");
            _allowScrollToChangeState = Config.Bind("Controls", "AllowScrolling", true, "When using the scroll zoom option, scroll into and out of first person. When going from third person into first person, it takes the option that comes after ThirdPerson. When going from first person into third person, if there is no ThirdPerson in the list, it will stay in first person when zooming in.");
            _configShowMessageOnSwitching = Config.Bind("Controls", "ShowMessageWhenSwitching", true, "Show a notification message in the topleft when switching camera mode");
            _overrideFoV = Config.Bind("Camera", "OverrideFoV", false, "Override the game's default FoV of 65 with your own setting");
            _configFoVThirdPerson = Config.Bind("Camera", "FovThirdPerson", 90, "The FoV used when in third person");
            _configFoVFirstPerson = Config.Bind("Camera", "FovFirstPerson", 90, "The FoV used when in first person");
            _configForceBodyRotationModeWhileStandingStill = Config.Bind("Body", "ForceBodyRotationModeWhileStandingStill", ForceBodyRotationMode.ForceRotateAtShoulders, "Choose how the body should act when the camera rotates left or right; AlwaysForward will force the body to rotate if possible. The shoulders modes will kick in once you rotate 90 degrees to the side, while rotate freely allows free 360 movement");
            _configLimitCameraRotationWhenInIdleAnimation = Config.Bind("Body", "LimitCameraRotationWhenInIdleAnimation", true, "During certain non-action animation states (e.g. sitting down or holding the mast), limit the camera to only rotate 90 degrees left or right. During action animation states (e.g. combat, dodge rolls), free rotation is always available.");

            _setVisibleFieldInfo = typeof(Character).GetMethod("SetVisible", BindingFlags.NonPublic | BindingFlags.Instance);
            _characterFieldInfo = AccessTools.Field(typeof(Attack), "m_character");
            _currentZoomDistance = AccessTools.Field(typeof(GameCamera), "m_distance");
            _inventoryAnimator = AccessTools.Field(typeof(InventoryGui), "m_animator");

            _VPlusCompatibility = Config.Bind("Compatibility", "ValheimPlus", false, "Experimental compatibility mode with ValheimPlus. Enabling this will prevent zooming when in their build/edit mode.");


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

        private static int _animationHeadToShoulderBias = 50;

        public IEnumerator PlayerFixedUpdate()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();

                if (IsThirdPerson(CurrentFPMode))
                    continue;

                if (CurrentFPMode == FirstPersonModes.FirstPersonNoBody || CurrentFPMode == FirstPersonModes.FirstPersonOnlyWeapons || CurrentFPMode == FirstPersonModes.FirstPersonHelmet)
                    continue;

                try
                {
                    var __instance = Player.m_localPlayer;
                    var cam = GameCamera.instance.transform;
                    var camDir = new Vector3(0, cam.eulerAngles.y, 0);

                    // When idling, we want to rotate our head so our arms etc stay the same.
                    // When attacking or doing other arm things, we want to rotate our spine so the arms are also affected by our cam direction
                    // However, hard switching between the two is jarring, so here we attempt to smooth it out a little.
                    if (CurrentAnimationState(__instance) != AnimationState.Action)
                    {
                        _animationHeadToShoulderBias++;
                    }
                    else
                    {
                        _animationHeadToShoulderBias--;
                    }

                    if (_animationHeadToShoulderBias < 0)
                        _animationHeadToShoulderBias = 0;
                    if (_animationHeadToShoulderBias > 15)
                        _animationHeadToShoulderBias = 15;

                    var angle = cam.localEulerAngles.x;
                    if (angle > 180)
                        angle -= 360;

                    var camRotHead = new Vector3(angle * (_animationHeadToShoulderBias / 15f), 0, 0);
                    var camRotSpine = new Vector3(angle * (1 - (_animationHeadToShoulderBias / 15f)), 0, 0);

                    _head.transform.Rotate(-camDir, Space.World);
                    _head.transform.Rotate(camRotHead, Space.World);
                    _head.transform.Rotate(camDir, Space.World);

                    _spine.transform.Rotate(-camDir, Space.World);
                    _spine.transform.Rotate(camRotSpine, Space.World);
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
            if (!IsThirdPerson(CurrentFPMode) && _meleeAimFix.Value && __instance.GetComponentInParent<Character>() == Player.m_localPlayer && CurrentFPMode != FirstPersonModes.FirstPersonHelmet)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterAnimEvent), "UpdateHeadRotation")]
        [HarmonyPrefix]
        static bool PreUpdateHeadRotation(CharacterAnimEvent __instance)
        {
            if (!IsThirdPerson(CurrentFPMode) && _meleeAimFix.Value && __instance.GetComponentInParent<Character>() == Player.m_localPlayer && CurrentFPMode != FirstPersonModes.FirstPersonHelmet)
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
            _animator = Player.m_localPlayer.GetComponentInChildren<Animator>();
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
                CycleMode();
            }

            //if (IsDown(new KeyboardShortcut(KeyCode.B)))
            //{
            //    CycleCamLockMode();
            //}

            //if (IsDown(new KeyboardShortcut(KeyCode.V)))
            //{
            //    _configLimitCameraRotationWhenInIdleAnimation.Value = !_configLimitCameraRotationWhenInIdleAnimation.Value;
            //    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Changing lock mode to " + _configLimitCameraRotationWhenInIdleAnimation.Value);

            //}


            if (CurrentFPMode == FirstPersonModes.FirstPersonHelmet || CurrentFPMode == FirstPersonModes.FirstPersonNoBody || CurrentFPMode == FirstPersonModes.FirstPersonOnlyWeapons || CurrentFPMode == FirstPersonModes.FirstPersonNoHelmetAlt)
            {
                pos = _helmetAttach.position;
            }
            else if (CurrentFPMode == FirstPersonModes.FirstPersonNoHelmet)
            {
                _head.localScale = _originalHeadScale;
                pos = _helmetAttach.position;
                _head.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
            }

            if (!IsThirdPerson(CurrentFPMode))
            {
                UpdateRotation(Player.m_localPlayer, __instance, ref rot);
            }
        }

        private static void CycleCamLockMode()
        {
            switch (_configForceBodyRotationModeWhileStandingStill.Value)
            {
                case ForceBodyRotationMode.AlwaysForward:
                    _configForceBodyRotationModeWhileStandingStill.Value = ForceBodyRotationMode.LockNearShoulders;
                    break;
                case ForceBodyRotationMode.LockNearShoulders:
                    _configForceBodyRotationModeWhileStandingStill.Value = ForceBodyRotationMode.ForceRotateAtShoulders;
                    break;
                case ForceBodyRotationMode.ForceRotateAtShoulders:
                    _configForceBodyRotationModeWhileStandingStill.Value = ForceBodyRotationMode.SnapRotateAtShoulders;
                    break;
                case ForceBodyRotationMode.SnapRotateAtShoulders:
                    _configForceBodyRotationModeWhileStandingStill.Value = ForceBodyRotationMode.RotateFreely;
                    break;
                case ForceBodyRotationMode.RotateFreely:
                    _configForceBodyRotationModeWhileStandingStill.Value = ForceBodyRotationMode.AlwaysForward;
                    break;
                default:
                    break;
            }

            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Changing rotation mode to " + _configForceBodyRotationModeWhileStandingStill.Value);
        }

        private static void CycleMode()
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

        private static Transform _currentHelmet;

        [HarmonyPatch(typeof(VisEquipment), "AttachItem")]
        [HarmonyPostfix]
        static void FixHelmetScale(Transform joint, ref GameObject __result)
        {
            if (__result == null)
                return;

            if (joint == _helmetAttach)
            {
                _currentHelmet = __result.transform;

                if (CurrentFPMode == FirstPersonModes.FirstPersonNoHelmet)
                {
                    __result.transform.localScale = Vector3.one;
                }
                else if (CurrentFPMode == FirstPersonModes.FirstPersonNoHelmetAlt)
                {
                    __result.SetActive(false);
                }
            }
        }

        static void ChangeMode(GameCamera __instance)
        {
            foreach (Transform child in _helmetAttach)
            {
                child.gameObject.SetActive(IsThirdPerson(CurrentFPMode));
            }

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

            if (CurrentFPMode == FirstPersonModes.FirstPersonHelmet)
            {
                if (_currentHelmet != null)
                {
                    _currentHelmet.gameObject.SetActive(true);
                }
            }

            if (CurrentFPMode == FirstPersonModes.FirstPersonHelmet || CurrentFPMode == FirstPersonModes.FirstPersonNoHelmetAlt)
            {
                if (_jawFix.Value)
                {
                    _jaw.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
                }
            }


        }

        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        [HarmonyPostfix]
        static void ForceCharacterModelVisible(Player __instance, ZNetView ___m_nview)
        {
            // See Player code aborting on desync issues
            if (___m_nview == null || ___m_nview.GetZDO() == null || !___m_nview.IsOwner() || Player.m_localPlayer != __instance)
                return;

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                if (CanScroll(__instance) && _allowScrollToChangeState.Value)
                {
                    if (scroll < 0)
                    {
                        // zoom out
                        if (!IsThirdPerson(CurrentFPMode) && _statesAllowed.Contains(FirstPersonModes.ThirdPerson))
                        {
                            CurrentFPMode = FirstPersonModes.ThirdPerson;
                        }
                    }
                    else if (scroll > 0)
                    {
                        // zoom in
                        if (IsThirdPerson(CurrentFPMode))
                        {
                            var gc = GameCamera.instance;
                            if ((float)_currentZoomDistance.GetValue(gc) <= gc.m_minDistance)
                            {
                                CycleMode();
                            }
                        }
                    }
                }
            }

            // Late bind to instance
            if (_setVisible == null)
            {
                _setVisible = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), __instance, _setVisibleFieldInfo);
            }

            if (!IsThirdPerson(CurrentFPMode))
            {
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

        private static void UpdateRotation(Player p, GameCamera c, ref Quaternion rot)
        {
            var state = CurrentAnimationState(p);

            switch (state)
            {
                case AnimationState.FrozenAction: // Do nothing!
                    break;
                case AnimationState.FrozenIdle:
                    UpdateFrozenAnimationState(p, c, ref rot);
                    break;
                case AnimationState.StandingStill:
                    UpdateStandingStillAnimationState(p, c, ref rot);
                    break;
                case AnimationState.Action:
                    p.FaceLookDirection();
                    break;
                default:
                    break;
            }
        }

        private static void UpdateStandingStillAnimationState(Player p, GameCamera c, ref Quaternion rot)
        {
            switch (_configForceBodyRotationModeWhileStandingStill.Value)
            {
                case ForceBodyRotationMode.AlwaysForward:
                    p.FaceLookDirection();
                    break;
                case ForceBodyRotationMode.LockNearShoulders:
                    LockToShoulders(p, c, ref rot);
                    break;
                case ForceBodyRotationMode.ForceRotateAtShoulders:
                    ForceRotateAtShoulders(p, c);
                    break;
                case ForceBodyRotationMode.SnapRotateAtShoulders:
                    SnapRotateAtShoulders(p, c);
                    break;
                case ForceBodyRotationMode.RotateFreely: // do nothing!
                    break;
                default:
                    break;
            }
        }

        private static void LockToShoulders(Player p, GameCamera c, ref Quaternion rot)
        {
            CalcAngleAndCross(p, c, out var angle, out var cross);

            if (angle > 90)
            {
                var diff = angle - 90;

                if (cross < 0)
                {
                    angle = -angle;
                    diff = angle + 90;
                }

                p.SetMouseLook(new Vector2(diff, 0));
                rot = p.m_eye.transform.rotation;
            }
        }

        private static void SnapRotateAtShoulders(Player p, GameCamera c)
        {
            CalcAngleAndCross(p, c, out var angle, out var cross);
            if (angle > 90)
            {
                p.FaceLookDirection();
            }
        }

        private static void ForceRotateAtShoulders(Player p, GameCamera c)
        {
            CalcAngleAndCross(p, c, out var angle, out var cross);
            if (angle > 90)
            {
                var diff = angle - 90;

                if (cross < 0)
                {
                    angle = -angle;
                    diff = angle + 90;
                }
                p.transform.eulerAngles -= new Vector3(0, diff, 0);
            }
        }

        private static void CalcAngleAndCross(Player p, GameCamera c, out float angle, out float cross)
        {
            var cam = c.transform;
            var camEuler = cam.eulerAngles;
            var camDir = new Vector3(0, camEuler.y, 0);
            var pDir = new Vector3(0, p.transform.eulerAngles.y, 0);

            var qcam = Quaternion.Euler(camDir);
            var qp = Quaternion.Euler(pDir);

            var fcam = qcam * Vector3.forward;
            var fp = qp * Vector3.forward;

            cross = Vector3.Cross(fcam, fp).y;
            angle = Quaternion.Angle(qcam, qp);

        }

        private static void UpdateFrozenAnimationState(Player p, GameCamera c, ref Quaternion rot)
        {
            if (_configLimitCameraRotationWhenInIdleAnimation.Value)
            {
                LockToShoulders(p, c, ref rot);
            }
            else
            {
                // do nothing!
            }
        }

        private static bool CanScroll(Player p)
        {
            if (p.InPlaceMode())
                return false;
            if (Minimap.IsOpen())
                return false;
            if (p.GetCurrentCraftingStation())
                return false;
            if (VPlusAxMIsActive())
                return false;
            if (InventoryGui.instance && ((Animator)_inventoryAnimator.GetValue(InventoryGui.instance)).GetBool("visible"))
                return false;

            return true;
        }

        private static bool VPlusAxMIsActive()
        {
            if (_VPlusCompatibility.Value)
            {
                if (_vplusTypeAem == null)
                {
                    _vplusTypeAbm = AccessTools.TypeByName("ABM");
                    _vplusTypeAem = AccessTools.TypeByName("AEM");
                }

                try
                {
                    if (AccessTools.StaticFieldRefAccess<bool>(_vplusTypeAbm, "isActive"))
                        return true;
                    if (AccessTools.StaticFieldRefAccess<bool>(_vplusTypeAem, "isActive"))
                        return true;
                }
                catch (Exception ex)
                {
                    Debug.Log("Error with VPlus compatibility: " + ex);
                }
            }
            return false;
        }

        private static AnimationState CurrentAnimationState(Player __instance)
        {
            if (__instance.IsDodgeInvincible())
                return AnimationState.FrozenAction;

            var currentAnim = _animator.GetCurrentAnimatorStateInfo(0);

            if (currentAnim.IsName("Dodge") || currentAnim.IsTag("knockeddown") || currentAnim.IsName("HoldDragon"))
                return AnimationState.FrozenAction;

            if (currentAnim.IsTag("freeze") || currentAnim.IsTag("sitting") || currentAnim.IsTag("cutscene") || currentAnim.IsName("HoldMast"))
                return AnimationState.FrozenIdle;

            if (currentAnim.IsName("Movement") || currentAnim.IsName("Encumbered"))
            {
                if (__instance.GetVelocity().magnitude < 0.01f)
                {
                    return AnimationState.StandingStill;
                }
            }

            return AnimationState.Action;
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

                if (_configShowMessageOnSwitching.Value)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Changing camera to " + value);
                }

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
        FirstPersonNoHelmetAlt,
        FirstPersonOnlyWeapons,
        FirstPersonNoBody,
    }

    public enum ForceBodyRotationMode
    {
        AlwaysForward,
        LockNearShoulders,
        ForceRotateAtShoulders,
        SnapRotateAtShoulders,
        RotateFreely,
    }

    public enum AnimationState
    {
        FrozenAction,
        FrozenIdle,
        StandingStill,
        Action
    }
}