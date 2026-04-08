# PlateGuard — Step-by-Step Implementation Guide

Version: 1.0  
Document Type: Detailed Build Guideline / Project Roadmap  
Project Type: Offline desktop application  
Recommended Stack: C# + .NET + Avalonia UI + SQLite  
Primary Delivery Target: Windows desktop  
Future Compatibility Goal: Windows, macOS, Linux

---

## 1. Purpose of This Document

This document explains, in very small steps, how to go from **nothing** to a **fully packaged, installable, offline desktop application** for the PlateGuard project.

It is written as a practical build roadmap, not as code.

The goal is to help you move through the project in the correct order without missing anything important.

---

## 2. What You Are Building

You are building a small offline desktop application called **PlateGuard**.

The purpose of the application is to prevent the same vehicle from claiming the same promotion more than once.

### Core idea

- Staff search for a vehicle.
- If the selected promotion was already used by that vehicle, the app clearly shows that.
- If the promotion was not used, staff can add the record.
- The system must work offline.
- The system runs on one computer.
- It opens as a normal desktop application window, not inside a browser.

---

## 3. Final Agreed Scope

These are the rules this guide is based on.

### 3.1 Environment

- One computer only
- Fully offline forever
- English only
- Minimal UI is acceptable
- Desktop app window, not a browser app

### 3.2 Required fields when saving a promotion usage

- Vehicle number
- Promotion
- Phone number

### 3.3 Optional fields

- Owner name
- Brand
- Model
- Mileage
- Normal price
- Discounted price
- Amount paid
- Notes

### 3.4 Functional rules

- A vehicle can use a promotion only once
- The same vehicle can use another promotion later
- Promotion history must remain in the system
- Promotions can be active or inactive
- Deleted usage records make that vehicle eligible for that promotion again
- Delete actions should require a simple password
- CSV export is required
- Search should support vehicle number, owner name, and phone number

---

## 4. Final Recommended Stack

### 4.1 Main stack

- **Language:** C#
- **Runtime:** .NET 8 or later
- **UI framework:** Avalonia UI
- **Database:** SQLite
- **Export format:** CSV
- **Installer target for V1:** Windows

### 4.2 Why this stack is suitable

- C# is good for maintainable desktop apps
- Avalonia gives a real desktop window and future cross-platform support
- SQLite is lightweight and perfect for an offline local database
- CSV export is simple and reliable

---

## 5. Project Outcome at the End

By the end of this guide, you should have:

- a working Avalonia desktop application
- a local SQLite database file
- a smart search bar
- promotion management
- vehicle reuse across promotions
- record editing
- password-protected deletion
- CSV export
- a published Windows build
- a shortcut on the user’s desktop after installation

---

## 6. Recommended Build Strategy

Do not try to build everything at once.

Build the application in this order:

1. Finalize scope
2. Prepare tools
3. Create project structure
4. Create data model
5. Create database
6. Implement core rules
7. Build search flow
8. Build add flow
9. Build promotions flow
10. Build edit/delete flow
11. Build export flow
12. Test everything
13. Package the application
14. Install and verify on the client machine

---

# Phase 0 — Lock the Project Before You Build

## 7. Step 0.1 — Create a small project folder on your computer

1. Create a main folder for the project.
2. Name it `PlateGuard`.
3. Inside that folder, create these subfolders:
   - `docs`
   - `design`
   - `screenshots`
   - `builds`
   - `notes`
4. Put the existing design spec into the `docs` folder.
5. Put this implementation guide into the `docs` folder too.

## 8. Step 0.2 — Write down the final V1 scope in one page

Create a simple note called `v1-scope.md` and write only the V1 features.

Include:

- search vehicle / owner / phone
- create promotion
- activate/deactivate promotion
- add promotion usage
- block duplicate usage
- edit record
- delete record with password
- export CSV

Do not include:

- printing
- multi-user login
- cloud sync
- internet features
- analytics dashboard
- billing system
- SMS / email

## 9. Step 0.3 — Define what “done” means

Write a short checklist called `definition-of-done.md`.

Your V1 is done only if:

- the app opens on Windows
- the database is created automatically if missing
- a promotion can be added
- a vehicle can be searched
- duplicate promotion usage is blocked
- delete asks for password
- CSV export works
- the app can be installed and run from a desktop shortcut

---

# Phase 1 — Prepare the Development Environment

## 10. Step 1.1 — Confirm your development machine

1. Decide which machine you will use to develop.
2. Make sure it is stable and has enough free disk space.
3. Make sure you can install software on it.
4. Make sure you have administrator rights if needed.

## 11. Step 1.2 — Install the .NET SDK

1. Check whether .NET is already installed.
2. If not installed, install .NET 8 SDK.
3. After installation, verify it from the terminal.
4. Confirm the SDK version is visible.

## 12. Step 1.3 — Install an IDE or code editor

Choose one:

- Visual Studio
- JetBrains Rider
- VS Code with C# extensions

Recommended for easiest desktop workflow on Windows:

- Visual Studio or Rider

## 13. Step 1.4 — Install Git

1. Install Git if you do not already have it.
2. Verify Git works in the terminal.
3. Set your name and email in Git.
4. Initialize version control for the project later.

## 14. Step 1.5 — Install SQLite inspection tools

This is optional but very useful.

Install one SQLite viewer, for example:

- DB Browser for SQLite

This helps you inspect records while testing.

## 15. Step 1.6 — Install the Avalonia templates if needed

1. Open terminal.
2. Install Avalonia templates if your chosen setup requires it.
3. Verify templates are available.
4. Do a small template test if you want.

## 16. Step 1.7 — Create a test location for published builds

1. Inside the `builds` folder, create:
   - `windows-dev`
   - `windows-release`
2. You will later place published app builds here.

---

# Phase 2 — Create the Project Skeleton

## 17. Step 2.1 — Create the root solution

1. Open terminal in the `PlateGuard` folder.
2. Create a .NET solution.
3. Name it `PlateGuard.sln`.

## 18. Step 2.2 — Decide project structure

Use a simple structure.

Recommended:

- `PlateGuard.App` — Avalonia UI project
- `PlateGuard.Core` — business rules, models, helpers
- `PlateGuard.Data` — SQLite/database access

You may also keep everything in one project for absolute simplicity, but a 3-project structure is cleaner.

## 19. Step 2.3 — Create the UI project

1. Create the Avalonia app project.
2. Name it `PlateGuard.App`.
3. Add it to the solution.
4. Make sure it runs as the startup project.

## 20. Step 2.4 — Create the Core project

1. Create a class library project.
2. Name it `PlateGuard.Core`.
3. Add it to the solution.
4. Add a project reference from `PlateGuard.App` to `PlateGuard.Core`.

## 21. Step 2.5 — Create the Data project

1. Create another class library project.
2. Name it `PlateGuard.Data`.
3. Add it to the solution.
4. Add a project reference from `PlateGuard.Data` to `PlateGuard.Core`.
5. Add a project reference from `PlateGuard.App` to `PlateGuard.Data`.

## 22. Step 2.6 — Initialize Git

1. Run `git init` in the root folder.
2. Create a `.gitignore` file suitable for .NET projects.
3. Make your first commit after the solution builds.

## 23. Step 2.7 — Run the empty app once

1. Build the solution.
2. Run the app.
3. Confirm that an Avalonia desktop window opens.
4. Close the app.
5. This confirms your base environment works.

---

# Phase 3 — Define the Data Model Before Writing Logic

## 24. Step 3.1 — List all real-world things in the system

Write these down:

- Vehicle
- Promotion
- Promotion usage
- App settings

These become your main tables.

## 25. Step 3.2 — Decide what the database must remember

For each vehicle, the system should remember at least:

- number plate
- normalized number plate
- phone number
- owner name if given
- brand and model if given

For each promotion, remember:

- name
- description
- start date
- end date
- active status

For each promotion usage, remember:

- which vehicle used it
- which promotion it belongs to
- mileage if given
- price fields if given
- notes if given
- date created

For settings, remember:

- admin delete password
- default export folder if needed

## 26. Step 3.3 — Decide the relationship between tables

1. One vehicle can have many promotion usage records.
2. One promotion can have many promotion usage records.
3. One usage record belongs to one vehicle and one promotion.
4. Settings will typically have only one row.

## 27. Step 3.4 — Decide the most important unique rule

The key business rule is not “vehicle must be unique everywhere.”

The actual rule is:

**A vehicle may use the same promotion only once.**

So your most important database rule is:

- unique combination of `VehicleId + PromotionId`

## 28. Step 3.5 — Decide how to treat number plate formatting

You must normalize the vehicle number.

Examples that should match the same vehicle:

- `CAB 1234`
- `cab1234`
- `CAB-1234`

Your normalization rule should:

1. trim leading and trailing spaces
2. convert to uppercase
3. remove internal spaces
4. remove dashes
5. optionally remove other separators if needed

## 29. Step 3.6 — Decide delete behavior now, not later

When a usage record is deleted:

- the history entry disappears
- the vehicle becomes eligible for that promotion again

This should be an intentional rule, not an accident.

## 30. Step 3.7 — Decide minimum required fields clearly

A promotion usage cannot be saved without:

- vehicle number
- selected promotion
- phone number

Everything else can be optional.

---

# Phase 4 — Write the Database Design

## 31. Step 4.1 — Draft the table list in a markdown file

Create `database-design.md`.

List four tables:

- Vehicles
- Promotions
- PromotionUsages
- Settings

## 32. Step 4.2 — Draft the Vehicles table

Suggested fields:

- `Id`
- `VehicleNumberRaw`
- `VehicleNumberNormalized`
- `OwnerName`
- `PhoneNumber`
- `Brand`
- `Model`
- `CreatedAt`
- `UpdatedAt`

## 33. Step 4.3 — Draft the Promotions table

Suggested fields:

- `Id`
- `PromotionName`
- `Description`
- `StartDate`
- `EndDate`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

## 34. Step 4.4 — Draft the PromotionUsages table

Suggested fields:

- `Id`
- `VehicleId`
- `PromotionId`
- `Mileage`
- `NormalPrice`
- `DiscountedPrice`
- `AmountPaid`
- `Notes`
- `ServiceDate`
- `CreatedAt`
- `UpdatedAt`

## 35. Step 4.5 — Draft the Settings table

Suggested fields:

- `Id`
- `DeletePasswordHash` or simple stored value depending on implementation choice
- `ExportFolder`
- `CreatedAt`
- `UpdatedAt`

## 36. Step 4.6 — Decide whether to store raw and normalized plate separately

Decision: yes.

Why:

- raw value preserves what user typed
- normalized value is used for matching and duplicate checking

## 37. Step 4.7 — Add indexes in the design

Plan indexes for:

- `VehicleNumberNormalized`
- `PhoneNumber`
- `OwnerName`
- `IsActive` in promotions if useful
- unique composite index on `(VehicleId, PromotionId)`

## 38. Step 4.8 — Decide database file location

Pick a local file location strategy.

Examples:

- inside app data folder
- inside a dedicated local application folder

Prefer a stable folder, not a temporary folder.

## 39. Step 4.9 — Decide first-run behavior

On first run:

1. app checks whether database file exists
2. if not, create database
3. create tables
4. create indexes
5. insert default settings row
6. optionally create one sample promotion only for internal testing, not production

---

# Phase 5 — Create the Core Application Flow on Paper First

## 40. Step 5.1 — Draw the main screens before coding UI

Create a small wireframe sketch for these screens:

- Main Search screen
- Add / Edit Vehicle & Usage screen
- Promotions screen
- History screen
- Settings / Delete Password screen

Use paper, Figma, Excalidraw, or even a notebook.

## 41. Step 5.2 — Define the main search screen behavior

The main screen should contain:

- one large search box
- a way to select or view the current promotion
- search results list
- vehicle detail area
- eligibility status area
- add button if eligible

## 42. Step 5.3 — Decide search logic flow

When user types text:

1. app reads input
2. app trims the input
3. if input is empty, show nothing or recent items
4. if input looks like a plate, normalize and search by normalized plate
5. also allow matching owner name or phone number
6. show matching items in a results list
7. if one result is selected, show its details and promotion history

## 43. Step 5.4 — Define vehicle-number search outcome

If the search is by vehicle number and the selected promotion already exists for that vehicle:

- show a clear warning, such as “Promotion already used”
- do not allow adding a duplicate usage

If not used:

- show a clear “Eligible to add” message
- show a button to add promotion usage

## 44. Step 5.5 — Define owner/phone search outcome

If the user searches by owner name or phone number:

- show a list of related vehicles
- when a vehicle is clicked, show its details
- then show promotion history for that vehicle
- then check selected promotion eligibility

## 45. Step 5.6 — Define add flow on paper

The add flow should work like this:

1. user selects active promotion
2. user searches a vehicle
3. if vehicle exists and promotion not used, open add usage form
4. if vehicle does not exist, allow creating vehicle + usage in one flow
5. save the record
6. show success message
7. refresh the screen

## 46. Step 5.7 — Define edit flow on paper

1. user opens history or vehicle record
2. selects a record
3. clicks edit
4. changes fields
5. saves changes
6. list refreshes

## 47. Step 5.8 — Define delete flow on paper

1. user selects a usage record
2. clicks delete
3. app shows password prompt
4. user enters admin password
5. app validates password
6. if password correct, delete record
7. if password incorrect, show error and cancel deletion

## 48. Step 5.9 — Define promotion management flow on paper

1. user opens promotions screen
2. user adds new promotion
3. user edits promotion if needed
4. user activates or deactivates promotion
5. inactive promotions remain in history but should not be used for new entries

## 49. Step 5.10 — Define export flow on paper

1. user opens history or export section
2. user chooses export action
3. app asks where to save CSV or uses default folder
4. app writes file
5. app shows success or failure message

---

# Phase 6 — Create the Code Structure

## 50. Step 6.1 — Create folders inside the Core project

Suggested folders:

- `Models`
- `Services`
- `Interfaces`
- `Helpers`
- `Validation`

## 51. Step 6.2 — Create folders inside the Data project

Suggested folders:

- `Entities`
- `Repositories`
- `Db`
- `Mappers`

## 52. Step 6.3 — Create folders inside the App project

Suggested folders:

- `Views`
- `ViewModels`
- `Dialogs`
- `Converters`
- `Assets`

## 53. Step 6.4 — Decide naming consistency early

Use one naming style everywhere.

Examples:

- singular class names: `Vehicle`, `Promotion`, `PromotionUsage`
- service names: `VehicleService`, `PromotionService`
- repository names: `VehicleRepository`, `PromotionUsageRepository`

---

# Phase 7 — Implement Core Models and Helpers First

## 54. Step 7.1 — Create the domain models

Create models for:

- Vehicle
- Promotion
- PromotionUsage
- AppSettings

These are your clean business objects.

## 55. Step 7.2 — Create a vehicle normalization helper

This helper should:

1. accept a string
2. return a normalized number plate string
3. safely handle null or empty input
4. remove spaces and dashes
5. convert to uppercase

This helper will be used everywhere plate matching happens.

## 56. Step 7.3 — Create validation rules

Create validation rules for:

- required vehicle number
- required phone number
- required promotion
- non-empty promotion name
- no duplicate active promotion names if you want that rule

## 57. Step 7.4 — Create result message models if useful

You may want small response models such as:

- operation success / failure
- validation errors
- eligibility check result

This keeps UI logic cleaner.

---

# Phase 8 — Implement the Database Layer

## 58. Step 8.1 — Choose data access style

Pick one:

- Entity Framework Core
- Dapper / ADO.NET manual queries

Recommendation for easier maintainability and clearer structure:

- Entity Framework Core

## 59. Step 8.2 — Install database packages

Add the packages required for:

- SQLite provider
- data access framework
- migrations if using EF Core

## 60. Step 8.3 — Create database entity classes

Mirror the tables:

- VehicleEntity
- PromotionEntity
- PromotionUsageEntity
- SettingsEntity

## 61. Step 8.4 — Create the database context

If using EF Core:

1. create the context class
2. add `DbSet` properties
3. configure the database file path
4. configure relationships
5. configure indexes
6. configure the composite unique rule on vehicle + promotion

## 62. Step 8.5 — Configure the vehicle normalized index

Make sure searches are fast.

Configure index for normalized vehicle number.

## 63. Step 8.6 — Configure the phone number index

Phone lookup should also be fast.

## 64. Step 8.7 — Configure the unique composite rule

This is critical.

Add a unique rule for:

- `VehicleId`
- `PromotionId`

This enforces the core business rule at database level.

## 65. Step 8.8 — Add created and updated timestamps

Make sure all important tables have timestamps.

This helps with history and future debugging.

## 66. Step 8.9 — Create initial migration or creation script

1. generate initial schema
2. verify the generated output
3. create the database locally
4. inspect it with SQLite viewer

## 67. Step 8.10 — Create repository classes

Create repositories for:

- Vehicles
- Promotions
- PromotionUsages
- Settings

These should handle basic CRUD operations.

## 68. Step 8.11 — Test the database layer before touching UI

Manually test:

- create vehicle
- create promotion
- create usage
- search vehicle
- search phone number
- block duplicate usage for same promotion

Do this early.

---

# Phase 9 — Implement the Business Services

## 69. Step 9.1 — Create the Vehicle service

Responsibilities:

- create vehicle
- update vehicle
- find by normalized plate
- search by owner name
- search by phone number

## 70. Step 9.2 — Create the Promotion service

Responsibilities:

- create promotion
- edit promotion
- activate promotion
- deactivate promotion
- list active promotions
- list all promotions

## 71. Step 9.3 — Create the PromotionUsage service

Responsibilities:

- check eligibility
- add usage
- edit usage
- delete usage
- get usage history for vehicle
- get usage count for promotion

## 72. Step 9.4 — Create the eligibility check method

This method should:

1. accept vehicle reference and promotion reference
2. check whether a usage record already exists
3. return eligible or not eligible
4. return a clear message for UI display

## 73. Step 9.5 — Create a combined “save vehicle and usage” flow

Needed for first-time vehicles.

Flow:

1. normalize plate
2. search existing vehicle
3. if vehicle does not exist, create vehicle
4. then create usage record
5. if vehicle exists, only create usage record
6. ensure duplicate promotion usage is still blocked

## 74. Step 9.6 — Create delete password logic

Implement a simple flow:

1. read saved password from settings
2. ask user for password when deleting
3. compare entered value
4. delete only if correct

You may store a hashed version if you want a cleaner implementation, even if full security is not the project goal.

## 75. Step 9.7 — Create CSV export service

Responsibilities:

- export selected records
- export all usage history
- write column headers
- write rows safely
- escape commas and quotes correctly

## 76. Step 9.8 — Create startup initialization service

At app startup:

1. ensure database exists
2. ensure tables exist
3. ensure default settings exist
4. ensure delete password exists or prompt for first setup

---

# Phase 10 — Build the UI Step by Step

## 77. Step 10.1 — Build the main window shell first

Create a clean main window with:

- title
- navigation area or tabs
- main content area
- simple status area if needed

Do not add all logic yet.

## 78. Step 10.2 — Decide navigation style

Recommended simple layout:

- Search
- Promotions
- History
- Settings

This is easier than hiding everything in one crowded screen.

## 79. Step 10.3 — Build the Search screen layout only

Place these UI parts:

- promotion selector
- large search textbox
- search results list
- selected vehicle details panel
- promotion status label
- add usage button

## 80. Step 10.4 — Connect search textbox to view model

1. bind the search text
2. detect text changes
3. debounce lightly if needed
4. run search method
5. update results list

## 81. Step 10.5 — Show search result types clearly

Each result item should show useful information, such as:

- vehicle number
- owner name
- phone number

This helps staff identify the right entry quickly.

## 82. Step 10.6 — Show the selected vehicle details

When a result is clicked, show:

- vehicle number
- owner
- phone
- brand/model if available
- list of past promotion usages

## 83. Step 10.7 — Show the promotion eligibility state

If selected promotion has already been used:

- show strong warning text
- disable add button

If eligible:

- show positive message
- enable add button

## 84. Step 10.8 — Build the Add Usage dialog

Include fields for:

- vehicle number
- phone number
- owner name
- promotion
- brand/model
- mileage
- normal price
- discounted price
- amount paid
- notes

Mark required fields clearly.

## 85. Step 10.9 — Support both “existing vehicle” and “new vehicle” in add flow

If vehicle already exists:

- prefill its details
- allow editing non-key details if needed

If vehicle does not exist:

- allow entry of required and optional details

## 86. Step 10.10 — Build the Promotions screen

Include:

- list of promotions
- add promotion button
- edit button
- activate/deactivate toggle
- usage count display if available

## 87. Step 10.11 — Build the Add Promotion dialog

Fields:

- promotion name
- description
- start date
- end date
- active status

Keep it simple.

## 88. Step 10.12 — Build the History screen

Include:

- searchable table or list
- filter by promotion if useful
- filter by date if useful
- edit button
- delete button
- export button

## 89. Step 10.13 — Build the Edit Usage dialog

Allow editing of:

- phone
- owner
- brand/model
- mileage
- prices
- notes
- service date if needed

Be careful when allowing promotion changes because that affects uniqueness rules.

## 90. Step 10.14 — Build the Delete confirmation dialog

Flow:

1. user clicks delete
2. show confirmation message
3. ask for password
4. if password correct, proceed
5. if wrong, cancel and show error

## 91. Step 10.15 — Build the Settings screen

Keep it minimal.

Include:

- change delete password
- choose default export folder
- maybe basic app info/version

---

# Phase 11 — Implement Validation and Edge Cases

## 92. Step 11.1 — Validate required fields in UI and service layer

Validate in both places.

Do not rely only on UI validation.

## 93. Step 11.2 — Validate plate normalization before save

Always normalize before checking duplicates.

## 94. Step 11.3 — Prevent duplicate promotion usage gracefully

Even if UI misses something, database may reject duplicate insert.

Catch that error and show a clean message like:

- “This vehicle has already used the selected promotion.”

## 95. Step 11.4 — Handle empty searches

If search box is empty:

- show nothing, or
- show recent vehicles, depending on preference

For V1, showing nothing is simpler.

## 96. Step 11.5 — Handle no results found

If no result matches:

- show “No matching vehicle found”
- show “Add new vehicle and usage” button

## 97. Step 11.6 — Handle inactive promotions

When inactive promotion is selected or viewed:

- show history
- do not allow new usage creation

## 98. Step 11.7 — Handle deletion safely

Before deletion:

- confirm record identity clearly
- show which vehicle and promotion will be removed
- warn that eligibility will be restored

## 99. Step 11.8 — Handle CSV export failures

Examples:

- folder not writable
- file already open in Excel
- invalid export path

Show clear user-friendly messages.

---

# Phase 12 — Test in Small Layers

## 100. Step 12.1 — Start with unit-level testing of helpers

Test the plate normalization helper with inputs like:

- `cab 1234`
- `CAB-1234`
- ` Cab 1234 `

Confirm all become the same normalized value.

## 101. Step 12.2 — Test the database uniqueness rule

1. create one vehicle
2. create one promotion
3. create one usage
4. try to create the same usage again
5. confirm it is blocked

## 102. Step 12.3 — Test different promotions for same vehicle

1. create vehicle
2. create promotion A usage
3. create promotion B usage
4. confirm both are allowed

## 103. Step 12.4 — Test search behavior

Test:

- by exact plate
- by plate with spaces/dashes
- by phone number
- by owner name

## 104. Step 12.5 — Test first-time vehicle flow

1. search unknown plate
2. confirm no result
3. add vehicle and usage
4. search again
5. confirm it now appears

## 105. Step 12.6 — Test deletion flow

1. create usage
2. try wrong password
3. confirm deletion blocked
4. try correct password
5. confirm deletion succeeds
6. check vehicle becomes eligible again

## 106. Step 12.7 — Test inactive promotion rules

1. create active promotion
2. deactivate it
3. confirm it no longer allows new usage
4. confirm history still appears

## 107. Step 12.8 — Test CSV export

1. export records
2. open CSV in Excel
3. verify headers
4. verify rows
5. verify special characters do not break the file

## 108. Step 12.9 — Test startup behavior from a clean state

1. delete local test database
2. run app fresh
3. confirm database recreates correctly
4. confirm default settings exist

---

# Phase 13 — Refine the User Experience

## 109. Step 13.1 — Improve field labels

Use plain labels staff can understand immediately.

Example:

- “Vehicle Number” instead of internal terms
- “Phone Number” instead of “Contact Identifier”

## 110. Step 13.2 — Use clear status messages

Examples:

- “Eligible for selected promotion”
- “Promotion already used”
- “Vehicle not found”
- “Record saved successfully”
- “Incorrect delete password”

## 111. Step 13.3 — Keep the main workflow fast

The main staff workflow should be possible in very few clicks.

Try to make this flow short:

1. select promotion
2. search vehicle
3. see status
4. add if eligible

## 112. Step 13.4 — Keep layout clean and not crowded

Because this is a small business tool, clarity matters more than visual fancy design.

## 113. Step 13.5 — Keep keyboard flow practical

If possible:

- search field gets focus when screen opens
- Enter can confirm selection or search
- Tab order is logical

This makes the app faster in real usage.

---

# Phase 14 — Prepare the App for Release

## 114. Step 14.1 — Clean the solution

Before release:

- remove unused files
- remove test dummy data
- remove debug-only messages
- remove internal placeholder UI text

## 115. Step 14.2 — Set application name and icon

1. set app title to `PlateGuard`
2. add a simple app icon
3. check the window title is correct
4. check published build uses correct name

## 116. Step 14.3 — Decide default settings for production

Examples:

- default delete password
- default export folder
- whether to create sample promotions or not

For a real client install, do not include fake data.

## 117. Step 14.4 — Decide where the database should live in production

Make sure the app stores its database in a safe, stable local location.

Avoid storing it in a temporary folder.

## 118. Step 14.5 — Decide backup approach

Even if backup is not built into V1, decide a simple manual backup plan.

For example:

- periodically copy the SQLite database file to a USB drive

Write this into handover notes.

---

# Phase 15 — Publish the Windows Build

## 119. Step 15.1 — Choose release mode

Use:

- Release build, not Debug

## 120. Step 15.2 — Publish a self-contained Windows build

Goal:

- the client should not need to separately install .NET runtime

Publish a self-contained Windows build for the correct architecture.

## 121. Step 15.3 — Decide portable build vs installer

You have two practical options.

### Option A — Portable build

- copy app folder to machine
- create desktop shortcut manually or with script

### Option B — Installer build

- install app to proper folder
- create desktop shortcut automatically

For client handover, installer is nicer.

## 122. Step 15.4 — Test the published executable locally

1. run the published app from the output folder
2. confirm it opens normally
3. confirm database works
4. confirm CSV export works

## 123. Step 15.5 — Verify the app is not opening in a browser

Confirm:

- it opens as a standalone desktop window
- it has its own process
- it does not require Chrome or internet

## 124. Step 15.6 — Create the client delivery folder

Create a folder containing:

- installer or published app
- short README
- default database if needed
- handover notes

---

# Phase 16 — Create the Installer / Shortcut Experience

## 125. Step 16.1 — Decide installer tooling

Choose an installer tool suitable for Windows.

Examples include:

- Inno Setup
- NSIS
- MSIX-based packaging depending on your preference

For simple client delivery, Inno Setup is often practical.

## 126. Step 16.2 — Define what the installer must do

The installer should:

1. copy the app files
2. create application folder
3. create desktop shortcut
4. optionally create Start Menu shortcut
5. optionally create initial app data folder

## 127. Step 16.3 — Test installation on your own machine

1. run installer
2. finish install
3. launch from shortcut
4. confirm app works

## 128. Step 16.4 — Test uninstall and reinstall behavior

Check:

- whether app files remove correctly
- whether database should be kept or removed

For business apps, keeping user data is often safer.

---

# Phase 17 — Final End-to-End Testing

## 129. Step 17.1 — Simulate real staff workflow

Test this exact scenario:

1. open app
2. select active promotion
3. search unknown vehicle
4. add vehicle and usage
5. search same vehicle again
6. confirm promotion is blocked
7. create another promotion
8. confirm same vehicle can use new promotion
9. deactivate old promotion
10. confirm history remains visible
11. export CSV

## 130. Step 17.2 — Test bad input scenarios

Test:

- blank vehicle number
- blank phone number
- no promotion selected
- invalid delete password
- duplicate usage attempt
- export to locked file

## 131. Step 17.3 — Test database persistence after app restart

1. add records
2. close app
3. open app again
4. confirm records are still there

## 132. Step 17.4 — Test from a copied build on another Windows machine if possible

This is very important.

A build that works only on your own machine is not enough.

---

# Phase 18 — Handover Preparation

## 133. Step 18.1 — Create a short user guide

Write one simple markdown or PDF guide covering:

- how to open the app
- how to search
- how to add a record
- how to manage promotions
- how to delete a record
- how to export CSV

## 134. Step 18.2 — Create a short admin note

Include:

- where the database file is stored
- how to back it up
- how to change delete password
- what happens when a record is deleted

## 135. Step 18.3 — Create a clean demo dataset if needed

If you want to show the client the app before going live:

- create a small sample database
- keep it separate from production data

## 136. Step 18.4 — Prepare the first live setup

For the actual installation day:

1. carry installer on USB
2. install app on target computer
3. confirm desktop shortcut is created
4. launch app
5. create first real promotion
6. test one real or sample vehicle record
7. confirm export works

---

# Phase 19 — Suggested Build Order for You Personally

If you want the cleanest development order, do it exactly like this:

1. Create solution
2. Run empty app
3. Create models
4. Create database context / tables
5. Test DB manually
6. Create normalization helper
7. Create services
8. Implement promotion creation
9. Implement vehicle search
10. Implement eligibility check
11. Implement add usage flow
12. Implement history list
13. Implement edit flow
14. Implement delete password flow
15. Implement CSV export
16. Improve UI
17. Publish release build
18. Create installer
19. Test install on another PC
20. Deliver

---

# Phase 20 — Common Mistakes to Avoid

## 137. Do not start with UI styling first

Get the rules and data correct first.

## 138. Do not compare raw plate text directly

Always normalize first.

## 139. Do not rely only on UI to block duplicates

Also enforce uniqueness in the database.

## 140. Do not hardcode temporary paths carelessly

Decide app data and export locations properly.

## 141. Do not skip clean release testing

Debug success does not guarantee release success.

## 142. Do not delay packaging until the very end

Test publishing early at least once.

## 143. Do not forget real client workflow speed

This app is mainly a search-and-check tool. Keep that workflow fast.

---

# Phase 21 — Minimum Deliverables Checklist

Use this before saying the project is complete.

## 144. Functional deliverables

- [ ] App opens as desktop window
- [ ] Database creates successfully
- [ ] Promotion can be added
- [ ] Promotion can be activated/deactivated
- [ ] Vehicle can be searched by plate
- [ ] Vehicle can be searched by owner
- [ ] Vehicle can be searched by phone
- [ ] New vehicle usage can be added
- [ ] Existing vehicle can be reused for another promotion
- [ ] Same promotion cannot be reused by same vehicle
- [ ] Record can be edited
- [ ] Record can be deleted with password
- [ ] CSV export works

## 145. Release deliverables

- [ ] Release build created
- [ ] Installer or portable package created
- [ ] Desktop shortcut works
- [ ] User guide created
- [ ] Backup note created
- [ ] Final test on target machine completed

---

# Phase 22 — Strong Recommendation for Your First Development Pass

For your first full pass, build only this:

1. Promotion creation
2. Search by plate
3. Create vehicle + usage
4. Duplicate promotion block
5. Search by phone
6. Search by owner
7. Delete with password
8. CSV export

Then polish the rest.

That keeps the project under control.

---

# Phase 23 — Final Summary

This project is very manageable if you build it in the correct order.

The most important things are:

- keep the scope small
- normalize vehicle numbers properly
- enforce one-promotion-per-vehicle in the database
- make the search flow fast and clear
- keep the UI simple
- package it like a real desktop app

The correct mindset is:

**First make it correct. Then make it clean. Then make it easy to install.**

---

# Phase 24 — The Simplest Possible One-Line Roadmap

If you want the whole project in one line:

**Plan the rules → create the database → build the search flow → build add/edit/delete → export CSV → publish Windows app → install and test on the client PC.**

---

End of document.
