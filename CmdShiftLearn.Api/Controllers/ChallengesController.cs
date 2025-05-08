using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allow anonymous access to all methods in this controller
    public class ChallengesController : ControllerBase
    {
        private readonly ChallengeService _challengeService;
        private readonly ILogger<ChallengesController> _logger;
        
        public ChallengesController(ChallengeService challengeService, ILogger<ChallengesController> logger)
        {
            _challengeService = challengeService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets a list of all available challenges with metadata
        /// </summary>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/challenges
        ///     [
        ///       {
        ///         "id": "powershell-variables",
        ///         "title": "PowerShell Variables Challenge",
        ///         "description": "Test your knowledge of PowerShell variables and data types.",
        ///         "xp": 50,
        ///         "difficulty": "Beginner",
        ///         "tutorialId": "powershell-basics-1"
        ///       },
        ///       {
        ///         "id": "powershell-loops",
        ///         "title": "PowerShell Loops Challenge",
        ///         "description": "Practice using loops in PowerShell to solve problems.",
        ///         "xp": 75,
        ///         "difficulty": "Intermediate",
        ///         "tutorialId": "powershell-basics-2"
        ///       }
        ///     ]
        ///
        /// </remarks>
        /// <returns>A list of challenge metadata</returns>
        /// <response code="200">Returns the list of challenges</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ChallengeMetadata>>> GetAllChallenges()
        {
            try
            {
                var challenges = await _challengeService.GetAllChallengeMetadataAsync();
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all challenges");
                return StatusCode(500, "An error occurred while retrieving challenges");
            }
        }
        
        /// <summary>
        /// Gets a specific challenge by ID including its script
        /// </summary>
        /// <param name="id">The challenge ID</param>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/challenges/powershell-variables
        ///     {
        ///       "id": "powershell-variables",
        ///       "title": "PowerShell Variables Challenge",
        ///       "description": "Test your knowledge of PowerShell variables and data types.",
        ///       "xp": 50,
        ///       "difficulty": "Beginner",
        ///       "tutorialId": "powershell-basics-1",
        ///       "script": "# PowerShell Variables Challenge\n\n# 1. Create a variable named $name and assign your name to it\n# 2. Create a variable named $age and assign your age to it\n# 3. Create a variable named $skills as an array with at least three skills\n# 4. Print a message using these variables\n\n# Your solution below:\n\n"
        ///     }
        ///
        /// </remarks>
        /// <returns>The challenge with script</returns>
        /// <response code="200">Returns the challenge</response>
        /// <response code="404">If the challenge is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Challenge>> GetChallengeById(string id)
        {
            try
            {
                var challenge = await _challengeService.GetChallengeByIdAsync(id);
                
                if (challenge == null)
                {
                    return NotFound();
                }
                
                return Ok(challenge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge by ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the challenge");
            }
        }
        
        /// <summary>
        /// Gets all challenges associated with a specific tutorial
        /// </summary>
        /// <param name="tutorialId">The tutorial ID</param>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/challenges/by-tutorial/powershell-basics-1
        ///     [
        ///       {
        ///         "id": "powershell-variables",
        ///         "title": "PowerShell Variables Challenge",
        ///         "description": "Test your knowledge of PowerShell variables and data types.",
        ///         "xp": 50,
        ///         "difficulty": "Beginner",
        ///         "tutorialId": "powershell-basics-1"
        ///       },
        ///       {
        ///         "id": "powershell-commands",
        ///         "title": "PowerShell Commands Challenge",
        ///         "description": "Practice using basic PowerShell commands.",
        ///         "xp": 60,
        ///         "difficulty": "Beginner",
        ///         "tutorialId": "powershell-basics-1"
        ///       }
        ///     ]
        ///
        /// </remarks>
        /// <returns>A list of challenge metadata for the specified tutorial</returns>
        /// <response code="200">Returns the list of challenges for the tutorial</response>
        [HttpGet("by-tutorial/{tutorialId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ChallengeMetadata>>> GetChallengesByTutorialId(string tutorialId)
        {
            try
            {
                var challenges = await _challengeService.GetChallengesByTutorialIdAsync(tutorialId);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenges by tutorial ID: {TutorialId}", tutorialId);
                return StatusCode(500, "An error occurred while retrieving challenges for the tutorial");
            }
        }
    }
}