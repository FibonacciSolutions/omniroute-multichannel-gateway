using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniRoute.Infrastructure.Interfaces
{
    public interface IWhatsAppClient
    {
        Task<(bool IsSuccess, string? ProviderMessageId, string? ErrorReason)> SendTemplateMessageAsync(
            string recipientPhone,
            string metaTemplateName,
            string languageCode,
            Dictionary<string, string> textParameters);
    }
}