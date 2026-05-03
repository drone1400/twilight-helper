using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace TwilightHelper {
    internal class Program {
        
        const string DEFAULT_PROCESS_NAME = "dinodday";
        const ProcessPriorityClass DEFAULT_PRIORITY = ProcessPriorityClass.High;
        const int DEFAULT_SLEEP_SECONDS = 10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);
        
        const uint CTRL_CLOSE_EVENT = 2;
        const uint CTRL_LOGOFF_EVENT = 5;
        const uint CTRL_SHUTDOWN_EVENT = 6;

        private delegate bool ConsoleCtrlDelegate(uint ctrlType);

        private static volatile bool __isExiting = false;
        private static readonly ManualResetEvent __exitEvent = new ManualResetEvent(false);
        private static readonly object __logLock = new object();
        private static readonly object __exitLogLock = new object();
        private static string __logFilePath;
        private static bool __hasLoggedExit = false;
        private static string __monitoredProcessName;
        private static ConsoleCtrlDelegate __consoleCtrlHandler;

        private static string TimestampNow() {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private static string GetAppVersion() {
            // try to read version from assembly info
            string assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString();
            if (!string.IsNullOrWhiteSpace(assemblyVersion) && assemblyVersion != "0.0.0.0") {
                return assemblyVersion;
            }

            // return hardcoded version value, remember to always sync this when updating the .csproj file!!!
            return "1.2.0.0";
        }

        private static void InitializeLogging(string processName) {
            try {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logsDirectory = Path.Combine(baseDirectory, "logs");
                Directory.CreateDirectory(logsDirectory);

                string safeProcessName = SanitizeFileName(processName);
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                Program.__logFilePath = Path.Combine(logsDirectory, $"{date}_{safeProcessName}.log");
            } catch {
                // if something goes wrong, we won't have a log file
                Program.__logFilePath = null;
            }
        }

        private static string SanitizeFileName(string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return "process";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++) {
                for (int j = 0; j < invalidChars.Length; j++) {
                    if (chars[i] == invalidChars[j]) {
                        chars[i] = '_';
                        break;
                    }
                }
            }

            string sanitized = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "process" : sanitized;
        }

        private static void LogTimestamped(string message) {
            string entry = $"[{TimestampNow()}] {message}";
            try {
                Console.WriteLine(entry);
            } catch {
                // Ignore console write failures during shutdown.
            }

            if (string.IsNullOrWhiteSpace(Program.__logFilePath)) {
                return;
            }

            lock (Program.__logLock) {
                try {
                    File.AppendAllText(Program.__logFilePath, entry + Environment.NewLine);
                } catch {
                    // Ignore file write failures during shutdown.
                }
            }
        }

        private static void LogExitOnce(string processName) {
            lock (Program.__exitLogLock) {
                if (Program.__hasLoggedExit) {
                    return;
                }

                Program.__hasLoggedExit = true;
            }

            LogTimestamped($"TwilightHelper exiting. No longer monitoring process '{processName}'.");
        }

        private static bool HandleConsoleClose(uint ctrlType) {
            if (ctrlType == CTRL_CLOSE_EVENT || ctrlType == CTRL_LOGOFF_EVENT || ctrlType == CTRL_SHUTDOWN_EVENT) {
                string ctrlTypeName = ctrlType == CTRL_CLOSE_EVENT
                    ? "CTRL_CLOSE_EVENT"
                    : ctrlType == CTRL_LOGOFF_EVENT
                        ? "CTRL_LOGOFF_EVENT"
                        : "CTRL_SHUTDOWN_EVENT";

                RequestExit(Program.__monitoredProcessName, $"console close event {ctrlTypeName} [{ctrlType}]");
            }

            // Intentionally return false so Windows continues its default close/logoff/shutdown handling.
            // We still request a graceful exit above as a best-effort cleanup/logging signal.
            return false;
        }

        private static void RequestExit(string processName, string reason) {
            if (Program.__isExiting) {
                return;
            }

            Program.__isExiting = true;
            Program.__exitEvent.Set();
            LogTimestamped($"Exit requested ({reason}).");
            LogExitOnce(processName);
        }

        private static bool IsLaunchedFromManualShortcut() {
            try {
                return Console.CursorLeft == 0 && Console.CursorTop == 0;
            } catch {
                return false;
            }
        }

        public static void Main(string[] args) {
            bool shouldWait = IsLaunchedFromManualShortcut();
            string processName = "";
            ProcessPriorityClass priority = Program.DEFAULT_PRIORITY;
            int sleepSeconds = Program.DEFAULT_SLEEP_SECONDS;

            if (args.Length == 0) {
                // Interactive mode
                Console.WriteLine($"--- TwilightHelper v{GetAppVersion()} Interactive Mode ---");
                Console.WriteLine("Tip: Use the -help argument to learn about command-line options for automation.");

                // 1. Process Name (Required)
                while (string.IsNullOrWhiteSpace(processName)) {
                    Console.Write($"Enter process name ({DEFAULT_PROCESS_NAME}): ");
                    processName = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(processName)) {
                        processName = DEFAULT_PROCESS_NAME;
                        break;
                    }
                }

                // 2. Sleep Timer (Optional, default provided)
                while (true) {
                    Console.Write($"Enter sleep timer in seconds ({DEFAULT_SLEEP_SECONDS}): ");
                    string input = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(input)) {
                        sleepSeconds = DEFAULT_SLEEP_SECONDS;
                        break;
                    }
                    if (int.TryParse(input, out int s) && s > 0) {
                        sleepSeconds = s;
                        break;
                    }
                    Console.WriteLine("Error: Please enter a valid integer greater than 0.");
                }

                // 3. Priority (Optional, default provided)
                while (true) {
                    Console.Write($"Enter priority ({DEFAULT_PRIORITY}): ");
                    string input = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(input)) {
                        priority = DEFAULT_PRIORITY;
                        break;
                    }
                    if (Enum.TryParse(input, true, out ProcessPriorityClass p) && Enum.IsDefined(typeof(ProcessPriorityClass), p)) {
                        priority = p;
                        break;
                    }
                    Console.WriteLine($"Error: Invalid priority. Valid values: {string.Join(", ", Enum.GetNames(typeof(ProcessPriorityClass)))}");
                }
                Console.WriteLine();
            } else {
                // parse the input args
                for (int i = 0; i < args.Length; i++) {
                    string arg = args[i].ToLower();
                    switch (arg) {
                        case "-name":
                        case "-n":
                            if (i + 1 < args.Length) {
                                processName = args[++i];
                                if (string.IsNullOrWhiteSpace(processName)) {
                                    ShowError("Invalid argument! Process name can not be empty or whitespace!", shouldWait);
                                    return;
                                }
                            } else {
                                ShowError("Missing argument value for -name", shouldWait);
                                return;
                            }
                            break;
                        case "-priority":
                        case "-p":
                            if (i + 1 < args.Length) {
                                string priorityStr = args[++i];
                                if (!Enum.TryParse(priorityStr, true, out priority) || !Enum.IsDefined(typeof(ProcessPriorityClass), priority)) {
                                    ShowError($"Invalid argument! Priority = {priorityStr}... Could not parse priority name or value is not a valid priority.", shouldWait);
                                    return;
                                }
                            } else {
                                ShowError("Missing argument value for -priority", shouldWait);
                                return;
                            }
                            break;
                        case "-sleep":
                        case "-s":
                            if (i + 1 < args.Length) {
                                string sleepStr = args[++i];
                                if (int.TryParse(sleepStr, out int s)) {
                                    if (s <= 0) {
                                        ShowError($"Invalid argument! Sleep = {s}... Sleep interval must be greater than 0!", shouldWait);
                                        return;
                                    }
                                    sleepSeconds = s;
                                } else {
                                    ShowError($"Invalid argument! Sleep = {sleepStr}... Could not parse sleep interval as an integer.", shouldWait);
                                    return;
                                }
                            } else {
                                ShowError("Missing argument value for -sleep", shouldWait);
                                return;
                            }
                            break;
                        case "-help":
                        case "-h":
                            ShowHelp(shouldWait);
                            return;
                        default:
                            ShowError($"Unrecognized argument '{arg}'", shouldWait);
                            return;
                    }
                }

                // application was invoked with command line arguments, but without specifying the process name
                // show error message / help text, and exit
                if (string.IsNullOrWhiteSpace(processName)) {
                    ShowError("Process name is required. Use -name or -n.", shouldWait);
                    return;
                }
            }

            // inform the user of the monitoring parameters
            Console.WriteLine($"Monitoring process: {processName}");
            Console.WriteLine($"Target priority: {priority}");
            Console.WriteLine($"Sleep interval: {sleepSeconds} seconds");

            // initialize logging
            InitializeLogging(processName);
            
            Program.__monitoredProcessName = processName;
            
            // Exiting via ConsoleControlHandler 
            Program.__consoleCtrlHandler = HandleConsoleClose;
            SetConsoleCtrlHandler(Program.__consoleCtrlHandler, true);
            
            // Exiting via cancel request (Ctrl+C)
            Console.CancelKeyPress += (_, e) => {
                if (!Program.__isExiting) {
                    e.Cancel = true;
                    RequestExit(processName, "Ctrl+C");
                } else {
                    e.Cancel = false;
                }
            };
            
            // Logging message on Procss Exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => 
                LogExitOnce(processName);

            // Inform user about CTRL+C cancel shortcut
            Console.WriteLine("Press Ctrl+C to exit cleanly from the terminal.");

            // display app version
            LogTimestamped($"TwilightHelper v{GetAppVersion()} launched. Monitoring process '{processName}' with priority '{priority}'.");

            // execute monitoring loop
            while (!Program.__isExiting) {
                SetProcessPriority(processName, priority);
                if (Program.__exitEvent.WaitOne(sleepSeconds * 1000)) {
                    break;
                }
            }

            // we are done here!
            LogExitOnce(processName);
        }
        
        /// <summary>
        /// Shows in the console an error message related to an invalid argument value, then displays the help text.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="waitAtEnd">if true, waits for key input after showing help text</param>
        private static void ShowError(string message, bool waitAtEnd = false) {
            Console.WriteLine();
            Console.WriteLine("ERROR:");
            Console.WriteLine("    " + message);
            Console.WriteLine();
            ShowHelp(waitAtEnd);
        }

        /// <summary>
        /// Shows the help text in the console.
        /// </summary>
        /// <param name="waitAtEnd">if true, waits for key input after showing help text</param>
        private static void ShowHelp(bool waitAtEnd = false) {
            Console.WriteLine($"TwilightHelper v{GetAppVersion()} usage:");
            Console.WriteLine( "  -name, -n <ProcessName>");
            Console.WriteLine( "      The name of the process to monitor");
            Console.WriteLine($"  -priority, -p <Priority>");
            Console.WriteLine($"      The priority to set (default: {Program.DEFAULT_PRIORITY})");
            Console.WriteLine( "      Possible values:");
            Console.WriteLine( "          Idle, BelowNormal, Normal, AboveNormal, High, RealTime");
            Console.WriteLine( "      NOTE: setting priority to RealTime is not recommended and");
            Console.WriteLine( "          might require elevated privileges");
            Console.WriteLine($"  -sleep, -s <Seconds>");
            Console.WriteLine($"      The sleep interval between checks in seconds (default: {Program.DEFAULT_SLEEP_SECONDS})");
            Console.WriteLine( "  -help, -h");
            Console.WriteLine( "      Display this help message");
            Console.WriteLine( "  Runtime hotkeys (terminal launch):");
            Console.WriteLine( "      Ctrl+C = clean exit");
            Console.WriteLine( "      Press Ctrl+C again during shutdown to force terminate if needed");
            Console.WriteLine();
            Console.WriteLine( "Example:");
            Console.WriteLine( "  TwilightHelper.exe -name dinodday -priority High -sleep 10");
            Console.WriteLine( "  TwilightHelper.exe -name dinodday");

            if (waitAtEnd) {
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        ///  the main execution logic for setting the priority of a process 
        /// </summary>
        /// <param name="processName">the process name to look out for</param>
        /// <param name="priority">the priority to set</param>
        private static void SetProcessPriority(string processName, ProcessPriorityClass priority) {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes) {
                try {
                    if (process.PriorityClass != priority) {
                        process.PriorityClass = priority;
                        LogTimestamped($"Successfully set priority of {process.ProcessName} (ID: {process.Id}) to {priority}");
                    }
                }
                catch (Exception ex) {
                    LogTimestamped($"Failed to set priority for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                }
            }
        }
    }
}
