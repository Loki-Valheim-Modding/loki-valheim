<p align="center">
   <img src="https://raw.githubusercontent.com/Loki-Valheim-Modding/loki-valheim/development/logo.png"/>
</p>

Download the specific mod you want [here](https://github.com/Loki-Valheim-Modding/loki-valheim/releases/tag/v1.0.0).


## Build status

| GitHubActions Builds |
|:--------------------:|
| [![GitHubActions Builds](https://github.com/Loki-Valheim-Modding/loki-valheim/workflows/GitHubActionsBuilds/badge.svg)](https://github.com/Loki-Valheim-Modding/loki-valheim/actions/workflows/githubactionsbuilds.yml) |

## Use

- Navigate to the Valheim game directory
   - Rightclick the game in Steam -> manage -> browse local files to get the `Valheim` game directory
- Grab BepInEx X64 (5.4.5) & extract to game directory: https://github.com/BepInEx/BepInEx/releases/download/v5.4.5/BepInEx_x64_5.4.5.0.zip
   - [CHECK] Make sure the folder structure is `Valheim\BepInEx\core` and the Valheim folder has the file `winhttp.dll`.
- Grab & unpack into `Valheim\valheim_Data\Managed` (confirm to overwrite): https://cdn.discordapp.com/attachments/623910091132895232/809851661975420989/unstripped_managed.7z
- Create a folder `plugins` so you end up with `Valheim\BepInEx\plugins`
- Download the mod DLLs you want from the [releases section](https://github.com/Loki-Valheim-Modding/loki-valheim/releases/tag/v1.0.0).
- Copy the downloaded mod DLLs to `BepInEx\plugins`
- Run the game!
  - [OPTIONAL]: After running the game at least once, navigate to `BepInEx\config` to edit mod configurations by opening them with a text editor, then run the game again!

## Current mods

Please note that client mods should work on online servers as well, so respect the server rules and don't ruin the game for your friends.
Note that the mod configuration files (`BepInEx\config`) contain various options such as hotkeys to tweak to your hearts content!

- **First Person mode:** Allows you to play the game in first person. 
  - Multiple First-Person modes! Press H to cycle between: third person, first person without helmet and first person with helmet. 
  - What-You-See-Is-What-You-Hit (WYSIWYH): Instead of hitting things directly in front of you, the attack is now angled to where you are looking!
- **Immersion mode:** Aims to make the game more immersive
  - Removes the map and minimap! Press Ctrl+O to toggle.
  - By default, allow simple wooden poles & signs to be built without a nearby workbench. (Which you'll need without a minimap!)
  - Allow users to change and set what pieces can be built without a nearby workbench (see config file).
  - Hides health bar of enemies and enemy bosses.
- **Autopickup filter:** Cycles between 3 modes when pressing L (accept all, block trash, block all) and has a customizable trash list in the config tailored for the endgame. Items won't be picked up automatically if blocked, but can still be picked up manually.
- **Death announcer:** When a player dies, it is announced to every other player
- **Repair items anywhere:** Press U to repair everything anywhere
- **Stamina mod:** Allows you to configure stamina regeneration and the usage modifier


## Contribute 
If you want to contribute mods / help, see the [wiki](https://github.com/Loki-Valheim-Modding/loki-valheim/wiki/Develop-a-Mod)
