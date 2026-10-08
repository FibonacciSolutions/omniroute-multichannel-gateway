using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Core.Entities;
using OmniRoute.Infrastructure.Data;
using OmniRoute.Infrastructure.Interfaces;

namespace OmniRoute.Infrastructure.Services
{
    public class MessagingEngine : IMessagingEngine
    {
        private readonly ApplicationDbContext _context;
        private readonly IWhatsAppClient _whatsAppClient;
        private readonly ISmsClient _smsClient;

        public MessagingEngine(
            ApplicationDbContext context,
            IWhatsAppClient whatsAppClient,
            ISmsClient smsClient)
        {
            _context = context;
            _whatsAppClient = whatsAppClient;
            _smsClient = smsClient;
        }

        public async Task<MessageLog> RouteAndDispatchAsync(
            Guid tenantId,
            string phoneNumber,
            string templateCode,
            Dictionary<string, string> parameters)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null || !tenant.IsActive)
                throw new Exception("Unauthorized or deactivated corporate tenant console.");

            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.TemplateCode == templateCode && t.IsActive);
            if (template == null)
                throw new Exception($"Active routing template configuration mapping not found for code: {templateCode}");

            decimal whatsappRate = 0.50m;
            decimal smsRate = 1.30m;

            if (tenant.AccountBalance < smsRate)
                throw new Exception("Transaction terminated. Insufficient system utility API balance.");

            var log = new MessageLog
            {
                TenantId = tenantId,
                DestinationNumber = phoneNumber,
                RequestedParametersJson = JsonSerializer.Serialize(parameters),
                DispatchedChannel = "SMS",
                DeliveryStatus = "Queued",
                CostCharged = 0.0000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            bool isWhatsAppEligible = SimulateChannelVerification(phoneNumber);

            if (isWhatsAppEligible)
            {
                var whatsappResponse = await _whatsAppClient.SendTemplateMessageAsync(
                    phoneNumber,
                    template.MetaTemplateName,
                    template.LanguageCode,
                    parameters);

                if (whatsappResponse.IsSuccess)
                {
                    tenant.AccountBalance -= whatsappRate;
                    log.DispatchedChannel = "WhatsApp";
                    log.DeliveryStatus = "Dispatched";
                    log.CostCharged = whatsappRate;
                    log.ProviderMessageId = whatsappResponse.ProviderMessageId;

                    _context.MessageLogs.Add(log);
                    await _context.SaveChangesAsync();
                    return log;
                }

                System.Diagnostics.Debug.WriteLine($"Primary WhatsApp route failed: {whatsappResponse.ErrorReason}. Initializing fallback SMS.");
            }

            string compiledSmsBody = CompilePlainSmsBody(template.SmsRawTextBody, parameters);
            var smsResponse = await _smsClient.SendSmsAsync(phoneNumber, compiledSmsBody);

            if (smsResponse.IsSuccess)
            {
                tenant.AccountBalance -= smsRate;
                log.DispatchedChannel = "SMS";
                log.DeliveryStatus = "Dispatched";
                log.CostCharged = smsRate;
                log.ProviderMessageId = smsResponse.ProviderMessageId;
            }
            else
            {
                log.DispatchedChannel = "SMS";
                log.DeliveryStatus = "Failed";
                log.CostCharged = 0.0000m;
                log.ErrorMessage = smsResponse.ErrorReason;
            }

            _context.MessageLogs.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }

        private bool SimulateChannelVerification(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return false;
            char lastChar = phoneNumber[^1];
            return char.IsDigit(lastChar) && (lastChar % 2 == 0);
        }

        private string CompilePlainSmsBody(string rawBody, Dictionary<string, string> parameters)
        {
            string output = rawBody;
            foreach (var kvp in parameters)
            {
                output = output.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }
            return output;
        }
    }
}