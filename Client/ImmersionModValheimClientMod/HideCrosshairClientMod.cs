using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ImmersionModValheimClientMod
{
    [BepInPlugin("com.loki.clientmods.valheim.immersion.hidecrosshair", "Hide Crosshair mod", "1.0.0.0")]
    public class HideCrosshairClientMod : BaseUnityPlugin
    {
        private ConfigEntry<bool> _configEnabled;
        private ConfigEntry<bool> _configHide;
        private ConfigEntry<KeyboardShortcut> _shortcut;

        private static bool hide;

        void Awake()
        {
            _configEnabled = Config.Bind("Settings", "EnableMod", true, "Whether to enable this mod.");
            _configHide = Config.Bind("Settings", "HideCrosshair", true, "Set to true to hide the crosshair by default, false to show it");
            _shortcut = Config.Bind("Settings", "Hotkey", new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl), "The key shortcut used to toggle this mod");
            hide = _configHide.Value;

            if (_configEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(HideCrosshairClientMod));
            }
        }

        void Update()
        {
            if (LokiUtils.IsDown(_shortcut.Value))
            {
                hide = !hide;
            }
        }

        // 	public override void UseStamina(float v)
        [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
        [HarmonyPostfix]
        public static void PostUpdateCrosshair(Hud __instance)
        {
            if (hide)
            {
                __instance.m_crosshair.color = UnityEngine.Color.clear;
                __instance.m_crosshairBow.color = UnityEngine.Color.clear;
            }
        }

        // 	public override void UseStamina(float v)
        [HarmonyPatch(typeof(Hud), "UpdateStealth")]
        [HarmonyPostfix]
        public static void PostUpdateStealth(Hud __instance)
        {
            if (hide)
            {
                __instance.m_targetedAlert.SetActive(false);
                __instance.m_hidden.SetActive(false);
                __instance.m_targeted.SetActive(false);
                __instance.m_stealthBar.gameObject.SetActive(false);
            }
        }
    }
}
