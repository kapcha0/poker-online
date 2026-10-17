# Реальная проверка окружения проекта «Poker Онлайн».
# Ничего не устанавливает — только проверяет и печатает факты.
#
# Запуск:
#   powershell -ExecutionPolicy Bypass -File tools/verify-env.ps1
#
# Код выхода 0 — всё найдено, 1 — есть отсутствующие компоненты.

$ErrorActionPreference = "Continue"
$script:missing = 0
$script:rows = @()

function Add-Row($name, $status, $detail) {
    $script:rows += [pscustomobject]@{ Компонент = $name; Статус = $status; Детали = $detail }
    if ($status -ne "OK") { $script:missing++ }
}

function Test-Command($name, $cmd, [scriptblock]$getVersion) {
    $exe = Get-Command $cmd -ErrorAction SilentlyContinue
    if (-not $exe) {
        Add-Row $name "НЕТ" "$cmd не найден в PATH"
        return
    }
    try {
        $v = & $getVersion
        Add-Row $name "OK" ($v -join " ").Trim()
    } catch {
        Add-Row $name "ОШИБКА" $_.Exception.Message
    }
}

Write-Host "Проверка окружения...`n" -ForegroundColor Cyan

# ---------- Базовые инструменты ----------
Test-Command "Git" "git" { (& git --version) }
Test-Command "Node.js" "node" { (& node --version) }
Test-Command "npm" "npm" { (& npm --version) }

$dotnetUser = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
if (Test-Path $dotnetUser) {
    Add-Row ".NET SDK (локальный)" "OK" ((& $dotnetUser --version))
} else {
    Add-Row ".NET SDK (локальный)" "НЕТ" $dotnetUser
}

if (Test-Path "C:\Program Files\dotnet\dotnet.exe") {
    Add-Row ".NET SDK (машинный)" "OK" (((& "C:\Program Files\dotnet\dotnet.exe" --list-sdks) -join "; "))
} else {
    Add-Row ".NET SDK (машинный)" "НЕТ" "C:\Program Files\dotnet\dotnet.exe"
}

# ---------- Unity ----------
$hub = (Get-AppxPackage -Name "*UnityHub*" -ErrorAction SilentlyContinue).InstallLocation
if ($hub) {
    Add-Row "Unity Hub" "OK" ((Get-Item (Join-Path $hub "app\Unity Hub.exe")).VersionInfo.ProductVersion)
} else {
    Add-Row "Unity Hub" "НЕТ" "пакет UnityHub не найден"
}

$unity = "C:\UnityEditors\6000.3.10f1\Editor\Unity.exe"
if (Test-Path $unity) {
    Add-Row "Unity Editor" "OK" ((Get-Item $unity).VersionInfo.ProductVersion)
} else {
    Add-Row "Unity Editor" "НЕТ" $unity
}

$androidPlayer = "C:\UnityEditors\6000.3.10f1\Editor\Data\PlaybackEngines\AndroidPlayer"
if (Test-Path $androidPlayer) {
    $parts = @()
    foreach ($m in @("SDK", "NDK", "OpenJDK")) {
        if (Test-Path (Join-Path $androidPlayer $m)) { $parts += $m }
    }
    Add-Row "Unity: Android-модуль" "OK" ($parts -join ", ")
} else {
    Add-Row "Unity: Android-модуль" "НЕТ" $androidPlayer
}

# ---------- Visual Studio ----------
$vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $path = & $vswhere -products * -format value -property installationPath 2>$null
    $ver = & $vswhere -products * -format value -property installationVersion 2>$null
    if ($path) {
        $unityWl = & $vswhere -products * -requires Microsoft.VisualStudio.Workload.ManagedGame -format value -property installationPath 2>$null
        $netWl = & $vswhere -products * -requires Microsoft.VisualStudio.Workload.ManagedDesktop -format value -property installationPath 2>$null
        Add-Row "Visual Studio 2022" "OK" "$ver ($path)"
        Add-Row "VS: workload Unity" $(if ($unityWl) { "OK" } else { "НЕТ" }) "ManagedGame"
        Add-Row "VS: workload .NET desktop" $(if ($netWl) { "OK" } else { "НЕТ" }) "ManagedDesktop"
    } else {
        Add-Row "Visual Studio 2022" "НЕТ" "vswhere есть, продукт не установлен"
    }
} else {
    Add-Row "Visual Studio 2022" "НЕТ" "vswhere.exe не найден"
}

# ---------- Android ----------
$studio = @(
    "C:\Program Files\Android\Android Studio\bin\studio64.exe",
    "$env:LOCALAPPDATA\Programs\Android Studio\bin\studio64.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($studio) {
    Add-Row "Android Studio" "OK" $studio
} else {
    Add-Row "Android Studio" "НЕТ" "studio64.exe не найден"
}

$adb = Get-Command adb -ErrorAction SilentlyContinue
if ($adb) {
    Add-Row "adb (Android Debug Bridge)" "OK" ((& adb version) | Select-Object -First 1)
} else {
    Add-Row "adb (Android Debug Bridge)" "НЕТ" "adb не в PATH"
}

# ---------- Docker ----------
$docker = Get-Command docker -ErrorAction SilentlyContinue
if ($docker) {
    Add-Row "Docker" "OK" ((& docker --version))
} else {
    Add-Row "Docker" "НЕТ" "docker не в PATH"
}

$wsl = Get-Command wsl -ErrorAction SilentlyContinue
if ($wsl) {
    $distros = (& wsl --list --quiet 2>$null) -join ", "
    Add-Row "WSL" $(if ($distros) { "OK" } else { "ЧАСТИЧНО" }) ($distros -replace "`0", "").Trim()
} else {
    Add-Row "WSL" "НЕТ" "не установлен (нужен для Docker Desktop)"
}

# ---------- GitHub CLI ----------
$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($gh) {
    Add-Row "GitHub CLI" "OK" ((& gh --version) | Select-Object -First 1)
} else {
    Add-Row "GitHub CLI" "НЕТ" "gh не в PATH"
}

# ---------- Отчёт ----------
""
$script:rows | Format-Table -AutoSize

$ok = ($script:rows | Where-Object Статус -eq "OK").Count
$total = $script:rows.Count
""
Write-Host ("Итог: $ok из $total — OK") -ForegroundColor $(if ($script:missing -eq 0) { "Green" } else { "Yellow" })

if ($script:missing -gt 0) { exit 1 } else { exit 0 }
