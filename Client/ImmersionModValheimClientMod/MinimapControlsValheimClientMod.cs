using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loki.Mods {
    
    /// <summary>
    /// Allows disabling the minimap.
    /// </summary>
    [BepInPlugin("com.loki.clientmods.valheim.immersion.minimap", "Minimap Control Mod", "1.0.0.0")]
    public class MinimapControlsValheimClientMod : BaseUnityPlugin {
        
        private static ConfigEntry<KeyboardShortcut> _toggleMinimap;
        private static ConfigEntry<bool> _defaultState;
        
        private static bool _disableMinimap;
        
        void Awake() {
            _toggleMinimap = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.O, KeyCode.LeftControl));
            _defaultState = Config.Bind("Controls", "StartsEnabled", false);

            _disableMinimap = _defaultState.Value;
            
            Harmony.CreateAndPatchAll(typeof(MinimapControlsValheimClientMod));
        }


        [HarmonyPatch(typeof(Minimap), "Update")]
        [HarmonyPostfix]
        void DisableMinimapPostUpdate(Minimap __instance) {
            
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
                        || Utils.GetMainCamera() == null) {
                return;
            }

            if (_toggleMinimap.Value.IsDown()) {
                _disableMinimap = !_disableMinimap;
                
                // Ensure that we re-enable the small root if we toggle.
                if(!_disableMinimap) __instance.m_smallRoot.SetActive(false);
            }

            // Disable minimap
            if (_disableMinimap) {
                __instance.m_largeRoot.SetActive(false);
                __instance.m_smallRoot.SetActive(false);
            }

        }

    }
}