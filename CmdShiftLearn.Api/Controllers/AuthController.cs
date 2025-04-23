using System.Security.Claims;
using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IUserProfileService userProfileService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userProfileService = userProfileService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates Google OAuth login flow
        /// </summary>
        [HttpGet("google/login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(OAuthCallback), new { provider = "google" }),
                Items =
                {
                    { "scheme", GoogleDefaults.AuthenticationScheme }
                }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
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
                // Ensure the user is authenticated
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("OAuth callback received but user is not authenticated");
                    return Unauthorized(new { error = "Authentication failed" });
                }

                // Validate the provider
                if (string.IsNullOrEmpty(provider) || 
                    (provider.ToLower() != "google" && provider.ToLower() != "github"))
                {
                    _logger.LogWarning("Invalid provider specified: {Provider}", provider);
                    return BadRequest(new { error = "Invalid provider" });
                }

                // Handle the OAuth callback
                var response = await _authService.HandleOAuthCallbackAsync(provider.ToLower(), User);

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
                return StatusCode(500, new { error = "An error occurred during authentication" });
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