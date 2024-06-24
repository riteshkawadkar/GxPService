using GxPService.Dto;
using log4net;
using Microsoft.Win32;
using OSVersionExtension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GxPService.Helpers
{
    public class AhkHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly List<ExecutableInfo> ExecutableInfos = new List<ExecutableInfo>
        {
            new ExecutableInfo
            {
                Name = "kproc",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "kproc.exe"),
                OSVersions = new List<string> { "WindowsXP", "Windows7", "Windows8", "Windows10", "Windows11" },
                Architectures = new List<string> { "x86", "x64" },
                Hash = GlobalConstants.KprocHash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },
            new ExecutableInfo
            {
                Name = "mproc",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "mproc.exe"),
                OSVersions = new List<string> { "WindowsXP", "Windows7", "Windows8", "Windows10", "Windows11" },
                Architectures = new List<string> { "x86", "x64" },
                Hash = GlobalConstants.MprocHash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },
            new ExecutableInfo
            {
                Name = "qat",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "qat.exe"),
                OSVersions = new List<string> { "Windows7", "Windows8", "Windows10" },
                Architectures = new List<string> { "x86", "x64" },
                Hash = GlobalConstants.QatHash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },
            new ExecutableInfo
            {
                Name = "ribbon",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "ribbon.exe"),
                OSVersions = new List<string> { "Windows8", "Windows10" },
                Architectures = new List<string> { "x86", "x64" },
                Hash = GlobalConstants.RibbonHash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },
            new ExecutableInfo
            {
                Name = "ribbon7",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "ribbon7.exe"),
                OSVersions = new List<string> { "Windows7" },
                Architectures = new List<string> { "x86", "x64" },
                Hash = GlobalConstants.Ribbon7Hash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },
            new ExecutableInfo
            {
                Name = "ribbon11",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "ribbon11.exe"),
                OSVersions = new List<string> { "Windows11" },
                Architectures = new List<string> { "x64" },
                Hash = GlobalConstants.Ribbon11Hash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },
            new ExecutableInfo
            {
                Name = "rns",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "rns.exe"),
                OSVersions = new List<string> { "WindowsXP", "Windows7", "Windows8", "Windows10", "Windows11" },
                Architectures = new List<string> { "x86", "x64" },
                Hash = GlobalConstants.RnsHash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },
            new ExecutableInfo
            {
                Name = "StartShell",
                Path = Path.Combine(GlobalConstants.AgentsExecutablePath, "StartShell.exe"),
                OSVersions = new List<string> { "WindowsXP", "Windows7", "Windows8", "Windows10", "Windows11" },
                Architectures = new List<string> { "x86", "x64" },
                Hash = GlobalConstants.StartShellHash,
                SecretCode = GlobalConstants.AgentsRunnerCode
            },

        };


        public static bool IsTestExeIntegrityCompromised()
        {
            foreach (var exeInfo in ExecutableInfos)
            {
                if (!File.Exists(exeInfo.Path))
                {
                    log.Error($"Original {exeInfo.Path} file not found.");
                    return true;
                }

                using (var stream = File.OpenRead(exeInfo.Path))
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(stream);
                        var hashString = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

                        if (!string.Equals(hashString, exeInfo.Hash, StringComparison.OrdinalIgnoreCase))
                        {
                            log.Error($"The current hash {hashString} of {exeInfo.Name} does not match the original hash {exeInfo.Hash}. Possible file modification.");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void RunAllExecutables()
        {
            var operatingSystem = OSVersion.GetOperatingSystem();
            string architecture = GetOSArchitecture();

            foreach (var exeInfo in ExecutableInfos)
            {
                if (exeInfo.OSVersions.Contains(operatingSystem.ToString()) && exeInfo.Architectures.Contains(architecture))
                {
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(GlobalConstants.RegistryAgentsTrustedPathKey + exeInfo.Name, true))
                        {
                            if (key == null)
                            {
                                key.CreateSubKey(GlobalConstants.RegistryAgentsTrustedPathKey + exeInfo.Name, true);
                            }

                            key.SetValue("TrustedLauncher", "true");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Failed to add TrustedLauncher {exeInfo.Path}: {ex.Message}");
                        return;
                    }

                    try
                    {
                        var isLaunched = ApplicationLauncher.CreateProcessInConsoleSession($"{exeInfo.Path} {exeInfo.SecretCode}", false);
                        if (isLaunched)
                        {
                            log.Info($"{exeInfo.Name} started successfully.");
                        }
                        else
                        {
                            log.Error($"{exeInfo.Name} failed to start.");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Failed to start process {exeInfo.Path}: {ex.Message}");
                    }

                }
            }
        }

        public static void CheckAllExecutablesRunning()
        {
            var operatingSystem = OSVersion.GetOperatingSystem();
            string architecture = GetOSArchitecture();

            foreach (var exeInfo in ExecutableInfos)
            {
                if (exeInfo.OSVersions.Contains(operatingSystem.ToString()) && exeInfo.Architectures.Contains(architecture))
                {
                    try
                    {
                        string exeName = Path.GetFileNameWithoutExtension(exeInfo.Name);
                        if (!IsProcessRunning(exeName))
                        {
                            log.Warn($"{exeName}.exe is not running, attempting to restart it.");
                            try
                            {
                                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(GlobalConstants.RegistryAgentsTrustedPathKey + exeInfo.Name, true))
                                {
                                    if (key == null)
                                    {
                                        key.CreateSubKey(GlobalConstants.RegistryAgentsTrustedPathKey + exeInfo.Name, true);
                                    }

                                    key.SetValue("TrustedLauncher", "true");
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error($"Failed to set TrustedLauncher {exeName}.exe: {ex.Message}");
                                log.Error($"Failed to set TrustedLauncher {exeName}.exe: {ex.StackTrace}");

                                // Log off the user if the executable fails to restart
                                ShowLogOffMessageAndLogOffUser();
                            }

                            try
                            {
                                var isLaunched = ApplicationLauncher.CreateProcessInConsoleSession($"{exeInfo.Path} {exeInfo.SecretCode}", false);
                                if (isLaunched)
                                {
                                    log.Info($"{exeInfo.Name} restarted successfully.");
                                }
                                else
                                {
                                    log.Error($"{exeInfo.Name} failed to restart.");

                                    // Log off the user if the executable fails to restart
                                    ShowLogOffMessageAndLogOffUser();
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error($"Failed to restart {exeName}.exe: {ex.Message}");
                                log.Error($"Failed to restart {exeName}.exe: {ex.StackTrace}");

                                // Log off the user if the executable fails to restart
                                ShowLogOffMessageAndLogOffUser();
                            }
                        }
                        else
                        {
                            log.Info($"Process {exeName} is running.");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Failed to check process {exeInfo.Path}: {ex.Message}");
                        ShowLogOffMessageAndLogOffUser();
                    }
                }
            }
        }

        private static bool IsProcessRunning(string processName)
        {
            var runningProcesses = Process.GetProcessesByName(processName);
            return runningProcesses.Any();
        }

        public static void KillAllExecutables()
        {
            foreach (var exeInfo in ExecutableInfos)
            {
                // Get all processes with the specified name
                Process[] processes = Process.GetProcessesByName(exeInfo.Name);

                // Kill each process
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }
        }

        private static string GetOSArchitecture()
        {
            return Environment.Is64BitOperatingSystem ? "x64" : "x86";
        }

        public void LogOffUser()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/l",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                log.Info("User logged off successfully.");
            }
            catch (Exception ex)
            {
                log.Error("Failed to log off user: " + ex.Message);
                log.Error("Failed to log off user: " + ex.StackTrace);
            }
        }

        // Import the MessageBox function from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        public static void ShowLogOffMessageAndLogOffUser()
        {
            try
            {
                // Show the log off message
                MessageBox(IntPtr.Zero, "The system will log off now due to a critical error.", "Logging Off", 0);

                // Wait for a few seconds to allow the user to read the message
                Task.Delay(5000).Wait();

                /*// Log off the user
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/l",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });*/
                log.Info("User logged off successfully.");
            }
            catch (Exception ex)
            {
                log.Error("Failed to log off user: " + ex.Message);
                log.Error("Failed to log off user: " + ex.StackTrace);
            }
        }

        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedText)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedText);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
