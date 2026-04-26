using System;
using System.Diagnostics;

namespace TwilightHelper {
    internal class Program {
        public static void Main(string[] args) {
            if (args.Length == 0) {
                ShowHelp();
                return;
            }

            string processName = "";
            string priorityStr = "High";
            int sleepSeconds = 10;

            for (int i = 0; i < args.Length; i++) {
                string arg = args[i].ToLower();
                switch (arg) {
                    case "-name":
                    case "-n":
                        if (i + 1 < args.Length) processName = args[++i];
                        break;
                    case "-priority":
                    case "-p":
                        if (i + 1 < args.Length) priorityStr = args[++i];
                        break;
                    case "-sleep":
                    case "-s":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int s)) sleepSeconds = s;
                        break;
                    case "-help":
                    case "-h":
                        ShowHelp();
                        return;
                }
            }

            ProcessPriorityClass priority;
            if (!Enum.TryParse(priorityStr, true, out priority)) {
                Console.WriteLine($"Invalid priority: {priorityStr}. Defaulting to High.");
                priority = ProcessPriorityClass.High;
            }

            Console.WriteLine($"Monitoring process: {processName}");
            Console.WriteLine($"Target priority: {priority}");
            Console.WriteLine($"Sleep interval: {sleepSeconds} seconds");

            while (true) {
                SetProcessPriority(processName, priority);
                System.Threading.Thread.Sleep(sleepSeconds * 1000);
            }
        }

        public static void ShowHelp() {
            Console.WriteLine("TwilightHelper Usage:");
            Console.WriteLine("  -name, -n <ProcessName>    The name of the process to monitor");
            Console.WriteLine("  -priority, -p <Priority>   The priority to set (default: High)");
            Console.WriteLine("                             Possible values: Normal, Idle, High, RealTime, BelowNormal, AboveNormal");
            Console.WriteLine("  -sleep, -s <Seconds>       The sleep interval between checks in seconds (default: 5)");
            Console.WriteLine("  -help, -h                  Display this help message");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  TwilightHelper.exe -name dinodday -priority High -sleep 30");
            Console.WriteLine("  TwilightHelper.exe -name dinodday");
        }

        public static void SetProcessPriority(string processName, ProcessPriorityClass priority) {
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
