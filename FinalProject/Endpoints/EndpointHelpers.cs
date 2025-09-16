using System.Security.Cryptography;
using System.Text;

namespace FinalProject.Endpoints
{
    internal static class EndpointHelpers
    {
        public static string GetIpHash(HttpContext ctx)
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));
        }

        public static string Trunc(string s, int n)
            => string.IsNullOrEmpty(s) ? s : (s.Length <= n ? s : s.Substring(0, n));

        public static string MaskEmail(string? email)
            => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];
    }
}
