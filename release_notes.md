GitHub workflow automatic release.
  
### V2.0.0
- Note: Native AOT compilation is currently disabled for .NET 10 due to WinForms (Tray Icon) incompatibility.
- Embedded system tray icon ("bbwulf.ico") as an assembly resource.
- Updated system tray icon to use custom "bbwulf.ico" and set it as the application icon.
- Added system tray integration: the application now hides the console window and runs from the system tray.
- Added a context menu to the tray icon with an "Exit" option and a "Toggle Window" command.
- Added `-autohide` (`-ah`) parameter to automatically hide the console window on startup.
- Added autohide option to the interactive input mode.
- Added default values for interactive mode: process name ("dinodday"), sleep timer (10s), priority (High), and autohide (true).
- Rebranded versioning to V2.0.0.0.