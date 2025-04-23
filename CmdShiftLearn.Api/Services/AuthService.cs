using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CmdShiftLearn.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CmdShiftLearn.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> HandleOAuthCallbackAsync(string provider, ClaimsPrincipal user);
        Task<AuthResponse> HandleBlueskyLoginAsync(BlueskyLoginRequest request);
        string GenerateJwtToken(string userId, string email, string provider, Dictionary<string, string> additionalClaims = null);
        ClaimsPrincipal ValidateToken(string token);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IEventLogger _eventLogger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserProfileService userProfileService,
            IEventLogger eventLogger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userProfileService = userProfileService;
            _eventLogger = eventLogger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Handles OAuth callback from Google or GitHub
        /// </summary>
        public async Task<AuthResponse> HandleOAuthCallbackAsync(string provider, ClaimsPrincipal user)
        {
            // Extract user information from claims
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                         user.FindFirstValue("sub") ?? 
                         throw new InvalidOperationException("User ID claim not found");
            
            var email = user.FindFirstValue(ClaimTypes.Email) ?? 
                        user.FindFirstValue("email") ?? 
                        string.Empty;
            
            var name = user.FindFirstValue(ClaimTypes.Name) ?? 
                       user.FindFirstValue("name") ?? 
                       email.Split('@')[0];

            // Create a unique user ID that includes the provider to avoid conflicts
            var providerUserId = $"{provider}:{userId}";
            
            // Check if user exists
            var userProfile = await _userProfileService.GetUserProfileAsync(providerUserId);
            bool isNewUser = userProfile == null;
            int xpBonus = 0;

            if (isNewUser)
            {
                // Create new user profile
                userProfile = await _userProfileService.CreateUserProfileAsync(providerUserId, email);
                
                // Award first login bonus
                int previousXp = userProfile.XP;
                userProfile.XP += 50;
                xpBonus += 50;
                
                // Update the user's level based on new XP
                userProfile.Level = _userProfileService.CalculateLevel(userProfile.XP);
                
                // Log the XP bonus
                await _userProfileService.LogXpAddedAsync(userProfile, 50, "First login bonus");
                
                // Check for XP-based rewards
                await _userProfileService.CheckAndAwardXpRewardsAsync(userProfile, previousXp);
                
                // Log the event
                await _eventLogger.LogAsync(new PlatformEvent
                {
                    EventType = "user.created",
                    UserId = providerUserId,
                    Description = $"New user created via {provider} authentication"
                });
            }

            // Update last login time
            userProfile.LastLoginAt = DateTime.UtcNow;
            await _userProfileService.UpdateUserProfileAsync(userProfile);
            
            // Log the login event
            await _eventLogger.LogAsync(new PlatformEvent
            {
                EventType = "user.login",
                UserId = providerUserId,
                Description = $"User logged in via {provider}"
            });

            // Generate JWT token
            var additionalClaims = new Dictionary<string, string>
            {
                { "name", name }
            };
            
            string token = GenerateJwtToken(providerUserId, email, provider, additionalClaims);

            return new AuthResponse
            {
                Token = token,
                Provider = provider,
                UserId = providerUserId,
                Email = email,
                Name = name,
                IsNewUser = isNewUser,
                XpBonus = xpBonus,
                TotalXp = userProfile.XP,
                Level = userProfile.Level
            };
        }

        /// <summary>
        /// Handles Bluesky login
        /// </summary>
        public async Task<AuthResponse> HandleBlueskyLoginAsync(BlueskyLoginRequest request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                
                // Call Bluesky API to create a session
                var response = await httpClient.PostAsJsonAsync(
                    "https://bsky.social/xrpc/com.atproto.server.createSession",
                    new { identifier = request.Handle, password = request.Password }
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Bluesky authentication failed: {ErrorContent}", errorContent);
                    throw new InvalidOperationException($"Bluesky authentication failed: {response.StatusCode}");
                }

                // Parse the response
                var sessionResponse = await response.Content.ReadFromJsonAsync<BlueskySessionResponse>();
                if (sessionResponse == null)
                {
                    throw new InvalidOperationException("Failed to parse Bluesky session response");
                }

                // Create a unique user ID for Bluesky users
                var providerUserId = $"bluesky:{sessionResponse.Did}";
                
                // Check if user exists
                var userProfile = await _userProfileService.GetUserProfileAsync(providerUserId);
                bool isNewUser = userProfile == null;
                int xpBonus = 0;

                if (isNewUser)
                {
                    // Create new user profile
                    userProfile = await _userProfileService.CreateUserProfileAsync(providerUserId, sessionResponse.Handle);
                    
                    // Award first login bonus
                    int previousXp = userProfile.XP;
                    userProfile.XP += 50;
                    xpBonus += 50;
                    
                    // Award Bluesky bonus
                    userProfile.XP += 100;
                    xpBonus += 100;
                    
                    // Update the user's level based on new XP
                    userProfile.Level = _userProfileService.CalculateLevel(userProfile.XP);
                    
                    // Log the XP bonuses
                    await _userProfileService.LogXpAddedAsync(userProfile, 50, "First login bonus");
                    await _userProfileService.LogXpAddedAsync(userProfile, 100, "Bluesky login bonus");
                    
                    // Check for XP-based rewards
                    await _userProfileService.CheckAndAwardXpRewardsAsync(userProfile, previousXp);
                    
                    // Log the event
                    await _eventLogger.LogAsync(new PlatformEvent
                    {
                        EventType = "user.created",
                        UserId = providerUserId,
                        Description = "New user created via Bluesky authentication"
                    });
                }
                else
                {
                    // Award Bluesky bonus if not a new user but hasn't received it yet
                    // Check if there's a log entry for Bluesky login bonus
                    if (!userProfile.XpLog.Any(x => x.Reason == "Bluesky login bonus"))
                    {
                        int previousXp = userProfile.XP;
                        userProfile.XP += 100;
                        xpBonus += 100;
                        
                        // Update the user's level based on new XP
                        userProfile.Level = _userProfileService.CalculateLevel(userProfile.XP);
                        
                        // Log the XP bonus
                        await _userProfileService.LogXpAddedAsync(userProfile, 100, "Bluesky login bonus");
                        
                        // Check for XP-based rewards
                        await _userProfileService.CheckAndAwardXpRewardsAsync(userProfile, previousXp);
                    }
                }

                // Update last login time
                userProfile.LastLoginAt = DateTime.UtcNow;
                await _userProfileService.UpdateUserProfileAsync(userProfile);
                
                // Log the login event
                await _eventLogger.LogAsync(new PlatformEvent
                {
                    EventType = "user.login",
                    UserId = providerUserId,
                    Description = "User logged in via Bluesky"
                });

                // Generate JWT token
                var additionalClaims = new Dictionary<string, string>
                {
                    { "name", sessionResponse.Handle },
                    { "did", sessionResponse.Did }
                };
                
                string token = GenerateJwtToken(providerUserId, sessionResponse.Handle, "bluesky", additionalClaims);

                return new AuthResponse
                {
                    Token = token,
                    Provider = "bluesky",
                    UserId = providerUserId,
                    Email = sessionResponse.Handle,
                    Name = sessionResponse.User?.DisplayName ?? sessionResponse.Handle,
                    IsNewUser = isNewUser,
                    XpBonus = xpBonus,
                    TotalXp = userProfile.XP,
                    Level = userProfile.Level
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Bluesky authentication");
                throw;
            }
        }

        /// <summary>
        /// Generates a JWT token for the authenticated user
        /// </summary>
        public string GenerateJwtToken(string userId, string email, string provider, Dictionary<string, string> additionalClaims = null)
        {
            var jwtSecret = _configuration["Authentication:Jwt:Secret"] ?? 
                           _configuration["Supabase:JwtSecret"] ?? 
                           throw new InvalidOperationException("JWT secret not configured");
            
            var jwtIssuer = _configuration["Authentication:Jwt:Issuer"] ?? 
                           _configuration["Supabase:Issuer"] ?? 
                           "https://cmdshiftlearn.api";
            
            var jwtAudience = _configuration["Authentication:Jwt:Audience"] ?? "cmdshiftlearn-api";
            
            var jwtExpiryMinutes = int.TryParse(_configuration["Authentication:Jwt:ExpiryMinutes"], out int expiryMinutes) 
                ? expiryMinutes 
                : 60;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);
            
            var claims = new List<Claim>
            {
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("email", email),
                new Claim("provider", provider)
            };
            
            // Add additional claims if provided
            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Validates a JWT token and returns the claims principal
        /// </summary>
        public ClaimsPrincipal ValidateToken(string token)
        {
            var jwtSecret = _configuration["Authentication:Jwt:Secret"] ?? 
                           _configuration["Supabase:JwtSecret"] ?? 
                           throw new InvalidOperationException("JWT secret not configured");
            
            var jwtIssuer = _configuration["Authentication:Jwt:Issuer"] ?? 
                           _configuration["Supabase:Issuer"] ?? 
                           "https://cmdshiftlearn.api";
            
            var jwtAudience = _configuration["Authentication:Jwt:Audience"] ?? "cmdshiftlearn-api";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
            
            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                throw;
            }
        }
    }
}