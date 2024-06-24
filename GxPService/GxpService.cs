using GxPService.Dto;
using GxPService.Helpers;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using OSVersionExtension;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace GxPService
{
    public partial class GxPService : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static string _apiEndpoint, _timeIntervalInMinutes;
        private readonly HttpClient _httpClient;
        private string _token;

        private DatabaseHelper _databaseHelper;
        private RegistryHelper _registryHelper;
        private ConfigHelper _configHelper;
        private CustomCodeHelper _customCodeHelper;

        private Timer _timer;


        // Import the MessageBox function from user32
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        public GxPService()
        {
            InitializeComponent();

            // Initialize HttpClient with custom certificate validation
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(httpClientHandler);

            // Update log4net configuration with dynamic log file name
            UpdateLog4netConfig();


            // Initialize helpers
            _databaseHelper = new DatabaseHelper();
            _registryHelper = new RegistryHelper(_databaseHelper);
            _configHelper = new ConfigHelper(_databaseHelper);
            _customCodeHelper = new CustomCodeHelper(_databaseHelper);

            // Fetch endpoint from registry
            _apiEndpoint = _registryHelper.GetValueFromRegistry("ApiEndpoint", "Not Found");
            _timeIntervalInMinutes = _registryHelper.GetValueFromRegistry("TimeInterval", "2");

            _httpClient.BaseAddress = new Uri(_apiEndpoint);
        }

        public static bool IsMachineConnectedToDomain()
        {
            try
            {
                var domain = Domain.GetComputerDomain(); // Attempt to get the computer's domain
                log.Info("Machine is connected to Domain - " + domain.Name.ToString());
                return true; // If successful, the machine is connected to a domain
            }
            catch (Exception ex)
            {
                log.Error("Machine is not connected to Domain - " + ex.Message);
                return false; // If exception occurs, the machine is not connected to a domain
            }
        }

        public void OnDebug(string[] args)
        {
            OnStart(args);
        }


        public static void UpdateLog4netConfig()
        {
            // Set up a property for the temporary directory
            string now = DateTime.Now.ToString("yyyyMMddHHmmss");
            string tempLogFile = $"GxP_{now}";

            // Update log4net properties with dynamic file name
            GlobalContext.Properties["TempFile"] = tempLogFile;

            // Reconfigure log4net with updated properties
            string log4netConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            XmlConfigurator.Configure(new FileInfo(log4netConfigFilePath));

        }

        protected override async void OnStart(string[] args)
        {
            log.Info("Service is started");

            try
            {
                if (AhkHelper.IsTestExeIntegrityCompromised())
                {
                    AhkHelper.ShowLogOffMessageAndLogOffUser();
                }
                else
                {
                    AhkHelper.RunAllExecutables();
                }

                // Add whitelisted USB SerialNumbers based on domain connection status
                if (IsMachineConnectedToDomain())
                {
                    _token = await new AuthHelper(_httpClient).AuthenticateAsync();

                    _timer = new Timer
                    {
                        Interval = int.Parse(_timeIntervalInMinutes) * 60 * 1000
                    };
                    _timer.Elapsed += async (sender, e) => await HandlePeriodicTasks(sender, e);

                    // This line immediately invokes the TimerElapsedEventHandlerAsync method once without waiting for the timer to elapse.
                    // It uses _ to discard the result of the method call.
                    _ = HandlePeriodicTasks(null, null);
                    _timer.Start();
                }
                else
                {
                    await HandleOfflineTask();
                }


            }
            catch (Exception ex)
            {
                log.Error($"Error during OnStart: {ex.Message}");
                log.Error($"Error during OnStart: {ex.StackTrace}");
            }

        }

        protected override void OnStop()
        {
            log.Info("Service is stopped at " + DateTime.Now);
            _timer?.Stop();
        }


        private async Task HandlePeriodicTasks(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Check if authentication has not been performed yet
                if (string.IsNullOrEmpty(_token))
                {
                    _token = await new AuthHelper(_httpClient).AuthenticateAsync();
                }

                // Check if authentication has not been performed yet
                if (string.IsNullOrEmpty(_token))
                {
                    return;
                }

                AhkHelper.CheckAllExecutablesRunning();

                // Make a request to the server to get the latest policies
                string encryptedPolicies = await GetPoliciesByMachineNameAsync(_apiEndpoint);

                // Process the received policies
                if (!string.IsNullOrEmpty(encryptedPolicies))
                {
                    var encryptedPoliciesObj = JsonConvert.DeserializeObject<EncryptedPoliciesDto>(encryptedPolicies);
                    var policies = EncryptionHelper.Decrypt(encryptedPoliciesObj.Response);
                    var registryPolicy = JsonConvert.DeserializeObject<RegistryPolicyDto>(policies);
                    await UpdatePolicies(registryPolicy);
                }
            }
            catch (Exception ex)
            {
                log.Error("HandlePeriodicTasks" + ex.Message);
                log.Error("HandlePeriodicTasks" + ex.StackTrace);
            }

        }

        private async Task HandleOfflineTask()
        {
            try
            {
                AhkHelper.CheckAllExecutablesRunning();

                // Make a request to the server to get the latest policies
                string encryptedPolicies = GetPoliciesFromConfiguration();

                // Process the received policies
                if (!string.IsNullOrEmpty(encryptedPolicies))
                {
                    var policies = EncryptionHelper.Decrypt(encryptedPolicies);
                    var registryPolicy = JsonConvert.DeserializeObject<RegistryPolicyDto>(policies);
                    await UpdatePolicies(registryPolicy);
                }
            }
            catch (Exception ex)
            {
                log.Error("HandleOfflineTask" + ex.Message);
                log.Error("HandleOfflineTask" + ex.StackTrace);
            }

        }

        private string GetPoliciesFromConfiguration()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string policyDirectory = Path.Combine(appDataPath, GlobalConstants.DatabasePath);

            string usbConfigFilePath = Path.Combine(policyDirectory, GlobalConstants.PolicyFileName);
            if (!File.Exists(usbConfigFilePath))
            {
                log.Error("Policies config file not found at" + usbConfigFilePath);
                return null;
            }

            string cipherContent = File.ReadAllText(usbConfigFilePath);
            string decryptedContent = EncryptionHelper.Decrypt(cipherContent);

            if (string.IsNullOrEmpty(decryptedContent))
            {
                log.Error("Decrypted content is empty.");
                return null;
            }

            return decryptedContent;
        }

        public async Task<string> GetPoliciesByMachineNameAsync(string url)
        {
            url += "policies/machine/" + Environment.MachineName;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    log.Info("Fetching Policy");
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    log.Error($"HTTP request failed with status code {response.StatusCode}");
                    return string.Empty;
                }

            }
            catch (Exception ex)
            {
                log.Error("Failed to fetch policies - Error: " + ex.Message);
                log.Error("Failed to fetch policies - Error: " + ex.StackTrace);
                return string.Empty;
            }
        }

        public async Task SendNotificationPolicyApplied()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            try
            {
                // Prepare data to send
                var policyUpdateNotification = new NotificationDto
                {
                    MachineName = Environment.MachineName,
                    IsApplied = true,
                    TimeStamp = DateTime.Now
                };

                // Serialize object to JSON
                var json = JsonConvert.SerializeObject(policyUpdateNotification);

                // Create HTTP request content
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send POST request to the authentication endpoint
                HttpResponseMessage response = await _httpClient.PostAsync("notification/notification", content);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Read response content
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // Deserialize response JSON to anonymous object
                    var responseData = JsonConvert.DeserializeAnonymousType(responseBody, new { Success = false, Token = "" });

                    if (responseData.Success)
                    {
                        // Authentication successful, return the JWT token
                        log.Info("Notification sent successfully");
                    }
                    else
                    {
                        log.Error($"HTTP Post request failed with status code {response.StatusCode}");
                        return;
                    }
                }
                else
                {
                    // Handle unsuccessful response
                    log.Error($"HTTP Post request failed with status code {response.StatusCode}");
                    return;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                log.Error($"Error while sending notification to server: {ex.Message}");
                return;
            }

        }

        public async Task UpdatePolicies(RegistryPolicyDto registryPolicy)
        {
            // Retrieve the last applied policy details from the database
            DateTime? lastUpdatedServerTimestamp = _databaseHelper.GetLastServerTimestamp();
            DateTime currentServerTimestamp = registryPolicy.ServerTimeStamp;

            // Truncate to seconds
            DateTime? truncatedLastUpdatedServerTimestamp = lastUpdatedServerTimestamp?.AddTicks(-(lastUpdatedServerTimestamp.Value.Ticks % TimeSpan.TicksPerSecond));
            DateTime truncatedCurrentServerTimestamp = currentServerTimestamp.AddTicks(-(currentServerTimestamp.Ticks % TimeSpan.TicksPerSecond));

            // Log detailed DateTime information for debugging
            log.Info($"Truncated Last Updated Server Timestamp: {(truncatedLastUpdatedServerTimestamp.HasValue ? truncatedLastUpdatedServerTimestamp.Value.ToString("o") : "null")}");
            log.Info($"Truncated Current Server Timestamp: {truncatedCurrentServerTimestamp.ToString("o")}");

            // Determine if a new policy should be applied
            bool shouldApplyPolicy = truncatedLastUpdatedServerTimestamp == null || truncatedCurrentServerTimestamp > truncatedLastUpdatedServerTimestamp.Value;

            if (!shouldApplyPolicy)
            {
                log.Info("No new policy added, skipping updating policy.");
                return;
            }
            else
            {
                log.Info("New policy detected, updating policy.");
            }



            foreach (var appliedPolicy in registryPolicy.AppliedPolicies)
            {
                log.Info("");
                log.Info("--------------------***--------------------");
                var registryName = appliedPolicy.Name;
                var registryPath = appliedPolicy.Path;
                var registryEntry = appliedPolicy.Entry;
                var registryValue = appliedPolicy.Value;
                var registryType = appliedPolicy.RegType;
                var osVersions = appliedPolicy.WindowsOperatingSystem.Split(',')
                                .Select(value => value.Trim())
                                .Select(value => value.Replace(" ", ""))
                                .ToList();
                var serverTimestamp = registryPolicy.ServerTimeStamp.ToString();
                var currentOperatingSystem = OSVersion.GetOperatingSystem().ToString();
                if (!osVersions.Contains(currentOperatingSystem))
                {
                    log.Warn($"Current OS: {currentOperatingSystem} is not compatible for policy. Policy: {registryEntry}, {appliedPolicy.WindowsOperatingSystem}");
                    continue;
                }

                if (registryType == "IO")
                {
                    _configHelper.WriteToConfigFile(registryEntry, registryValue, "Applied", serverTimestamp);
                }
                else if (registryType == "CustomCode")
                {
                    switch (registryName)
                    {
                        case "Block Edge Settings":
                            _customCodeHelper.EdgeSettings(registryName, "Applied", serverTimestamp);
                            break;

                        case "Restrict Desktop":
                            _customCodeHelper.ManageDesktopPermissions(registryName, "Applied", serverTimestamp);
                            break;

                        default:
                            break;
                    }

                }
                else
                {
                    if (registryPath.StartsWith("HKEY_LOCAL_MACHINE"))
                    {
                        registryPath = registryPath.Replace("HKEY_LOCAL_MACHINE\\", "");
                        _registryHelper.WriteToRegistry(registryName,
                                                    registryPath,
                                                    registryEntry,
                                                    registryValue,
                                                    registryType,
                                                    currentOperatingSystem,
                                                    "Applied",
                                                    serverTimestamp);
                    }
                    else if (registryPath.StartsWith("HKEY_CURRENT_USER"))
                    {
                        registryPath = registryPath.Replace("HKEY_CURRENT_USER\\", "");
                        _registryHelper.WriteToUserRegistry(registryName,
                                                    registryPath,
                                                    registryEntry,
                                                    registryValue,
                                                    registryType,
                                                    currentOperatingSystem,
                                                    "Applied",
                                                    serverTimestamp);
                    }
                }
            }

            foreach (var removedPolicy in registryPolicy.RemovedPolicies)
            {
                log.Info("");
                log.Info("--------------------***--------------------");
                var registryName = removedPolicy.Name;
                var registryPath = removedPolicy.Path;
                var registryEntry = removedPolicy.Entry;
                var registryValue = removedPolicy.Value;
                var registryType = removedPolicy.RegType;
                var osVersions = removedPolicy.WindowsOperatingSystem.Split(',')
                                .Select(value => value.Trim())
                                .Select(value => value.Replace(" ", ""))
                                .ToList();
                var serverTimestamp = registryPolicy.ServerTimeStamp.ToString();
                var currentOperatingSystem = OSVersion.GetOperatingSystem().ToString();
                if (!osVersions.Contains(currentOperatingSystem))
                {
                    log.Warn($"Current OS: {currentOperatingSystem} is not compatible for policy. Policy: {registryEntry}, {removedPolicy.WindowsOperatingSystem}");
                    continue;
                }

                if (registryType == "IO")
                {
                    _configHelper.WriteToConfigFile(registryEntry, registryValue, "Removed", serverTimestamp);
                }
                else if (registryType == "CustomCode")
                {
                    switch (registryName)
                    {
                        case "Block Edge Settings":
                            _customCodeHelper.EdgeSettings(registryName, "Removed", serverTimestamp);
                            break;

                        case "Restrict Desktop":
                            _customCodeHelper.ManageDesktopPermissions(registryName, "Removed", serverTimestamp);
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    if (registryPath.StartsWith("HKEY_LOCAL_MACHINE"))
                    {
                        registryPath = registryPath.Replace("HKEY_LOCAL_MACHINE\\", "");
                        _registryHelper.WriteToRegistry(registryName,
                                                    registryPath,
                                                    registryEntry,
                                                    registryValue,
                                                    registryType,
                                                    currentOperatingSystem,
                                                    "Removed",
                                                    serverTimestamp);
                    }
                    else if (registryPath.StartsWith("HKEY_CURRENT_USER"))
                    {
                        registryPath = registryPath.Replace("HKEY_CURRENT_USER\\", "");
                        _registryHelper.WriteToUserRegistry(registryName,
                                                    registryPath,
                                                    registryEntry,
                                                    registryValue,
                                                    registryType,
                                                    currentOperatingSystem,
                                                    "Removed",
                                                    serverTimestamp);
                    }
                }
            }


            // Show the log off message
            MessageBox(IntPtr.Zero, "Policies have been updated, system restart is required.", "Please Restart!", 0);

            //restart all the executables
            AhkHelper.KillAllExecutables();
            AhkHelper.RunAllExecutables();

            //update server
            await SendNotificationPolicyApplied();
        }



    }
}
