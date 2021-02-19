
## Use

- Grab BepInEx X64 (5.4.5) & extract to game directory: https://github.com/BepInEx/BepInEx/releases/download/v5.4.5/BepInEx_x64_5.4.5.0.zip
- Grab & unpack into `<VALHEIM_DIR>\valheim_Data\Managed` (confirm to overwrite): https://cdn.discordapp.com/attachments/623910091132895232/809851661975420989/unstripped_managed.7z
   - Necessary because Valheim strips Unity assemblies: https://github.com/NeighTools/UnityDoorstop/issues/10
- Copy any mod DLLs to `BepInEx\plugins`
- Run the game!

## Developers

Start by following the plugin setup for Bepinex:
- https://bepinex.github.io/bepinex_docs/master/articles/dev_guide/plugin_tutorial/index.html

Valheim seems to be using mscorlib.dll 4.6.57.0 (.Net Framework 4.6)

Follow the DLL lib copying instructions in the above "Getting Started", and copy them all to `Libs\`
- All `assembly_*.dll` files from `<VALHEIM_DIR>\valheim_Data\Managed`
- UnityEngine.dll (and UnityEngine.CoreModule.dll if needed): `<VALHEIM_DIR>\valheim_Data\Managed`
- BepInEx & Harmony DLLs (see above link): 0Harmony.dll, BepInEx.Harmony.dll, BepInEx.dll

### Starting a new mod

Read: https://bepinex.github.io/bepinex_docs/master/articles/dev_guide/plugin_tutorial/2_plugin_start.html
(I.e. create a new project, reference everything under `Libs\`, and start with BaseUnityPlugin + [BepInPlugin]`. See ExampleValheimMod.)
(or use this template: https://github.com/BepInEx/BepInEx.PluginTemplate)

Reference work:
- HarmonyX: https://bepinex.github.io/bepinex_docs/master/articles/dev_guide/runtime_patching.html
- General approach to finding out what to patch, etc: https://bepinex.github.io/bepinex_docs/master/articles/dev_guide/dev_tools.html
