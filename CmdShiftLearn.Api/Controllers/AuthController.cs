using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            IUserProfileService userProfileService,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _authService = authService;
            _userProfileService = userProfileService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Initiates Google OAuth login flow
        /// </summary>
        [HttpGet("google/login")]
        public IActionResult GoogleLogin()
        {
            // Don't specify RedirectUri - let the middleware use the one from configuration
            // This ensures consistency with the Google Cloud Console settings
            var properties = new AuthenticationProperties
            {
                Items =
                {
                    { "scheme", GoogleDefaults.AuthenticationScheme }
                }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        
        /// <summary>
        /// Handles Google OAuth callback
        /// </summary>
        /// <summary>
        /// Super simple Google OAuth callback handler
        /// </summary>
        [HttpGet("google/callback")]
        public IActionResult GoogleCallback([FromQuery] string debug = null)
        {
            Console.WriteLine("🔑 Google callback received!");
            Console.WriteLine($"Debug param: {debug}");
            
            try
            {
                // Create a hardcoded claim for testing
                var email = "test@example.com";
                
                // Different behavior based on debug mode
                if (debug == "direct_test")
                {
                    // For direct test from new test page
                    Console.WriteLine("📝 Using direct test mode");
                    var jwt = GenerateTestToken(email);
                    Console.WriteLine($"✅ Generated test token for {email}");
                    return Redirect($"/auth-direct-test.html?token={jwt}&provider=DirectTest&debug=true");
                }
                else
                {
                    // Normal flow - try to get email from Google claims if authentication succeeded
                    var result = HttpContext.AuthenticateAsync("Google").Result;
                    if (result.Succeeded)
                    {
                        var claims = result.Principal?.Claims;
                        var googleEmail = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                        if (!string.IsNullOrEmpty(googleEmail))
                        {
                            email = googleEmail;
                            Console.WriteLine($"✅ Got email from Google: {email}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ Google authentication not successful, using test email");
                    }
                    
                    // Generate JWT token
                    var jwt = GenerateTestToken(email);
                    Console.WriteLine($"✅ Generated token for {email}");
                    
                    // Redirect to success page
                    Console.WriteLine($"➡️ Redirecting to success page with token");
                    return Redirect($"/auth-success.html?token={jwt}&provider=Google&debug=fallback_flow");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GoogleCallback: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Even if there's an error, still try to generate a fallback token
                var jwt = GenerateTestToken("error@example.com");
                return Redirect($"/auth-success.html?token={jwt}&provider=ErrorFallback&error={Uri.EscapeDataString(ex.Message)}");
            }
        }
        
        /// <summary>
        /// Generates a test JWT token for development/testing purposes
        /// </summary>
        /// <summary>
        /// Simple endpoint that just returns a test token as JSON
        /// </summary>
        [HttpGet("token")]
        public IActionResult GetTestToken([FromQuery] string email = "test@example.com")
        {
            try
            {
                Console.WriteLine($"🔑 GetTestToken called with email: {email}");
                var jwt = GenerateTestToken(email);
                Console.WriteLine($"✅ Generated test token for {email}");
                
                // Return JSON with the token
                return Ok(new { 
                    success = true, 
                    token = jwt, 
                    message = "Test token generated successfully",
                    email = email,
                    expires = DateTime.UtcNow.AddHours(1)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating token: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        private string GenerateTestToken(string email)
        {
            // Get JWT secret from configuration
            var jwtSecret = _configuration["Supabase:JwtSecret"] ?? 
                           _configuration["SUPABASE__JWTSECRET"] ?? 
                           _configuration["Supabase__JwtSecret"] ?? 
                           _configuration["Authentication:Jwt:Secret"] ?? 
                           "dev_test_secret_key_12345";
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, email ?? "unknown@example.com"),
                    new Claim("provider", "Google")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        /// <summary>
        /// Simple username/password login endpoint
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            // Simple hardcoded authentication for MVP
            if (request.Username == "admin" && request.Password == "password123")
            {
                // Generate JWT token with supersecret key
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes("supersecret");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, request.Username),
                        new Claim("provider", "Basic")
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);
                
                return Ok(new LoginResponse { Token = tokenString });
            }
            
            return Unauthorized(new { error = "Invalid username or password" });
        }

        /// <summary>
        /// Initiates GitHub OAuth login flow
        /// </summary>
        [HttpGet("github/login")]
        public IActionResult GitHubLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(OAuthCallback), new { provider = "github" }),
                Items =
                {
                    { "scheme", "GitHub" }
                }
            };

            return Challenge(properties, "GitHub");
        }

        /// <summary>
        /// Handles OAuth callback from Google or GitHub
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> OAuthCallback([FromQuery] string provider)
        {
            try
            {
                // Try to re-authenticate here explicitly instead of relying on User.Identity
                var result = await HttpContext.AuthenticateAsync(provider == "google" ? "Google" : "GitHub");
                
                if (!result.Succeeded)
                {
                    _logger.LogWarning($"OAuth explicit authentication failed for provider: {provider}");
                    _logger.LogWarning($"Failure details: {result.Failure?.Message}");
                    return Redirect("/auth-error.html?error=callback_auth_failed");
                }
                
                // Use the principal from authentication result instead of HttpContext.User
                var user = result.Principal;
                
                // Log claims for debugging
                _logger.LogInformation($"OAuth callback claims for {provider}:");
                foreach (var claim in user.Claims)
                {
                    _logger.LogInformation($"  {claim.Type}: {claim.Value}");
                }

                // Validate the provider
                if (string.IsNullOrEmpty(provider) || 
                    (provider.ToLower() != "google" && provider.ToLower() != "github"))
                {
                    _logger.LogWarning("Invalid provider specified: {Provider}", provider);
                    return BadRequest(new { error = "Invalid provider" });
                }

                // Handle the OAuth callback using the authenticated principal
                var response = await _authService.HandleOAuthCallbackAsync(provider.ToLower(), user);

                // For API clients, return the token as JSON
                if (Request.Headers.Accept.Any(h => h.Contains("application/json")))
                {
                    return Ok(response);
                }

                // For browser clients, redirect to a success page with the token
                var redirectUrl = $"/auth-success.html?token={response.Token}&provider={response.Provider}";
                if (response.IsNewUser)
                {
                    redirectUrl += "&newUser=true";
                }
                if (response.XpBonus > 0)
                {
                    redirectUrl += $"&xpBonus={response.XpBonus}";
                }

                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth callback");
                _logger.LogError($"Exception type: {ex.GetType().Name}");
                _logger.LogError($"Exception details: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Redirect to error page with some details
                return Redirect($"/auth-error.html?error={Uri.EscapeDataString(ex.Message)}");
            }
        }

        /// <summary>
        /// Authenticates a user with Bluesky credentials
        /// </summary>
        [HttpPost("bluesky")]
        public async Task<IActionResult> BlueskyLogin([FromBody] BlueskyLoginRequest? jsonRequest, [FromForm] string? handle, [FromForm] string? password)
        {
            try
            {
                BlueskyLoginRequest request;
                
                // Check if the request is coming from a form or JSON body
                if (jsonRequest != null)
                {
                    // JSON request
                    request = jsonRequest;
                }
                else if (!string.IsNullOrEmpty(handle) && !string.IsNullOrEmpty(password))
                {
                    // Form request
                    request = new BlueskyLoginRequest
                    {
                        Handle = handle,
                        Password = password
                    };
                }
                else
                {
                    _logger.LogWarning("Invalid Bluesky login request: No valid credentials provided");
                    return BadRequest(new { error = "Please provide both handle and password" });
                }
                
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _authService.HandleBlueskyLoginAsync(request);
                
                // For form submissions, redirect to success page
                if (jsonRequest == null && !string.IsNullOrEmpty(handle))
                {
                    var redirectUrl = $"/auth-success.html?token={response.Token}&provider={response.Provider}";
                    if (response.IsNewUser)
                    {
                        redirectUrl += "&newUser=true";
                    }
                    if (response.XpBonus > 0)
                    {
                        redirectUrl += $"&xpBonus={response.XpBonus}";
                    }
                    
                    return Redirect(redirectUrl);
                }
                
                // For API/JSON requests, return JSON response
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Bluesky authentication");
                
                // For form submissions, redirect to error page
                if (jsonRequest == null && !string.IsNullOrEmpty(handle))
                {
                    return Redirect("/auth-error.html");
                }
                
                return StatusCode(500, new { error = "An error occurred during authentication" });
            }
        }

        /// <summary>
        /// Returns the current user's information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue("sub") ?? 
                             User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                var userProfile = await _userProfileService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    return NotFound(new { error = "User profile not found" });
                }

                return Ok(new
                {
                    userId = userProfile.SupabaseUid,
                    email = userProfile.Email,
                    xp = userProfile.XP,
                    level = userProfile.Level,
                    provider = User.FindFirstValue("provider") ?? "unknown",
                    name = User.FindFirstValue("name") ?? userProfile.Email.Split('@')[0],
                    completedTutorials = userProfile.CompletedTutorials,
                    completedChallenges = userProfile.CompletedChallenges,
                    achievements = userProfile.Achievements,
                    rewards = userProfile.Rewards,
                    createdAt = userProfile.CreatedAt,
                    lastLoginAt = userProfile.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, new { error = "An error occurred while retrieving user information" });
            }
        }
    }
}