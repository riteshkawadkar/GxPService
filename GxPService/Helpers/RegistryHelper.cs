using GxPService.Dto;
using log4net;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

namespace GxPService.Helpers
{
    public class RegistryHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private DatabaseHelper _databaseHelper;

        public RegistryHelper(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public string GetValueFromRegistry(string valueName, string defaultValue)
        {
            string retrievedValue = defaultValue;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(GlobalConstants.RegistryGxPServiceKey))
                {
                    // Check if the key exists
                    if (key != null)
                    {
                        object value = key.GetValue(valueName);
                        if (value != null)
                        {
                            retrievedValue = value.ToString();
                        }
                    }
                }

                log.Info($"Fetching {valueName} from Registry, {valueName} = {retrievedValue}");
            }
            catch (Exception ex)
            {
                log.Error($"{valueName} Not Found, setting to default {defaultValue} - {ex.Message}");
            }

            return retrievedValue;
        }

        public void WriteToRegistry(string registryName, string registryPath, string registryEntry,
            string registryValue, string registryType, string operatingSystem, string state, string serverTimestamp)
        {
            log.Info($"Updating Policy - '{registryName}'");

            try
            {
                string rootKey = @"HKEY_LOCAL_MACHINE";
                string fullKey = $"{rootKey}\\{registryPath}";

                // Check if the subkey exists, create it if it doesn't
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath, true))
                {
                    if (key == null)
                    {
                        key.CreateSubKey(fullKey, true);
                        log.Info($"WriteToRegistry: Subkey Created - {fullKey}");
                    }

                    // Get the old value if it exists
                    object oldValue = key.GetValue(registryEntry);
                    string oldStringValue = oldValue != null ? oldValue.ToString() : "NA";

                    try
                    {
                        switch (registryType)
                        {
                            case "REG_DWORD":
                                int intValue;
                                if (int.TryParse(registryValue, out intValue))
                                {
                                    Registry.SetValue(fullKey, registryEntry, intValue, RegistryValueKind.DWord);
                                }
                                else
                                {
                                    log.Error($"Invalid REG_DWORD value: {registryValue}");
                                    return;
                                }
                                break;

                            case "REG_STRING":
                                Registry.SetValue(fullKey, registryEntry, registryValue, RegistryValueKind.String);
                                break;

                            case "REG_BINARY":
                                try
                                {
                                    string val = registryValue.Replace("hex:", "");
                                    var data = val.Split(',')
                                            .Select(x => Convert.ToByte(x, 16))
                                            .ToArray();
                                    Registry.SetValue(fullKey, registryEntry, data, RegistryValueKind.Binary);
                                }
                                catch (FormatException ex)
                                {
                                    log.Error($"Invalid REG_BINARY value: {registryValue} - {ex.Message}");
                                    return;
                                }
                                break;

                            default:
                                log.Error($"Unknown registry value type: {registryType}");
                                break;
                        }

                    }
                    catch (UnauthorizedAccessException uex)
                    {
                        log.Error($"Unauthorized Access: Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {uex.Message}");
                        return;
                    }
                    catch (SecurityException sex)
                    {
                        log.Error($"Security Exception: Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {sex.Message}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Exception: Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {ex.Message}");
                        return;
                    }

                    var policyLog = new PolicyLog
                    {
                        Name = registryName,
                        Path = registryPath,
                        Entry = registryEntry,
                        OldValue = oldStringValue,
                        NewValue = registryValue,
                        RegType = registryType,
                        WindowsOperatingSystem = operatingSystem,
                        State = state,
                        Timestamp = DateTime.Now.ToString(),
                        ServerTimestamp = serverTimestamp
                    };

                    _databaseHelper.WriteToDatabase(policyLog);
                }

                log.Info($"Policy {state} : {registryEntry}, Path: {fullKey} Value: {registryValue} ");
            }
            catch (SecurityException sex)
            {
                log.Error($"Security Exception: Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {sex.Message}");
            }
            catch (UnauthorizedAccessException uex)
            {
                log.Error($"Unauthorized Access: Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {uex.Message}");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {ex.Message}");
                log.Error($"Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {ex.StackTrace}");
                log.Error($"Failed to apply policy HKEY_LOCAL_MACHINE: {registryEntry} - {ex.GetBaseException().Message}");
            }

        }
        /*
        public string GetSidFromUserName(string userName)
        {
            try
            {
                NTAccount account = new NTAccount(userName);
                SecurityIdentifier sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                return sid.Value;
            }
            catch (Exception ex)
            {
                log.Warn($"Error obtaining SID for user {userName}: {ex.Message}");
                return null;
            }
        }
        
        public void WriteToRegistryForAllUsers(string registryName, string registryPath, string registryEntry,
            string registryValue, string registryType, string operatingSystem, string state, string appliedRemovedTimestmap)
        {
            try
            {
                // Get all user profiles except the Administrator profile
                string[] userDirectories = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                foreach (string userProfileDir in userDirectories)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(userProfileDir);
                    string userName = dirInfo.Name;
                    *//*
                        if (userName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                        continue; // Skip the Administrator profile
                    *//*

                    // Get the SID from the user name
                    string sid = GetSidFromUserName(userName);
                    if (sid != null)
                    {
                        // Write to the user's registry key
                        log.Info($"Applying Policy for {userName}: {sid}");
                        WriteToUserRegistry(registryName, registryPath, registryEntry, registryValue, registryType, operatingSystem, state, appliedRemovedTimestmap, sid);
                    }
                }

                log.Info($"Applying Policy for : .DEFAULT");
                // Write to the default user registry key
                WriteToUserRegistry(registryName, registryPath, registryEntry, registryValue, registryType, operatingSystem, state, appliedRemovedTimestmap, ".DEFAULT");

            }
            catch (Exception ex)
            {
                log.Error($"Error writing to registry for all users: {ex.Message}");
            }
        }*/

        public void WriteToUserRegistry(string registryName, string registryPath, string registryEntry, string registryValue, string registryType, string operatingSystem, string state, string serverTimestamp)
        {
            var currentUser = WindowsIdentityHelper.GetLoggedOnUsers().FirstOrDefault();
            var sid = currentUser.Owner.Value;
            string rootKey = @"HKEY_USERS";
            string fullKey = $"{rootKey}\\{sid}\\{registryPath}";

            log.Info($"Updating Policy for {currentUser.Name.ToUpper()}- '{registryName}'");

            try
            {
                // Open or create the subkey
                //using (RegistryKey baseKey = Registry.Users.OpenSubKey(sid, true))
                using (RegistryKey baseKey = Registry.Users.OpenSubKey(sid, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ReadKey | RegistryRights.WriteKey | RegistryRights.CreateSubKey))
                {
                    if (baseKey == null)
                    {
                        log.Error($"Failed to open base registry key for User: {currentUser.Name.ToUpper()}");
                        return;
                    }

                    //using (RegistryKey subKey = baseKey.CreateSubKey(registryPath, true))
                    using (RegistryKey subKey = baseKey.CreateSubKey(registryPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        if (subKey == null)
                        {
                            log.Error($"Failed to create or open subkey: {fullKey}");
                            return;
                        }

                        // Get the old value if it exists
                        object oldValue = subKey.GetValue(registryEntry);
                        string oldStringValue = oldValue != null ? oldValue.ToString() : "NA";

                        try
                        {
                            switch (registryType)
                            {
                                case "REG_DWORD":
                                    if (int.TryParse(registryValue, out int intValue))
                                    {
                                        subKey.SetValue(registryEntry, intValue, RegistryValueKind.DWord);
                                    }
                                    else
                                    {
                                        log.Error($"Invalid REG_DWORD value: {registryValue}");
                                        return;
                                    }
                                    break;

                                case "REG_STRING":
                                    subKey.SetValue(registryEntry, registryValue, RegistryValueKind.String);
                                    break;

                                case "REG_BINARY":
                                    try
                                    {
                                        var data = registryValue.Replace("hex:", "").Split(',')
                                            .Select(x => Convert.ToByte(x, 16)).ToArray();
                                        subKey.SetValue(registryEntry, data, RegistryValueKind.Binary);
                                    }
                                    catch (FormatException ex)
                                    {
                                        log.Error($"Invalid REG_BINARY value: {registryValue} - {ex.Message}");
                                        return;
                                    }
                                    break;

                                default:
                                    log.Error($"Unknown registry value type: {registryType}");
                                    break;
                            }

                        }
                        catch (UnauthorizedAccessException uex)
                        {
                            log.Error($"Unauthorized Access: Failed to apply policy HKEY_USERS: {registryEntry} - {uex.Message}");
                            return;
                        }
                        catch (SecurityException sex)
                        {
                            log.Error($"Security Exception: Failed to apply policy HKEY_USERS: {registryEntry} - {sex.Message}");
                            return;
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Exception: Failed to apply policy HKEY_USERS: {registryEntry} - {ex.Message}");
                            return;
                        }

                        var policyLog = new PolicyLog
                        {
                            Name = registryName,
                            Path = registryPath,
                            Entry = registryEntry,
                            OldValue = oldStringValue,
                            NewValue = registryValue,
                            RegType = registryType,
                            WindowsOperatingSystem = operatingSystem,
                            State = state,
                            Timestamp = DateTime.Now.ToString(),
                            ServerTimestamp = serverTimestamp
                        };

                        _databaseHelper.WriteToDatabase(policyLog);
                    }
                }
                log.Info($"Policy {state} : {registryEntry}, Path: {fullKey} Value: {registryValue} ");
            }
            catch (SecurityException sex)
            {
                log.Error($"Security Exception: Failed to apply policy HKEY_USERS: {registryEntry} - {sex.Message}");
            }
            catch (UnauthorizedAccessException uex)
            {
                log.Error($"Unauthorized Access: Failed to apply policy HKEY_USERS: {registryEntry} - {uex.Message}");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to apply policy HKEY_USERS: {registryEntry} - {ex.Message}");
                log.Error($"Failed to apply policy HKEY_USERS: {registryEntry} - {ex.StackTrace}");
            }
        }

    }
}
