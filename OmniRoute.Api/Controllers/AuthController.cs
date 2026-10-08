using System;
using System.Data;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Infrastructure.Data;

namespace OmniRoute.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Username and password are required." });
            }

            if (request.Username == "upcode" && request.Password == "UpCode@2026#SMS")
            {
                return Ok(new
                {
                    success = true,
                    message = "Login successful.",
                    tenantId = "e88adad2-82c1-48e1-a9f2-732bbcc3fbab"
                });
            }

            return Unauthorized(new { error = "Invalid username or password." });
        }
        [HttpPost("generate-api-key")]
        public async Task<IActionResult> SaveOrGenerateApiKey([FromBody] ApiKeyRequest request)
        {
            string keyToUse = !string.IsNullOrWhiteSpace(request?.CustomApiKey)
                ? request.CustomApiKey.Trim()
                : "upcode_live_" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLower();

            var tenantId = request?.TenantId ?? "e88adad2-82c1-48e1-a9f2-732bbcc3fbab";

            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // 1. Revoke all old keys
                using (var revokeCmd = connection.CreateCommand())
                {
                    revokeCmd.CommandText = "UPDATE ApiKeys SET IsActive = 0";
                    await revokeCmd.ExecuteNonQueryAsync();
                }

                // 2. Insert new key using exact column [Key]
                using (var insertCmd = connection.CreateCommand())
                {
                    insertCmd.CommandText = @"
                INSERT INTO ApiKeys ([Key], TenantId, IsActive, CreatedAt)
                VALUES (@key, @tenant, 1, @created)";

                    var pKey = insertCmd.CreateParameter();
                    pKey.ParameterName = "@key";
                    pKey.Value = keyToUse;
                    insertCmd.Parameters.Add(pKey);

                    var pTenant = insertCmd.CreateParameter();
                    pTenant.ParameterName = "@tenant";
                    pTenant.Value = tenantId;
                    insertCmd.Parameters.Add(pTenant);

                    var pCreated = insertCmd.CreateParameter();
                    pCreated.ParameterName = "@created";
                    pCreated.Value = DateTime.UtcNow;
                    insertCmd.Parameters.Add(pCreated);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                return Ok(new { success = true, apiKey = keyToUse, headerName = "X-API-KEY" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to save key: {ex.Message}" });
            }
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ApiKeyRequest
    {
        public string TenantId { get; set; } = "e88adad2-82c1-48e1-a9f2-732bbcc3fbab";
        public string? CustomApiKey { get; set; }
    }
}