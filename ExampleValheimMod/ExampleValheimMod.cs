using BepInEx;

namespace ExampleValheimMod
{
   
    [BepInPlugin("com.freecode.mods.valheim.examplemod", "Example Mod", "1.0.0.0")]
    public class ExampleValheimMod : BaseUnityPlugin {

        void Awake(){
            UnityEngine.Debug.Log("Hello, world!");
        }
        
        
    }
}