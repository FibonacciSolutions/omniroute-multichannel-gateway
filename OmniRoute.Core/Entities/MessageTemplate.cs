using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniRoute.Core.Entities
{
    [Table("MessageTemplates")]
    public class MessageTemplate
    {
        [Key]
        [Column("TemplateId")]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? TemplateCode { get; set; }

        public Guid? TenantId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? Channel { get; set; } = "SMS";

        // नयाँ थपिएका प्रोपर्टीहरू (WhatsApp/Viber/SMS का लागि)
        public string? MetaTemplateName { get; set; }

        public string? LanguageCode { get; set; }

        public string? SmsRawTextBody { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}