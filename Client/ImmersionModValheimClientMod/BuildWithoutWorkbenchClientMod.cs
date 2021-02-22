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
        private ConfigEntry<string> _configAllowedPieces;
        private ConfigEntry<string> _configIgnoredStations;

        private static string[] _allowedPieces = new string[]
        {
            "$piece_logbeam2",
            "$piece_logbeam4",
            "$piece_logpole2",
            "$piece_logpole4",
            "$piece_woodbeam1",
            "$piece_woodbeam2",
            "$piece_woodbeam26",
            "$piece_woodbeam45",
            "$piece_woodpole1",
            "$piece_woodpole2",
            "$piece_groundtorchwood",
            "$piece_sign",
        };

        private static string[] _ignoredStations = new string[]
        {

        };


        void Awake()
        {
            _configEnabled = Config.Bind("Settings", "Enabled", true, "Whether to enable this mod.");
            _configAllowedPieces = Config.Bind("Settings", "PiecesAllowedWithoutCraftingStation", String.Join(",", _allowedPieces), "A list of individual pieces where the crafting station check is ignored. Use this to allow specific pieces such as poles while still limiting complex pieces like walls and roofs. See https://valheim.fandom.com/wiki/Localization for a list");
            _configIgnoredStations = Config.Bind("Settings", "IgnoreCraftingStations", String.Join(",", _ignoredStations), "A list of crafting stations to ignore when checking whether you can build something. Use this to instantly allow all wooden building pieces without having a \"$piece_workbench\" nearby, for example");

            _allowedPieces = _configAllowedPieces.Value.Split(',');
            _ignoredStations = _configIgnoredStations.Value.Split(',');

            if (_configEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(BuildWithoutWorkbenchClientMod));
            }
        }

        [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Piece), typeof(Player.RequirementMode))]
        [HarmonyPrefix]
        public static void PlacePiecePre(Player __instance, Piece piece)
        {
            if (piece.m_craftingStation)
            {
                if (_ignoredStations.Contains(piece.m_craftingStation.m_name) || _allowedPieces.Contains(piece.m_name))
                {
                    piece.m_craftingStation = null;
                }
            }
        }
    }
}
