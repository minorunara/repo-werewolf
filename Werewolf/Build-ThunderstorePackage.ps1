param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("Stable", "Beta")]
    [string]$Channel = "Stable"
)

$ErrorActionPreference = "Stop"

$projectDirectory = $PSScriptRoot
$projectPath = Join-Path $projectDirectory "Werewolf.csproj"
$stableMetadataDirectory = Join-Path $projectDirectory "thunderstore"
$metadataDirectory = if ($Channel -eq "Beta") {
    Join-Path $projectDirectory "thunderstore-beta"
} else {
    $stableMetadataDirectory
}
$manifestPath = Join-Path $metadataDirectory "manifest.json"
$outputDirectory = Join-Path $projectDirectory "out"
$iconPath = Join-Path $metadataDirectory "icon.png"
$replayViewerPath = Join-Path (Split-Path -Parent $projectDirectory) "tools\replay-viewer.html"

if (-not (Test-Path -LiteralPath $replayViewerPath -PathType Leaf)) {
    throw "Replay viewer was not found: $replayViewerPath"
}

if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
    throw "Thumbnail was not found: $iconPath"
}
Add-Type -AssemblyName System.Drawing
$icon = [Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "Thunderstore icon must be 256x256: $iconPath is $($icon.Width)x$($icon.Height)"
    }
}
finally {
    $icon.Dispose()
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$packageName = [string]$manifest.name
if ($packageName -notmatch '^[A-Za-z0-9_]+$') {
    throw "manifest.json name contains characters that Thunderstore does not allow: $packageName"
}

$version = [string]$manifest.version_number
if ($version -notmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$') {
    throw "manifest.json version_number is not a Thunderstore-compatible version: $version"
}

$changelogPath = Join-Path $metadataDirectory "CHANGELOG.md"
$changelogSource = Get-Content -LiteralPath $changelogPath -Raw -Encoding UTF8
if ($changelogSource -notmatch "(?m)^##\s+$([regex]::Escape($version))\s*$") {
    throw "CHANGELOG.md has no section for version $version. Add '## $version' with release notes before packaging."
}

$channelSlug = $Channel.ToLowerInvariant()
$stageDirectory = Join-Path $outputDirectory "thunderstore-$channelSlug-stage-$version"
$zipPath = Join-Path $outputDirectory "$packageName-$version.zip"
$pluginFolderName = if ($Channel -eq "Beta") { "Minorunara_Werewolf_BETA" } else { "Minorunara_Werewolf" }
$pluginDirectory = Join-Path $stageDirectory "BepInEx\plugins\$pluginFolderName"
$assemblyPath = Join-Path $outputDirectory "Minorunara_Werewolf.dll"

dotnet build $projectPath -c $Configuration --nologo -p:SkipDeploy=true -p:WerewolfVersion=$version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$expectedAssemblyVersion = "$version.0"
$actualAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($assemblyPath).Version.ToString()
if ($actualAssemblyVersion -ne $expectedAssemblyVersion) {
    throw "Version mismatch after build: manifest=$version, assembly=$actualAssemblyVersion"
}

if (Test-Path -LiteralPath $stageDirectory) {
    Remove-Item -LiteralPath $stageDirectory -Recurse -Force
}
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
foreach ($fileName in @("manifest.json", "CHANGELOG.md")) {
    Copy-Item -LiteralPath (Join-Path $metadataDirectory $fileName) -Destination $stageDirectory
}
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $stageDirectory "icon.png")
Copy-Item -LiteralPath (Join-Path $stableMetadataDirectory "github\LICENSE") -Destination (Join-Path $stageDirectory "LICENSE")
# 埋め込みNoto Emoji（OFL 1.1）は「受領者へライセンス本文の写しを渡す」ためzip直下へ同梱する
Copy-Item -LiteralPath (Join-Path $stableMetadataDirectory "github\LICENSE-NotoEmoji.txt") -Destination (Join-Path $stageDirectory "LICENSE-NotoEmoji.txt")

$readmeSource = Get-Content -LiteralPath (Join-Path $metadataDirectory "README.md") -Raw -Encoding UTF8
if ($Channel -eq "Beta") {
    $stableReadmeSource = Get-Content -LiteralPath (Join-Path $stableMetadataDirectory "README.md") -Raw -Encoding UTF8
    $readmeSource = $readmeSource.TrimEnd() + "`n`n---`n`n" + $stableReadmeSource.TrimStart()
}
if ($readmeSource.Length -gt 32768) {
    throw "README.md exceeds Thunderstore's 32,768-character limit: $($readmeSource.Length)"
}
$utf8NoBom = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $stageDirectory "README.md"), $readmeSource, $utf8NoBom)

Copy-Item -LiteralPath $assemblyPath -Destination $pluginDirectory
# 外部リプレイビューアはDLLと同じフォルダへ置く（プロファイル配下へ展開され、保存先の
# フォールバック Replays\ と同居する。単一HTMLで依存なし＝ブラウザで直接開ける）
Copy-Item -LiteralPath $replayViewerPath -Destination $pluginDirectory

Compress-Archive -Path (Join-Path $stageDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $requiredEntries = @(
        "manifest.json",
        "README.md",
        "CHANGELOG.md",
        "icon.png",
        "LICENSE",
        "LICENSE-NotoEmoji.txt",
        "BepInEx/plugins/$pluginFolderName/Minorunara_Werewolf.dll",
        "BepInEx/plugins/$pluginFolderName/replay-viewer.html"
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
