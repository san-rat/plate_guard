# PlateGuard — Offline Vehicle Promotion Control System

Version: 1.0  
Document Type: Detailed Design Specification  
Target Platform for V1: Windows desktop  
Future Platform Goal: Windows, macOS, Linux  
Recommended Stack: C# + .NET + Avalonia UI + SQLite

---

## 1. Project Summary

**PlateGuard** is a lightweight offline desktop application for vehicle service businesses that run limited-time promotions and need to prevent the same vehicle from claiming the same promotion more than once.

The application will run on a **single computer** and must work **fully offline**. Staff will use a simple desktop window to search for a vehicle by number plate, owner name, or phone number, check whether that vehicle has already used a selected promotion, and add a new usage record if eligible.

The system is intentionally simple:
- no internet dependency
- no cloud sync
- no browser-based interface
- no heavy graphics
- no staff login for normal use
- password confirmation only for delete actions

The main business rule is:

**A vehicle may use a given promotion only once.**

That means the same vehicle can still use a different promotion in the future, but it cannot use the exact same promotion twice unless the previous record is deleted.

---

## 2. Final Understanding from Requirements

This specification is based on the clarified requirements from the discussion.

### 2.1 Confirmed Requirements

- The app will be used on **one computer only**.
- It must work **offline forever**.
- It should preferably be **Windows-first**, but the design should allow **future cross-platform support**.
- The UI can be **minimal and simple**.
- The app must handle **thousands of records** without feeling slow.
- It must support **multiple promotions** over time.
- Promotions can be **activated** or **deactivated**.
- History must be **preserved**.
- Staff can **edit** records.
- Staff can **delete** records, but delete actions must ask for a **simple admin password**.
- Exporting records to **CSV** is required.
- The app only needs **English**.
- No printing is needed.
- No advanced security or user-account system is required for version 1.
- The smart search bar should support:
  - vehicle number search
  - owner name search
  - phone number search

### 2.2 Required vs Optional Data

Required when recording a promotion usage:
- vehicle number
- promotion
- phone number

Optional:
- owner name
- vehicle brand
- model
- mileage
- normal price
- discounted price
- amount paid
- notes

### 2.3 Clarified Functional Behavior

- A vehicle record can be reused later for a **different promotion**.
- History remains visible even when a promotion becomes inactive.
- If a record is deleted, that vehicle becomes eligible for that promotion again.
- Searching by vehicle number should clearly show whether the promotion has already been used.
- Searching by owner name or phone number should show a list of related vehicles.

---

## 3. Recommended Application Name

### Selected Product Name

# **PlateGuard**

### Why this name fits

- It sounds professional and product-like.
- It connects directly to the vehicle plate / number plate concept.
- It clearly suggests protection against misuse.
- It is short, easy to remember, and suitable for a desktop application shortcut.

### Optional subtitle

**PlateGuard — Promotion Control for Vehicle Service Centers**

---

## 4. Recommended Tech Stack

### 4.1 Chosen Stack

- **Language:** C#
- **Framework:** .NET 8+
- **Desktop UI:** Avalonia UI
- **Database:** SQLite
- **Data Access:** Entity Framework Core with SQLite provider, or lightweight repository-based ADO.NET/Dapper style access
- **Export:** CSV file generation
- **Installer for V1:** Windows installer package or portable packaged build with desktop shortcut

### 4.2 Why this stack is the best fit

#### C# + .NET
Good for business desktop applications, structured code, packaging, maintainability, and long-term handover.

#### Avalonia UI
Allows the app to be built as a real desktop application window rather than a browser page. It supports future cross-platform publishing from one codebase.

#### SQLite
Best fit for a local offline database because it is file-based, lightweight, fast, and does not require a database server.

### 4.3 Important Clarification About the UI Window

**Yes — this will open as a separate desktop application window, not inside Google Chrome or another web browser.**

That is one of the reasons Avalonia was chosen instead of a browser-based approach.

---

## 5. System Goals

The system should achieve the following:

1. Prevent duplicate use of the same promotion by the same vehicle.
2. Let staff quickly search before granting a promotion.
3. Let staff add a new record in a few seconds.
4. Keep a reusable database of vehicles and promotion histories.
5. Allow future promotions to be created without resetting the whole system.
6. Allow delete protection through a simple password prompt.
7. Stay fast and simple even with thousands of records.
8. Be easy to install and easy to use by non-technical staff.

---

## 6. Core Business Rules

### 6.1 Rule 1 — Vehicle Number Normalization
Vehicle number input must be normalized before matching.

Examples:
- `CAB 1234`
- `cab-1234`
- `CAB1234`

All of the above should be treated as the same value after normalization.

### 6.2 Rule 2 — One Promotion Per Vehicle Per Campaign
A vehicle may use a specific promotion only once.

Examples:
- Vehicle `CAB1234` can use **New Year Promo** once.
- The same vehicle can use **April Service Offer** once.
- The same vehicle cannot use **New Year Promo** twice.

### 6.3 Rule 3 — Inactive Promotions
Inactive promotions must:
- remain visible in history
- no longer appear in normal selection lists unless the screen allows viewing inactive promotions
- not accept new usage entries while inactive

### 6.4 Rule 4 — Delete Restores Eligibility
If a promotion usage record is deleted, the vehicle becomes eligible for that promotion again.

### 6.5 Rule 5 — Minimal Required Data
A promotion usage record should not be saved unless the following are present:
- normalized vehicle number
- selected promotion
- phone number

---

## 7. Functional Modules

The system can be divided into five modules.

### 7.1 Smart Search Module
Used from the main screen.

Capabilities:
- Search by vehicle number
- Search by owner name
- Search by phone number
- Auto-suggest matching results while typing
- Show selected vehicle and related promotion history
- Show whether the selected promotion has already been used

### 7.2 Vehicle Management Module
Stores and updates reusable vehicle-related details.

Capabilities:
- Create a new vehicle master record
- Edit vehicle details
- View all vehicles linked to a phone number or owner
- Reuse an existing vehicle for future promotions

### 7.3 Promotion Management Module
Manages promotional campaigns.

Capabilities:
- Add promotion
- Edit promotion
- Activate or deactivate promotion
- View usage count per promotion
- Keep history after deactivation

### 7.4 Promotion Usage Module
Main operational module.

Capabilities:
- Check eligibility
- Add usage record
- Block duplicates automatically
- Save pricing and service details if desired
- Show a success or duplicate warning immediately

### 7.5 History and Export Module
Administrative / reporting module.

Capabilities:
- View usage history
- Filter by date, promotion, vehicle, phone number
- Edit records
- Delete records with password confirmation
- Export records to CSV

---

## 8. Proposed Screen Structure

A simple tabbed structure is recommended rather than a very complex dashboard.

### 8.1 Screen A — Home / Smart Search
Primary operational screen.

#### Layout
- Large search bar at the top
- Search results list below
- Promotion selector
- Eligibility result panel
- Quick action buttons

#### Search behavior
When the user types:
- If input resembles a vehicle number, show matching plates
- If input resembles a phone number, show matching owners/vehicles
- If input resembles text, show owner matches and associated vehicles

#### Expected results
- If a vehicle + selected promotion already exists:
  - show **Promotion already used**
  - show usage date and relevant details
- If the vehicle exists but not for this promotion:
  - show **Eligible**
  - allow quick add
- If no vehicle is found:
  - show **No matching vehicle found**
  - offer **Add New Vehicle and Record Usage**

### 8.2 Screen B — Add / Edit Usage
Form screen for creating a usage record.

#### Required fields
- Vehicle number
- Promotion
- Phone number

#### Optional fields
- Owner name
- Brand
- Model
- Mileage
- Normal price
- Discounted price
- Amount paid
- Notes
- Service date (default = today)

#### Behavior
- Vehicle number is normalized automatically
- If vehicle exists, existing details can prefill optional fields
- If the selected promotion has already been used by that vehicle, save must be blocked

### 8.3 Screen C — Promotions
Promotion administration screen.

#### Capabilities
- Create new promotion
- Edit promotion name / description / date range
- Set status to active or inactive
- View number of usage records for each promotion

#### Suggested fields
- Promotion name
- Description
- Start date
- End date
- Is active

### 8.4 Screen D — History
View and manage records.

#### Capabilities
- Search and filter records
- Edit a record
- Delete a record
- Export filtered records to CSV

#### Suggested filters
- Promotion
- Vehicle number
- Owner name
- Phone number
- Date range

### 8.5 Screen E — Settings
Small configuration screen.

#### Capabilities
- Change delete password
- Set default export location
- View app version
- Optionally set shop name

---

## 9. Smart Search Behavior Specification

This is one of the most important parts of the app.

### 9.1 Search Input Rules
The app should try to interpret user input intelligently:

#### Case 1 — Vehicle number style input
Examples:
- `CAB1234`
- `CAB-1234`
- `WP CAD 2345`

Action:
- normalize input
- search `VehicleNumberNormalized`
- show exact or close normalized matches

#### Case 2 — Phone number style input
Examples:
- `0771234567`
- partial phone number like `0771`

Action:
- search phone numbers
- show all associated vehicles

#### Case 3 — Owner name text input
Examples:
- `Nimal`
- `Kamal Perera`

Action:
- search owner names
- show matching owners and their vehicles

### 9.2 Search Result Card Example
Each result row can display:
- Vehicle number
- Owner name
- Phone number
- Brand / model (optional)
- Promotions already used
- Last service date

### 9.3 Search Result Actions
For each result, allow:
- View history
- Add usage for selected promotion
- Edit vehicle details

---

## 10. Database Design

A four-table design is appropriate and clean for this system.

---

### 10.1 Table 1 — `Vehicles`
Stores each vehicle as a reusable master record.

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | Primary key |
| VehicleNumberRaw | TEXT | Yes | Original input from user |
| VehicleNumberNormalized | TEXT | Yes | Used for matching/search |
| OwnerName | TEXT | No | Optional |
| PhoneNumber | TEXT | Yes | Required as discussed |
| Brand | TEXT | No | Optional |
| Model | TEXT | No | Optional |
| CreatedAt | TEXT / DATETIME | Yes | Record creation timestamp |
| UpdatedAt | TEXT / DATETIME | No | Last updated timestamp |

#### Notes
- `VehicleNumberNormalized` should be indexed.
- `VehicleNumberNormalized` should normally be unique at vehicle master level.
- A vehicle is stored once, then reused for future promotion usage records.

---

### 10.2 Table 2 — `Promotions`
Stores campaigns or offers.

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | Primary key |
| PromotionName | TEXT | Yes | Display name |
| Description | TEXT | No | Optional description |
| StartDate | TEXT / DATETIME | No | Optional |
| EndDate | TEXT / DATETIME | No | Optional |
| IsActive | INTEGER / BOOLEAN | Yes | 1 = active, 0 = inactive |
| CreatedAt | TEXT / DATETIME | Yes | Timestamp |
| UpdatedAt | TEXT / DATETIME | No | Timestamp |

#### Notes
- Promotions are not deleted routinely; they are generally deactivated.
- History must remain even if a promotion becomes inactive.

---

### 10.3 Table 3 — `PromotionUsages`
Stores actual usage of a promotion by a vehicle.

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | Primary key |
| VehicleId | INTEGER | Yes | FK to Vehicles |
| PromotionId | INTEGER | Yes | FK to Promotions |
| ServiceDate | TEXT / DATETIME | Yes | Default = current date/time |
| Mileage | TEXT | No | Optional |
| NormalPrice | REAL | No | Optional |
| DiscountedPrice | REAL | No | Optional |
| AmountPaid | REAL | No | Optional |
| Notes | TEXT | No | Optional |
| CreatedAt | TEXT / DATETIME | Yes | Timestamp |
| UpdatedAt | TEXT / DATETIME | No | Timestamp |

#### Critical Constraint
There must be a unique constraint on:

`(VehicleId, PromotionId)`

This is the core rule that prevents duplicate promotion use.

---

### 10.4 Table 4 — `Settings`
Stores small application-level settings.

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | INTEGER | Yes | Primary key |
| DeletePasswordHash | TEXT | Yes | Stored password value |
| ShopName | TEXT | No | Optional |
| ExportFolder | TEXT | No | Default CSV export location |
| CreatedAt | TEXT / DATETIME | Yes | Timestamp |
| UpdatedAt | TEXT / DATETIME | No | Timestamp |

#### Notes
- Even though the password is simple, it should still not be stored as plain text if possible.
- Version 1 can use a simple hash-based approach for minimal protection.

---

## 11. Normalization Rules for Vehicle Number

This logic is essential.

### 11.1 Normalization Steps
When a user enters a vehicle number:
1. Trim leading/trailing spaces
2. Convert to uppercase
3. Remove internal extra spaces
4. Remove hyphens / dashes
5. Optionally remove other separator symbols

### 11.2 Example Conversions
| Raw Input | Normalized Result |
|---|---|
| CAB 1234 | CAB1234 |
| cab-1234 | CAB1234 |
| WP CAD 2345 | WPCAD2345 |
| wp-cad-2345 | WPCAD2345 |

### 11.3 Why it matters
Without normalization, staff could accidentally create duplicates for the same real-world vehicle.

---

## 12. Relationships Between Entities

- One vehicle can have many promotion usage records.
- One promotion can be used by many vehicles.
- One promotion usage belongs to exactly one vehicle and one promotion.
- Settings are global to the application.

### Relationship Summary
- `Vehicles 1 -> many PromotionUsages`
- `Promotions 1 -> many PromotionUsages`
- `Settings` stands alone

---

## 13. Key Workflows

### 13.1 Workflow A — Check Eligibility by Vehicle Number
1. Staff opens PlateGuard.
2. Staff selects an active promotion.
3. Staff types vehicle number in the search bar.
4. App normalizes the input.
5. App searches the vehicle database.
6. If no vehicle exists:
   - show message: `No vehicle found`
   - show action: `Add New Vehicle`
7. If vehicle exists:
   - app checks `PromotionUsages` for that vehicle + promotion
8. If a usage exists:
   - show message: `Promotion already used`
   - show date and history details
9. If no usage exists:
   - show message: `Eligible`
   - show action: `Add Promotion Usage`

### 13.2 Workflow B — Add New Vehicle and Use Promotion
1. Staff clicks `Add New Vehicle`.
2. Staff enters required details.
3. App normalizes the vehicle number.
4. App checks whether the vehicle already exists.
5. If vehicle does not exist:
   - create vehicle record
6. App saves promotion usage record.
7. App shows confirmation: `Usage saved successfully`.

### 13.3 Workflow C — Existing Vehicle Uses New Promotion
1. Staff searches existing vehicle.
2. Selects another promotion.
3. App checks for duplicate usage under that promotion only.
4. If no duplicate exists, save new usage.

### 13.4 Workflow D — Search by Phone Number or Owner Name
1. Staff types a phone number or owner name.
2. App shows matching owners/phone records.
3. App lists associated vehicles.
4. Staff selects a vehicle from the results.
5. App then shows history and current eligibility for the selected promotion.

### 13.5 Workflow E — Delete Record
1. Staff opens history.
2. Staff selects a usage record.
3. Staff clicks delete.
4. App asks for delete password.
5. If password is correct:
   - delete the record
   - vehicle becomes eligible again for that promotion
6. If password is wrong:
   - deny the action
   - keep the record intact

### 13.6 Workflow F — Export CSV
1. Staff opens history.
2. Applies optional filters.
3. Clicks `Export CSV`.
4. App generates CSV file in selected export folder.
5. App shows success message with file path.

---

## 14. Validation Rules

### 14.1 Required Input Validation
Must block save if any required field is missing:
- vehicle number
- promotion
- phone number

### 14.2 Duplicate Prevention
Must block save if `(VehicleId, PromotionId)` already exists.

### 14.3 Basic Data Validation
Recommended basic checks:
- trim text fields
- ensure phone number is not empty
- ensure price fields are numeric if entered
- ensure amount paid is not negative
- ensure dates are valid if entered manually

### 14.4 Promotion State Validation
If a promotion is inactive, the user should not be allowed to create a new usage record under it.

---

## 15. Non-Functional Requirements

### 15.1 Offline Operation
The application must work fully offline with no internet connection.

### 15.2 Performance
The app should feel responsive for:
- thousands of vehicle records
- fast search suggestions
- quick eligibility checks
- CSV export of normal record volumes

### 15.3 Simplicity
UI must remain simple and easy to train.

### 15.4 Maintainability
The codebase should be modular so that future features can be added later.

### 15.5 Packaging
V1 should be easy to install from a USB onto a Windows PC.

---

## 16. Indexing Strategy

To keep searches fast, create indexes for:

- `Vehicles.VehicleNumberNormalized`
- `Vehicles.PhoneNumber`
- `Vehicles.OwnerName`
- `PromotionUsages.VehicleId`
- `PromotionUsages.PromotionId`
- unique composite index on `PromotionUsages(VehicleId, PromotionId)`

This will keep search, lookup, and duplicate checks fast even with large local data.

---

## 17. Suggested Project Architecture

A clean layered structure is recommended.

```text
PlateGuard/
  src/
    PlateGuard.App/
      Views/
      ViewModels/
      Assets/
      App.axaml
    PlateGuard.Core/
      Models/
      Interfaces/
      Services/
      Validators/
      Helpers/
    PlateGuard.Data/
      Db/
      Repositories/
      Migrations/
    PlateGuard.Shared/
      DTOs/
      Constants/
  installer/
  docs/
```

### 17.1 Layer Responsibilities

#### UI Layer
- Avalonia windows/views
- forms
- bindings
- user interactions

#### Core Layer
- business rules
- normalization logic
- validation logic
- eligibility checking

#### Data Layer
- SQLite access
- repository implementation
- schema creation / migrations
- CSV export helpers if desired

#### Shared Layer
- shared constants
- DTOs
- reusable helpers

---

## 18. Suggested Main Classes / Components

### Core Entities
- `Vehicle`
- `Promotion`
- `PromotionUsage`
- `AppSetting`

### Services
- `VehicleService`
- `PromotionService`
- `PromotionUsageService`
- `SearchService`
- `SettingsService`
- `ExportService`
- `DeleteAuthorizationService`
- `VehicleNumberNormalizer`

### Repositories
- `VehicleRepository`
- `PromotionRepository`
- `PromotionUsageRepository`
- `SettingsRepository`

---

## 19. Permissions / Security Model for V1

This system does **not** need full account-based security in version 1.

### Minimal security approach
- No login for daily use
- Password required only for delete actions
- Optional password also for bulk actions in the future

### Recommended protected actions
- Delete one usage record
- Delete vehicle record if implemented
- Bulk clear promotion usages if added later
- Reset settings if desired later

---

## 20. Export Specification

### 20.1 Export Format
CSV is sufficient for version 1.

### 20.2 Suggested Export Columns
- Service date
- Promotion name
- Vehicle number
- Owner name
- Phone number
- Brand
- Model
- Mileage
- Normal price
- Discounted price
- Amount paid
- Notes

### 20.3 Why CSV First
- simple to implement
- very portable
- opens in Excel easily
- suitable for offline environments

---

## 21. Packaging and Deployment Plan

### 21.1 V1 Delivery Goal
Install from a USB onto a Windows machine and create a desktop shortcut.

### 21.2 User Experience Goal
The client should be able to:
1. run the installer
2. finish installation in a few steps
3. see a desktop shortcut
4. open PlateGuard as a normal desktop window
5. start using it immediately

### 21.3 Deployment Contents
The packaged app should include:
- application executable
- required runtime if published self-contained
- local SQLite database file or first-run database creation
- icons/assets
- desktop shortcut

### 21.4 V1 Recommendation
Publish **Windows first**. Keep the architecture cross-platform so future macOS/Linux builds remain possible.

---

## 22. Error Handling and Messaging

The app should use clear, simple messages.

### Suggested messages
- `Promotion already used for this vehicle.`
- `Vehicle is eligible for this promotion.`
- `No matching vehicle found.`
- `Record saved successfully.`
- `Record deleted successfully.`
- `Incorrect delete password.`
- `This promotion is inactive.`
- `Please fill all required fields.`

Avoid technical error messages for normal users.

---

## 23. Future Enhancements (Not Required for V1)

These are possible later but should not delay version 1.

- Excel `.xlsx` export
- receipt generation
- multi-user mode over LAN
- user login system
- activity log / audit trail
- image attachments
- automatic backups
- dashboard with counts and charts
- import existing vehicle lists from CSV
- support for multiple branches/locations

---

## 24. Scope Boundary for Version 1

### In Scope
- offline desktop application
- smart search
- promotion checking
- duplicate blocking
- promotion management
- history viewing
- edit records
- delete with password
- CSV export
- installer and desktop shortcut

### Out of Scope
- web version
- cloud sync
- printing
- advanced authentication
- networked multi-computer usage
- mobile app
- advanced analytics dashboard

---

## 25. Final Design Decision Summary

### Final Product Name
**PlateGuard**

### Final Stack
- C#
- .NET 8+
- Avalonia UI
- SQLite
- CSV export

### Final Database Design
4 tables:
- `Vehicles`
- `Promotions`
- `PromotionUsages`
- `Settings`

### Final Core Rule
**One vehicle can use one promotion only once.**

Implemented through:
- normalized vehicle number matching
- unique usage rule on `(VehicleId, PromotionId)`

### Final UI Direction
A lightweight **desktop app window**, not a browser app.

### Final V1 Focus
Simple, reliable, installable, offline, fast.

---

## 26. Recommended Build Order for Development

1. Create project structure
2. Set up SQLite database and schema
3. Implement vehicle normalization logic
4. Implement promotions CRUD
5. Implement vehicle and usage save flow
6. Implement duplicate checking
7. Build smart search UI
8. Add history and edit/delete flows
9. Add delete password handling
10. Add CSV export
11. Package Windows installer
12. Final testing with sample data

---

## 27. Final Conclusion

PlateGuard is a small but very practical offline desktop application. The project is not overly complex, but it must be designed carefully around one central business rule: **prevent the same vehicle from reusing the same promotion**.

The chosen architecture is intentionally balanced:
- simple enough for version 1
- strong enough to handle real operational use
- flexible enough for future promotions
- structured enough for future expansion

This specification provides a strong foundation for implementation, UI planning, database creation, and packaging.

