using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OmniRoute.Core.Configurations;
using OmniRoute.Infrastructure.Interfaces;

namespace OmniRoute.Infrastructure.Services
{
    public class LocalSmsClient : ISmsClient
    {
        private readonly HttpClient _httpClient;
        private readonly SmsGatewayOptions _options;

        public LocalSmsClient(HttpClient httpClient, IOptions<SmsGatewayOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            if (!string.IsNullOrEmpty(_options.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }
        }

        public async Task<(bool IsSuccess, string? ProviderMessageId, string? ErrorReason)> SendSmsAsync(
            string recipientPhone,
            string messageText)
        {
            string cleanPhone = recipientPhone.Replace("+977", "").Replace(" ", "").Trim();

            var formFields = new Dictionary<string, string>
            {
                { "token", _options.ApiToken },
                { "from", _options.SenderId },
                { "to", cleanPhone },
                { "text", messageText }
            };

            var content = new FormUrlEncodedContent(formFields);

            try
            {
                var response = await _httpClient.PostAsync("", content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string providerId = Guid.NewGuid().ToString();
                    return (true, providerId, null);
                }
                else
                {
                    return (false, null, $"Carrier Gateway Connection Error: {response.StatusCode} - {responseString}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"SMS Infrastructure Processing Exception: {ex.Message}");
            }
        }
    }
}