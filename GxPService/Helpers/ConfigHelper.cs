using GxPService.Dto;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GxPService.Helpers
{
    public class ConfigHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private DatabaseHelper _databaseHelper;
        private static string _filePath;

        public ConfigHelper(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string ahkDirectory = Path.Combine(appDataPath, GlobalConstants.AHKPath);
            if (!Directory.Exists(ahkDirectory))
            {
                Directory.CreateDirectory(ahkDirectory);
                log.Info($"Created directory: {ahkDirectory}");
            }
            else
            {
                log.Info($"Directory already exists: {ahkDirectory}");
            }
            _filePath = Path.Combine(ahkDirectory, GlobalConstants.AHKFileName);

        }


        private Dictionary<string, string> configValues = new Dictionary<string, string>
        {
            { "Cut", "0" },
            { "EmptyRecycleBin", "0" },
            { "Copy", "0" },
            { "Paste", "0" },
            { "Delete", "0" },
            { "CopyTo", "0" },
            { "MoveTo", "0" },
            { "SendTo", "0" },
            { "Rename", "0" },
            { "OpenWith", "0" },
            { "CreateShortcut", "0" },
            { "BurnToDisc", "0" },
            { "Properties", "0" },
            { "Share", "0" },
            { "StartButton", "0" },
            { "DisableRibbonControl", "0" },
            { "AltF4Shortcut", "0" },
            { "AltTabShortcut", "0" },
            { "EscShortcut", "0" },
            { "BackspaceShortcut", "0" },
            { "TabShortcut", "0" },
            { "CtrlShortcut", "0" },
            { "AltShortcut", "0" },
            { "AltGrShortcut", "0" },
            { "FunctionKeysShortcut", "0" },
            { "MouseMiddleButton", "0" }
        };


        private static string ConfigContentTemplate = @"[ContextSettings]
Cut = {Cut}
EmptyRecycleBin = {EmptyRecycleBin}
Copy = {Copy}
Paste = {Paste}
Delete = {Delete}
CopyTo = {CopyTo}
MoveTo = {MoveTo}
SendTo = {SendTo}
Rename = {Rename}
OpenWith = {OpenWith}
CreateShortcut = {CreateShortcut}
BurnToDisc = {BurnToDisc}
Properties = {Properties}
Share = {Share}
StartButton = {StartButton}
DisableRibbonControl = {DisableRibbonControl}
AltF4Shortcut = {AltF4Shortcut}
AltTabShortcut = {AltTabShortcut}
EscShortcut = {EscShortcut}
BackspaceShortcut = {BackspaceShortcut}
TabShortcut = {TabShortcut}
CtrlShortcut = {CtrlShortcut}
AltShortcut = {AltShortcut}
AltGrShortcut = {AltGrShortcut}
FunctionKeysShortcut = {FunctionKeysShortcut}
MouseMiddleButton = {MouseMiddleButton}
";


        public static void SaveConfig(string filePath, Dictionary<string, string> configValues)
        {
            string configContent = ConfigContentTemplate;

            foreach (var key in configValues.Keys)
            {
                configContent = configContent.Replace("{" + key + "}", configValues[key]);
            }

            // Encrypt the config content
            string encryptedContent = AhkHelper.Base64Encode(configContent);

            File.WriteAllText(filePath, encryptedContent);
        }

        public void WriteToConfigFile(string ioName, string ioValue, string state, string serverTimestamp)
        {
            if (configValues.ContainsKey(ioName))
            {
                configValues[ioName] = ioValue;
            }
            else
            {
                log.Warn($"Unknown configuration name: {ioName}");
                return;
            }

            ConfigHelper.SaveConfig(_filePath, configValues);
            log.Info("Configuration file encrypted and saved as " + _filePath);

            // Call the method to get the last AppliedRemovedTimestamp for IO policy
            DateTime? lastTimestamp = _databaseHelper.GetLastServerTimestampForIOPolicy(ioName);

            IOLog ioLog = new IOLog
            {
                Name = ioName,
                Value = ioValue,
                State = state,
                Timestamp = DateTime.Now.ToString(),
                ServerTimestamp = serverTimestamp,
            };

            // Write to the database only if the AppliedRemovedTimestamp has changed
            if (lastTimestamp == null || lastTimestamp.ToString() != ioLog.Timestamp)
            {
                _databaseHelper.WriteToDatabase(ioLog);
                log.Info($"Log entry added to the database: {ioName} = {ioValue} ({state}) and saved as {_filePath}");
            }
            else
            {
                log.Info($"No changes detected for IO policy: {ioName}. Skipping database write.");
            }
        }


    }
}
