using Microsoft.Extensions.Logging;

namespace CmdShiftLearn.Api.Auth
{
    public class InMemoryApiKeyValidator : IApiKeyValidator
    {
        private readonly Dictionary<string, string> _apiKeys = new Dictionary<string, string>
        {
            // ApiKey -> UserId mapping
            { "devkey123", "dev-user-id" },
            { "testkey456", "test-user-id" }
        };
        
        private readonly ILogger<InMemoryApiKeyValidator> _logger;

        public InMemoryApiKeyValidator(ILogger<InMemoryApiKeyValidator> logger)
        {
            _logger = logger;
        }

        public bool IsValidApiKey(string apiKey, out string userId)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                userId = null;
                _logger.LogWarning("API key validation failed: Empty API key");
                return false;
            }

            if (_apiKeys.TryGetValue(apiKey, out userId))
            {
                _logger.LogInformation("API key validation succeeded for user {UserId}", userId);
                return true;
            }
            
            _logger.LogWarning("API key validation failed: Invalid API key {ApiKeyPrefix}", 
                apiKey.Substring(0, Math.Min(3, apiKey.Length)) + "***");
            userId = null;
            return false;
        }
    }
}