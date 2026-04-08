# PlateGuard User Guide

## Purpose

PlateGuard helps staff check whether a vehicle has already used a promotion and record new usage without internet access.

## Open The App

1. Open `PlateGuard.exe` or launch PlateGuard from the desktop shortcut.
2. Wait for the main window to load.
3. If this is the first run, go to `Settings` and set the shop name and delete password before normal use.

## Main Sections

- `Search`: search vehicles, check eligibility, and add new usage.
- `Promotions`: create promotions, edit them, and activate or deactivate them.
- `History`: review records, edit records, delete records, and export CSV.
- `Settings`: update shop name, export folder, and delete password.

## Search And Check Eligibility

1. Go to `Search`.
2. Choose an active promotion from `Selected Promotion`.
3. Type a search value.
4. Search by vehicle number to find one exact vehicle or prepare a new vehicle record.
5. Search by phone number or owner name to find existing vehicles only.
6. Select a vehicle from `Search Results` when a match appears.
7. Review the eligibility panel before adding usage.

Important:

- `Add Usage` is available only when the selected promotion can be used by that vehicle.
- If no vehicle is found and the search was a vehicle number search, `Add Usage` can be used to create the new vehicle and save the usage in one step.
- If the promotion is inactive, new usage cannot be recorded until that promotion is activated again.

## Add A Usage Record

1. Start from `Search`.
2. Choose the promotion.
3. Search for the vehicle by vehicle number.
4. Click `Add Usage`.
5. Complete the form.

Required fields:

- vehicle number
- promotion
- phone number

Optional fields:

- owner name
- brand
- model
- mileage
- normal price
- discounted price
- amount paid
- notes

Notes:

- PlateGuard prevents the same vehicle from using the same promotion more than once.
- When the vehicle already exists, the vehicle number stays fixed in the add dialog.

## Manage Promotions

1. Go to `Promotions`.
2. Click `Add Promotion` to create a new promotion.
3. Enter the promotion name.
4. Optionally enter description, start date, and end date.
5. Leave `Active promotion` checked if staff should use it immediately.
6. Save the promotion.

You can also:

- use `Edit Promotion` to change promotion details
- use `Activate` or `Deactivate` to control whether new usage can be recorded

## Review History And Records

1. Go to `History`.
2. Use the search box to search by vehicle number, phone, owner, or promotion.
3. Optionally filter by promotion and date range.
4. Select a record to review full details.

Available actions:

- `Edit Record` updates service date and non-key details
- `Delete Record` removes the usage after the correct delete password is entered

Important:

- editing does not change the vehicle number or promotion on an existing record
- deleting a record makes that vehicle eligible for that promotion again

## Export CSV

1. Go to `History`.
2. Apply filters if you only want part of the history.
3. Click `Export Filtered CSV` for the current filtered list.
4. Click `Export All CSV` to export all records.

Export folder behavior:

- if an export folder is saved in `Settings`, PlateGuard exports there
- if no export folder is saved, PlateGuard uses `Documents\\PlateGuard Exports`

## Settings

Use `Settings` to manage:

- `Shop Name`
- `Default Export Folder`
- delete password changes

Rules:

- the export folder must be a full folder path if one is entered
- leaving the export folder blank uses the default Documents export folder

## Delete Password

Deleting a record always requires the delete password.

For a fresh database:

- the development default password is `admin`
- change it from `Settings` before live use

## Troubleshooting

- If `Add Usage` is disabled, check that the promotion is active and the vehicle is eligible.
- If no results appear, verify the search text and try a vehicle number search.
- If export fails, confirm that the export folder exists or that Windows allows writing to that folder.
