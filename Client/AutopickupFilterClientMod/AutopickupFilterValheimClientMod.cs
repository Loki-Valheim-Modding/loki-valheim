using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AutopickupFilterValheimClientMod
{
    [BepInPlugin("com.loki.clientmods.valheim.autopickupfilter", "Autopickup Filter", "1.0.0.0")]
    public class AutopickupFilterValheimClientMod : BaseUnityPlugin
    {
        private static LootOption lootmode = LootOption.BlockTrash;
        private static bool blockTrophies;
        static string[] trash = new string[]
        {
            "$item_stone",
            "$item_wood",
            "$item_resin",

            "$item_chain",
            "$item_witheredbone",
            "$item_bonefragments",
            "$item_hardantler",
            "$item_ancientseed",

            "$item_leatherscraps",
            "$item_deerhide",
            "$item_trollhide",
            "$item_greydwarfeye",

            "$item_dandelion",
            "$item_fircone",
            "$item_pinecone",
            "$item_beechseeds",
        };
        private ConfigEntry<KeyboardShortcut> _configChangeModeKey;
        private ConfigEntry<LootOption> _configLootmode;
        private ConfigEntry<string> _configTrashList;
        private ConfigEntry<bool> _configBlockTrophiesTrash;

        private static bool firstStartup = true;

        void Awake()
        {
            _configChangeModeKey = Config.Bind("Input", "LootKey", new KeyboardShortcut(KeyCode.L), "The key used to cycle between loot modes");
            _configLootmode = Config.Bind("Options", "DefaultLootMode", LootOption.BlockTrash, "The default loot mode when the game starts.");
            _configBlockTrophiesTrash = Config.Bind("Options", "BlockTrophies", true, "Whether to block all trophies when in BlockTrash mode, in addition to the blocklist. You can also disable this and manually add trophy types to the trash list if you don't want to block all trophies.");
            _configTrashList = Config.Bind("Options", "TrashList", String.Join(",", trash), "The default list of trash items used when in the BlockTrash mode, separated by commas and containing no spaces. You can view a list of everything here https://valheim.fandom.com/wiki/Localization but you MUST add a $ in front of each word");
            
            lootmode = _configLootmode.Value;
            trash = _configTrashList.Value.Split(',');
            blockTrophies = _configBlockTrophiesTrash.Value;

            Harmony.CreateAndPatchAll(typeof(AutopickupFilterValheimClientMod));
        }

        [HarmonyPatch(typeof(ItemDrop), "Start")]
        [HarmonyPostfix]
        public static void StartFix(ItemDrop __instance)
        {
            if (firstStartup)
            {
                firstStartup = false;
                Console.instance.Print("Autoloot filter enabled and set to " + lootmode);
                Console.instance.Print("Current trash list: " + String.Join(", ", trash));
            }

            if (lootmode == LootOption.BlockNone)
            {
                return;
            }

            if (isTrash(__instance.m_itemData.m_shared.m_name))
            {
                __instance.m_autoPickup = false;
            }
        }

        public static bool isTrash(string name)
        {
            if (trash.Contains(name))
                return true;

            if (blockTrophies && name.StartsWith("$item_trophy_"))
                return true;

            return false;
        }

        void Update()
        {
            if (IsDown(_configChangeModeKey.Value) && Player.m_localPlayer != null && !Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible())
            {
                switch (lootmode)
                {
                    case LootOption.BlockNone:
                        lootmode = LootOption.BlockTrash;
                        break;
                    case LootOption.BlockTrash:
                        lootmode = LootOption.BlockNone;
                        break;
                    default:
                        break;
                }

                foreach (var item in GameObject.FindObjectsOfType<ItemDrop>())
                {
                    switch (lootmode)
                    {
                        case LootOption.BlockNone:
                            item.m_autoPickup = true;
                            break;
                        case LootOption.BlockTrash:
                            item.m_autoPickup = !isTrash(name);
                            break;
                        default:
                            break;
                    }
                }

                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Autoloot filter set to " + lootmode);
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

    }

    public enum LootOption
    {
        BlockNone,
        BlockTrash
    }
}
