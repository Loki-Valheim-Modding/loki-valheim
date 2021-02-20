using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Loki.Mods {
    
    [BepInPlugin("com.loki.clientmods.valheim.stamina", "Stamina Mod", "1.0.0.0")]
    public class StaminaValheimClientMod : BaseUnityPlugin {
        
        private static ConfigEntry<float> _regeneration, _usage;
        
        void Awake() {
            _regeneration = Config.Bind("Modifiers", "StaminaRegeneration", 2f, "Modifier of stamina regeneration");
            _usage = Config.Bind("Modifiers", "StaminaUsage", 0.8f, "Modifier of stamina usage");
            Harmony.CreateAndPatchAll(typeof(StaminaValheimClientMod));
        }
        
        // 	public override void UseStamina(float v)
        [HarmonyPatch(typeof(Player), "UseStamina")]
        [HarmonyPrefix]
        static void ChangeStaminaUsage(Player __instance, ref float v) {
            if (Player.m_localPlayer != __instance) return;
            v *= _usage.Value;
        }
        
        // 	public override void AddStamina(float v)
        [HarmonyPatch(typeof(Player), "AddStamina")]
        [HarmonyPrefix]
        static void ChangeStaminaAdding(Player __instance, ref float v) {
            if (Player.m_localPlayer != __instance) return;
            v *= _regeneration.Value;
        }

    }
}