using System;
using System.Collections.Generic;

namespace OmniRoute.Core.Entities
{
    public class Tenant
    {
        public Guid TenantId { get; set; } = Guid.NewGuid();
        public string CompanyName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public decimal AccountBalance { get; set; } = 0.0000m;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
        public ICollection<MessageLog> MessageLogs { get; set; } = new List<MessageLog>();
    }
}