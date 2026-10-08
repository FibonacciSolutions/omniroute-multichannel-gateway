using System;
using System.Security.Cryptography;
using System.Text;

namespace OmniRoute.Infrastructure.Security
{
    // FIX: Must be explicitly declared public
    public static class ApiKeyGenerator
    {
        public static string ComputeHash(string rawKey)
        {
            if (string.IsNullOrEmpty(rawKey)) return string.Empty;

            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}