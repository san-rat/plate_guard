# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PlateGuard is an offline Windows-first desktop app for tracking vehicle promotion usage in service shops. It prevents the same vehicle from redeeming the same promotion twice, enforced at both the service layer and database level via a unique constraint on `(VehicleId, PromotionId)`.

## Commands

```powershell
# Build
dotnet build .\PlateGuard.sln

# Run the app
dotnet run --project .\src\PlateGuard.App\PlateGuard.App.csproj

# Run all tests
dotnet test .\PlateGuard.sln

# Run unit tests only
dotnet test .\tests\PlateGuard.Core.Tests\PlateGuard.Core.Tests.csproj

# Run integration tests only
dotnet test .\tests\PlateGuard.IntegrationTests\PlateGuard.IntegrationTests.csproj

# Run a single test by name
dotnet test .\tests\PlateGuard.Core.Tests\PlateGuard.Core.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run smoke tests (E2E)
dotnet run --project .\tools\PlateGuard.SmokeTests\PlateGuard.SmokeTests.csproj

# EF Core migrations (requires dotnet-ef local tool)
dotnet ef migrations add <MigrationName> --project .\src\PlateGuard.Data --startup-project .\src\PlateGuard.App
```

## Architecture

Three-project solution with clean layer separation:

```
PlateGuard.App   →   PlateGuard.Core   →   PlateGuard.Data
  (Avalonia UI)      (Services/Models)      (EF Core/SQLite)
```

- **PlateGuard.Core** — Domain models, service interfaces, and business logic implementations. No external dependencies beyond .NET base libraries. Services depend only on repository interfaces defined here.
- **PlateGuard.Data** — EF Core DbContext, entity classes, repository implementations, and mappers (Model ↔ Entity). Database path resolves to `%LOCALAPPDATA%\PlateGuard\plateguard.db`, overridable via `PLATEGUARD_DB_PATH`.
- **PlateGuard.App** — Avalonia MVVM UI. ViewModels use `CommunityToolkit.Mvvm` observables. DI wired in `Composition/ServiceCollectionExtensions.cs`. `Program.cs` bootstraps the container and runs `PlateGuardDatabaseInitializer` on startup to auto-apply pending migrations.

### Key patterns

- **MVVM**: ViewModels in `PlateGuard.App/ViewModels/`, Views in `PlateGuard.App/Views/`. `ViewLocator.cs` maps ViewModel types to View types.
- **Repository pattern**: Interfaces in `PlateGuard.Core/Interfaces/`, implementations in `PlateGuard.Data/Repositories/`. All services depend on interfaces, not concrete classes.
- **Transactional writes**: `PromotionUsageTransactionalWriter` handles usage insertion with a DB transaction to coordinate with the eligibility check.
- **Vehicle number normalization**: `VehicleNumberNormalizer.cs` normalizes plate numbers before storage and search — always use this helper rather than raw string comparison.
- **Delete protection**: Delete operations require a password verified via `DeletePasswordHasher.cs`.

### Tests

- **Unit tests** (`PlateGuard.Core.Tests`) — Pure service/helper tests with mocked repository interfaces.
- **Integration tests** (`PlateGuard.IntegrationTests`) — Use a real in-memory or temp SQLite database spun up via `IntegrationTestApp.cs`. Cover end-to-end service flows and `MainWindowViewModel` behavior.
- **Smoke tests** (`tools/PlateGuard.SmokeTests`) — Manual E2E runner; not part of CI.

### Database schema notes

- `Vehicles` — stores both raw and normalized plate numbers for flexible querying.
- `PromotionUsages` — unique index on `(VehicleId, PromotionId)` is the hard enforcement of the one-redemption rule.
- Foreign keys use `ON DELETE RESTRICT`; do not delete vehicles or promotions that have associated usage records.
- Migrations live in `PlateGuard.Data/Migrations/`. New migrations require the `dotnet-ef` local tool (already in `.config/dotnet-tools.json`).
