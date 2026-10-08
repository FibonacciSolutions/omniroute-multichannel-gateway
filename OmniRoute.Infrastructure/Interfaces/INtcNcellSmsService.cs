using System.Threading.Tasks;

namespace OmniRoute.Infrastructure.Interfaces
{
    public interface INtcNcellSmsService
    {
        Task<bool> SendViaCarrierAsync(string to, string message, string senderId);
    }
}