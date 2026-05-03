GitHub workflow automatic release.
  
### V2.1.0.0
- Added per-day, per-process log files.
- Added timestamped console/log output.
- Included app version in console startup and logs (fallback value: 2.1.0.0)
- Added graceful exit flow control for Ctrl+C, tray Exit, and console close/logoff/shutdown events.
- Can now toggle console visibility via a single left-click on the tray icon.
- Some internal code/project cleanup