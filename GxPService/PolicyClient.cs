using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Threading;

namespace GxPService
{
    internal class PolicyClient
    {
        public void ListenForPolicies()
        {
            // Periodically check with the server for new policies
            while (true)
            {
                // Make a request to the server to get the latest policies
                string policies = GetPoliciesFromServer();

                // Process the received policies
                ApplyPolicy(policies);

                // Sleep for a defined interval before checking again
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }

        private string GetPoliciesFromServer()
        {
            try
            {
                // Implement logic to request policies from the server
                // You can use HTTP, WebSocket, or any other communication mechanism
                // Example: Make an HTTP request to a server endpoint
                using (var client = new WebClient())
                {
                    Helper.WriteToFile("Fetching Policy");
                    return client.DownloadString("https://a670-4-213-118-130.ngrok-free.app/api/policies");
                }
            }
            catch (WebException webEx)
            {
                // Handle web exception (e.g., network error)
                // Log or handle the error appropriately
                Console.WriteLine("Error downloading policies: " + webEx.Message);
                throw new Exception("Error downloading policies: " + webEx.Message);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                // Log or handle the error appropriately
                Console.WriteLine("Error: " + ex.Message);
                throw new Exception("Error: " + ex.Message);
            }
        }

        private void ApplyPolicy(string policies)
        {
            var registryItems = JsonConvert.DeserializeObject<List<Policy>>(policies);

            foreach (var item in registryItems)
            {
                var registryPath = item.Path;
                var registryEntry = item.Entry;
                var registryValue = item.Value;
                RegistryValueKind registryRegType;

                switch (item.RegType)
                {
                    case "REG_DWORD":
                        registryRegType = RegistryValueKind.DWord;
                        break;
                    case "REG_STRING":
                        registryRegType = RegistryValueKind.String;
                        break;
                    case "REG_BINARY":
                        registryRegType = RegistryValueKind.Binary;
                        break;
                    default:
                        // Handle the case when RegType does not match any known types
                        // You could throw an exception, set a default value, or log an error.
                        // For example:
                        throw new ArgumentException("Unknown registry value type: " + item.RegType);
                }

                try
                {                    
                    // Modify the registry as the current user
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            Helper.WriteToFile("Fetching Policy - " + registryEntry);
                            key.SetValue(registryEntry, registryValue, registryRegType);
                        }
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            Helper.WriteToFile("DD" );
                            key.SetValue("DragHeight", "3000", RegistryValueKind.String);
                            key.SetValue("DragHeight", "3000", RegistryValueKind.String);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    Console.WriteLine(ex.Message);
                    Helper.WriteToFile("Error - " + ex.Message);
                }


            }
        }
    }

    public class Policy
    {
        public string PolicyUID { get; set; }
        public string Path { get; set; }
        public string Entry { get; set; }
        public string Value { get; set; }
        public string RegType { get; set; }
        public string OperatingSystem { get; set; }
    }
}
