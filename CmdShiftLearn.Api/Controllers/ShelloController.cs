using Microsoft.AspNetCore.Mvc;
using CmdShiftLearn.Api.Services;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShelloController : ControllerBase
    {
        private readonly IShelloService _shelloService;
        private readonly ILogger<ShelloController> _logger;
        
        public ShelloController(IShelloService shelloService, ILogger<ShelloController> logger)
        {
            _shelloService = shelloService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets a hint from Shello for a specific tutorial step
        /// </summary>
        /// <param name="tutorialId">The ID of the tutorial</param>
        /// <param name="stepId">The ID of the step</param>
        /// <param name="userInput">The user's input</param>
        /// <param name="previousHint">Optional previous hint that was given</param>
        /// <returns>A hint from Shello</returns>
        [HttpGet("hint")]
        public async Task<ActionResult<string>> GetHint(
            [FromQuery] string tutorialId,
            [FromQuery] string stepId,
            [FromQuery] string userInput,
            [FromQuery] string? previousHint = null)
        {
            try
            {
                _logger.LogInformation("Getting hint from Shello for tutorial: {TutorialId}, step: {StepId}", 
                    tutorialId, stepId);
                
                var hint = await _shelloService.GetHintFromShelloAsync(
                    tutorialId, stepId, userInput, previousHint);
                
                return Ok(new { hint });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hint from Shello");
                return StatusCode(500, new { message = "An error occurred while getting a hint from Shello" });
            }
        }
    }
}