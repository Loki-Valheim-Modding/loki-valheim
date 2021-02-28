param (
    [Parameter(Mandatory=$true)]
    [string] $desiredVersion
)

if (!($desiredVersion -match "(\d+\.\d+\.\d+\.\d+)")) {
    throw "desiredVersion parameter ($desiredVersion) needs to be in format 1.0.0.0"
}

$startDir = Join-Path $PSScriptRoot ".."

$csharpFiles = Get-ChildItem -Path $startDir -Recurse -Include *.cs

$regexString = '\[BepInPlugin\(\"com\.loki\.clientmods\.valheim\..+\"\, \".+\"\, \"(\d+\.\d+\.\d+\.\d+)\"\)\]'

$replaceCount = 0

foreach ($csharpFile in $csharpFiles) {
    $content = Get-Content $csharpFile | Out-String

    if ($content -match $regexString) {
        $m1 = $Matches[0]
        $m2 = $Matches[1]

        $result = $m1.Replace($m2, $desiredVersion)
        if ($m1 -ne $result) {
            $newContent = $content.Replace($m1, $result)

            Set-Content -Path $csharpFile -Value $newContent

            Write-Host "Replacing content of file: $csharpFile"
            Write-Host $newContent
            Write-Host ""
            $replaceCount++
        } else {
            Write-Host "Skipping $csharpFile because the expected version number was already in this file."
        }
    }
}

Write-Host "Version script completed. Version numbers replaced: $replaceCount"