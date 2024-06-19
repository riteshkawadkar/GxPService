using GxPService.Dto;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.AccessControl;

namespace GxPService.Helpers
{
    public class CustomCodeHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private DatabaseHelper _databaseHelper;

        public CustomCodeHelper(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public void EdgeSettings(string registryName, string state, string serverTimestamp)
        {
            string hostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
            string entry = "127.0.0.1 edge://*\n";

            try
            {

                if (!File.Exists(hostsFilePath))
                {
                    log.Error("Hosts file not found.");
                    return;
                }

                string[] lines = File.ReadAllLines(hostsFilePath);
                bool entryExists = false;

                // Check if the entry exists
                foreach (string line in lines)
                {
                    if (line.Trim().Equals(entry.Trim()))
                    {
                        entryExists = true;
                        break;
                    }
                }

                if (state == "Applied")
                {
                    if (!entryExists)
                    {
                        using (StreamWriter sw = File.AppendText(hostsFilePath))
                        {
                            sw.WriteLine(entry);
                        }
                    }
                    else
                    {
                        log.Info("Entry already exists in the hosts file.");
                    }
                }
                else
                {
                    if (entryExists)
                    {
                        // Remove the entry by creating a new list without the entry
                        List<string> newLines = new List<string>();
                        foreach (string line in lines)
                        {
                            if (!line.Trim().Equals(entry.Trim()))
                            {
                                newLines.Add(line);
                            }
                        }

                        // Write the updated lines back to the hosts file
                        File.WriteAllLines(hostsFilePath, newLines.ToArray());
                        log.Info("Entry removed from the hosts file.");
                    }
                    else
                    {
                        log.Info("Entry not found in the hosts file.");
                    }
                }


                var policyLog = new PolicyLog
                {
                    Name = registryName,
                    Path = "",
                    Entry = state == "Applied" ? entry : "",
                    OldValue = "",
                    NewValue = "",
                    RegType = "",
                    WindowsOperatingSystem = "",
                    State = state,
                    Timestamp = DateTime.Now.ToString(),
                    ServerTimestamp = serverTimestamp
                };

                _databaseHelper.WriteToDatabase(policyLog);
                log.Info($"Policy Updated - Block Edge Settings");

            }
            catch (UnauthorizedAccessException)
            {
                log.Error("Access to the hosts file is denied. Please run the application as Administrator.");
            }
            catch (Exception ex)
            {
                log.Error($"An error occurred: {ex.Message}");
            }
        }




        public void ManageDesktopPermissions(string registryName, string action, string serverTimestamp)
        {
            List<string> userProfiles = GetUserProfiles();

            foreach (string profile in userProfiles)
            {
                string userDesktopPath = Path.Combine(profile, "Desktop");
                string oneDrivePath = Path.Combine(profile, "OneDrive");
                string oneDriveBusinessPathPattern = Path.Combine(profile, "OneDrive - ");

                if (action == "Applied")
                {
                    SetDenyPermissions(userDesktopPath);
                    SetDenyPermissions(Path.Combine(oneDrivePath, "Desktop"));
                    foreach (string oneDriveBusinessPath in Directory.GetDirectories(profile, "OneDrive - *"))
                    {
                        SetDenyPermissions(Path.Combine(oneDriveBusinessPath, "Desktop"));
                    }
                }
                else if (action == "Removed")
                {
                    RemoveDenyPermissions(userDesktopPath);
                    RemoveDenyPermissions(Path.Combine(oneDrivePath, "Desktop"));
                    foreach (string oneDriveBusinessPath in Directory.GetDirectories(profile, "OneDrive - *"))
                    {
                        RemoveDenyPermissions(Path.Combine(oneDriveBusinessPath, "Desktop"));
                    }
                }

                var policyLog = new PolicyLog
                {
                    Name = registryName,
                    Path = "",
                    Entry = "",
                    OldValue = "",
                    NewValue = "",
                    RegType = "",
                    WindowsOperatingSystem = "",
                    State = action,
                    Timestamp = DateTime.Now.ToString(),
                    ServerTimestamp = serverTimestamp
                };

                _databaseHelper.WriteToDatabase(policyLog);
                log.Info($"Desktop Restriction - {action}");
            }

            log.Info($"Action '{action}' completed on all valid desktop locations.");
        }

        static List<string> GetUserProfiles()
        {
            List<string> userProfiles = new List<string>();
            string[] excludedProfiles = new string[] {
                "Default", "Public", "All Users", "DefaultAppPool",
                "NetworkService", "LocalService", "SystemProfile",
                "MSSQL", "SQLTELEMETRY", "LansweeperLocalDbService",
                "Classic .NET AppPool", "AppPool", "NetService", "LocalSystem"
            };

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserProfile WHERE Special = FALSE");

            foreach (ManagementObject profile in searcher.Get())
            {
                string localPath = (string)profile["LocalPath"];
                string profileName = Path.GetFileName(localPath);

                if (!excludedProfiles.Any(excluded => profileName.Contains(excluded)))
                {
                    userProfiles.Add(localPath);
                }
            }

            return userProfiles;
        }

        static void SetDenyPermissions(string path)
        {
            if (Directory.Exists(path))
            {
                DirectorySecurity security = Directory.GetAccessControl(path);
                FileSystemAccessRule denyRule = new FileSystemAccessRule("Everyone", FileSystemRights.CreateFiles | FileSystemRights.WriteData | FileSystemRights.Delete, AccessControlType.Deny);
                security.AddAccessRule(denyRule);
                Directory.SetAccessControl(path, security);

                log.Info($"Permissions set on {path}");
            }
            else
            {
                log.Warn($"Path not found: {path}");
            }
        }

        static void RemoveDenyPermissions(string path)
        {
            if (Directory.Exists(path))
            {
                DirectorySecurity security = Directory.GetAccessControl(path);
                FileSystemAccessRule denyRule = new FileSystemAccessRule("Everyone", FileSystemRights.CreateFiles | FileSystemRights.WriteData | FileSystemRights.Delete, AccessControlType.Deny);
                security.RemoveAccessRuleSpecific(denyRule);
                Directory.SetAccessControl(path, security);

                log.Info($"Permissions reverted on {path}");
            }
            else
            {
                log.Warn($"Path not found: {path}");
            }
        }



    }
}
