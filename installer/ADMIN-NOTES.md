# PlateGuard Admin Notes

## Purpose

This note is for the person maintaining the PlateGuard installation on the business computer.

## Application Data Location

- Default database path: `%LOCALAPPDATA%\PlateGuard\plateguard.db`
- The app creates the database automatically on first run.
- If the database path is not overridden, this is the live data file that must be protected.

Related files that may appear beside the database:

- `plateguard.db-wal`
- `plateguard.db-shm`

## Backup

- Close PlateGuard before taking a manual backup.
- Copy `%LOCALAPPDATA%\PlateGuard\plateguard.db` to a USB drive or another safe folder.
- If present, also copy the matching `-wal` and `-shm` files.
- Keep at least one older backup in addition to the latest backup.
- If the business depends on the data, create a simple backup routine and write down who is responsible.

## Restore

1. Close PlateGuard.
2. Replace the existing `%LOCALAPPDATA%\PlateGuard\plateguard.db` with the backup copy.
3. If backed up, also restore the matching `-wal` and `-shm` files.
4. Start PlateGuard and confirm the expected records are visible.

## Delete Password

- Deleting a usage record makes the vehicle eligible for that promotion again.
- The delete password can be changed from the Settings screen.
- Current development default for fresh databases is `admin`; change it after setup.
- The current password is required before a new one can be saved.
- Store the live password in the business's normal admin records, not inside the app folder.

## Export

- CSV export runs from the History screen.
- If no export folder is saved in Settings, PlateGuard uses `Documents\PlateGuard Exports`.
- If an export folder is entered in Settings, it must be a full folder path.
- Exported files are written as UTF-8 CSV with a timestamped filename.

CSV columns:

- Service Date
- Promotion Name
- Vehicle Number
- Owner Name
- Phone Number
- Brand
- Model
- Mileage
- Normal Price
- Discounted Price
- Amount Paid
- Notes

## First Live Setup

Before the business starts using the app for real:

1. open PlateGuard
2. set the shop name in `Settings`
3. change the delete password in `Settings`
4. create the first active promotion
5. save one sample or real vehicle usage
6. export one CSV successfully

## Record Deletion Effect

Deleting a usage record does not delete the promotion itself.

What it does:

- removes that vehicle and promotion usage pair from history
- allows that same vehicle to use that same promotion again later

Use deletion carefully because it changes eligibility.

## Uninstall Note

- The installer removes the application files.
- User data in `%LOCALAPPDATA%\PlateGuard` should be kept unless you delete it manually on purpose.

## Demo Data Note

This package does not include a separate demo database.

If a demo is needed later:

- create a small separate sample database
- keep it separate from the live business data
