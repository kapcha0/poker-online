<#
  Проверка компиляции клиентских скриптов Unity БЕЗ запуска редактора.

  Зачем это нужно: любой запуск Unity.exe требует активированной лицензии
  (иначе код 198), а импорт проекта занимает минуты. Этот скрипт компилирует
  Assets\Scripts и Assets\Editor тем же компилятором (Roslyn) и против тех же
  сборок UnityEngine/UnityEditor, что использует сам редактор, поэтому ловит
  ошибки в API (неверные имена методов/свойств) за секунды и без лицензии.

  Пример:
    powershell -File tools/check-client-compiles.ps1
    powershell -File tools/check-client-compiles.ps1 -UnityEditor "D:\Unity\6000.3.10f1"
#>
param(
    [string]$UnityEditor = "C:\UnityEditors\6000.3.10f1",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

function Fail([string]$message) {
    Write-Host "[check-client] ОШИБКА: $message" -ForegroundColor Red
    exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$project  = Join-Path $repoRoot "client\PokerOnline"
$managed  = Join-Path $UnityEditor "Editor\Data\Managed"

if (-not (Test-Path $project)) { Fail "не найден проект клиента: $project" }
if (-not (Test-Path $managed)) { Fail "не найдены сборки Unity: $managed (укажите -UnityEditor)" }

# Локальный .NET SDK 8 из профиля пользователя, иначе системный.
$dotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

$work = Join-Path $env:TEMP "poker-clientcheck"
Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $work | Out-Null

# Собираем ссылки на все сборки UnityEngine/UnityEditor редактора.
$dlls = @()
$dlls += Get-ChildItem (Join-Path $managed "UnityEngine") -File -Filter *.dll
$dlls += Get-ChildItem $managed -File -Filter "UnityEditor*.dll"
$dlls += Get-ChildItem $managed -File -Filter "UnityEngine*.dll"

$byName = @{}
foreach ($dll in $dlls) {
    if (-not $byName.ContainsKey($dll.Name)) { $byName[$dll.Name] = $dll.FullName }
}
if ($byName.Count -eq 0) { Fail "в $managed не найдено ни одной сборки Unity" }

Write-Host "[check-client] Сборок Unity: $($byName.Count)"

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<Project Sdk="Microsoft.NET.Sdk">')
[void]$sb.AppendLine('  <PropertyGroup>')
[void]$sb.AppendLine('    <TargetFramework>netstandard2.1</TargetFramework>')
[void]$sb.AppendLine('    <LangVersion>9.0</LangVersion>')
[void]$sb.AppendLine('    <Nullable>disable</Nullable>')
[void]$sb.AppendLine('    <ImplicitUsings>disable</ImplicitUsings>')
[void]$sb.AppendLine('    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>')
[void]$sb.AppendLine('    <AssemblyName>ClientCompileCheck</AssemblyName>')
[void]$sb.AppendLine('    <NoWarn>CS8632;CS0169;CS0649;CS0414;CS0067;CS0108;CS1591</NoWarn>')
[void]$sb.AppendLine('  </PropertyGroup>')
[void]$sb.AppendLine('  <ItemGroup>')
[void]$sb.AppendLine(('    <Compile Include="{0}\Assets\Scripts\*.cs" />' -f $project))
[void]$sb.AppendLine(('    <Compile Include="{0}\Assets\Editor\*.cs" />' -f $project))
[void]$sb.AppendLine('  </ItemGroup>')
[void]$sb.AppendLine('  <ItemGroup>')
[void]$sb.AppendLine(('    <Reference Include="PokerCore"><HintPath>{0}\Assets\Plugins\PokerCore\PokerCore.dll</HintPath><Private>false</Private></Reference>' -f $project))
[void]$sb.AppendLine('  </ItemGroup>')
[void]$sb.AppendLine('  <ItemGroup>')
foreach ($name in ($byName.Keys | Sort-Object)) {
    $include = [IO.Path]::GetFileNameWithoutExtension($name)
    [void]$sb.AppendLine(('    <Reference Include="{0}"><HintPath>{1}</HintPath><Private>false</Private></Reference>' -f $include, $byName[$name]))
}
[void]$sb.AppendLine('  </ItemGroup>')
[void]$sb.AppendLine('</Project>')

[IO.File]::WriteAllText((Join-Path $work "clientcheck.csproj"), $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))

Write-Host "[check-client] Компиляция клиентских скриптов..."
$output = & $dotnet build (Join-Path $work "clientcheck.csproj") -c $Configuration --nologo -v minimal 2>&1
$code = $LASTEXITCODE
$output | Where-Object { $_ -match 'error|Error|Ошибок|Предупреждений|clientcheck ->' } | ForEach-Object { Write-Host "  $_" }

if ($code -ne 0) { Fail "клиентские скрипты не компилируются (код $code)" }

Write-Host "[check-client] ГОТОВО: клиентские скрипты компилируются без ошибок" -ForegroundColor Green
exit 0
