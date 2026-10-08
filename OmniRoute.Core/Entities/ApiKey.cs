using System;

namespace OmniRoute.Core.Entities
{
    public class ApiKey
    {
        public Guid ApiKeyId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string HashedSecretKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty; // e.g., "Production", "Staging"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public Tenant Tenant { get; set; } = null!;
    }
}