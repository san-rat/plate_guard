# PlateGuard Database Design

## Purpose

This note locks the database structure for the first implementation phase of PlateGuard.

Database engine:
- SQLite

Primary goal:
- enforce the rule that the same vehicle cannot use the same promotion more than once

---

## Main tables

The database contains four tables:

1. `Vehicles`
2. `Promotions`
3. `PromotionUsages`
4. `Settings`

---

## Table: Vehicles

Purpose:
- store one reusable master record per vehicle

Fields:

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | PK, autoincrement |
| VehicleNumberRaw | TEXT | Yes | original user input |
| VehicleNumberNormalized | TEXT | Yes | normalized business key |
| OwnerName | TEXT | No | optional |
| PhoneNumber | TEXT | Yes | required |
| Brand | TEXT | No | optional |
| Model | TEXT | No | optional |
| CreatedAt | TEXT | Yes | UTC timestamp |
| UpdatedAt | TEXT | No | UTC timestamp |

Constraints:
- Primary key on `Id`
- Unique constraint on `VehicleNumberNormalized`

Indexes:
- unique index on `VehicleNumberNormalized`
- non-unique index on `PhoneNumber`
- non-unique index on `OwnerName`

Decision:
- store both raw and normalized vehicle numbers

Reason:
- raw preserves what staff typed
- normalized is used for matching and duplicate prevention

---

## Table: Promotions

Purpose:
- store promotion campaigns that can be reused over time

Fields:

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | PK, autoincrement |
| PromotionName | TEXT | Yes | display name |
| Description | TEXT | No | optional |
| StartDate | TEXT | No | optional |
| EndDate | TEXT | No | optional |
| IsActive | INTEGER | Yes | `1` active, `0` inactive |
| CreatedAt | TEXT | Yes | UTC timestamp |
| UpdatedAt | TEXT | No | UTC timestamp |

Constraints:
- Primary key on `Id`

Indexes:
- optional non-unique index on `IsActive`

Decision:
- do not enforce a unique constraint on `PromotionName` in V1

Reason:
- the business rule is tied to `Id`
- this keeps future repeated or similarly named campaigns flexible

---

## Table: PromotionUsages

Purpose:
- store each promotion use by a vehicle

Fields:

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | PK, autoincrement |
| VehicleId | INTEGER | Yes | FK to `Vehicles.Id` |
| PromotionId | INTEGER | Yes | FK to `Promotions.Id` |
| ServiceDate | TEXT | Yes | service timestamp |
| Mileage | INTEGER | No | optional |
| NormalPrice | REAL | No | optional |
| DiscountedPrice | REAL | No | optional |
| AmountPaid | REAL | No | optional |
| Notes | TEXT | No | optional |
| CreatedAt | TEXT | Yes | UTC timestamp |
| UpdatedAt | TEXT | No | UTC timestamp |

Constraints:
- Primary key on `Id`
- Foreign key `VehicleId` -> `Vehicles.Id`
- Foreign key `PromotionId` -> `Promotions.Id`
- Unique constraint on `(VehicleId, PromotionId)`

Indexes:
- unique composite index on `(VehicleId, PromotionId)`
- non-unique index on `VehicleId`
- non-unique index on `PromotionId`

Decision:
- mileage is stored as `INTEGER` in V1

Reason:
- the field is operationally numeric
- it is easier to validate, sort, and filter later

---

## Table: Settings

Purpose:
- store application-level settings

Fields:

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | PK |
| DeletePasswordHash | TEXT | Yes | do not store plain text |
| ShopName | TEXT | No | optional |
| ExportFolder | TEXT | No | optional |
| CreatedAt | TEXT | Yes | UTC timestamp |
| UpdatedAt | TEXT | No | UTC timestamp |

Constraints:
- Primary key on `Id`

Decision:
- use a single settings row with `Id = 1`

---

## Relationships

- one `Vehicle` can have many `PromotionUsages`
- one `Promotion` can have many `PromotionUsages`
- one `PromotionUsage` belongs to exactly one `Vehicle`
- one `PromotionUsage` belongs to exactly one `Promotion`
- `Settings` is a global single-row table

---

## Delete rules

- deleting a `PromotionUsage` makes that vehicle eligible for that promotion again
- do not allow vehicle deletion when usage history exists
- do not allow promotion deletion when usage history exists
- promotions should normally be deactivated, not deleted

---

## Database file location

V1 decision:
- store the SQLite database in the local application data folder, not beside the executable and not in a temporary directory

Windows target path:
- `%LOCALAPPDATA%/PlateGuard/plateguard.db`

Future cross-platform rule:
- use the OS-specific local application data directory and keep the file name `plateguard.db`

---

## First-run behavior

On first launch:

1. check whether the database file exists
2. create the parent app-data folder if needed
3. create the database file if missing
4. create tables
5. create indexes and constraints
6. insert the default settings row with `Id = 1`
7. do not insert sample data in production

---

## Non-negotiable rules enforced by the database

- `VehicleNumberNormalized` must be unique
- `(VehicleId, PromotionId)` must be unique
- `PhoneNumber` is required for a vehicle record used in promotion tracking
- inactive promotions remain in history but must not accept new usage records
