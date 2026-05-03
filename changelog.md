### V1.2.0.1
- fixed AOT publishing for net10
- removed a forgotten and unused WinForms dependency in csproj after porting code over from V2.1

### V1.2.0.0
- Embedded app icon ("bbwulf.ico") as an assembly resource.
- Added default values for interactive mode: process name ("dinodday"), sleep timer (10s), priority (High), and autohide (true).
- Added per-day, per-process log files.
- Added timestamped console/log output.
- Included app version in console startup and logs (fallback value: 2.1.0.0)
- Added graceful exit flow control for Ctrl+C, tray Exit, and console close/logoff/shutdown events.
- Can now toggle console visibility via a single left-click on the tray icon.
- Some internal code/project cleanup

### V1.1.0.0
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