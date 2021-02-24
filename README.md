<p align="center">
   <img src="https://raw.githubusercontent.com/Loki-Valheim-Modding/loki-valheim/development/logo.png"/>
</p>

Works for Valheim 0.145.6 (2021-02-16).

Download the specific mod you want [here](https://github.com/Loki-Valheim-Modding/loki-valheim/releases/tag/v1.0.0).

## Use

- Navigate to the Valheim game directory
   - Rightclick the game in Steam -> manage -> browse local files to get the `Valheim` game directory
- Grab BepInEx X64 (5.4.5) & extract to game directory: https://github.com/BepInEx/BepInEx/releases/download/v5.4.5/BepInEx_x64_5.4.5.0.zip
   - Make sure the folder structure is `Valheim\BepInEx\core` and that the 3 loose files are directly inside `Valheim`
- Grab & unpack into `Valheim\valheim_Data\Managed` (confirm to overwrite): https://cdn.discordapp.com/attachments/623910091132895232/809851661975420989/unstripped_managed.7z
   - Necessary because Valheim strips Unity assemblies: https://github.com/NeighTools/UnityDoorstop/issues/10
- Create the folder plugins so you end up with `Valheim\BepInEx\plugins`
- Download any mod DLLs you want from the releases section
  - Each DLL can be used standalone, you don't need all of them.
- Copy the mod DLLs that you downloaded into `BepInEx\plugins`
- Run the game!
  - [OPTIONAL]: After running the game at least once, navigate to `BepInEx\config` to edit mod configurations by opening them with a text editor, then run the game again!

## Current mods

Please note that client mods should work on online servers as well, so respect the server rules and don't ruin the game for your friends.
Note that the mod configuration files (`BepInEx\config`) contain various options such as hotkeys to tweak to your hearts content!

- First Person mode: Allows you to play the game in first person. 
  - Multiple First-Person modes! Press H to cycle between: third person, first person without helmet and first person with helmet. 
  - What-You-See-Is-What-You-Hit (WYSIWYH): Instead of hitting things directly in front of you, the attack is now angled to where you are looking!
- Immersion mode: Aims to make the game more immersive
  - Removes the map and minimap! Press Ctrl+O to toggle.
  - By default, allow simple wooden poles & signs to be built without a nearby workbench. (Which you'll need without a minimap!)
  - Allow users to change and set what pieces can be built without a nearby workbench (see config file).
  - Hides health bar of enemies and enemy bosses.
- Autopickup filter: Cycles between 3 modes when pressing L (accept all, block trash, block all) and has a customizable trash list in the config tailored for the endgame. Items won't be picked up automatically if blocked, but can still be picked up manually.
- Death announcer: When a player dies, it is announced to every other player
- Repair items anywhere: Press U to repair everything anywhere
- Stamina mod: Allows you to configure stamina regeneration and the usage modifier

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
