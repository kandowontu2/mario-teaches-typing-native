[CmdletBinding()]
param(
    [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $PackageDirectory = Join-Path $repoRoot 'dist\Mario Teaches Typing Native'
}
$package = [IO.Path]::GetFullPath($PackageDirectory)
$application = Join-Path $package 'Mario Teaches Typing Native.exe'
if (-not (Test-Path -LiteralPath $application -PathType Leaf)) {
    throw "Native application is missing: $application"
}

$forbiddenFiles = Get-ChildItem -LiteralPath $package -Recurse -File |
    Where-Object { $_.Name -match '(?i)dosbox|\.chd$|\.vhd$|marcd\.exe$' }
if ($forbiddenFiles) {
    throw "The package contains an external emulator or game-media file: $($forbiddenFiles.FullName -join ', ')"
}

$packageFiles = @(Get-ChildItem -LiteralPath $package -Recurse -File)
if ($packageFiles.Count -ne 1 -or $packageFiles[0].FullName -ne $application) {
    throw "The native package must contain exactly one executable; found $($packageFiles.Count) files."
}
if ((Get-Item -LiteralPath $application).Length -le 150958312L) {
    throw 'The executable is too small to contain both complete source images.'
}

$report = Join-Path ([IO.Path]::GetTempPath()) ('mtt-native-self-test-' + [Guid]::NewGuid().ToString('N') + '.json')
try {
    Write-Host 'Running native resource and lesson-engine self-test...'
    $process = Start-Process -FilePath $application -ArgumentList @('--self-test', $report) -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "Native self-test exited with code $($process.ExitCode)."
    }
    if (-not (Test-Path -LiteralPath $report -PathType Leaf)) {
        throw 'The native self-test did not create its report.'
    }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if (-not $result.success) {
        throw "Native self-test failed: $($result.error)"
    }
    $result.checks | ForEach-Object { Write-Host "  PASS: $_" }
    Write-Host 'Native package test passed: one executable, complete embedded media, no emulator, and no external-media dependency.'
}
finally {
    if (Test-Path -LiteralPath $report) {
        Remove-Item -LiteralPath $report -Force
    }
}
