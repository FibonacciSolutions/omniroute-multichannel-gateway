using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmniRoute.Infrastructure.Data;

namespace OmniRoute.Api.Controllers
{
    [ApiController]
    [Route("api/v1/webhooks/whatsapp")]
    public class WhatsAppWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _verifyToken;

        public WhatsAppWebhookController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            // Pull the secure handshake token from configuration settings
            _verifyToken = configuration["WhatsAppSettings:WebhookVerifyToken"] ?? "omni_webhook_handshake_secret_2026";
        }

        /// <summary>
        /// Meta Webhook Verification Handshake (HTTP GET)
        /// Used by Meta to verify the legitimacy of your endpoint upon setup.
        /// </summary>
        [HttpGet]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.verify_token")] string token,
            [FromQuery(Name = "hub.challenge")] string challenge)
        {
            if (mode == "subscribe" && token == _verifyToken)
            {
                return Ok(challenge); // Return the challenge token back to Meta safely
            }

            return Forbid("Webhook authorization handshake verification failed.");
        }

        /// <summary>
        /// Real-time Status Update Receiver (HTTP POST)
        /// Listens to Meta updates and processes status variations dynamically.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ReceiveUpdate()
        {
            using var reader = new StreamReader(Request.Body);
            string jsonPayload = await reader.ReadToEndAsync();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonPayload);
                JsonElement root = doc.RootElement;

                // Drill down through Meta's standard payload structure: entry -> changes -> value -> statuses
                if (root.TryGetProperty("entry", out var entryArray) && entryArray.GetArrayLength() > 0)
                {
                    var entry = entryArray[0];
                    if (entry.TryGetProperty("changes", out var changesArray) && changesArray.GetArrayLength() > 0)
                    {
                        var change = changesArray[0];
                        var value = change.GetProperty("value");

                        if (value.TryGetProperty("statuses", out var statusesArray) && statusesArray.GetArrayLength() > 0)
                        {
                            var statusElement = statusesArray[0];

                            string providerMessageId = statusElement.GetProperty("id").GetString() ?? string.Empty;
                            string deliveryStatus = statusElement.GetProperty("status").GetString() ?? string.Empty;

                            if (!string.IsNullOrEmpty(providerMessageId))
                            {
                                // Locate the corresponding log entry using Meta's unique tracking identifier
                                var logEntry = await _context.MessageLogs
                                    .FirstOrDefaultAsync(l => l.ProviderMessageId == providerMessageId);

                                if (logEntry != null)
                                {
                                    // Map Meta internal values to system states (delivered, read, failed)
                                    logEntry.DeliveryStatus = char.ToUpper(deliveryStatus[0]) + deliveryStatus.Substring(1);
                                    logEntry.UpdatedAt = DateTime.UtcNow;

                                    // If message failed, extract the error code/reason text dynamically
                                    if (deliveryStatus == "failed" && statusElement.TryGetProperty("errors", out var errorsArray) && errorsArray.GetArrayLength() > 0)
                                    {
                                        var error = errorsArray[0];
                                        logEntry.ErrorMessage = error.GetProperty("message").GetString();
                                    }

                                    await _context.SaveChangesAsync();
                                }
                            }
                        }
                    }
                }

                return Ok(new { status = "success" }); // Always acknowledge Meta with an HTTP 200 immediately
            }
            catch (Exception)
            {
                // Return 200 to prevent Meta from retrying or temporarily disabling the webhook due to app errors
                return Ok(new { status = "parsed_with_exception" });
            }
        }
    }
}