using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImmersionModValheimClientMod
{
    [BepInPlugin("com.loki.clientmods.valheim.immersion.nohealthbars", "No Healthbars Mod", "1.0.0.0")]
    public class HideEnemyHealthBarsClientMod : BaseUnityPlugin
    {
        private ConfigEntry<bool> _configEnabled;

        void Awake()
        {
            _configEnabled = Config.Bind("Settings", "Enabled", true, "Whether to enable this mod.");
            
            if (_configEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(HideEnemyHealthBarsClientMod));
            }
        }

        [HarmonyPatch(typeof(EnemyHud), "TestShow")]
        [HarmonyPrefix]
        public static bool OverrideResult(ref bool __result)
        {
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(Hud), "IsUserHidden")]
        [HarmonyPrefix]
        public static bool PostHudIsUserHidden(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
