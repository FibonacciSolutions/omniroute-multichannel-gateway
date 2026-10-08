using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OmniRoute.Infrastructure.Interfaces;

namespace OmniRoute.Infrastructure.Services
{
    public class NtcNcellSmsService : INtcNcellSmsService
    {
        // Fully qualified namespace eliminates any ambiguous type or namespace errors
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;

        public NtcNcellSmsService(System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> SendViaCarrierAsync(string to, string message, string senderId)
        {
            try
            {
                string directTelecomUrl = "https://DIRECT_TELECOM_GATEWAY_ENDPOINT_HERE";

                var client = _httpClientFactory.CreateClient();

                var payload = new
                {
                    userId = "mechi_nagar",
                    senderId = senderId,
                    recipient = to,
                    body = message
                };

                var jsonContent = new System.Net.Http.StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(directTelecomUrl, jsonContent);

                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Fallback mechanism triggers if connection or endpoint details are missing
                return false;
            }
        }
    }
}