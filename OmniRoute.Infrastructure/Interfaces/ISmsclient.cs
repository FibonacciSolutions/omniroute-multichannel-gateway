using System.Threading.Tasks;

namespace OmniRoute.Infrastructure.Interfaces
{
    public interface ISmsClient
    {
        Task<(bool IsSuccess, string? ProviderMessageId, string? ErrorReason)> SendSmsAsync(
            string recipientPhone,
            string messageText);
    }
}