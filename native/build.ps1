param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $here "StarDream.FbxNative"
$output = Join-Path $here "bin/x64"
New-Item -ItemType Directory -Force $output | Out-Null

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw "vswhere not found; Visual Studio with the C++ workload is required." }

$vsPath = & $vswhere -latest -products * -property installationPath
if (-not $vsPath) { throw "No Visual Studio installation found." }

$vcvars = Join-Path $vsPath "VC\Auxiliary\Build\vcvars64.bat"
if (-not (Test-Path $vcvars)) { throw "vcvars64.bat not found under $vsPath" }

$command = "cd /d `"$source`" && `"$vcvars`" >nul && cl /nologo /LD /O2 /DNDEBUG /W0 /Dufbxw_abi=__declspec(dllexport) /Dufbxw_abi_data=__declspec(dllexport) ufbx_write.c /Fe:`"$output\StarDream.FbxNative.dll`" /Fo:`"$output\\`" /link /IMPLIB:`"$output\StarDream.FbxNative.lib`""
cmd /c $command
if ($LASTEXITCODE -ne 0) { throw "cl failed with exit code $LASTEXITCODE" }

$dll = Join-Path $output "StarDream.FbxNative.dll"
if (-not (Test-Path $dll)) { throw "Build produced no DLL." }
Write-Host "built $dll"
