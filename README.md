# DanW Backup Manager
Bring Google Cloud data down to your local machine on a cron schedule so it can be backed up. Execution is logged to the local machine with optional emails (requires SMTP.)

Supports:

 - Google Docs (with conversion)
 - Google Contacts
 - Google Photos (without location data due to API limitations)
 - Google Calendar

------

## Build

- In `BackupManagerLibrary\Constants.cs`, update the following constants from [Google Cloud API Credentials](https://console.cloud.google.com/apis/credentials):
  - `Constants.GoogleAccess.ClientId`
  - `Constants.GoogleAccess.ClientSecret`
  - `Constants.GoogleAccess.ApiClientSecretJson`
- In `BackupManagerWiX\Defines.wxi`, update the `*GUID` defines with GUIDs, (e.g. `{222B5A14-B494-402C-9B10-1C115D1E0296}`

------

## Deploy

### Windows

1. Update the application version in `BackupManagerDaemon\appsettings.json` 
2. Publish the `BackupManagerDaemon` Windows and MacOS profiles
3. Update the `BuildVersion` and `BuildGUID` in `BackupManagerWiX\Defines.wxi` 
4. Build `BackupManagerWiX` in `x64`
5. Run the `bin/Release/Windows x64/*.msi` to update the local machine
6. If MacOS will be desired, then commit the generated Mac binary to git and push it (to your own repo)
7. Update the user settings in `AppData\Local\DanW\Backup Manager\Settings\UserSettings.json` 
8. Rerun the application from the Start menu after updating settings (prior daemons should be killed)

### MacOS

1. On a Mac, follow the instructions in the `BackupManagerXamarin\Setup\MacOs\*` files to install the permissions git hooks
2. Pull the updated MacOS binary from the Windows instructions
3. Build the `BackupManagerXamarin` project in `Install` configuration
4. Run the `bin/Release/PkgBuild/*` file to install to the local machine (or AirDrop to a different machine)
5. Update the user settings in `~/.Local/share/DanW/Backup Manager/Settings/UserSettings.json`
6. Rerun the application from Spotlight after updating settings (prior daemons should be killed)

------

*Notes:*

- *If installing on new Mac, sometimes you have to create `~/.local/share` directories manually*
- *Don't modify the MacOS resources on a PC*
  - *`BackupManagerPkgBuild`*
  - *`BackupManagerXamarin`*
  - *`Setup/MacOS/*`*
- *If you accidentally run the PC app as an admin, then you can run the following in an elevated command prompt to kill the daemon*
  *`taskkill /im "DanW Backup Manager.exe" /f`*



