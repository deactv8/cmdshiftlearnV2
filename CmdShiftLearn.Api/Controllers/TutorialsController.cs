using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TutorialsController : ControllerBase
    {
        private readonly TutorialService _tutorialService;
        private readonly ILogger<TutorialsController> _logger;
        
        public TutorialsController(TutorialService tutorialService, ILogger<TutorialsController> logger)
        {
            _tutorialService = tutorialService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets a list of all available tutorials with metadata
        /// </summary>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/tutorials
        ///     [
        ///       {
        ///         "id": "powershell-basics-1",
        ///         "title": "PowerShell Basics: Part 1",
        ///         "description": "Learn the basics of PowerShell including commands, variables, and pipelines.",
        ///         "xp": 100,
        ///         "difficulty": "Beginner"
        ///       },
        ///       {
        ///         "id": "powershell-basics-2",
        ///         "title": "PowerShell Basics: Part 2",
        ///         "description": "Continue learning PowerShell with functions, loops, and conditionals.",
        ///         "xp": 150,
        ///         "difficulty": "Beginner"
        ///       }
        ///     ]
        ///
        /// </remarks>
        /// <returns>A list of tutorial metadata</returns>
        /// <response code="200">Returns the list of tutorials</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TutorialMetadata>>> GetAllTutorials()
        {
            try
            {
                var tutorials = await _tutorialService.GetAllTutorialMetadataAsync();
                return Ok(tutorials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tutorials");
                return StatusCode(500, "An error occurred while retrieving tutorials");
            }
        }
        
        /// <summary>
        /// Gets a specific tutorial by ID including its content
        /// </summary>
        /// <param name="id">The tutorial ID</param>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/tutorials/powershell-basics-1
        ///     {
        ///       "id": "powershell-basics-1",
        ///       "title": "PowerShell Basics: Part 1",
        ///       "description": "Learn the basics of PowerShell including commands, variables, and pipelines.",
        ///       "xp": 100,
        ///       "difficulty": "Beginner",
        ///       "content": "# PowerShell Basics: Part 1\n\n## Introduction\nPowerShell is a powerful scripting language...\n\n## Commands\n$name = 'World'\nWrite-Host \"Hello, $name!\"\n"
        ///     }
        ///
        /// </remarks>
        /// <returns>The tutorial with content</returns>
        /// <response code="200">Returns the tutorial</response>
        /// <response code="404">If the tutorial is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Tutorial>> GetTutorialById(string id)
        {
            try
            {
                var tutorial = await _tutorialService.GetTutorialByIdAsync(id);
                
                if (tutorial == null)
                {
                    return NotFound();
                }
                
                return Ok(tutorial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial by ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the tutorial");
            }
        }
    }
}