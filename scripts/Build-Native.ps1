[CmdletBinding()]
param(
    [string] $SourceMediaDirectory = '',
    [string] $OutputDirectory,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$repoPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'dist\Mario Teaches Typing Native'
}
$outputFull = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $outputFull.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must be inside the repository: $repoRoot"
}

$media = @(
    [pscustomobject]@{
        Name = 'mario teaches typing.chd'
        Length = 143617768L
        Sha256 = '3481BE69AA99F89A68CCAB4C1C79419A7D85C6A5CD9B696FD266C7A4E5235E08'
    },
    [pscustomobject]@{
        Name = 'mario teaches typing.vhd'
        Length = 7340544L
        Sha256 = '38B00F9533EBAEE12A54B0B01C6DF81004F5B9E436D62F754178A74EB3889967'
    }
)

$embeddedMediaDirectory = Join-Path $repoRoot 'assets\original\media'
New-Item -ItemType Directory -Force -Path $embeddedMediaDirectory | Out-Null
foreach ($item in $media) {
    $destination = Join-Path $embeddedMediaDirectory $item.Name
    $destinationMatches = (Test-Path -LiteralPath $destination -PathType Leaf) -and
        ((Get-Item -LiteralPath $destination).Length -eq $item.Length) -and
        ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $item.Sha256)
    if ($destinationMatches) {
        Write-Host "Using validated embedded media: $($item.Name)"
        continue
    }

    if ([string]::IsNullOrWhiteSpace($SourceMediaDirectory)) {
        throw "Embedded image is missing or invalid and no SourceMediaDirectory was supplied: $destination"
    }
    $sourceFull = [IO.Path]::GetFullPath($SourceMediaDirectory)
    $source = Join-Path $sourceFull $item.Name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required source image not found: $source"
    }
    if ((Get-Item -LiteralPath $source).Length -ne $item.Length) {
        throw "Unexpected source-image length: $source"
    }
    $actualHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    if ($actualHash -ne $item.Sha256) {
        throw "Source-image checksum mismatch for $($item.Name)."
    }

    Write-Host "Importing $($item.Name) for compile-time embedding..."
    $temporaryDestination = $destination + '.importing'
    Copy-Item -LiteralPath $source -Destination $temporaryDestination -Force
    if ((Get-Item -LiteralPath $temporaryDestination).Length -ne $item.Length -or
        (Get-FileHash -LiteralPath $temporaryDestination -Algorithm SHA256).Hash -ne $item.Sha256) {
        Remove-Item -LiteralPath $temporaryDestination -Force
        throw "Imported-image verification failed for $($item.Name)."
    }
    Move-Item -LiteralPath $temporaryDestination -Destination $destination -Force
}

$lessons = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'assets\original\lessons') -Filter '*.LEE' -File
if ($lessons.Count -lt 20) {
    throw "Expected at least 20 imported lesson files; found $($lessons.Count)."
}

$distRoot = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Force -Path $distRoot | Out-Null
$staging = Join-Path $distRoot ('.native-staging-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null

try {
    $project = Join-Path $repoRoot 'src\MarioTypingWindows\MarioTypingWindows.csproj'
    Write-Host 'Publishing the self-contained native Windows application...'
    & dotnet publish $project `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        --output $staging `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:UseSharedCompilation=false `
        -nodeReuse:false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    $application = Join-Path $staging 'Mario Teaches Typing Native.exe'
    if (-not (Test-Path -LiteralPath $application -PathType Leaf)) {
        throw "Published application is missing: $application"
    }

    $unexpectedRuntime = Get-ChildItem -LiteralPath $staging -Recurse -File |
        Where-Object { $_.Name -match '(?i)dosbox|\.chd$|\.vhd$' }
    if ($unexpectedRuntime) {
        throw "External media or compatibility-runtime files were found in the package."
    }

    if (Test-Path -LiteralPath $outputFull) {
        $resolvedOutput = [IO.Path]::GetFullPath($outputFull)
        if ($resolvedOutput -eq $repoRoot -or
            -not $resolvedOutput.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to replace an unsafe output folder: $resolvedOutput"
        }
        Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
    }
    Move-Item -LiteralPath $staging -Destination $outputFull
    Write-Host "Native package created at: $outputFull"
}
finally {
    if (Test-Path -LiteralPath $staging) {
        $resolvedStaging = [IO.Path]::GetFullPath($staging)
        if ($resolvedStaging.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
        }
    }
}
