using System;
using System.Diagnostics;

namespace TwilightHelper {
    internal class Program {
        
        const ProcessPriorityClass DEFAULT_PRIORITY = ProcessPriorityClass.High;
        const int DEFAULT_SLEEP_SECONDS = 10;
        
        public static void Main(string[] args) {
            bool shouldWait = IsLaunchedFromManualShortcut();
            if (args.Length == 0) {
                ShowHelp(shouldWait);
                return;
            }

            string processName = "";
            ProcessPriorityClass priority = Program.DEFAULT_PRIORITY;
            int sleepSeconds = Program.DEFAULT_SLEEP_SECONDS;

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

            if (string.IsNullOrWhiteSpace(processName)) {
                ShowError("Process name is required. Use -name or -n.", shouldWait);
                return;
            }

            Console.WriteLine($"Monitoring process: {processName}");
            Console.WriteLine($"Target priority: {priority}");
            Console.WriteLine($"Sleep interval: {sleepSeconds} seconds");

            while (true) {
                SetProcessPriority(processName, priority);
                System.Threading.Thread.Sleep(sleepSeconds * 1000);
            }
        }

        private static void ShowError(string message, bool waitAtEnd = false) {
            Console.WriteLine();
            Console.WriteLine("ERROR:");
            Console.WriteLine("    " + message);
            Console.WriteLine();
            ShowHelp(waitAtEnd);
        }

        private static bool IsLaunchedFromManualShortcut() {
            try {
                return Console.CursorLeft == 0 && Console.CursorTop == 0;
            } catch {
                return false;
            }
        }

        private static void ShowHelp(bool waitAtEnd = false) {
            Console.WriteLine( "TwilightHelper Usage:");
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

        private static void SetProcessPriority(string processName, ProcessPriorityClass priority) {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes) {
                try {
                    if (process.PriorityClass != priority) {
                        process.PriorityClass = priority;
                        Console.WriteLine($"Successfully set priority of {process.ProcessName} (ID: {process.Id}) to {priority}");
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to set priority for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                }
            }
        }
    }
}
