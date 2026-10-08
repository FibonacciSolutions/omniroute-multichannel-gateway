using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OmniRoute.Core.Entities;

namespace OmniRoute.Infrastructure.Interfaces
{
    public interface IMessagingEngine
    {
        Task<MessageLog> RouteAndDispatchAsync(
            Guid tenantId,
            string phoneNumber,
            string templateCode,
            Dictionary<string, string> parameters);
    }
}