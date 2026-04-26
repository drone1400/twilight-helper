# TwilightHelper
A simple command line tool for automatically setting a process priority. Made it for a friend playing dino d day on an old computer.

Check the [Changelog](changelog.md) for details on version history.

# How to use
The program takes the following arguments:
* `-name` or `-n` followed by `<ProcessName>` - The name of the process to monitor
* `-priority` or `-p` followed by `<Priority>` - The priority to set (default: High); Possible values: 
  * `Idle` - Specifies that the threads of this process run only when the system is idle, such as a screen saver. The threads of the process are preempted by the threads of any process running in a higher priority class.
  * `BelowNormal` - Specifies that the process has priority above Idle but below Normal.
  * `Normal` - Specifies that the process has no special scheduling needs.
  * `AboveNormal` - Specifies that the process has priority above Normal but below High.
  * `High` (Default value) - Specifies that the process performs time-critical tasks that must be executed immediately, such as the Task List dialog, which must respond quickly when called by the user, regardless of the load on the operating system. The threads of the process preempt the threads of normal or idle priority class processes.
  * `RealTime` - Specifies that the process has the highest possible priority. (NOTE: this will probably not work unless you run it as administrator)
* `-sleep` or `-s` followed by `<Seconds>` - The sleep interval between checks in seconds (default: 10)
* `-help` or  `-h` - Display this help message

Note: If the application is launched directly (e.g., by double-clicking the .exe) rather than from an existing terminal, it will wait for a key press after displaying help or errors so you can read the output.

## Example Usage
`TwilightHelper.exe -name dinodday -priority High -sleep 30` - this will monitor for the dinodday process and check every 30 seconds if the process needs to be set to High priority.
`TwilightHelper.exe -name dinodday` - this will monitor for the dinodday process and check every 10 seconds if the process needs to be set to High priority. 


If you simply run the program without any arguments, it should display a help message like this:
```
TwilightHelper Usage:
  -name, -n <ProcessName>
      The name of the process to monitor
  -priority, -p <Priority>
      The priority to set (default: High)
      Possible values:
          Idle, BelowNormal, Normal, AboveNormal, High, RealTime
      NOTE: setting priority to RealTime is not recommended and
          might require elevated privileges
  -sleep, -s <Seconds>
      The sleep interval between checks in seconds (default: 10)
  -help, -h
      Display this help message

Example:
  TwilightHelper.exe -name dinodday -priority High -sleep 10
  TwilightHelper.exe -name dinodday
```

