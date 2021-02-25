using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ImmersionModValheimClientMod {
    
    /// <summary>
    /// Allows disabling the minimap.
    /// </summary>
    [BepInPlugin("com.loki.clientmods.valheim.immersion.minimap", "Minimap Control Mod", "1.0.0.0")]
    public class MinimapControlsValheimClientMod : BaseUnityPlugin {
        private ConfigEntry<bool> _configEnabled;
        private static ConfigEntry<bool> _allowToggle;
        private static ConfigEntry<KeyboardShortcut> _toggleMinimap;
        
        private static bool _disableMinimap;
        
        void Awake() {

            _configEnabled = Config.Bind("Settings", "EnableMod", true, "Enables the mod if true");
            _allowToggle = Config.Bind("Settings", "AllowShown", true, "Allows the minimap to be toggled by hotkey. Disable this if you never want the minimap");
            _toggleMinimap = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.O, KeyCode.LeftControl), "If allowed to toggle the minimap, use this hotkey to toggle it");
            _disableMinimap = true;
            
            if (_configEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(MinimapControlsValheimClientMod));
            }
        }


        [HarmonyPatch(typeof(Minimap), "Update")]
        [HarmonyPostfix]
        static void DisableMinimapPostUpdate(Minimap __instance) {
            
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
                        || Utils.GetMainCamera() == null) {
                return;
            }

            if (_allowToggle.Value && LokiUtils.IsDown(_toggleMinimap.Value)) {
                _disableMinimap = !_disableMinimap;
                
                // Ensure that we re-enable the small root if we toggle.
                if(!_disableMinimap) __instance.m_smallRoot.SetActive(true);
            }

            // Disable minimap
            if (_disableMinimap) {
                __instance.m_largeRoot.SetActive(false);
                __instance.m_smallRoot.SetActive(false);
            }

        }

    }
}