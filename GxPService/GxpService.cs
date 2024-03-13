using Microsoft.Win32;
using Newtonsoft.Json;
using OSVersionExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;

namespace GxPService
{
    public partial class GxpService : ServiceBase
    {       
        private static string tempLogFilePath, apiEndpoint;

        public GxpService()
        {
            InitializeComponent();
        }

        public void WriteLog(string message)
        {
            File.AppendAllText(tempLogFilePath, message + Environment.NewLine);
        }

        protected override void OnStart(string[] args)
        {
            string now = DateTime.Now.ToString("yyyyMMddHHmmss");
            tempLogFilePath = Path.Combine(Path.GetTempPath(), $"GxP_{now}.log");

            WriteLog("Service is started");

            Timer timer = new Timer();
            timer.Interval = 1 * 60 * 1000; // 1 minutes
            timer.Elapsed += async (sender, e) => await TimerElapsedEventHandlerAsync(sender, e);
            // This line immediately invokes the TimerElapsedEventHandlerAsync method once without waiting for the timer to elapse.
            // It uses _ to discard the result of the method call.
            _ = TimerElapsedEventHandlerAsync(null, null);
            timer.Start();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
            WriteLog("Service is stopped at " + DateTime.Now);
        }

        private async Task TimerElapsedEventHandlerAsync(object sender, ElapsedEventArgs e)
        {            
            //Fetch Endpoint from HKLM
            var apiEndpoint = GetApiEndpointFromRegistry();

            // Make a request to the server to get the latest policies
            string policies = await GetPoliciesFromServer(apiEndpoint);

            // Process the received policies
            ApplyPolicy(policies);
        }


        public string GetApiEndpointFromRegistry()
        {
            WriteLog("Fetching API Endpoint from Registry");

            // Open the registry key
            using (RegistryKey key = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\VShield\Service", "", null) as RegistryKey)
            {
                // Check if the key exists
                if (key != null)
                {
                    // Read the registry value
                    apiEndpoint = key.GetValue("ApiEndpoint").ToString();
                }
            }

            WriteLog("API Endpoint = " + apiEndpoint);
            return apiEndpoint;
        }

        public async Task<string> GetPoliciesFromServer(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        WriteLog( "Fetching Policy");
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        string message = $"HTTP request failed with status code {response.StatusCode}";
                        WriteLog($"Error: {message}");
                        throw new HttpRequestException(message);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("Error: " + ex.Message);
                throw new Exception("Error: " + ex.Message);
            }
        }

        public void ApplyPolicy(string policies)
        {
            var registryPolicies = JsonConvert.DeserializeObject<List<PolicyDto>>(policies);

            foreach (var registryPolicy in registryPolicies)
            {
                var registryPath = registryPolicy.Path;
                var registryEntry = registryPolicy.Entry;
                var registryValue = registryPolicy.Value;
                var osVersions = registryPolicy.OperatingSystem.Split(',')
                               .Select(value => $"Windows{value.Trim()}")
                               .ToArray();

                var operatingSystem = OSVersion.GetOperatingSystem();
                if (!osVersions.Contains(operatingSystem.ToString()))
                {
                    WriteLog($"Current OS is not compatible for policy. Policy ID: {registryPolicy.PolicyUID}");
                    throw new Exception($"Current OS is not compatible for policy. Policy ID: {registryPolicy.PolicyUID}");
                }


                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                string sidString = identity.User.Value;
                WriteLog("Current User SID: " + sidString);
                SecurityIdentifier sid = new SecurityIdentifier(sidString);
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                WriteLog("Current User Name: " + account.Value);

                string rootKey = @"HKEY_USERS\" + sid;
                string fullKey = $"{rootKey}\\registryPath";


                switch (registryPolicy.RegType)
                {
                    case "REG_DWORD":
                        int intValue;
                        if (int.TryParse(registryValue, out intValue))
                        {
                            Registry.SetValue(fullKey, registryEntry, intValue, RegistryValueKind.DWord);
                        }
                        else
                        {
                            WriteLog($"Invalid REG_DWORD value: {intValue}");
                        }
                        break;
                    case "REG_STRING":
                        WriteLog("Applying String value...");
                        Registry.SetValue(fullKey, registryEntry, registryValue, RegistryValueKind.String);
                        break;
                    case "REG_BINARY":
                        WriteLog($"Applying Binary value...");
                        string val = registryValue.Replace("hex:", "");
                        WriteLog($"Val: {val}");
                        var data = val.Split(',')
                                .Select(x => Convert.ToByte(x, 16))
                                .ToArray();
                        WriteLog($"Value: {data}");
                        Registry.SetValue(fullKey, registryEntry, data, RegistryValueKind.Binary);
                        WriteLog($"Value written");
                        break;
                    default:
                        // Handle the case when RegType does not match any known types
                        // You could throw an exception, set a default value, or log an error.
                        // For example:
                        throw new ArgumentException("Unknown registry value type: " + registryPolicy.RegType);
                }
            }
        }
    }
}
