# PlateGuard Admin Notes

## Database Location

- Default database path: `%LOCALAPPDATA%\PlateGuard\plateguard.db`
- The app creates the database automatically on first run.

## Backup

- Close PlateGuard before taking a manual backup.
- Copy `%LOCALAPPDATA%\PlateGuard\plateguard.db` to a USB drive or another safe folder.
- If present, also copy the matching `-wal` and `-shm` files.

## Delete Password

- Deleting a usage record makes the vehicle eligible for that promotion again.
- The delete password can be changed from the Settings screen.
- Current development default for fresh databases is `admin`; change it after setup.

## Export

- CSV export runs from the History screen.
- If no export folder is saved in Settings, PlateGuard uses `Documents\PlateGuard Exports`.

## Uninstall Note

- The installer removes the application files.
- User data in `%LOCALAPPDATA%\PlateGuard` should be kept unless you delete it manually on purpose.
