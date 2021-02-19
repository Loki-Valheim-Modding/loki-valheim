
## Use

- Grab and extract to game directory: https://mega.nz/file/0UAlxQwK#47InGOb8ViI6GyBDArpbhkbMTBklXdyRSmAc4-BZpJY
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
