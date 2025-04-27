using System.ComponentModel.DataAnnotations;

namespace CmdShiftLearn.Api.Models
{
    /// <summary>
    /// Authentication settings for various providers
    /// </summary>
    public class AuthSettings
    {
        public GoogleAuthSettings? Google { get; set; }
        public GitHubAuthSettings? GitHub { get; set; }
        public BlueskyAuthSettings? Bluesky { get; set; }
        public JwtSettings? Jwt { get; set; }
    }
    
    /// <summary>
    /// Simple username/password login request
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Simple login response with token
    /// </summary>
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Google OAuth settings
    /// </summary>
    public class GoogleAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    /// <summary>
    /// GitHub OAuth settings
    /// </summary>
    public class GitHubAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bluesky authentication settings
    /// </summary>
    public class BlueskyAuthSettings
    {
        public string ApiUrl { get; set; } = "https://bsky.social/xrpc";
    }

    /// <summary>
    /// JWT token settings
    /// </summary>
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;
    }

    /// <summary>
    /// Bluesky login request model
    /// </summary>
    public class BlueskyLoginRequest
    {
        [Required]
        public string Handle { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bluesky session response model
    /// </summary>
    public class BlueskySessionResponse
    {
        public string Did { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
        public string AccessJwt { get; set; } = string.Empty;
        public string RefreshJwt { get; set; } = string.Empty;
        public BlueskyUserProfile? User { get; set; }
    }

    /// <summary>
    /// Bluesky user profile model
    /// </summary>
    public class BlueskyUserProfile
    {
        public string Did { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }

    /// <summary>
    /// Authentication response model
    /// </summary>
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsNewUser { get; set; }
        public int XpBonus { get; set; }
        public int TotalXp { get; set; }
        public int Level { get; set; }
    }
}