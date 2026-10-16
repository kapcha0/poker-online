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
| 5 | Unity Editor (LTS) | 6000.3.10f1 (Unity 6.3 LTS) | `C:\Program Files\Unity\Hub\Editor\6000.3.10f1` | ⏳ устанавливается |
| 5a | — модуль Android Build Support | | (внутри Unity) | ⏳ |
| 5b | — модуль Android SDK & NDK Tools | | (внутри Unity) | ⏳ |
| 5c | — модуль OpenJDK | | (внутри Unity) | ⏳ |
| 6 | Visual Studio 2022 Community | | `C:\Program Files\Microsoft Visual Studio\2022\Community` | ❌ ещё не установлен |
| 7 | Android Studio + Emulator | | | ❌ ещё не установлен |
| 8 | Docker Desktop | | | ❌ не установлен (отложено до серверного этапа) |
| 9 | PostgreSQL + Redis | | (через Docker) | ❌ отложено |

Легенда: ✅ готово · ⏳ в процессе · ❌ ещё не сделано

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
  --module android android-sdk-ndk-tools android-open-jdk --childModules
```

---

## Заметки по среде

- `core.autocrlf=true` — переводы строк CRLF локально, LF в репозитории (см. `.gitattributes`).
- Рабочая папка проекта вынесена из OneDrive: синхронизация OneDrive ломала бы Unity
  (`Library/` весит гигабайты и постоянно перезаписывается).
- На этом ПК наблюдались проблемы с проверкой отзыва TLS-сертификатов
  (`CRYPT_E_REVOCATION_OFFLINE`) и с BITS — для загрузок использовался `curl.exe`.
