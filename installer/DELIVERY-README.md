# PlateGuard Delivery

## Contents

- `PlateGuard/` contains the published Windows app.
- `Create-Desktop-Shortcut.ps1` creates a desktop shortcut for the portable build.
- `ADMIN-NOTES.md` explains database location, backup, and delete-password notes.

## Portable Use

1. Copy the full delivery folder to the target machine.
2. Open the `PlateGuard/` folder.
3. Run `PlateGuard.exe`.
4. Optionally run `Create-Desktop-Shortcut.ps1` from PowerShell to create a desktop shortcut.

## Installer Use

If an installer `.exe` is included separately, prefer the installer because it places the app in the user profile and creates shortcuts automatically.

## First Run Check

1. Open PlateGuard.
2. Confirm the app opens as a desktop window.
3. Create or review one promotion.
4. Search for a vehicle and verify history loads.
5. Export a CSV once to confirm the export folder works.
