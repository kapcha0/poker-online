# SETUP.md — рабочее окружение проекта «Poker Онлайн»

Документ фиксирует всё, что установлено и настроено на рабочем ПК для разработки.
Обновляется по мере установки инструментов.

**ОС:** Windows  
**Дата настройки:** 2026-10-16 (этап 0), обновлено 2026-10-17  
**Путь проекта:** `C:\Projects\poker-online` (вне OneDrive, без кириллицы)

---

## Чек-лист окружения

| # | Инструмент | Версия | Путь | Статус |
|---|------------|--------|------|--------|
| 1 | Git | 2.55.0.windows.5 | `C:\Program Files\Git` | ✅ установлен и настроен |
| 2 | Node.js (LTS) | 24.20.0 | `C:\Program Files\nodejs\node.exe` | ✅ установлен |
| 3 | npm | 11.19.0 | `C:\Program Files\nodejs\npm` | ✅ работает |
| 4 | Unity Hub | 3.22.3.65535 | `C:\Program Files\WindowsApps\UnityTechnologies.UnityHub_3.22.3.65535_x64__2vrhnee42bhxm\app\Unity Hub.exe` | ✅ установлен (msix) |
| 5 | Unity Editor (LTS) | 6000.3.10f1 (Unity 6.3 LTS), changeset `e35f0c77bd8e` | `C:\UnityEditors\6000.3.10f1\Editor\Unity.exe` | ✅ установлен, запускается |
| 5a | — модуль Android Build Support | | `...\PlaybackEngines\AndroidPlayer\` | ✅ `AndroidPlayerBuildProgram.exe` на месте |
| 5b | — Android SDK 36 (platforms 34/35/36, build-tools 36.0.0, platform-tools, cmake 3.22.1) | | `...\AndroidPlayer\SDK` | ✅ собрано вручную из кэша Hub |
| 5c | — Android NDK | r27c (27.2.12479018) | `...\AndroidPlayer\NDK` | ✅ |
| 5d | — OpenJDK | 17.0.9 | `...\AndroidPlayer\OpenJDK` | ✅ |
| 5e | — лицензия Unity Personal | | — | ❌ **требуется вход в Unity Hub; без неё любая сборка падает с кодом 198** |
| 6 | Visual Studio 2022 Community | 17.14.37806.7 | `C:\Program Files\Microsoft Visual Studio\2022\Community` | ✅ установлен (workloads: Unity + .NET desktop) |
| 6a | — workload «Game development with Unity» (`ManagedGame`) | | | ✅ подтверждён через `vswhere -requires` |
| 6b | — workload «.NET desktop development» (`ManagedDesktop`) | | | ✅ подтверждён через `vswhere -requires` |
| 6c | — `devenv.exe` | | `...\Common7\IDE\devenv.exe` | ✅ |
| 6d | .NET SDK (от VS) | 9.0.319 | `C:\Program Files\dotnet\dotnet.exe` | ✅ |
| 6e | .NET SDK (локальный, для сборки ядра) | 8.0.426 | `%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe` | ✅ |
| 7 | Android Studio | 2026.2.1.8 | `C:\Program Files\Android\Android Studio\bin\studio64.exe` | ✅ установлен (winget) |
| 8 | Docker Desktop | | | ❌ не установлен (нужен WSL2) |
| 9 | PostgreSQL + Redis | | (через Docker, `docker-compose.yml` готов) | ❌ отложено до серверного этапа |

Легенда: ✅ готово · ⏳ в процессе · ❌ ещё не сделано

### Проверено работоспособным

| Проверка | Результат |
|---|---|
| `dotnet build shared/PokerCore/PokerCore.csproj` | ✅ 0 ошибок, 0 предупреждений |
| `dotnet test shared/PokerCore.Tests` | ✅ 27 из 27 тестов (24 + 3 property-based) |
| `dotnet run --project tools/ClientSim` | ✅ 8 674 раздачи, 0 нелегальных действий бота, 0 зависаний |
| Android SDK, собранный вручную | ✅ 11 из 11 компонентов, 9.42 ГБ, 45 948 файлов |
| Редактор Unity запускается | ✅ `ProductVersion 6000.3.10f1_e35f0c77bd8e` |
| Сборка через Unity `-batchmode` | ❌ код 198 — `No valid Unity Editor license found` |
| CI на GitHub Actions (`develop`) | ✅ зелёный, сборка + тесты + симуляция бота |
| `gh repo` → `kapcha0/poker-online` | ✅ публичное (`visibility: public`, анонимный запрос → `200`) |

---

## Выполненные настройки

### Git
```
git config --global core.autocrlf true
git config --global init.defaultBranch main
git config --global user.name "kapcha0"
git config --global user.email "nepomnuooo@gmail.com"
```

### Node.js / PowerShell
Исправлена политика выполнения скриптов PowerShell для пользователя, чтобы работал `npm`:
```
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned -Force
```

### Unity Hub
Устанавливался как `.msix`-пакет (per-user, без обязательных прав администратора):
```
Add-AppxPackage -Path "UnityHubSetup-3.22.3-x64.msix"
```
Установщик скачан с `https://public-cdn.cloud.unity3d.com/hub/prod/3.22.3/UnityHubSetup-3.22.3-x64.msix`
(SHA-256 проверен).

### Unity Editor
Установка через CLI Unity Hub (headless) **не удалась**: на этом канале загрузка
дважды обрывалась (`getaddrinfo ENOTFOUND download.unity3d.com`,
`ERR_NETWORK_CHANGED`, `Error while downloading. Request timeout.`), причём Hub
начинает каждую повторную попытку с нуля (в кэше видны файлы вида `...(7).exe`).

Рабочий способ — скачать установщик напрямую с CDN и запустить NSIS-установщик
(докачка через `curl -C -`, подпись установщика проверена: Authenticode Valid,
`CN=Unity Technologies SF`):
```
curl.exe -C - -L --retry 5 --retry-all-errors -o UnitySetup64-6000.3.10f1.exe <CDN-URL>
UnitySetup64-6000.3.10f1.exe /S /D=C:\UnityEditors\6000.3.10f1
```
(В NSIS-установщике `/D` должен быть последним аргументом и без кавычек.)

Модуль Android Build Support ставится так же, отдельным установщиком
`UnitySetup-Android-Support-6000.3.10f1.exe /S /D=<тот же путь>`.

### Android SDK / NDK / OpenJDK
`Autoinstall`-модули Hub поставить **не смог**: редактор установлен не через Hub,
и команда отвечает
`No modules found for this editor. Module installation is only supported for editors installed with Unity Hub.`

Поэтому компоненты собраны вручную из уже скачанного кэша Hub
(`%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_*\LocalCache\Roaming\UnityHub\downloads`).
Все архивы перед распаковкой проверены `7z t` (архив `commandlinetools` пришлось
докачать — он был оборван). Раскладка:

| Архив | Куда распакован |
|---|---|
| `platform-tools_r36.0.0-win.zip` | `AndroidPlayer\SDK\platform-tools` |
| `build-tools_r36_windows.zip` (внутри `android-16`) | `AndroidPlayer\SDK\build-tools\36.0.0` (папка переименована) |
| `platform-34/35/36_*.zip` | `AndroidPlayer\SDK\platforms\android-34\|35\|36` |
| `cmake-3.22.1-windows.zip` | `AndroidPlayer\SDK\cmake\3.22.1` |
| `commandlinetools-win-*.zip` | `AndroidPlayer\SDK\cmdline-tools\latest` |
| `android-ndk-r27c-windows.zip` (внутри `android-ndk-r27c`) | `AndroidPlayer\NDK` (содержимое) |
| `jdk17.0.9-*.zip` | `AndroidPlayer\OpenJDK` |

### Лицензия Unity (обязательный шаг, делает человек)
Любая работа `Unity.exe` — включая `-batchmode` — требует активированной лицензии,
иначе возвращается код `198` и `No valid Unity Editor license found`.
У Hub CLI **нет** команды активации (есть только `editors`, `install`,
`install-modules`, `install-path`), поэтому нужно один раз войти в GUI Unity Hub
под бесплатным аккаунтом Unity Personal — лицензия активируется автоматически.

После входа Hub должен видеть редактор, для этого путь установки задаётся так:
```
"Unity Hub.exe" -- --headless install-path --set C:\UnityEditors
"Unity Hub.exe" -- --headless editors --installed --json
```
Без этого Hub ищет редакторы в `C:\Program Files\Unity\Hub\Editor` и не находит их.

### Visual Studio 2022 Community
Устанавливался через winget с явными workloads:
```
winget install --id Microsoft.VisualStudio.2022.Community --accept-package-agreements `
  --accept-source-agreements `
  --override "--quiet --wait --norestart --add Microsoft.VisualStudio.Workload.ManagedDesktop `
  --add Microsoft.VisualStudio.Workload.ManagedGame --includeRecommended"
```
Проверка установленных workloads:
```
& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -products * `
  -requires Microsoft.VisualStudio.Workload.ManagedGame -format value -property displayName
```

### .NET SDK 8 (для сборки и тестов ядра)
Ставился локально, в профиль пользователя — без прав администратора:
```
& $env:TEMP\dotnet-install.ps1 -Channel 8.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
```
Сборка: `dotnet build shared/PokerCore/PokerCore.csproj`<br/>
Тесты: `dotnet test shared/PokerCore.Tests/PokerCore.Tests.csproj`<br/>
Headless-симуляция клиентского бота (без Unity): `dotnet run --project tools/ClientSim/ClientSim.csproj -c Release`

### Сборка клиента
Ядро попадает в Unity-клиент как готовая сборка:
```
powershell -File tools/build-pokercore.ps1
```
Скрипт собирает `PokerCore.dll` и кладёт его в
`client/PokerOnline/Assets/Plugins/PokerCore/`.

Сборка Windows-плеера (требует активированной лицензии Unity):
```
C:\UnityEditors\6000.3.10f1\Editor\Unity.exe -batchmode -nographics -quit ^
  -projectPath C:\Projects\poker-online\client\PokerOnline ^
  -executeMethod PokerOnline.EditorTools.BuildScript.BuildWindows ^
  -logFile C:\Projects\poker-online\build\unity-build.log
```
Результат: `build/windows/PokerOnline.exe`.

Проверка окружения (реальные команды, без заглушек): `powershell -File tools/verify-env.ps1`

### Проверка клиента без лицензии Unity
```
powershell -File tools/check-client-compiles.ps1
```
Скрипт компилирует `Assets\Scripts` и `Assets\Editor` тем же компилятором (Roslyn) и
против тех же сборок `UnityEngine`/`UnityEditor`, что использует редактор. Это позволяет
проверять клиентский код на ошибки API **не тратя лицензию и минуты на импорт проекта**.
Проверено: клиентские скрипты компилируются — 0 ошибок, 0 предупреждений.

---

## Заметки по среде

- `core.autocrlf=true` — переводы строк CRLF локально, LF в репозитории (см. `.gitattributes`).
- Рабочая папка проекта вынесена из OneDrive: синхронизация OneDrive ломала бы Unity
  (`Library/` весит гигабайты и постоянно перезаписывается).
- На этом ПК наблюдались проблемы с проверкой отзыва TLS-сертификатов
  (`CRYPT_E_REVOCATION_OFFLINE`) и с BITS — для загрузок использовался `curl.exe`.
- `client/PokerOnline/Packages/manifest.json` зафиксирован **вручную** и содержит только
  встроенные в редактор пакеты (`com.unity.modules.*` + `com.unity.ugui`), версии взяты
  из `Editor\Data\Resources\PackageManager\BuiltinPackages\*\package.json`.
  Причина: глобальный кэш пакетов Unity пуст, а сеть на этом ПК нестабильна —
  сгенерированный Unity манифест тянул бы `com.unity.ide.visualstudio`,
  `com.unity.collab-proxy`, `com.unity.timeline` и т.п. из интернета и мог застрять.
  С нашим манифестом первый импорт проекта проходит **полностью офлайн**.
