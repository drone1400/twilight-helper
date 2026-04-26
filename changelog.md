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

### V1.1.0
- Added interactive mode: if no arguments are passed, the program prompts the user for configuration.

### V1.0.2.1
- added net10 with ahead of time compilation to project target frameworks

### V1.0.2.0
- Added terminal detection: the application now waits for a key press before exiting if it was launched directly (e.g., from Explorer) to ensure help/error messages are readable.

### V1.0.1.0
- Added sanity checks for parameter inputs
- Cleaned up help message

### V1.0.0.0
- Initial version