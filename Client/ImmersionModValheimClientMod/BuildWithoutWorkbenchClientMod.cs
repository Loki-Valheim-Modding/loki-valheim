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
    [BepInPlugin("com.loki.clientmods.valheim.immersion.buildwithoutworkbench", "Build Without Workbench Mod", "1.0.0.0")]
    public class BuildWithoutWorkbenchClientMod : BaseUnityPlugin
    {
        private ConfigEntry<bool> _configEnabled;

        void Awake()
        {
            _configEnabled = Config.Bind("Settings", "Enabled", true, "Whether to enable this mod.");

            if (_configEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(BuildWithoutWorkbenchClientMod));
            }
        }

        [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Piece), typeof(Player.RequirementMode))]
        [HarmonyPrefix]
        public static void PlacePiecePre(Player __instance, Piece piece)
        {
            Console.instance.Print("Place piece pre");
            if (piece.m_craftingStation)
            {
                Console.instance.Print("Piece requires crafting station");
                if (piece.m_craftingStation.m_name == "$piece_workbench")
                {
                    Console.instance.Print("Piece requires workbench. Removing.");
                    piece.m_craftingStation = null;
                }
            }
        }
    }
}
