using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OmniRoute.Core.Configurations;
using OmniRoute.Infrastructure.Interfaces;

namespace OmniRoute.Infrastructure.Services
{
    public class WhatsAppClient : IWhatsAppClient
    {
        private readonly HttpClient _httpClient;
        private readonly WhatsAppOptions _options;

        public WhatsAppClient(HttpClient httpClient, IOptions<WhatsAppOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            // Configure mandatory authorization context headers for Meta Infrastructure
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.PermanentAccessToken);
        }

        public async Task<(bool IsSuccess, string? ProviderMessageId, string? ErrorReason)> SendTemplateMessageAsync(
            string recipientPhone,
            string metaTemplateName,
            string languageCode,
            Dictionary<string, string> textParameters)
        {
            // Clean phone number format to remove characters like '+' or spaces for Meta compliance (e.g., 97798XXXXXXXX)
            string cleanPhone = recipientPhone.Replace("+", "").Replace(" ", "").Trim();

            // Construct payload components according to Meta's strict JSON structural framework
            var componentParameters = new List<object>();
            foreach (var param in textParameters.Values)
            {
                componentParameters.Add(new { type = "text", text = param });
            }

            var payloadObject = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = cleanPhone,
                type = "template",
                template = new
                {
                    name = metaTemplateName,
                    language = new { code = languageCode },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = componentParameters
                        }
                    }
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payloadObject);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                // Send standard POST request to Meta's endpoint: /v21.0/{phone-number-id}/messages
                var response = await _httpClient.PostAsync($"{_options.PhoneNumberId}/messages", content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    // Extract root messaging array index zero wrapper tracking id
                    var messagesElement = doc.RootElement.GetProperty("messages")[0];
                    string providerId = messagesElement.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();

                    return (true, providerId, null);
                }
                else
                {
                    return (false, null, $"Meta API Error: status code {response.StatusCode} - {responseString}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Internal Engine Exception: {ex.Message}");
            }
        }
    }
}