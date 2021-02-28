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

foreach ($csharpFile in $csharpFiles) {
    $content = Get-Content $csharpFile | Out-String

    if ($content -match $regexString) {
        Write-Host "NU"
        $m1 = $Matches[0]
        $m2 = $Matches[1]

        $result = $m1.Replace($m2, $desiredVersion)
        $newContent = $content.Replace($m1, $result)

        Set-Content -Path $csharpFile -Value $newContent

        Write-Host $newContent
    }
}