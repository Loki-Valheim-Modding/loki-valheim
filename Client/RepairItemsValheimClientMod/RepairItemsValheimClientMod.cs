using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Loki.Mods {
    
    [BepInPlugin("com.loki.clientmods.valheim.repairitems", "Repair Items Mod", "1.0.0.0")]
    public class RepairItemsValheimClientMod : BaseUnityPlugin {
        
        private static ConfigEntry<KeyboardShortcut> _hotkey;
        
        void Awake() {
            _hotkey = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.U));
        }
        
        void Update() {
            var localPlayer = Player.m_localPlayer;

            if (localPlayer == null) return;

            if (!_hotkey.Value.IsDown()) return;
            
            foreach (var itemData in localPlayer.GetInventory().GetAllItems()) {
                itemData.m_durability = itemData.GetMaxDurability();
            }
        }

    }
    
}