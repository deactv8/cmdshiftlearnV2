using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Octokit;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DebugController> _logger;

        public DebugController(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<DebugController> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet("auth-status")]
        [Authorize]
        public IActionResult GetAuthStatus()
        {
            // This endpoint will only be accessible if authentication works
            var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
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
            
            // Get OAuth configuration
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
            var githubClientId = _configuration["Authentication:GitHub:ClientId"];
            var githubClientSecret = _configuration["Authentication:GitHub:ClientSecret"];
            var authJwtSecret = _configuration["Authentication:Jwt:Secret"];

            return Ok(new
            {
                Environment = _environment.EnvironmentName,
                Authentication = new
                {
                    Google = new
                    {
                        ClientId = MaskValue(googleClientId),
                        ClientSecret = MaskValue(googleClientSecret),
                        IsConfigured = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret)
                    },
                    GitHub = new
                    {
                        ClientId = MaskValue(githubClientId),
                        ClientSecret = MaskValue(githubClientSecret),
                        IsConfigured = !string.IsNullOrEmpty(githubClientId) && !string.IsNullOrEmpty(githubClientSecret)
                    },
                    Jwt = new
                    {
                        Secret = MaskValue(authJwtSecret),
                        Issuer = _configuration["Authentication:Jwt:Issuer"],
                        Audience = _configuration["Authentication:Jwt:Audience"],
                        ExpiryMinutes = _configuration["Authentication:Jwt:ExpiryMinutes"],
                        IsConfigured = !string.IsNullOrEmpty(authJwtSecret)
                    }
                },
                ApiKey = new
                {
                    Enabled = true,
                    DefaultKeys = new[] { "devkey123", "testkey456" }
                },
                GitHub = new
                {
                    AccessToken = MaskValue(_configuration["GitHub:AccessToken"]),
                    IsConfigured = !string.IsNullOrEmpty(_configuration["GitHub:AccessToken"])
                },
                OpenAI = new
                {
                    ApiKey = MaskValue(_configuration["OpenAI:ApiKey"]),
                    IsConfigured = !string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"])
                },
                ConfigurationSource = _configuration is IConfigurationRoot root ? root.GetDebugView() : "Debug view not available"
            });
        }
        
        /// <summary>
        /// Masks a sensitive value for display
        /// </summary>
        private string MaskValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "not set";
            }

            if (value.Length <= 8)
            {
                return "***" + value.Substring(value.Length - 1);
            }

            return value.Substring(0, 3) + "..." + value.Substring(value.Length - 3);
        }
        
        /// <summary>
        /// Debug endpoint to list all GitHub files in the tutorials directory
        /// </summary>
        /// <returns>A list of file information from the GitHub repository</returns>
        [HttpGet("github-files")]
        public async Task<ActionResult<IEnumerable<object>>> GetGitHubFiles()
        {
            try
            {
                _logger.LogInformation("Debug request: GET /api/debug/github-files");
                
                // Get GitHub configuration
                var owner = _configuration["GitHub:Owner"] ?? "deactv8";
                var repo = _configuration["GitHub:Repo"] ?? "content";
                var branch = _configuration["GitHub:Branch"] ?? "master";
                var tutorialsPath = _configuration["GitHub:TutorialsPath"] ?? "tutorials";
                
                _logger.LogInformation("GitHub configuration: {Owner}/{Repo}:{Branch}/{Path}", 
                    owner, repo, branch, tutorialsPath);
                
                // Initialize GitHub client
                var gitHubClient = new GitHubClient(new ProductHeaderValue("CmdShiftLearn-Debug"));
                
                // Add authentication if token is provided
                var token = _configuration["GitHub:AccessToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    gitHubClient.Credentials = new Credentials(token);
                    _logger.LogInformation("GitHub client initialized with authentication token");
                }
                else
                {
                    _logger.LogWarning("No GitHub authentication token provided. Using unauthenticated access (rate limits may apply)");
                }
                
                // Get all files in the tutorials directory
                var contents = await gitHubClient.Repository.Content.GetAllContents(
                    owner, repo, tutorialsPath);
                
                _logger.LogInformation("Found {Count} items in the tutorials directory", contents.Count);
                
                // Create a list of file information
                var files = contents.Select(content => new
                {
                    Name = content.Name,
                    Path = content.Path,
                    Type = content.Type.ToString(),
                    Size = content.Size,
                    Url = content.HtmlUrl,
                    DownloadUrl = content.DownloadUrl,
                    Extension = System.IO.Path.GetExtension(content.Name)
                }).ToList();
                
                _logger.LogInformation("Returning {Count} files", files.Count);
                
                return Ok(new
                {
                    Configuration = new
                    {
                        Owner = owner,
                        Repository = repo,
                        Branch = branch,
                        TutorialsPath = tutorialsPath,
                        IsAuthenticated = !string.IsNullOrEmpty(token)
                    },
                    Files = files
                });
            }
            catch (NotFoundException ex)
            {
                _logger.LogError(ex, "GitHub repository or path not found");
                return NotFound(new { message = "GitHub repository or path not found", details = ex.Message });
            }
            catch (RateLimitExceededException ex)
            {
                _logger.LogError(ex, "GitHub API rate limit exceeded. Reset at: {ResetTime}", ex.Reset);
                return StatusCode(429, new { message = "GitHub API rate limit exceeded", resetAt = ex.Reset });
            }
            catch (AuthorizationException ex)
            {
                _logger.LogError(ex, "GitHub authorization error");
                return StatusCode(401, new { message = "GitHub authorization error", details = ex.Message });
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "GitHub API error: {StatusCode}", ex.StatusCode);
                return StatusCode((int)ex.StatusCode, new { message = "GitHub API error", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting GitHub files");
                return StatusCode(500, new { message = "An error occurred while retrieving GitHub files", details = ex.Message });
            }
        }
        
        /// <summary>
        /// Debug endpoint to show GitHub configuration
        /// </summary>
        /// <returns>The current GitHub configuration</returns>
        [HttpGet("github-config")]
        public ActionResult<object> GetGitHubConfig()
        {
            try
            {
                _logger.LogInformation("Debug request: GET /api/debug/github-config");
                
                // Get GitHub configuration
                var owner = _configuration["GitHub:Owner"] ?? "deactv8";
                var repo = _configuration["GitHub:Repo"] ?? "content";
                var branch = _configuration["GitHub:Branch"] ?? "master";
                var tutorialsPath = _configuration["GitHub:TutorialsPath"] ?? "tutorials";
                var hasToken = !string.IsNullOrEmpty(_configuration["GitHub:AccessToken"]);
                
                return Ok(new
                {
                    Owner = owner,
                    Repository = repo,
                    Branch = branch,
                    TutorialsPath = tutorialsPath,
                    IsAuthenticated = hasToken,
                    FullPath = $"https://github.com/{owner}/{repo}/tree/{branch}/{tutorialsPath}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GitHub configuration");
                return StatusCode(500, new { message = "An error occurred while retrieving GitHub configuration" });
            }
        }
    }
}