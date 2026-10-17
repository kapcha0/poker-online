# Собирает PokerCore в DLL (netstandard2.1) и кладёт её в Plugins Unity-клиента.
# Использование:  powershell -ExecutionPolicy Bypass -File tools/build-pokercore.ps1
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $repoRoot "shared/PokerCore/PokerCore.csproj"
$outDir = Join-Path $repoRoot "client/PokerOnline/Assets/Plugins/PokerCore"

# Ищем dotnet: сначала локальный SDK в профиле, затем машинный.
$dotnet = $null
foreach ($candidate in @(
        "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe",
        "C:\Program Files\dotnet\dotnet.exe")) {
    if (Test-Path $candidate) { $dotnet = $candidate; break }
}
if (-not $dotnet) {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { $dotnet = $cmd.Source }
}
if (-not $dotnet) { throw "dotnet SDK не найден — установите .NET SDK 8" }

Write-Host "[Poker] Компилятор: $dotnet"
Write-Host "[Poker] Сборка PokerCore ($Configuration)..."
& $dotnet build $proj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed: $LASTEXITCODE" }

$dll = Join-Path $repoRoot "shared/PokerCore/bin/$Configuration/netstandard2.1/PokerCore.dll"
if (-not (Test-Path $dll)) { throw "DLL не найдена: $dll" }

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Copy-Item $dll (Join-Path $outDir "PokerCore.dll") -Force
Write-Host "[Poker] Готово: $outDir/PokerCore.dll"
