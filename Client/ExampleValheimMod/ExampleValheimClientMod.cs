using BepInEx;

namespace Loki.Mods
{
   
    [BepInPlugin("com.loki.clientmods.valheim.example", "Example Mod", "1.0.0.0")]
    public class ExampleValheimMod : BaseUnityPlugin {

        void Awake(){
            UnityEngine.Debug.Log("Hello, world!");
        }

    }
}