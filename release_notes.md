GitHub workflow automatic release.
  
### V1.2.0.0
- Embedded app icon ("bbwulf.ico") as an assembly resource.
- Added default values for interactive mode: process name ("dinodday"), sleep timer (10s), priority (High), and autohide (true).
- Added per-day, per-process log files.
- Added timestamped console/log output.
- Included app version in console startup and logs (fallback value: 1.2.0.0)
- Added graceful exit flow control for Ctrl+C, tray Exit, and console close/logoff/shutdown events.
- Can now toggle console visibility via a single left-click on the tray icon.
- Some internal code/project cleanup