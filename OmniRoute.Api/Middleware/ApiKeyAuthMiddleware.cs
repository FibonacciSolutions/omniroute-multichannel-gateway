using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Infrastructure.Data;

namespace OmniRoute.Api.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Allow public access to Swagger, Portal root UI, and static files
            if (path.Contains("/swagger") || path.Contains("/index.html") || path == "/" || path.EndsWith(".css") || path.EndsWith(".js"))
            {
                await _next(context);
                return;
            }

            // Verify X-API-KEY header presence
            if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey) || string.IsNullOrWhiteSpace(extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { success = false, error = "X-API-KEY header is missing." });
                return;
            }

            try
            {
                var connection = dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var cmd = connection.CreateCommand();
                // Validates strictly against [Key] and IsActive in ApiKeys table
                cmd.CommandText = "SELECT COUNT(1) FROM ApiKeys WHERE [Key] = @key AND IsActive = 1";

                var pKey = cmd.CreateParameter();
                pKey.ParameterName = "@key";
                pKey.Value = extractedApiKey.ToString().Trim();
                cmd.Parameters.Add(pKey);

                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                if (count == 0)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { success = false, error = "Invalid or revoked API key." });
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { success = false, error = $"Authentication check failed: {ex.Message}" });
            }
        }
    }
}