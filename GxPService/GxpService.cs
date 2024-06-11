using GxPService.Dto;
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
using System.Text;
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

        protected override void OnStop()
        {
            WriteLog("Service is stopped at " + DateTime.Now);
        }

        private async Task TimerElapsedEventHandlerAsync(object sender, ElapsedEventArgs e)
        {            
            //Fetch Endpoint from HKLM
            apiEndpoint = GetApiEndpointFromRegistry();

            // Make a request to the server to get the latest policies
            string policies = await GetPoliciesFromServer(apiEndpoint);

            // Process the received policies
            ApplyPolicy(policies);

            // Send data to API
            //await SendDataToApi();
        }

        private async Task SendDataToApi()
        {
            var currentUsername = WindowsIdentityHelper.GetLoggedOnUsers().FirstOrDefault();

            // Get machine name
            string machineName = Environment.MachineName;

            // Create data payload
            var requestData = new { Username = currentUsername.Name, MachineName = machineName };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Serialize the requestData object to JSON
                    string jsonRequestData = JsonConvert.SerializeObject(requestData);

                    // Create StringContent from the JSON data
                    var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");

                    // Specify the URI of the API endpoint
                    string apiUrl = "https://yourapi.com/api/usercount"; // Replace with your actual API endpoint

                    // Send POST request to API
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                // Handle error
                Console.WriteLine($"Error sending data to API: {ex.Message}");
            }
        }

        public string GetApiEndpointFromRegistry()
        {
            WriteLog("Fetching API Endpoint from Registry");

            // Open the registry key
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VShield\Service"))
            {
                // Check if the key exists
                if (key != null)
                {
                    // Read the registry value
                    apiEndpoint = key.GetValue("ApiEndpoint").ToString();
                }
            }

            WriteLog("API Endpoint = " + (apiEndpoint ?? "Not Found"));
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
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("Error: " + ex.Message);
                return "";
            }
        }

        private bool IsUserInGroup(string username, List<ClientUserDto> users)
        {
            return users.Any(user => user.User == username);
        }

        public void ApplyPolicy(string policies)
        {
            var registryPolicies = JsonConvert.DeserializeObject<List<RegistryPolicyDto>>(policies);            

            foreach (var registryPolicy in registryPolicies)
            {
                var currentUsername = WindowsIdentityHelper.GetLoggedOnUsers().FirstOrDefault();
                WriteLog($"Current User: {currentUsername.Name}");
                WriteLog($"Current User SID: {currentUsername.Owner.Value}");

                if (currentUsername != null && IsUserInGroup(currentUsername.Name, registryPolicy.Users))
                {
                    WriteLog($"Current user '{currentUsername.Name}' is not authorized for the policy: {registryPolicy}");
                    return;
                }

                foreach (var appliedPolicy in registryPolicy.AppliedPolicies)
                {
                    var registryPath = appliedPolicy.Path;
                    var registryEntry = appliedPolicy.Entry;
                    var registryValue = appliedPolicy.Value;
                    var registryType = appliedPolicy.RegType;
                    var osVersions = appliedPolicy.WindowsOperatingSystem.Split(',')
                                   .Select(value => $"Windows{value.Trim()}")
                                   .ToArray();

                    var operatingSystem = OSVersion.GetOperatingSystem();
                    if (!osVersions.Contains(operatingSystem.ToString()))
                    {
                        WriteLog($"Current OS is not compatible for policy. Policy: {registryEntry}");
                        continue;
                    }

                    WriteLog($"Applying Policy: {registryEntry}, Writing {registryType} value: {registryValue} to: {currentUsername}");
                    WriteToRegistry(registryPath, registryEntry, registryValue, registryType, currentUsername);
                }

                foreach (var removedPolicy in registryPolicy.RemovedPolicies)
                {
                    var registryPath = removedPolicy.Path;
                    var registryEntry = removedPolicy.Entry;
                    var registryValue = removedPolicy.Value;
                    var registryType = removedPolicy.RegType;
                    var osVersions = removedPolicy.WindowsOperatingSystem.Split(',')
                                   .Select(value => $"Windows{value.Trim()}")
                                   .ToArray();

                    var operatingSystem = OSVersion.GetOperatingSystem();
                    if (!osVersions.Contains(operatingSystem.ToString()))
                    {
                        WriteLog($"Current OS is not compatible for policy. Policy: {registryEntry}");
                        continue;
                    }

                    WriteLog($"Removed Policy: {registryEntry}, Writing {registryType} value: {registryValue} to: {currentUsername}");
                    WriteToRegistry(registryPath, registryEntry, registryValue, registryType, currentUsername);
                }
            }
        }

        public void WriteToRegistry(string registryPath, string registryEntry, string registryValue, string registryType, WindowsIdentity currentUsername)
        {         
            try
            {
                string rootKey = @"HKEY_USERS\" + currentUsername.Owner.Value;
                string fullKey = $"{rootKey}\\{registryPath}";

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
                            WriteLog($"Invalid REG_DWORD value: {intValue}");
                        }
                        break;

                    case "REG_STRING":
                        Registry.SetValue(fullKey, registryEntry, registryValue, RegistryValueKind.String);
                        break;

                    case "REG_BINARY":
                        string val = registryValue.Replace("hex:", "");
                        var data = val.Split(',')
                                .Select(x => Convert.ToByte(x, 16))
                                .ToArray();
                        Registry.SetValue(fullKey, registryEntry, data, RegistryValueKind.Binary);
                        break;

                    default:
                        WriteLog($"Unknown registry value type: {registryType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.GetBaseException().Message);
                WriteLog(ex.Message);
            }
        }
    }
}
