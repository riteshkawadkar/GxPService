using GxPService.Dto;
using log4net;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GxPService.Helpers
{
    public class AuthHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly HttpClient _httpClient;

        public AuthHelper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> AuthenticateAsync()
        {
            var token = string.Empty;
            try
            {
                // Prepare data to send
                var machineDto = new MachineDto
                {
                    HostName = Environment.MachineName
                };

                // Serialize object to JSON
                var json = JsonConvert.SerializeObject(machineDto);

                // Create HTTP request content
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send POST request to the authentication endpoint
                HttpResponseMessage response = await _httpClient.PostAsync("auth/authenticate", content);



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
                        log.Info("Authentication successful");
                        return responseData.Token;
                    }
                    else
                    {
                        // Authentication failed
                        log.Error("Authentication failed: Unauthorized");
                        return token;
                    }
                }
                else
                {
                    // Handle unsuccessful response
                    log.Error($"Failed to authenticate: {response.StatusCode}");
                    return token;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                log.Error($"Error during authentication: {ex.Message}");
                return token;
            }
        }

    }
}
