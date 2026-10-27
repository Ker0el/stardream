param(
    [string]$Output = "publish"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "src/StarDream.App/StarDream.App.csproj"
$out = Join-Path $root $Output

dotnet publish $proj -c Release -r win-x64 --self-contained true -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "published to $out"
