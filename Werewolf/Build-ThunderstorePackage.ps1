param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectDirectory = $PSScriptRoot
$projectPath = Join-Path $projectDirectory "Werewolf.csproj"
$metadataDirectory = Join-Path $projectDirectory "thunderstore"
$manifestPath = Join-Path $metadataDirectory "manifest.json"
$pluginSourcePath = Join-Path $projectDirectory "Plugin.cs"
$outputDirectory = Join-Path $projectDirectory "out"

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$version = [string]$manifest.version_number
if ($version -notmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$') {
    throw "manifest.json version_number is not a Thunderstore-compatible version: $version"
}

$pluginSource = Get-Content -LiteralPath $pluginSourcePath -Raw -Encoding UTF8
$pluginVersionMatch = [regex]::Match(
    $pluginSource,
    'PluginVersion\s*=\s*"(?<version>[^"]+)"')
if (-not $pluginVersionMatch.Success) {
    throw "PluginVersion was not found in Plugin.cs."
}
if ($pluginVersionMatch.Groups['version'].Value -ne $version) {
    throw "Version mismatch: manifest=$version, Plugin.cs=$($pluginVersionMatch.Groups['version'].Value)"
}

$projectSource = Get-Content -LiteralPath $projectPath -Raw -Encoding UTF8
$projectVersionMatch = [regex]::Match(
    $projectSource,
    '<Version>(?<version>[^<]+)</Version>')
if (-not $projectVersionMatch.Success) {
    throw "Version was not found in Werewolf.csproj."
}
if ($projectVersionMatch.Groups['version'].Value -ne $version) {
    throw "Version mismatch: manifest=$version, Werewolf.csproj=$($projectVersionMatch.Groups['version'].Value)"
}

$stageDirectory = Join-Path $outputDirectory "thunderstore-stage-$version"
$zipPath = Join-Path $outputDirectory "REPO_Werewolf-$version.zip"
$pluginDirectory = Join-Path $stageDirectory "BepInEx\plugins\Minorunara_Werewolf"
$assemblyPath = Join-Path $outputDirectory "Minorunara_Werewolf.dll"

dotnet build $projectPath -c $Configuration --nologo -p:SkipDeploy=true
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

if (Test-Path -LiteralPath $stageDirectory) {
    Remove-Item -LiteralPath $stageDirectory -Recurse -Force
}
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
foreach ($fileName in @("manifest.json", "README.md", "icon.png")) {
    Copy-Item -LiteralPath (Join-Path $metadataDirectory $fileName) -Destination $stageDirectory
}
Copy-Item -LiteralPath $assemblyPath -Destination $pluginDirectory

Compress-Archive -Path (Join-Path $stageDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $requiredEntries = @(
        "manifest.json",
        "README.md",
        "icon.png",
        "BepInEx/plugins/Minorunara_Werewolf/Minorunara_Werewolf.dll"
    )
    foreach ($requiredEntry in $requiredEntries) {
        if ($requiredEntry -notin $entries) {
            throw "Package validation failed. Missing entry: $requiredEntry"
        }
    }
}
finally {
    $archive.Dispose()
}

Remove-Item -LiteralPath $stageDirectory -Recurse -Force
Write-Output "Created Thunderstore package: $zipPath"
