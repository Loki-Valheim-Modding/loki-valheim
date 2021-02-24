if (!(Test-Path "Build" -PathType Container)) {
    New-Item "Build" -ItemType "directory"
}

if (!(Test-Path "Build/SteamCMD" -PathType Container)) {
    New-Item "Build/SteamCMD" -ItemType "directory"
}

if (!(Test-Path "Build/SteamCMD/steamcmd.exe" -PathType Leaf)) {
    Write-Host "Downloading SteamCMD"
    Invoke-WebRequest -Uri "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" -OutFile "Build/SteamCMD/steamcmd.zip"
    Expand-Archive -LiteralPath "Build/SteamCMD/steamcmd.zip" -DestinationPath "Build/SteamCMD/" -Force
}

#Game
#$gameId = "892970"

#Server
$gameId = "896660"

& "Build/SteamCMD/steamcmd.exe" +login anonymous +force_install_dir "../ValheimServer" +app_update $gameId validate +exit


if (!(Test-Path "Build/Libs" -PathType Container)) {
    New-Item "Build/Libs" -ItemType "directory"
}

Copy-Item "Build/ValheimServer/valheim_server_Data/Managed/*.dll" "Build/Libs"

Invoke-WebRequest -Uri "https://cdn.discordapp.com/attachments/623910091132895232/809851661975420989/unstripped_managed.7z" -OutFile "Build/unstripped_managed.7z"
& 7z e -y "Build/unstripped_managed.7z" -oBuild/Libs

Invoke-WebRequest -Uri "https://github.com/BepInEx/BepInEx/releases/download/v5.4.5/BepInEx_x64_5.4.5.0.zip" -OutFile "Build/BepInEx_x64.zip"
Expand-Archive -LiteralPath "Build/BepInEx_x64.zip" -DestinationPath "Build/Libs/" -Force


if (!(Test-Path "Libs" -PathType Container)) {
    New-Item "Libs" -ItemType "directory"
    Copy-Item "Build/Libs/*.dll" "Libs"
}
