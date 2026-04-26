using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TwilightHelper {
    internal class Program {
        
        const ProcessPriorityClass DEFAULT_PRIORITY = ProcessPriorityClass.High;
        const int DEFAULT_SLEEP_SECONDS = 10;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        private static NotifyIcon trayIcon;
        private static bool isExiting = false;
        
        public static void Main(string[] args) {
            bool shouldWait = IsLaunchedFromManualShortcut();
            string processName = "";
            ProcessPriorityClass priority = Program.DEFAULT_PRIORITY;
            int sleepSeconds = Program.DEFAULT_SLEEP_SECONDS;
            bool autohide = false;

            if (args.Length == 0) {
                // Interactive mode
                Console.WriteLine("--- TwilightHelper Interactive Mode ---");
                Console.WriteLine("Tip: Use the -help argument to learn about command-line options for automation.");

                // 1. Process Name (Optional in interactive, default: dinodday)
                while (string.IsNullOrWhiteSpace(processName)) {
                    Console.Write("Enter process name (dinodday): ");
                    processName = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(processName)) {
                        processName = "dinodday";
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

                // 4. Autohide (Optional, default: true)
                while (true) {
                    Console.Write("Autohide to tray? (y/n) [y]: ");
                    string input = Console.ReadLine()?.Trim().ToLower();
                    if (string.IsNullOrEmpty(input) || input == "y" || input == "yes") {
                        autohide = true;
                        break;
                    }
                    if (input == "n" || input == "no") {
                        autohide = false;
                        break;
                    }
                    Console.WriteLine("Error: Please enter 'y' or 'n'.");
                }
                Console.WriteLine();
            } else {
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
                        case "-autohide":
                        case "-ah":
                            autohide = true;
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
            }

            Console.WriteLine($"Monitoring process: {processName}");
            Console.WriteLine($"Target priority: {priority}");
            Console.WriteLine($"Sleep interval: {sleepSeconds} seconds");
            Console.WriteLine($"Autohide: {autohide}");

            SetupTrayIcon(processName);
            if (autohide) {
                HideConsole();
            }

            while (!isExiting) {
                SetProcessPriority(processName, priority);
                Thread.Sleep(sleepSeconds * 1000);
            }

            trayIcon.Dispose();
        }

        private static void HideConsole() {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        }

        private static void SetupTrayIcon(string processName) {
            Thread trayThread = new Thread(() => {
                Icon appIcon = SystemIcons.Application;
                try {
                    // Try to load from embedded resources
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    string resourceName = "TwilightHelper.res.bbwulf.ico";
                    using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                        if (stream != null) {
                            appIcon = new Icon(stream);
                        }
                    }
                } catch {
                    // Fallback to default if icon loading fails
                }

                trayIcon = new NotifyIcon {
                    Icon = appIcon,
                    Text = $"TwilightHelper - Monitoring {processName}",
                    Visible = true
                };

                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Toggle Window", null, (s, e) => {
                    var handle = GetConsoleWindow();
                    if (IsWindowVisible(handle)) {
                        ShowWindow(handle, SW_HIDE);
                    } else {
                        ShowWindow(handle, SW_SHOW);
                    }
                });
                contextMenu.Items.Add("Exit", null, (s, e) => {
                    isExiting = true;
                    Application.Exit();
                });

                trayIcon.ContextMenuStrip = contextMenu;
                Application.Run();
            });

            trayThread.SetApartmentState(ApartmentState.STA);
            trayThread.Start();
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
            Console.WriteLine( "  -autohide, -ah");
            Console.WriteLine( "      Automatically hide the console window to the system tray on startup");
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
