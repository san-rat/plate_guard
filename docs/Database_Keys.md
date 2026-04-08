# PlateGuard Database Keys Document

## 1. Purpose of this document

This document explains the **database keys** that should be used in the PlateGuard system.

PlateGuard is an offline desktop application used to:
- store vehicle information,
- manage promotions,
- prevent the same vehicle from using the same promotion more than once,
- keep promotion usage history,
- support searching by vehicle number, owner name, or phone number.

Because the main business rule is based on **preventing duplicate promotion use**, the database keys are one of the most important parts of the design.

---

## 2. What is a database key?

A **database key** is a field, or a combination of fields, used to identify records or create relationships between tables.

In this project, keys are needed to:
- uniquely identify each record,
- connect related tables,
- stop duplicate promotion usage,
- keep the data clean and consistent.

---

## 3. Key types used in PlateGuard

The PlateGuard database should use the following key types:

### 3.1 Primary Key (PK)
A **Primary Key** uniquely identifies each row in a table.

Example:
- `Vehicles.Id`
- `Promotions.Id`
- `PromotionUsages.Id`
- `Settings.Id`

Each table should have its own primary key.

### 3.2 Foreign Key (FK)
A **Foreign Key** links one table to another.

Example:
- `PromotionUsages.VehicleId` references `Vehicles.Id`
- `PromotionUsages.PromotionId` references `Promotions.Id`

This creates a relationship between:
- a vehicle,
- a promotion,
- and the usage record.

### 3.3 Unique Key / Unique Constraint
A **Unique Key** prevents duplicate values.

In PlateGuard, this is very important for:
- normalized vehicle numbers,
- the combination of vehicle and promotion.

### 3.4 Composite Unique Key
A **Composite Unique Key** means a combination of two or more fields must be unique.

This is the most important rule in the system:
- the same vehicle must not use the same promotion more than once.

So the table `PromotionUsages` should have a composite unique key on:
- `VehicleId`
- `PromotionId`

This means:
- vehicle A can use promotion X once,
- vehicle A can use promotion Y once,
- but vehicle A cannot use promotion X twice.

---

## 4. Recommended tables and their keys

The database has four main tables:

1. `Vehicles`
2. `Promotions`
3. `PromotionUsages`
4. `Settings`

---

## 5. Vehicles table keys

### 5.1 Table purpose
The `Vehicles` table stores the main vehicle information.

A vehicle should be created once and reused later when needed for another promotion.

### 5.2 Recommended fields
- `Id`
- `VehicleNumberRaw`
- `VehicleNumberNormalized`
- `OwnerName`
- `PhoneNumber`
- `Brand`
- `Model`
- `CreatedAt`
- `UpdatedAt`

### 5.3 Keys for Vehicles table

#### Primary Key
- `Id`

This should be the internal unique ID of the vehicle record.

#### Unique Key
- `VehicleNumberNormalized`

This is the best unique key for the vehicle table.

### 5.4 Why not use VehicleNumberRaw as the unique key?
Because staff may type the same plate in different ways, such as:
- `CAB 1234`
- `cab1234`
- `CAB-1234`

These should all be treated as the same vehicle.

So the system should normalize the value before saving or searching.

Example normalization:
- convert to uppercase,
- remove spaces,
- remove dashes,
- trim extra characters.

Examples:
- `cab 1234` -> `CAB1234`
- `CAB-1234` -> `CAB1234`
- ` cab1234 ` -> `CAB1234`

Because of this, the real unique value is:
- `VehicleNumberNormalized`

### 5.5 Final key design for Vehicles
- **Primary Key:** `Id`
- **Unique Key:** `VehicleNumberNormalized`

---

## 6. Promotions table keys

### 6.1 Table purpose
The `Promotions` table stores each promotion campaign.

Examples:
- New Year Discount
- April Service Promo
- Engine Check Offer

This makes the system reusable for future campaigns.

### 6.2 Recommended fields
- `Id`
- `PromotionName`
- `Description`
- `StartDate`
- `EndDate`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

### 6.3 Keys for Promotions table

#### Primary Key
- `Id`

This uniquely identifies each promotion.

#### Optional Unique Key
- `PromotionName`

This can be unique if the business wants every promotion name to be different.

However, this depends on how the business works.

Safer approach:
- keep `Id` as the real key,
- allow the business to decide whether `PromotionName` must always be unique.

### 6.4 Final key design for Promotions
- **Primary Key:** `Id`
- **Optional Unique Key:** `PromotionName`

---

## 7. PromotionUsages table keys

### 7.1 Table purpose
This is the most important table in the system.

It stores each time a vehicle uses a promotion.

This table is where the main business rule is enforced.

### 7.2 Recommended fields
- `Id`
- `VehicleId`
- `PromotionId`
- `Mileage`
- `NormalPrice`
- `DiscountedPrice`
- `AmountPaid`
- `ServiceDate`
- `Notes`
- `CreatedAt`
- `UpdatedAt`

### 7.3 Keys for PromotionUsages table

#### Primary Key
- `Id`

Each promotion usage record should have its own unique ID.

#### Foreign Keys
- `VehicleId` references `Vehicles.Id`
- `PromotionId` references `Promotions.Id`

These keys connect the usage record to:
- one vehicle,
- one promotion.

#### Composite Unique Key
- (`VehicleId`, `PromotionId`)

This is the most important key in the entire database.

### 7.4 Why this composite unique key is needed
The business rule is:

> One vehicle can use one promotion only once.

That means:
- a vehicle can appear in the table many times,
- a promotion can appear in the table many times,
- but the same vehicle-promotion pair must not appear twice.

Examples:

Allowed:
- Vehicle A + Promotion 1
- Vehicle A + Promotion 2
- Vehicle B + Promotion 1

Not allowed:
- Vehicle A + Promotion 1 again

This is exactly what the composite unique key prevents.

### 7.5 Final key design for PromotionUsages
- **Primary Key:** `Id`
- **Foreign Key:** `VehicleId` -> `Vehicles.Id`
- **Foreign Key:** `PromotionId` -> `Promotions.Id`
- **Composite Unique Key:** (`VehicleId`, `PromotionId`)

---

## 8. Settings table keys

### 8.1 Table purpose
The `Settings` table stores small application-level settings.

Examples:
- delete password,
- default export folder,
- shop name.

### 8.2 Recommended fields
- `Id`
- `DeletePasswordHash` or `DeletePassword`
- `ShopName`
- `ExportFolder`
- `UpdatedAt`

### 8.3 Keys for Settings table

#### Primary Key
- `Id`

Since this table may only contain one main row, the simplest approach is:
- use `Id = 1` for the default settings row.

### 8.4 Final key design for Settings
- **Primary Key:** `Id`

---

## 9. Summary of all keys

| Table | Key Type | Field(s) | Purpose |
|---|---|---|---|
| Vehicles | Primary Key | `Id` | Uniquely identifies each vehicle |
| Vehicles | Unique Key | `VehicleNumberNormalized` | Prevents duplicate vehicle records |
| Promotions | Primary Key | `Id` | Uniquely identifies each promotion |
| Promotions | Optional Unique Key | `PromotionName` | Prevents duplicate promotion names if needed |
| PromotionUsages | Primary Key | `Id` | Uniquely identifies each usage record |
| PromotionUsages | Foreign Key | `VehicleId` | Links usage to a vehicle |
| PromotionUsages | Foreign Key | `PromotionId` | Links usage to a promotion |
| PromotionUsages | Composite Unique Key | (`VehicleId`, `PromotionId`) | Prevents same vehicle from using same promotion twice |
| Settings | Primary Key | `Id` | Uniquely identifies settings row |

---

## 10. Recommended relationships between tables

### 10.1 Vehicles to PromotionUsages
Relationship:
- **One vehicle** can have **many promotion usage records**

Meaning:
- one vehicle may use many different promotions over time,
- but only once per promotion.

### 10.2 Promotions to PromotionUsages
Relationship:
- **One promotion** can have **many promotion usage records**

Meaning:
- many vehicles can use the same promotion,
- but each vehicle can only use it once.

### 10.3 Relationship diagram in words
- `Vehicles` -> one-to-many -> `PromotionUsages`
- `Promotions` -> one-to-many -> `PromotionUsages`

---

## 11. Why surrogate primary keys are better here

A **surrogate key** is an internal ID like `Id`.

Instead of using values like:
- vehicle number,
- promotion name,
- phone number,

as the primary key, it is better to use numeric IDs.

### Reasons:
1. It keeps relationships simpler.
2. It avoids problems if names or formats change.
3. It improves maintainability.
4. It keeps foreign keys smaller and cleaner.
5. It works better with future changes.

So even though `VehicleNumberNormalized` is unique, it should **not** be the main primary key.

Better approach:
- use `Id` as PK,
- use `VehicleNumberNormalized` as a unique business key.

---

## 12. Business keys vs technical keys

### 12.1 Technical key
A **technical key** is mainly for database structure.

Example:
- `Id`

### 12.2 Business key
A **business key** is meaningful in the real-world business process.

Examples:
- `VehicleNumberNormalized`
- (`VehicleId`, `PromotionId`)

In PlateGuard:
- `Id` is the technical key,
- the normalized vehicle number is a business key,
- the vehicle-promotion combination is also a business key.

Both are needed.

---

## 13. Delete behavior and key effects

The user already confirmed this rule:

> deleting a promotion usage record should make the vehicle eligible for that promotion again.

This means:
- if a row is removed from `PromotionUsages`,
- the composite unique key no longer blocks that vehicle-promotion pair,
- therefore the vehicle can use that promotion again.

This is correct for the requested business behavior.

### Important note
Deleting from `PromotionUsages` should **not** automatically delete:
- the vehicle record,
- the promotion record.

So the delete behavior should be carefully controlled.

Recommended approach:
- delete usage records only when confirmed,
- protect delete action with the admin/delete password.

---

## 14. Cascading rules recommendation

### 14.1 Vehicles table delete rule
If a vehicle is deleted while it has promotion usage history, the database must decide what happens.

Recommended rule:
- **Do not allow vehicle deletion if promotion usage records exist**

Reason:
- this protects history,
- avoids accidental orphan records.

Alternative:
- allow delete only after deleting related `PromotionUsages` rows.

### 14.2 Promotions table delete rule
Recommended rule:
- **Do not allow promotion deletion if usage records exist**

Reason:
- old history should remain visible,
- the promotion can be deactivated instead of deleted.

### 14.3 Best practical choice
For this project, the safest approach is:
- `Vehicles` and `Promotions` should usually be **soft-managed**,
- `Promotions` should be deactivated instead of deleted,
- `PromotionUsages` may be deleted only with password confirmation.

---

## 15. Suggested SQL-style key definitions

Below is a simplified SQL-style design showing how the keys would look conceptually.

```sql
CREATE TABLE Vehicles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VehicleNumberRaw TEXT NOT NULL,
    VehicleNumberNormalized TEXT NOT NULL UNIQUE,
    OwnerName TEXT,
    PhoneNumber TEXT NOT NULL,
    Brand TEXT,
    Model TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT
);

CREATE TABLE Promotions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PromotionName TEXT NOT NULL,
    Description TEXT,
    StartDate TEXT,
    EndDate TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT
);

CREATE TABLE PromotionUsages (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VehicleId INTEGER NOT NULL,
    PromotionId INTEGER NOT NULL,
    Mileage INTEGER,
    NormalPrice REAL,
    DiscountedPrice REAL,
    AmountPaid REAL,
    ServiceDate TEXT NOT NULL,
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id),
    FOREIGN KEY (PromotionId) REFERENCES Promotions(Id),
    UNIQUE (VehicleId, PromotionId)
);

CREATE TABLE Settings (
    Id INTEGER PRIMARY KEY,
    DeletePasswordHash TEXT,
    ShopName TEXT,
    ExportFolder TEXT,
    UpdatedAt TEXT
);
```

This is only a conceptual design for the document.

---

## 16. Indexes related to keys

Keys and indexes are connected, but they are not exactly the same thing.

### 16.1 Indexes automatically created
Usually:
- primary keys are indexed,
- unique keys are indexed.

So these will already help performance:
- `Vehicles.Id`
- `Vehicles.VehicleNumberNormalized`
- `Promotions.Id`
- `PromotionUsages.Id`
- `PromotionUsages(VehicleId, PromotionId)`
- `Settings.Id`

### 16.2 Additional indexes recommended
Since the app supports smart search, extra indexes are useful on:
- `Vehicles.PhoneNumber`
- `Vehicles.OwnerName`

These are not keys, but they improve speed for search features.

---

## 17. Data validation rules connected to keys

To make the keys work correctly, the system should validate data before saving.

### 17.1 Vehicle number validation
Before inserting into `Vehicles`:
- vehicle number must not be empty,
- vehicle number must be normalized,
- normalized number must be checked for duplicates.

### 17.2 Promotion selection validation
Before inserting into `PromotionUsages`:
- promotion must exist,
- promotion should usually be active,
- vehicle must exist or be created first.

### 17.3 Duplicate usage validation
Before saving a usage record:
- check if `VehicleId + PromotionId` already exists,
- if it exists, reject the insert,
- show message: `This vehicle has already used the selected promotion.`

Even if the UI checks this first, the database key must still enforce it.

---

## 18. Why database-level keys are necessary even if the UI checks duplicates

The application UI may already do this:
- search the vehicle,
- check the promotion,
- warn the user.

But the database must still enforce the rule.

### Reason:
UI checks can fail because of:
- programming mistakes,
- future code changes,
- unexpected save logic.

Database keys are the final protection layer.

So the rule should exist in **both places**:
- UI validation,
- database constraint.

---

## 19. Recommended final key design for PlateGuard

### Vehicles
- PK: `Id`
- Unique: `VehicleNumberNormalized`

### Promotions
- PK: `Id`
- Optional Unique: `PromotionName`

### PromotionUsages
- PK: `Id`
- FK: `VehicleId` -> `Vehicles.Id`
- FK: `PromotionId` -> `Promotions.Id`
- Composite Unique: (`VehicleId`, `PromotionId`)

### Settings
- PK: `Id`

---

## 20. Final conclusion

The most important database key decision in PlateGuard is this:

### Vehicle uniqueness
A vehicle should be uniquely identified by:
- `VehicleNumberNormalized`

### Promotion usage uniqueness
A promotion usage should be controlled by:
- `UNIQUE (VehicleId, PromotionId)`

This gives the system exactly the behavior needed:
- a vehicle can exist once,
- a promotion can exist once,
- a vehicle can use different promotions,
- but it cannot use the same promotion more than once,
- unless the record is intentionally deleted by an authorized user.

This key design is simple, scalable, correct, and fully aligned with the business rules discussed for PlateGuard.

