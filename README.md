# PlateGuard

PlateGuard is an offline Windows-first desktop application for tracking vehicle promotion usage in a service shop.

It is built for staff who need to:
- search vehicles by plate, phone, or owner
- check promotion eligibility
- record promotion usage
- review and edit history
- export usage records to CSV
- manage shop settings and delete-password protection

## Stack

- `.NET 9`
- `Avalonia UI`
- `EF Core 9`
- `SQLite`

## Solution Structure

- `src/PlateGuard.App`
  - Avalonia desktop UI
- `src/PlateGuard.Core`
  - business logic, services, models, helpers
- `src/PlateGuard.Data`
  - EF Core, SQLite, repositories, migrations
- `tests/PlateGuard.Core.Tests`
  - unit tests
- `tests/PlateGuard.IntegrationTests`
  - integration tests against real SQLite
- `tools/PlateGuard.SmokeTests`
  - end-to-end smoke runner
- `installer`
  - publish and installer scripts
- `docs`
  - detailed design and implementation docs

## Main Features

- vehicle lookup by:
  - vehicle number
  - phone number
  - owner name
- promotion management:
  - create
  - edit
  - activate/deactivate
- promotion eligibility checks
- add usage for new or existing vehicles
- usage history:
  - filter by text, promotion, and date range
  - edit and delete records
- CSV export
- settings:
  - shop name
  - export folder
  - delete password

## Database

- local SQLite database
- default path:
  - `%LOCALAPPDATA%\PlateGuard\plateguard.db`
- optional override:
  - `PLATEGUARD_DB_PATH`

Migrations are applied automatically on startup.

## Run The App

### PowerShell

```powershell
dotnet run --project .\src\PlateGuard.App\PlateGuard.App.csproj
```

Without rebuilding:

```powershell
dotnet run --project .\src\PlateGuard.App\PlateGuard.App.csproj --no-build
```

## Build

```powershell
dotnet build .\PlateGuard.sln
```

## Tests

### Unit Tests

```powershell
dotnet test .\tests\PlateGuard.Core.Tests\PlateGuard.Core.Tests.csproj --no-build
```

### Integration Tests

```powershell
dotnet test .\tests\PlateGuard.IntegrationTests\PlateGuard.IntegrationTests.csproj --no-build
```

### Smoke Tests

```powershell
dotnet run --project .\tools\PlateGuard.SmokeTests\PlateGuard.SmokeTests.csproj --no-build
```

### Run All Tests

```powershell
dotnet test .\PlateGuard.sln --no-build
```

## Package For Client Delivery

### Publish Windows Release

```powershell
powershell -ExecutionPolicy Bypass -File .\installer\publish-windows.ps1 -Configuration Release -Runtime win-x64 -Version 1.0.0
```

### Run The Published App

```powershell
.\builds\windows-release\publish\PlateGuard.exe
```

### Build Installer

Requires Inno Setup 6.

```powershell
powershell -ExecutionPolicy Bypass -File .\installer\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 1.0.0
```

If `ISCC.exe` is in a non-default location:

```powershell
powershell -ExecutionPolicy Bypass -File .\installer\build-installer.ps1 -IsccPath "C:\Path\To\ISCC.exe"
```

## Delivery Output

After packaging, check:

- `builds\windows-release\publish`
- `builds\windows-release\delivery`
- `builds\windows-release\installer`

Recommended client handoff files:

- `PlateGuard-Setup.exe`
- `USER-GUIDE.md`
- `ADMIN-NOTES.md`
- `FIRST-LIVE-SETUP-CHECKLIST.md`

## Notes

- this is an offline local-first system
- there is no server dependency
- SQLite handles persistence locally
- the practical scale limit depends more on query performance and UI behavior than on SQLite hard limits

## Reference Docs

- `docs/Design_Spec.md`
- `docs/Implementation_Guide.md`
- `docs/Database_Keys.md`
- `docs/UI_Guidelines.md`
- `installer/DELIVERY-README.md`
