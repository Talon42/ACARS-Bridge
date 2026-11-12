param([string]$AcarsDirectory = 'C:\Program Files\Delta Virtual\ACARSv3')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$release = Join-Path $root 'release'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$core = Join-Path $AcarsDirectory 'ACARS.dll'
$stock = Join-Path $AcarsDirectory 'A220FSUIPCBridge.bin'
if (!(Test-Path -LiteralPath $stock)) { $stock = Join-Path $AcarsDirectory 'StockFSUIPCBridge.bin' } # Earlier test installation
if (!(Test-Path -LiteralPath $stock)) { $stock = Join-Path $AcarsDirectory 'FSUIPCBridge.dll' }
foreach ($required in @($csc, $core, $stock)) {
    if (!(Test-Path -LiteralPath $required)) { throw "Required local dependency missing: $required" }
}
New-Item -ItemType Directory -Force -Path $build, $release | Out-Null
Copy-Item -LiteralPath $core -Destination (Join-Path $build 'ACARS.dll')
Copy-Item -LiteralPath $stock -Destination (Join-Path $build 'A220FSUIPCBridge.bin')
$oldTemp = $env:TEMP
$oldTmp = $env:TMP
try {
    $env:TEMP = $build
    $env:TMP = $build
    $common = @('/nologo', '/noconfig', '/platform:x64', '/debug-', '/optimize+', "/reference:$core", '/reference:System.Web.Extensions.dll', '/reference:System.dll', '/reference:System.Core.dll')
    $dll = Join-Path $build 'A220CompatibilityBridge.dll'
    $sources = @('Adapter.cs', 'Adapter.Delegation.cs', 'DebuggerTelemetrySource.cs', 'DebugScripts.cs') | ForEach-Object { Join-Path (Join-Path $root 'src') $_ }
    & $csc @common /target:library "/out:$dll" @sources
    if ($LASTEXITCODE -ne 0) { throw 'Bridge compilation failed' }
    $exe = Join-Path $build 'BridgeTests.exe'
    & $csc @common /target:exe "/out:$exe" "/reference:$dll" (Join-Path $root 'tests\RecordingBridge.cs') (Join-Path $root 'tests\BridgeTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Bridge regression tests failed' }
    Copy-Item -LiteralPath $dll -Destination (Join-Path $release 'A220CompatibilityBridge.dll')
    $hash = (Get-FileHash -LiteralPath (Join-Path $release 'A220CompatibilityBridge.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText((Join-Path $release 'SHA256SUMS.txt'), "$hash  A220CompatibilityBridge.dll`n")
    Write-Host 'Build and offline regression tests passed. Release updated.'
} finally {
    $env:TEMP = $oldTemp
    $env:TMP = $oldTmp
}
