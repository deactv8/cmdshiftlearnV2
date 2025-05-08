using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CmdShiftLearn.Api.Auth
{
    public class ApiKeyAuthOptions : AuthenticationSchemeOptions
    {
    }

    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthOptions>
    {
        private readonly IApiKeyValidator _apiKeyValidator;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IApiKeyValidator apiKeyValidator) : base(options, logger, encoder, clock)
        {
            _apiKeyValidator = apiKeyValidator;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Logger.LogDebug("Processing authentication for request {Path}", Request.Path);
            
            // Check if Authorization header exists
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                // Changed from LogWarning to LogDebug to reduce log noise for unauthenticated requests
                Logger.LogDebug("Authentication failed for request {Path}: Missing Authorization header", Request.Path);
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
            }
                
            var headerValue = authHeader.ToString();
            string apiKey;
            
            // Check if it starts with "ApiKey " (required format)
            if (headerValue.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the API key
                apiKey = headerValue.Substring(7).Trim();
            }
            else
            {
                // Support direct API key format for backward compatibility
                Logger.LogDebug("Using direct API key format (without 'ApiKey ' prefix) for backward compatibility");
                apiKey = headerValue.Trim();
            }
            
            // Validate the API key
            if (!_apiKeyValidator.IsValidApiKey(apiKey, out var userId))
            {
                Logger.LogWarning("Authentication failed for request {Path}: Invalid API key", Request.Path);
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }
                
            // Create claims principal for the authenticated user
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            Logger.LogInformation("Authentication succeeded for user {UserId}", userId);    
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}