# Poker Онлайн

Мобильная онлайн-покер-игра (**Texas Hold'em**) для Android с внутриигровой валютой
(без реальных денег). Разрабатывается на Unity (C#) с последующим выделенным
сервер-авторитетным бэкендом.

> **Важно:** в игре нет ставок на реальные деньги и вывода средств. Только
> внутриигровые коины, зарабатываемые в игре и покупаемые за деньги (по правилам
> Google Play).

## Принцип разработки

**Сначала играбельная локальная игра → потом локальный мультиплеер → потом серверы →
потом email-регистрация → потом монетизация и релиз.**

Каждый этап завершается рабочим результатом, тегом и записью в `docs/ROADMAP.md`.

## Структура репозитория

```
poker-online/
├── client/     Unity-проект (C#), Android-клиент
├── shared/     Доменное ядро правил покера (без привязки к движку), общие типы/протокол
├── server/     Сервер (Node.js/TypeScript) — появится на этапе 6
├── docs/       SETUP.md (окружение), ROADMAP.md (журнал прогресса), прочая документация
└── .github/    CI (GitHub Actions)
```

## Документация

- [`docs/SETUP.md`](docs/SETUP.md) — что и как установлено на рабочем ПК
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — дорожная карта и журнал прогресса

## Ветвление (Git-flow)

- `main` — стабильная версия, только релизные теги
- `develop` — рабочая ветка
- `feature/<имя>` — фичи (от `develop`)
- `hotfix/<имя>` — срочные правки (от `main`)

Соглашение о коммитах: [Conventional Commits](https://www.conventionalcommits.org/)
(`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`).

## Окружение

См. `docs/SETUP.md`. Кратко: Unity 6 LTS + Android-модули, Visual Studio 2022,
Android Studio Emulator, Git, Node.js LTS, Docker (позже).

## Как собрать и запустить (локально)

### 1. Ядро правил (PokerCore)

Доменная логика покера вынесена в `shared/PokerCore` (чистый C#, без Unity) и покрыта тестами.

```powershell
dotnet test shared/PokerCore.Tests/PokerCore.Tests.csproj
```

### 2. Unity-клиент

Открыть проект `client/PokerOnline` в Unity 6.3 LTS (или собрать из командной строки):

```powershell
# Собрать DLL ядра в Plugins клиента
powershell -ExecutionPolicy Bypass -File tools/build-pokercore.ps1

# Собрать Windows-билд (batchmode)
& "C:\UnityEditors\6000.3.10f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath client/PokerOnline `
  -logFile build/unity-build.log `
  -executeMethod PokerOnline.EditorTools.BuildScript.BuildWindows
```

Готовый .exe появится в `build/windows/PokerOnline.exe`.

> Игровой стол MVP сейчас рисуется на IMGUI — это осознанное решение, чтобы
> прототип гарантированно запускался и в Play Mode, и в собранном билде.
> Красивый UI (uGUI/UI Toolkit) — отдельная задача этапа 2.
