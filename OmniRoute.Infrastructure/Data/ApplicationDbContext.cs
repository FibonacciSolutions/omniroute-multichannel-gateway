using Microsoft.EntityFrameworkCore;
using OmniRoute.Core.Entities;

namespace OmniRoute.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MessageLog> MessageLogs { get; set; }
        public DbSet<MessageTemplate> MessageTemplates { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MessageLog>(entity =>
            {
                entity.ToTable("MessageLogs");

                // Primary Key म्यापिङ
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("MessageId");

                // ✨ Value Converter: C# मा Guid? हुने, DB मा nvarchar(string) मा सेभ हुने
                entity.Property(e => e.TenantId)
                    .HasColumnName("TenantId")
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToString() : null,
                        v => string.IsNullOrEmpty(v) ? (Guid?)null : Guid.Parse(v)
                    );

                entity.Property(e => e.DestinationNumber).HasColumnName("DestinationNumber");
                entity.Property(e => e.RequestedParametersJson).HasColumnName("RequestedParametersJson");
                entity.Property(e => e.DispatchedChannel).HasColumnName("DispatchedChannel");
                entity.Property(e => e.DeliveryStatus).HasColumnName("DeliveryStatus");
                entity.Property(e => e.CostCharged).HasColumnName("CostCharged");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");

                entity.Ignore(e => e.ProviderMessageId);
                entity.Ignore(e => e.ErrorMessage);
            });
        }
    }
}