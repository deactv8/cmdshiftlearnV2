using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CmdShiftLearn.Api.Models;
using System.Security.Claims;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IOptions<SupabaseSettings> _supabaseSettings;
        private readonly IWebHostEnvironment _environment;

        public DebugController(
            IConfiguration configuration,
            IOptions<SupabaseSettings> supabaseSettings,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _supabaseSettings = supabaseSettings;
            _environment = environment;
        }

        [HttpGet("auth-status")]
        [Authorize]
        public IActionResult GetAuthStatus()
        {
            // This endpoint will only be accessible if authentication works
            var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
            var userId = User.FindFirstValue("sub");
            
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserId = userId,
                Claims = claims
            });
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            // Only allow in development environment
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            // Get JWT secret from configuration
            var jwtSecret = _configuration["Supabase:JwtSecret"] ?? "NOT FOUND";
            var settingsSecret = _supabaseSettings.Value.JwtSecret;

            return Ok(new
            {
                Environment = _environment.EnvironmentName,
                JwtSecretFromConfig = string.IsNullOrEmpty(jwtSecret) 
                    ? "EMPTY" 
                    : $"{jwtSecret[..Math.Min(3, jwtSecret.Length)]}...{(jwtSecret.Length > 3 ? jwtSecret[^Math.Min(3, jwtSecret.Length)..] : "")}",
                JwtSecretFromSettings = string.IsNullOrEmpty(settingsSecret) 
                    ? "EMPTY" 
                    : $"{settingsSecret[..Math.Min(3, settingsSecret.Length)]}...{(settingsSecret.Length > 3 ? settingsSecret[^Math.Min(3, settingsSecret.Length)..] : "")}",
                JwtSecretLength = jwtSecret.Length,
                SettingsSecretLength = settingsSecret.Length,
                HasValidSecret = !string.IsNullOrEmpty(jwtSecret) && jwtSecret.Length > 32,
                ConfigurationSource = _configuration is IConfigurationRoot root ? root.GetDebugView() : "Debug view not available"
            });
        }
    }
}