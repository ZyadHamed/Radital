using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace RadiologistAppCore.Services
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string Scheme = "ApiKey";
        public const string HeaderName = "X-Api-Key";
    }

    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly IConfiguration _configuration;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,                
            IConfiguration configuration)
            : base(options, logger, encoder, clock)  
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var providedKey)
                || string.IsNullOrWhiteSpace(providedKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var clients = _configuration.GetSection("ServiceApiKeys").GetChildren();

            foreach (var client in clients)
            {
                var configuredKey = client["Key"];
                if (!string.IsNullOrEmpty(configuredKey) &&
                    CryptographicEquals(configuredKey, providedKey!))
                {
                    var role = client["Role"] ?? "System";
                    var systemUserId = client["SystemUserId"] ?? Guid.Empty.ToString();
                    var clientName = client["ClientName"] ?? client.Key;

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, systemUserId),
                        new Claim("sub",                    systemUserId),
                        new Claim(ClaimTypes.Name,          clientName),
                        new Claim(ClaimTypes.Role,          role),
                        new Claim("auth_type",              "service")
                    };

                    var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationOptions.Scheme);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.Scheme);

                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        private static bool CryptographicEquals(string a, string b)
        {
            var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
            var bBytes = System.Text.Encoding.UTF8.GetBytes(b);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
    }
}
