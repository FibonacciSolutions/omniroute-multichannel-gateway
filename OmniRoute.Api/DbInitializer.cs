using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OmniRoute.Core.Entities;
using OmniRoute.Infrastructure.Data;
using OmniRoute.Infrastructure.Security;

namespace OmniRoute.Api
{
    public static class DbInitializer
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Generate database if it does not exist locally
            context.Database.EnsureCreated();

            if (context.Tenants.Any())
            {
                return; // Already seeded
            }

            // Seed corporate tenant profile
            var demoTenant = new Tenant
            {
                CompanyName = "Apex International Academy",
                ContactEmail = "info@apex.edu.np",
                AccountBalance = 500.0000m,
                IsActive = true
            };
            context.Tenants.Add(demoTenant);

            // Generate secure test authorization state
            string rawKeyForDemo = "omni_live_devtestkey1234567890abcdef";
            string hashedKey = ApiKeyGenerator.ComputeHash(rawKeyForDemo);

            var demoApiKey = new ApiKey
            {
                TenantId = demoTenant.TenantId,
                HashedSecretKey = hashedKey,
                Label = "Development Testing Key",
                IsActive = true
            };
            context.ApiKeys.Add(demoApiKey);

            // Seed communication notification layout mapping
            var demoTemplate = new MessageTemplate
            {
                TenantId = demoTenant.TenantId,
                TemplateCode = "fee_reminder",
                MetaTemplateName = "school_fee_alert",
                LanguageCode = "en",
                SmsRawTextBody = "Dear Parent, the academic term fee for {{student_name}} of amount {{due_amount}} is due.",
                IsActive = true
            };
            context.MessageTemplates.Add(demoTemplate);

            context.SaveChanges();
        }
    }
}