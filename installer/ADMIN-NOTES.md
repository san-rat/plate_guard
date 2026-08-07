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

## Cloud Sync

Cloud sync copies records to a Supabase project so the data survives loss of the computer.

- Sync runs automatically every 6 hours, and once at startup if more than 6 hours have passed.
- The first sync after setup uploads every existing record, so it takes longer than later runs.
- Sync is not a backup. Keep the local backup routine above regardless.
- Settings come from `%LOCALAPPDATA%\PlateGuard\cloudsync.json`, written by the installer supplied for this shop.
- The app reads that file once at startup, so PlateGuard must be restarted after the file changes.

To check that sync is working, open `Settings` and look at the Sync panel:

- `Cloud sync completed` with a recent Last Synced time means everything is working.
- `Cloud sync is unconfigured` means the settings file is missing or incomplete; the panel states which.
- `Cloud sync failed` means the settings are present but the upload did not succeed, usually no internet or a changed password.

If the Supabase password is changed, the settings file is out of date. Either install the rebuilt setup file for this shop, or edit `cloudsync.json` and restart PlateGuard.

The settings file contains the account password in plain text. Anyone who can sign in to this Windows user account can read it, so the Windows login on this computer is what protects it.

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

- The installer removes the application files and the `cloudsync.json` settings file.
- The database in `%LOCALAPPDATA%\PlateGuard` is kept unless you delete it manually on purpose.
- Reinstalling from the setup file supplied for this shop restores the cloud sync settings.

## Demo Data Note

This package does not include a separate demo database.

If a demo is needed later:

- create a small separate sample database
- keep it separate from the live business data
