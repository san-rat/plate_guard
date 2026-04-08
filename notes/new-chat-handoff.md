# PlateGuard New Chat Handoff

This file is for starting a fresh chat and continuing the same project without losing context.

## Project Goal

PlateGuard is an offline, Windows-first desktop app for vehicle service businesses.

Core business rule:
- the same vehicle must not be allowed to use the same promotion more than once
- deleting a usage record restores eligibility for that vehicle/promotion pair

Primary source docs:
- [docs/Design_Spec.md](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/docs/Design_Spec.md)
- [docs/Implementation_Guide.md](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/docs/Implementation_Guide.md)
- [docs/UI_Guidelines.md](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/docs/UI_Guidelines.md)
- [docs/Database_Keys.md](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/docs/Database_Keys.md)

Local working notes created during implementation:
- [notes/v1-scope.md](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/notes/v1-scope.md)
- [notes/definition-of-done.md](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/notes/definition-of-done.md)
- [notes/database-design.md](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/notes/database-design.md)

## How The User Wants Work To Continue

The user explicitly wants the project done step by step.

Working rules from the current chat:
- do one phase at a time
- stop after each phase
- give a summary before continuing
- do not auto-continue into the next phase
- follow the docs rather than improvising a new product direction

## Environment And Tooling

Repo path:
- `/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard`

Current framework choice:
- `.NET 9`

Reason:
- the installed SDK was `.NET 9.0.311`
- the docs allow `.NET 8+`
- the Avalonia template originally came in as `net10.0` and was changed to `net9.0`

Current stack:
- Avalonia UI 12
- CommunityToolkit.Mvvm
- EF Core 9
- SQLite

Projects in the solution:
- `src/PlateGuard.App`
- `src/PlateGuard.Core`
- `src/PlateGuard.Data`
- `tools/PlateGuard.SmokeTests`

EF tool manifest:
- [dotnet-tools.json](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/.config/dotnet-tools.json)
- `dotnet-ef` is pinned to `9.0.0`

Preferred build command used in this repo:

```bash
'/mnt/c/Program Files/dotnet/dotnet.exe' build PlateGuard.sln --no-restore
```

## Database Setup

Database provider:
- SQLite

Default DB path:
- `%LOCALAPPDATA%/PlateGuard/plateguard.db`

Override for isolated runs:
- environment variable `PLATEGUARD_DB_PATH`

Relevant files:
- [PlateGuardDatabasePathProvider.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Db/PlateGuardDatabasePathProvider.cs)
- [PlateGuardDatabaseInitializer.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Db/PlateGuardDatabaseInitializer.cs)
- [PlateGuardDbContext.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Db/PlateGuardDbContext.cs)
- [20260408071727_InitialCreate.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Migrations/20260408071727_InitialCreate.cs)

Schema decisions already locked:
- `Vehicles`
- `Promotions`
- `PromotionUsages`
- `Settings`
- `Vehicles.VehicleNumberNormalized` is unique
- `PromotionUsages` has a unique `(VehicleId, PromotionId)` constraint
- `Settings` is a single-row table with `Id = 1`

Database initializer behavior:
- creates the app-data folder if missing
- applies EF migrations automatically
- seeds the `Settings` row if missing

Important current development default:
- the seeded delete password is `admin`
- this is only acceptable for development scaffolding and still needs a real settings flow to change it

## Current Architecture

The solution is intentionally simple right now.

`PlateGuard.Core` contains:
- domain models
- service interfaces
- service implementations
- helpers such as vehicle-number normalization and password hashing

`PlateGuard.Data` contains:
- EF Core entities
- `DbContext`
- migration files
- repository implementations
- mappers between entities and core models

`PlateGuard.App` contains:
- Avalonia views
- view models
- main window workflow
- dialog workflows

Current composition style:
- no DI container yet
- repositories and services are manually instantiated in [App.axaml.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/App.axaml.cs)

## What Has Been Implemented So Far

### 1. Project Scaffold

Created:
- solution and projects
- `builds/`
- `design/`
- `screenshots/`
- `notes/`
- `src/`
- `installer/`

Core files:
- [PlateGuard.sln](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/PlateGuard.sln)
- [PlateGuard.App.csproj](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/PlateGuard.App.csproj)
- [PlateGuard.Core.csproj](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/PlateGuard.Core.csproj)
- [PlateGuard.Data.csproj](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/PlateGuard.Data.csproj)

### 2. Core Domain Models And Helpers

Implemented:
- `Vehicle`
- `Promotion`
- `PromotionUsage`
- `AppSettings`
- vehicle-number normalization helper
- delete-password hashing helper

Relevant files:
- [Vehicle.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/Vehicle.cs)
- [Promotion.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/Promotion.cs)
- [PromotionUsage.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/PromotionUsage.cs)
- [AppSettings.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/AppSettings.cs)
- [VehicleNumberNormalizer.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Helpers/VehicleNumberNormalizer.cs)
- [DeletePasswordHasher.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Helpers/DeletePasswordHasher.cs)

### 3. EF Core Data Layer

Implemented:
- entity classes
- `PlateGuardDbContext`
- design-time `DbContext` factory
- migrations
- path provider

Constraints already enforced at the database level:
- unique normalized vehicle number
- unique vehicle/promotion usage pair
- restricted delete behavior for usage relations

Relevant files:
- [VehicleEntity.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Entities/VehicleEntity.cs)
- [PromotionEntity.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Entities/PromotionEntity.cs)
- [PromotionUsageEntity.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Entities/PromotionUsageEntity.cs)
- [SettingsEntity.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Entities/SettingsEntity.cs)
- [PlateGuardDbContext.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Db/PlateGuardDbContext.cs)
- [PlateGuardDbContextFactory.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Db/PlateGuardDbContextFactory.cs)
- [PlateGuardDbContextModelSnapshot.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Migrations/PlateGuardDbContextModelSnapshot.cs)

### 4. Repositories And Startup Initialization

Implemented repositories:
- vehicle repository
- promotion repository
- promotion-usage repository
- settings repository

Implemented startup behavior:
- app startup initializes the database through `Program.cs`
- the app applies migrations and seeds the settings row before the UI is used

Relevant files:
- [VehicleRepository.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Repositories/VehicleRepository.cs)
- [PromotionRepository.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Repositories/PromotionRepository.cs)
- [PromotionUsageRepository.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Repositories/PromotionUsageRepository.cs)
- [SettingsRepository.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Repositories/SettingsRepository.cs)
- [Program.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Program.cs)

### 5. Smoke-Test Runner

There is a dedicated console runner that exercises the current data/service path end to end.

It verifies:
- settings seed exists
- vehicle creation
- promotion creation
- usage creation
- duplicate usage blocking
- search by plate
- search by phone
- search by owner
- usage history
- active promotion lookup
- wrong delete password rejection
- correct delete password success
- eligibility restored after delete

Relevant files:
- [PlateGuard.SmokeTests.csproj](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/tools/PlateGuard.SmokeTests/PlateGuard.SmokeTests.csproj)
- [Program.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/tools/PlateGuard.SmokeTests/Program.cs)

Smoke-test database artifact:
- `builds/smoke-tests/plateguard-smoke.db`

### 6. Business Services

Implemented services:
- `VehicleService`
- `PromotionService`
- `PromotionUsageService`

Business rules currently handled in services:
- vehicle-number normalization
- search by vehicle number / owner / phone
- create and update promotion
- activate and deactivate promotion
- eligibility check for vehicle/promotion
- combined save flow for vehicle + usage
- delete with password verification
- update usage record
- usage-count lookup by promotion
- record search for history screen

Relevant files:
- [IVehicleService.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Interfaces/IVehicleService.cs)
- [IPromotionService.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Interfaces/IPromotionService.cs)
- [IPromotionUsageService.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Interfaces/IPromotionUsageService.cs)
- [VehicleService.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Services/VehicleService.cs)
- [PromotionService.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Services/PromotionService.cs)
- [PromotionUsageService.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Services/PromotionUsageService.cs)

History-related models added later:
- [PromotionUsageRecord.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/PromotionUsageRecord.cs)
- [PromotionUsageRecordQuery.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/PromotionUsageRecordQuery.cs)
- [UpdatePromotionUsageRecordRequest.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/UpdatePromotionUsageRecordRequest.cs)

### 7. Search-First Main UI

The main desktop shell has been converted away from the Avalonia template into a real app shell.

Current main areas:
- Search
- Promotions
- History
- Settings placeholder only

Search workflow currently supports:
- promotion selection
- search by plate / phone / owner
- selected vehicle summary
- eligibility status
- vehicle promotion history

Relevant files:
- [MainWindow.axaml](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/MainWindow.axaml)
- [MainWindow.axaml.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/MainWindow.axaml.cs)
- [MainWindowViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/MainWindowViewModel.cs)

### 8. Add Usage Dialog

Implemented:
- dedicated add-usage dialog
- required and optional fields
- support for existing vehicle flow
- support for new vehicle flow from a vehicle-number search
- validation for numeric inputs and required fields

Relevant files:
- [AddUsageDialogRequest.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/AddUsageDialogRequest.cs)
- [AddUsageDialogViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/AddUsageDialogViewModel.cs)
- [AddUsageWindow.axaml](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/AddUsageWindow.axaml)
- [AddUsageWindow.axaml.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/AddUsageWindow.axaml.cs)

### 9. Promotions Management

Implemented:
- promotions screen
- add promotion dialog
- edit promotion dialog
- active/inactive toggle
- usage count display
- sync with the search screen active-promotion picker

Relevant files:
- [PromotionManagementItemViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/PromotionManagementItemViewModel.cs)
- [PromotionDialogRequest.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/PromotionDialogRequest.cs)
- [PromotionDialogViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/PromotionDialogViewModel.cs)
- [PromotionDialogWindow.axaml](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/PromotionDialogWindow.axaml)
- [PromotionDialogWindow.axaml.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/PromotionDialogWindow.axaml.cs)

### 10. History / Records Workflow

Implemented:
- dedicated History section in the main shell
- records list
- text search
- promotion filter
- date range filters
- selected record details
- edit usage dialog
- delete usage dialog with password prompt

History search supports:
- plate
- normalized plate
- phone number
- owner name
- promotion name

Relevant files:
- [HistoryPromotionFilterOptionViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/HistoryPromotionFilterOptionViewModel.cs)
- [HistoryRecordItemViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/HistoryRecordItemViewModel.cs)
- [EditUsageDialogRequest.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/EditUsageDialogRequest.cs)
- [EditUsageDialogViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/EditUsageDialogViewModel.cs)
- [EditUsageWindow.axaml](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/EditUsageWindow.axaml)
- [EditUsageWindow.axaml.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/EditUsageWindow.axaml.cs)
- [DeleteUsageDialogRequest.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/DeleteUsageDialogRequest.cs)
- [DeleteUsageDialogViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/DeleteUsageDialogViewModel.cs)
- [DeleteUsageWindow.axaml](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/DeleteUsageWindow.axaml)
- [DeleteUsageWindow.axaml.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/DeleteUsageWindow.axaml.cs)

## Branding Assets

Source assets:
- [public/logo.png](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/public/logo.png)
- [public/favicon.ico](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/public/favicon.ico)

Copied into app assets:
- [src/PlateGuard.App/Assets/logo.png](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Assets/logo.png)
- [src/PlateGuard.App/Assets/favicon.ico](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Assets/favicon.ico)

## Current Functional State

As of this handoff, the codebase already supports:
- automatic DB creation and migration
- seeded settings row
- search by plate / owner / phone
- promotion creation and activation/deactivation
- usage creation through a dialog
- duplicate usage blocking for the same vehicle/promotion
- history list/filtering
- edit usage
- delete usage with password verification

## What Is Still Missing

Not implemented yet:
- settings screen
- export CSV flow
- default export folder editor
- change delete password flow
- installer packaging and desktop shortcut
- manual end-to-end UI walkthrough and QA

Important note:
- several build phases were verified
- the UI flows were mostly validated by successful builds and service/repository behavior
- there has not yet been a thorough manual click-through of every window

## Git And Ignore Notes

Important fix already made:
- `.gitignore` had a `*.app` rule that accidentally ignored `src/PlateGuard.App` on a case-insensitive Git setup
- this was fixed by explicitly unignoring `src/PlateGuard.App/` and re-ignoring its `bin/` and `obj/` folders

Also ignored now:
- `builds/smoke-tests/`
- `builds/windows-dev/`
- `builds/windows-release/`
- `*.db-wal`
- `*.db-shm`

At the time of this handoff:
- there are still local uncommitted changes
- do not assume the work has already been committed
- check `git status --short` before making the next phase changes

## Suggested First Checks In A New Chat

1. Read this file.
2. Confirm the user still wants phase-by-phase progress with a stop after each phase.
3. Read the primary docs if needed for the next phase:
   - `docs/Implementation_Guide.md`
   - `docs/UI_Guidelines.md`
   - `docs/Design_Spec.md`
4. Check current repo status with `git status --short`.
5. Build before changing code.

## Next Exact Phase

The next reviewable implementation phase should be:

### Export And Settings

Recommended scope for the next phase:
- create a basic Settings screen
- load and save `ShopName`
- load and save `ExportFolder`
- add a change-delete-password workflow
- add CSV export for records/history

Recommended rule for that next phase:
- do only the export/settings phase
- stop
- summarize what changed
- do not continue beyond that without user confirmation

## Useful Entry Points For The Next Chat

Start here for application composition:
- [App.axaml.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/App.axaml.cs)

Start here for the main shell workflow:
- [MainWindowViewModel.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/ViewModels/MainWindowViewModel.cs)
- [MainWindow.axaml](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.App/Views/MainWindow.axaml)

Start here for settings persistence:
- [AppSettings.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/AppSettings.cs)
- [ISettingsRepository.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Interfaces/ISettingsRepository.cs)
- [SettingsRepository.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Repositories/SettingsRepository.cs)

Start here for record data needed by CSV export:
- [PromotionUsageRecord.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/PromotionUsageRecord.cs)
- [PromotionUsageRecordQuery.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Models/PromotionUsageRecordQuery.cs)
- [PromotionUsageRepository.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Data/Repositories/PromotionUsageRepository.cs)
- [PromotionUsageService.cs](/mnt/c/Users/User/Desktop/coding/projects/2026/plate_guard/src/PlateGuard.Core/Services/PromotionUsageService.cs)

## Short Status Summary

The project is no longer just docs.

It already has:
- a working solution structure
- a real SQLite schema and migrations
- repositories and services
- a search-first Avalonia desktop shell
- add/edit/delete record workflows
- promotion management
- history filtering

The next unfinished product slice is export plus settings.
