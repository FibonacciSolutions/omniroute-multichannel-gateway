using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniRoute.Core.Entities
{
    public class MessageLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // C# मा Guid? राख्ने ताकि अरू सर्भिससँग म्याच होस्
        public Guid? TenantId { get; set; }

        public string? DestinationNumber { get; set; }

        public string? RequestedParametersJson { get; set; }

        public string? DispatchedChannel { get; set; } = "SMS";

        public string? DeliveryStatus { get; set; } = "Pending";

        public decimal CostCharged { get; set; } = 0.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public string? ProviderMessageId { get; set; }

        [NotMapped]
        public string? ErrorMessage { get; set; }
    }
}