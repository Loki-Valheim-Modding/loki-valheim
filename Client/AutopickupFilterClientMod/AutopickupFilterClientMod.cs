using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AutopickupFilterClientMod
{
    [BepInPlugin("com.freecode.mods.valheim.autopickupfilter", "Autopickup Filter", "1.0.0.0")]
    public class AutopickupFilterClientMod : BaseUnityPlugin
    {
        static LootOption lootmode = LootOption.BlockTrash;
        static string[] trash = new string[]
        {
            "$item_stone",
            "$item_wood",
            "$item_resin",

            "$item_chain",
            "$item_witheredbone",
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

        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(AutopickupFilterClientMod));
        }

        [HarmonyPatch(typeof(ItemDrop), "Start")]
        [HarmonyPostfix]
        public static void StartFix(ItemDrop __instance)
        {
            if (lootmode == LootOption.BlockNone)
            {
                return;
            }
            else if (lootmode == LootOption.BlockAll)
            {
                __instance.m_autoPickup = false;
                return;
            }

            var name = __instance.m_itemData.m_shared.m_name;
            if (trash.Contains(name) || name.StartsWith("$item_trophy_"))
            {
                __instance.m_autoPickup = false;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                switch (lootmode)
                {
                    case LootOption.BlockNone:
                        lootmode = LootOption.BlockTrash;
                        break;
                    case LootOption.BlockTrash:
                        lootmode = LootOption.BlockAll;
                        break;
                    case LootOption.BlockAll:
                        lootmode = LootOption.BlockNone;
                        break;
                    default:
                        break;
                }
                Console.instance.Print("Autoloot filter set to " + lootmode);

                foreach (var item in GameObject.FindObjectsOfType<ItemDrop>())
                {
                    switch (lootmode)
                    {
                        case LootOption.BlockNone:
                            item.m_autoPickup = true;
                            break;
                        case LootOption.BlockTrash:
                            var name = item.m_itemData.m_shared.m_name;
                            item.m_autoPickup = !(trash.Contains(name) || name.StartsWith("$item_trophy_"));
                            break;
                        case LootOption.BlockAll:
                            item.m_autoPickup = false;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

    public enum LootOption
    {
        BlockNone,
        BlockTrash,
        BlockAll
    }
}
