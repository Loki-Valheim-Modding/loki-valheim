using BepInEx;

namespace ExampleValheimMod
{
    
    [BepInPlugin("com.freecode.mods.valheim.deathannouncermod", "Death Announcer Mod", "1.0.0.0")]
    public class DeathAnnouncerValheimMod : BaseUnityPlugin {

        void Awake(){
            UnityEngine.Debug.Log("Hello, world!");
        }
        
        
    }
    
}