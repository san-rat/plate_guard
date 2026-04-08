# PlateGuard UI Guidelines

## Purpose of this document

This document defines the UI direction for **PlateGuard**, the offline desktop application used to manage vehicle-based promotional eligibility.

This is **not** a pixel-perfect design document and **not** a strict component-by-component wireframe. It is a **UI guideline** intended for a developer or coding agent such as Codex so it has:

- enough freedom to build the interface well,
- enough constraints to keep the app simple and professional,
- enough clarity to avoid overdesign, clutter, and unnecessary complexity.

The final product should feel like a **small business desktop utility**, not a flashy consumer app.

---

## 1. Overall UI philosophy

The UI must be:

- **simple**
- **clear**
- **professional**
- **fast to understand**
- **easy for non-technical staff to use**
- **optimized for daily repetitive use**

The user should be able to open the app and understand the main action within a few seconds.

This application is primarily a **search-and-check tool** with support for adding, editing, and exporting records. The design should reflect that priority.

### The UI should NOT be:

- flashy
- visually crowded
- overly colorful
- animation-heavy
- dashboard-heavy
- filled with unnecessary cards, graphs, or widgets
- styled like a website or browser app

The app should feel like a **real desktop software window**.

---

## 2. General design direction

### Recommended visual tone

Use a clean, modern, restrained design.

Think in terms of:

- soft neutral backgrounds
- strong readability
- clear section separation
- minimal accent color usage
- modest spacing
- consistent alignment
- rectangular or softly rounded controls

A professional workshop/service-center internal tool should look:

- trustworthy
- organized
- practical
- lightweight

### Visual personality

The UI should communicate:

- control
- clarity
- reliability
- efficiency

It should not try to communicate:

- luxury
- playfulness
- trendiness
- entertainment

---

## 3. Platform behavior expectations

This is a **desktop application**, not a browser page.

The UI should open in a **native application window**.

It must behave like a small business desktop tool:

- opens in its own window
- can be resized within sensible limits
- keeps a stable layout
- does not rely on web-style responsive complexity
- supports keyboard and mouse comfortably

The interface should work well on standard Windows desktop/laptop screens. If cross-platform builds are later created, the same structure should still feel natural on macOS and Linux.

---

## 4. Target users

The main users are employees who need to:

- check whether a vehicle has already used a promotion
- search by vehicle number, phone number, or owner name
- add a new vehicle usage record when eligible
- manage promotions
- occasionally edit, delete, or export records

These users are not expected to be highly technical.

Therefore, the UI must favor:

- obvious actions
- clear wording
- reduced decision-making
- minimal training
- low error risk

---

## 5. Core UI principle: search first

The most important element in the entire application is the **search experience**.

When the app opens, the interface should immediately suggest:

**“Search a vehicle / phone number / owner name”**

This must feel like the primary purpose of the system.

### Priority order in the layout

1. Search area
2. Search results / eligibility feedback
3. Quick action to add usage
4. Secondary management areas such as promotions, history, settings, export

The UI should not bury the search behind tabs or menus.

---

## 6. Recommended application structure

A simple desktop layout is preferred.

### Recommended high-level structure

Use one main window with:

- a top header/title area
- a primary search section near the top
- a main content area below
- optional side navigation or top navigation for secondary modules

### Good module structure

The app can be organized into these major sections:

1. **Search / Home**
2. **Add Usage**
3. **Promotions**
4. **History / Records**
5. **Export**
6. **Settings**

However, the first screen should still prioritize search.

### Navigation recommendation

Use either:

- a compact left sidebar, or
- a top tab/menu bar

For this project, a **small left sidebar** is usually cleaner for a desktop utility.

The sidebar should be simple, narrow, and text-based.

Do not overdesign the navigation.

---

## 7. Main window layout guidance

### Header area

The top area should contain:

- application name: **PlateGuard**
- optional small subtitle such as: **Vehicle Promotion Control**
- optional current active promotion indicator, if helpful

Keep the header modest.

Do not make it oversized.

### Search area

The search area should be visually prominent.

It should include:

- one large input field
- placeholder text explaining accepted input
- a search icon if desired
- optional filter chips or dropdown for search type

Example placeholder:

`Search by vehicle number, phone number, or owner name`

The search field should be the strongest visual focus on the screen.

### Results area

Below the search field, the results area should update clearly.

It should be able to display:

- vehicle found / not found
- promotion already used / eligible
- list of matching vehicles for phone/owner searches
- quick action buttons

### Action area

When appropriate, show clear actions such as:

- **Add Usage**
- **View History**
- **Edit Vehicle**
- **Open Promotion**

These actions should appear contextually rather than cluttering the screen at all times.

---

## 8. Search behavior UI guidance

### Search input behavior

The search bar should feel smart but predictable.

The user may type:

- a vehicle number
- a phone number
- an owner name

The UI should detect likely matches and present helpful results.

### Vehicle number search

If the typed value matches a vehicle number:

- show the vehicle clearly
- show whether it has already used the selected promotion
- if already used, show a strong but calm warning state
- if eligible, show a positive eligibility state and a clear button to add usage

### Phone number search

If the typed value matches a phone number:

- show the owner/phone match
- show all vehicle numbers under that phone number
- make each result clickable/selectable

### Owner name search

If the typed value matches an owner name:

- show possible matching owners
- show related vehicle numbers
- allow the user to pick one from the list

### Result style

Search results should be:

- readable
- grouped logically
- not overloaded with unnecessary data

Each vehicle result row/card can show:

- vehicle number
- owner name
- phone number
- brand/model if available
- promotion status for the active/selected promotion

### Empty state

If nothing matches:

- do not show a harsh error
- show a clean empty state
- suggest the next action

Example:

**No matching vehicle found.**
**You can add this vehicle to the selected promotion if eligible.**

---

## 9. Eligibility feedback design

The eligibility result is a critical UI state.

It must be immediately understandable.

### Already used state

If the vehicle has already used the selected promotion:

- display a clear message
- make the result visually distinct
- avoid panic/error styling that feels system-broken

Example tone:

- **Promotion already used for this vehicle**
- **This vehicle is not eligible for this promotion**

### Eligible state

If the vehicle has not used the selected promotion:

- display a positive status
- provide a direct next action

Example tone:

- **Vehicle is eligible for this promotion**
- **No previous usage found for this promotion**

### Tone guideline

Feedback should be:

- short
- clear
- polite
- operational

Avoid dramatic or emotional wording.

---

## 10. Add Usage screen guidelines

The Add Usage form should be simple and efficient.

### Required fields

Required:

- vehicle number
- promotion
- phone number

Optional:

- owner name
- brand
- model
- mileage
- normal price
- discounted price
- amount paid
- notes

### Form design principles

- keep required fields clearly marked
- place required fields near the top
- group optional fields below or in a secondary section
- avoid long single-column scrolling when possible
- do not overwhelm the user with too many visible fields at once

### Recommended layout

A two-section form works well:

#### Section A: Core details
- vehicle number
- normalized preview or automatic formatting
- promotion selector
- phone number
- owner name

#### Section B: Additional details
- brand/model
- mileage
- normal price
- discounted price
- amount paid
- notes

### Form behavior

- validation should be immediate but not annoying
- normalize the vehicle number automatically
- if the selected vehicle already exists, reuse it instead of forcing a duplicate create flow
- if the selected promotion already exists for that vehicle, block save clearly

### Buttons

Use a minimal button set:

- **Save**
- **Cancel**
- optional **Clear**

Do not add too many secondary buttons.

---

## 11. Promotions screen guidelines

The Promotions module should be clean and administrative.

Users should be able to:

- view all promotions
- create a new promotion
- activate/deactivate a promotion
- inspect usage count if useful

### Promotion list design

Each promotion row can show:

- promotion name
- active/inactive status
- start/end dates if used
- total usage count

### Promotion actions

Keep actions simple:

- Add Promotion
- Edit Promotion
- Activate / Deactivate
- View Related Usage

### Visual rule

The currently active promotion should be easy to identify.

If the app supports searching against a selected promotion, the selected promotion should be visible in the main UI.

---

## 12. History / Records screen guidelines

The History screen is for reviewing and maintaining records.

### Must support

- list view of records
- search/filter
- edit record
- delete record with password confirmation

### Recommended table columns

A records table may include:

- date
- vehicle number
- owner name
- phone number
- promotion
- amount paid
- status

Do not include too many columns by default.

Optional details can appear in a side panel, modal, or detail view.

### Filtering

Useful filters may include:

- promotion
- date range
- vehicle number
- phone number
- owner name

### Delete flow

Deleting a record should feel deliberate.

When a delete is requested:

1. ask for confirmation
2. request password
3. explain the effect: deleting makes the vehicle eligible again for that promotion

This message should be clear.

---

## 13. Export screen guidelines

The Export area should be straightforward.

Keep it minimal.

### Recommended export options

- Export all records to CSV
- Export filtered records to CSV
- Choose save location if needed

### UI behavior

- show success message after export
- mention the saved file path
- avoid complicated export settings for version 1

---

## 14. Settings screen guidelines

The Settings area should remain very small.

Likely settings:

- delete password
- default export folder
- app/display preferences if needed later

### Important rule

Settings should not become a large admin dashboard.

Only include what is necessary.

---

## 15. Component style guidance

### Buttons

Buttons should be:

- medium size
- easy to click
- clearly labeled
- visually consistent

Primary actions should stand out modestly.

Dangerous actions such as delete should be distinct but not overly aggressive.

### Text inputs

Inputs should be:

- readable
- high contrast
- comfortably sized
- aligned consistently

The main search input should be larger than regular form inputs.

### Dropdowns and selectors

Keep dropdowns simple.

Do not create highly stylized custom controls unless necessary.

### Tables/lists

Tables should be clean and readable.

- use clear column headers
- allow row selection
- maintain good spacing
- avoid dense cramped rows

### Dialogs/modals

Use dialogs only when useful:

- confirm delete
- request password
- confirm export success
- create/edit promotion

Do not overuse modal windows.

---

## 16. Typography guidance

Typography should prioritize clarity.

### General rules

- use a clean sans-serif font
- keep hierarchy obvious
- avoid overly small text
- avoid decorative fonts

### Suggested hierarchy

- App title: prominent but not oversized
- Section titles: medium emphasis
- Field labels: clear and consistent
- Body text: readable and plain
- Status messages: slightly emphasized when important

### Text style rules

- keep labels short
- keep button text action-oriented
- keep system messages direct
- avoid paragraph-heavy screens

---

## 17. Color guidance

The color system should be restrained.

### General palette direction

Use:

- neutral/light base colors
- one primary accent color
- one success color
- one warning/error color

### Accent usage

Accent color should be used for:

- primary button
- selected navigation item
- active highlights

Do not use too many bright colors.

### Semantic colors

Use clear semantic colors for:

- eligible
- already used
- delete/danger
- success messages

These should be visually distinct but still professional.

---

## 18. Spacing and layout guidance

Spacing matters a lot in making the app feel professional.

### Principles

- use consistent margins
- use consistent internal padding
- separate sections clearly
- avoid elements touching each other too tightly
- do not over-stretch content across the whole window unnecessarily

### Recommended feel

The app should feel:

- airy enough to be readable
- compact enough to be efficient

Not too tight, not too empty.

---

## 19. Icons and imagery

Keep icon usage light.

### Allowed usage

Use simple icons for:

- search
- add
- edit
- delete
- export
- settings

### Not recommended

- decorative illustrations
- hero banners
- stock photos
- unnecessary empty-state artwork

This is a utility app, not a marketing page.

---

## 20. Error handling and validation UI

Validation must be clear, calm, and useful.

### Good validation behavior

- indicate which field is invalid
- explain what is wrong
- explain how to fix it
- do not clear the form unnecessarily

### Examples

Bad:

- Invalid input

Better:

- Vehicle number is required
- Phone number is required
- This vehicle has already used the selected promotion

### Tone

Keep all validation and errors:

- neutral
- informative
- respectful

---

## 21. Keyboard and efficiency guidance

This app may be used repeatedly throughout the day, so keyboard usability is important.

### Recommended behavior

- cursor starts in search field when home screen opens
- Enter can trigger search/select
- Tab order should be logical
- Escape can close dialogs
- keyboard access should feel natural in forms

Do not make the app mouse-only if that can be avoided.

---

## 22. Smart search experience recommendations

Because the search bar is central, the UI should support an efficient lookup flow.

### Recommended search flow

1. User opens app
2. Cursor is already in search box
3. User types vehicle number / phone number / owner name
4. Matching results appear below
5. User selects result
6. Eligibility status becomes obvious
7. Relevant next actions are shown

### Good smart behaviors

- live suggestions after a small amount of input
- exact matches ranked first
- vehicle number matches prioritized when input resembles a plate
- owner/phone grouped clearly

### Avoid

- overly aggressive auto-search that flickers constantly
- confusing mixed result layouts
- too much hidden logic that makes results feel unpredictable

---

## 23. Professional tone for UI text

All interface text should use a calm business tone.

### Good examples

- Search vehicle, phone number, or owner
- Promotion already used
- Vehicle is eligible for this promotion
- Add new usage record
- Export completed successfully
- Password required to delete this record

### Avoid

- slang
- playful jokes
- excessive punctuation
- dramatic warnings
- highly technical wording for normal staff actions

---

## 24. Freedom given to the coding agent

A coding agent such as Codex should have freedom to decide:

- exact control arrangement
- exact spacing system
- exact component library usage
- exact icon choice
- exact modal or panel implementation
- exact screen proportions
- exact font sizes within a sensible hierarchy

### But the coding agent should NOT change these core intentions:

- app must remain simple
- app must remain professional
- search must remain the main focus
- UI must not become flashy or heavy
- required actions must stay obvious
- eligibility state must be immediately visible
- records and promotions must be easy to manage
- dangerous actions must require clear confirmation

---

## 25. What “good UI” means for this project

For this project, good UI does **not** mean impressive visuals.

For this project, good UI means:

- staff can learn it quickly
- staff can search quickly
- staff can avoid duplicate promotion usage
- staff can add a record without confusion
- staff can find previous records easily
- the interface feels stable and trustworthy

That is the success standard.

---

## 26. What the first version should prioritize

Version 1 UI should prioritize:

1. Search clarity
2. Eligibility clarity
3. Fast add flow
4. Clear promotion selection
5. Easy record lookup
6. Safe delete flow
7. Clean export flow

It should **not** prioritize:

- advanced styling
- analytics dashboards
- charts
- themes
- complex animation
- decorative visual elements

---

## 27. Suggested screen summaries

### Screen 1: Home / Search
Focus: fast lookup and status

Must include:

- large search input
- selected promotion visibility
- smart results area
- clear eligibility message
- quick add button when applicable

### Screen 2: Add Usage
Focus: efficient form entry

Must include:

- required fields first
- optional fields later
- save/cancel actions
- duplicate usage prevention feedback

### Screen 3: Promotions
Focus: simple promotion control

Must include:

- promotion list
- active/inactive status
- create/edit actions

### Screen 4: History / Records
Focus: review and maintenance

Must include:

- table/list of records
- search/filter tools
- edit/delete actions

### Screen 5: Export
Focus: simple CSV export

Must include:

- export all / filtered options
- success feedback

### Screen 6: Settings
Focus: minimal configuration

Must include:

- delete password management
- export folder settings if implemented

---

## 28. Final implementation instruction for the UI builder

Build the interface like a **small professional desktop business tool**.

Keep it:

- clean
- dependable
- restrained
- easy to scan
- easy to use repeatedly

Treat **search** as the center of the product.

Treat **eligibility visibility** as the most important feedback state.

Treat **simplicity** as more important than creativity.

If there is ever a choice between:

- visually impressive, or
- operationally clear,

choose **operationally clear**.

---

## 29. One-sentence UI brief

**Design PlateGuard as a clean, professional, lightweight desktop utility where staff can quickly search vehicles, verify promotion eligibility, and manage records with minimal training and minimal visual clutter.**

