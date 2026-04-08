# PlateGuard Delivery

## Contents

- `PlateGuard/` contains the published Windows app.
- `USER-GUIDE.md` explains the normal staff workflow.
- `ADMIN-NOTES.md` explains setup, backup, restore, export, and delete-password maintenance.
- `FIRST-LIVE-SETUP-CHECKLIST.md` is the go-live checklist for installation day.
- `Create-Desktop-Shortcut.ps1` creates a desktop shortcut for the portable build.

## Portable Use

1. Copy the full delivery folder to the target machine.
2. Open the `PlateGuard/` folder.
3. Run `PlateGuard.exe`.
4. Review `USER-GUIDE.md` and `ADMIN-NOTES.md`.
5. Optionally run `Create-Desktop-Shortcut.ps1` from PowerShell to create a desktop shortcut.

## Installer Use

If an installer `.exe` is included separately, prefer the installer because it places the app in the user profile and creates shortcuts automatically.

## Recommended First Documents To Open

1. `USER-GUIDE.md`
2. `ADMIN-NOTES.md`
3. `FIRST-LIVE-SETUP-CHECKLIST.md`

## Quick First Run Check

1. Open PlateGuard.
2. Confirm the app opens as a desktop window.
3. Go to `Settings` and set the shop name.
4. Go to `Settings` and change the default delete password.
5. Create the first active promotion.
6. Search by vehicle number and save one sample or real usage record.
7. Export a CSV once from `History / Records`.
