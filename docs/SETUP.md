# SETUP.md — рабочее окружение проекта «Poker Онлайн»

Документ фиксирует всё, что установлено и настроено на рабочем ПК для разработки.
Обновляется по мере установки инструментов.

**ОС:** Windows  
**Дата настройки:** 2026-10-16 (этап 0)  
**Путь проекта:** `C:\Projects\poker-online` (вне OneDrive, без кириллицы)

---

## Чек-лист окружения

| # | Инструмент | Версия | Путь | Статус |
|---|------------|--------|------|--------|
| 1 | Git | 2.55.0.windows.5 | `C:\Program Files\Git` | ✅ установлен и настроен |
| 2 | Node.js (LTS) | 24.20.0 | `C:\Program Files\nodejs\node.exe` | ✅ установлен |
| 3 | npm | 11.19.0 | `C:\Program Files\nodejs\npm` | ✅ работает |
| 4 | Unity Hub | 3.22.3.65535 | `C:\Program Files\WindowsApps\UnityTechnologies.UnityHub_3.22.3.65535_x64__2vrhnee42bhxm\app\Unity Hub.exe` | ✅ установлен (msix) |
| 5 | Unity Editor (LTS) | 6000.3.10f1 (Unity 6.3 LTS) | `C:\UnityEditors\6000.3.10f1` | ⏳ устанавливается (загрузка модулей идёт) |
| 5a | — модуль Android Build Support | | (внутри Unity) | ⏳ Android-модули загружены/распаковываются |
| 5b | — модуль Android SDK & NDK Tools | r27c | (внутри Unity) | ⏳ |
| 5c | — модуль OpenJDK | 17.0.9 | (внутри Unity) | ⏳ |
| 6 | Visual Studio 2022 Community | 17.14.37806.7 | `C:\Program Files\Microsoft Visual Studio\2022\Community` | ✅ установлен (workloads: Unity + .NET desktop) |
| 6a | — workload «Game development with Unity» (`ManagedGame`) | | | ✅ подтверждён через `vswhere -requires` |
| 6b | — workload «.NET desktop development» (`ManagedDesktop`) | | | ✅ подтверждён через `vswhere -requires` |
| 6c | — `devenv.exe` | | `...\Common7\IDE\devenv.exe` | ✅ |
| 6d | .NET SDK (от VS) | 9.0.319 | `C:\Program Files\dotnet\dotnet.exe` | ✅ |
| 6e | .NET SDK (локальный, для сборки ядра) | 8.0.426 | `%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe` | ✅ |
| 7 | Android Studio + Emulator | | | ❌ ещё не установлен |
| 8 | Docker Desktop | | | ❌ не установлен (нужен WSL2) |
| 9 | PostgreSQL + Redis | | (через Docker, `docker-compose.yml` готов) | ❌ отложено до серверного этапа |

Легенда: ✅ готово · ⏳ в процессе · ❌ ещё не сделано

### Проверено работоспособным

| Проверка | Результат |
|---|---|
| `dotnet build shared/PokerCore/PokerCore.csproj` | ✅ 0 ошибок, 0 предупреждений |
| `dotnet test shared/PokerCore.Tests` | ✅ 24 из 24 тестов |
| CI на GitHub Actions (`develop`) | ✅ зелёный, сборка + тесты проходят |
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
Ставится через CLI Unity Hub (headless), с модулями под Android:
```
"Unity Hub.exe" -- --headless install \
  --version 6000.3.10f1 --changeset e35f0c77bd8e \
  --module android android-sdk-ndk-tools android-open-jdk --childModules \
  --installPath C:\UnityEditors\6000.3.10f1
```
Установка запущена 2026-10-17 и в момент написания выполняется (нужны права
администратора, подтверждение UAC).

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
Тесты: `dotnet test shared/PokerCore.Tests/PokerCore.Tests.csproj`

---

## Заметки по среде

- `core.autocrlf=true` — переводы строк CRLF локально, LF в репозитории (см. `.gitattributes`).
- Рабочая папка проекта вынесена из OneDrive: синхронизация OneDrive ломала бы Unity
  (`Library/` весит гигабайты и постоянно перезаписывается).
- На этом ПК наблюдались проблемы с проверкой отзыва TLS-сертификатов
  (`CRYPT_E_REVOCATION_OFFLINE`) и с BITS — для загрузок использовался `curl.exe`.
