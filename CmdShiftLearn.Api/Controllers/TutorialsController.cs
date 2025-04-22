using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TutorialsController : ControllerBase
    {
        private readonly ITutorialService _tutorialService;
        private readonly IShelloService _shelloService;
        private readonly ILogger<TutorialsController> _logger;
        
        public TutorialsController(
            ITutorialService tutorialService, 
            IShelloService shelloService,
            ILogger<TutorialsController> logger)
        {
            _tutorialService = tutorialService;
            _shelloService = shelloService;
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
                _logger.LogInformation("API Request: GET /api/tutorials");
                
                var tutorials = await _tutorialService.GetAllTutorialMetadataAsync();
                
                _logger.LogInformation("Returning {Count} tutorials", tutorials?.Count() ?? 0);
                
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Tutorial>> GetTutorialById(string id)
        {
            try
            {
                _logger.LogInformation("API Request: GET /api/tutorials/{Id}", id);
                
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Invalid tutorial ID: null or empty");
                    return BadRequest("Tutorial ID cannot be null or empty");
                }
                
                try
                {
                    var tutorial = await _tutorialService.GetTutorialByIdAsync(id);
                    
                    if (tutorial == null)
                    {
                        _logger.LogWarning("Tutorial not found: {Id}", id);
                        return NotFound(new { message = $"Tutorial with ID '{id}' not found" });
                    }
                    
                    // Validate tutorial has required fields
                    if (string.IsNullOrEmpty(tutorial.Id))
                    {
                        _logger.LogWarning("Tutorial has no ID: {Id}", id);
                        tutorial.Id = id;
                    }
                    
                    if (string.IsNullOrEmpty(tutorial.Title))
                    {
                        _logger.LogWarning("Tutorial has no title: {Id}", id);
                        tutorial.Title = "Untitled Tutorial";
                    }
                    
                    if (tutorial.Steps == null)
                    {
                        _logger.LogWarning("Tutorial has null Steps collection: {Id}", id);
                        tutorial.Steps = new List<TutorialStep>();
                    }
                    
                    _logger.LogInformation("Returning tutorial: {Id}, Title: {Title}, Steps: {StepCount}", 
                        tutorial.Id, tutorial.Title, tutorial.Steps.Count);
                    
                    return Ok(tutorial);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in tutorial service when getting tutorial by ID: {Id}", id);
                    return StatusCode(500, new { message = "An error occurred in the tutorial service", error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error getting tutorial by ID: {Id}", id);
                return StatusCode(500, new { message = "An unhandled error occurred while retrieving the tutorial", error = ex.Message });
            }
        }
        
        /// <summary>
        /// Processes a user's input for a specific step in an interactive tutorial
        /// </summary>
        /// <param name="request">The request containing tutorial ID, step index, user input, and optional requestHint flag</param>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/tutorials/run-step
        ///     {
        ///       "tutorialId": "powershell-basics-1",
        ///       "stepIndex": 0,
        ///       "userInput": "Write-Host \"Hello, PowerShell!\"",
        ///       "requestHint": false
        ///     }
        ///
        /// Sample request with hint:
        ///
        ///     POST /api/tutorials/run-step
        ///     {
        ///       "tutorialId": "powershell-basics-1",
        ///       "stepIndex": 2,
        ///       "userInput": "Write-Host 'hi'",
        ///       "requestHint": true
        ///     }
        ///
        /// Sample response (correct):
        ///
        ///     {
        ///       "isCorrect": true,
        ///       "message": "Great job! That's the correct command.",
        ///       "hint": null,
        ///       "hintFromShello": null,
        ///       "nextStepIndex": 1,
        ///       "isComplete": false
        ///     }
        ///
        /// Sample response (incorrect):
        ///
        ///     {
        ///       "isCorrect": false,
        ///       "message": "That's not quite right. Try again!",
        ///       "hint": "Make sure to use Write-Host with proper quotes.",
        ///       "hintFromShello": null,
        ///       "nextStepIndex": null,
        ///       "isComplete": false
        ///     }
        ///
        /// Sample response (incorrect with Shello hint):
        ///
        ///     {
        ///       "isCorrect": false,
        ///       "message": "That's not quite right. Try again!",
        ///       "hint": "Make sure to use Write-Host with proper quotes.",
        ///       "hintFromShello": "It looks like you're close! Remember to use double quotes and match the expected phrase exactly.",
        ///       "nextStepIndex": null,
        ///       "isComplete": false
        ///     }
        ///
        /// Sample response (last step completed):
        ///
        ///     {
        ///       "isCorrect": true,
        ///       "message": "Congratulations! You've completed the tutorial.",
        ///       "hint": null,
        ///       "hintFromShello": null,
        ///       "nextStepIndex": null,
        ///       "isComplete": true
        ///     }
        ///
        /// </remarks>
        /// <returns>The result of processing the step</returns>
        /// <response code="200">Returns the result of processing the step</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the tutorial or step is not found</response>
        [HttpPost("run-step")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RunTutorialStepResponse>> RunTutorialStep([FromBody] RunTutorialStepRequest request)
        {
            try
            {
                // Log authentication status
                _logger.LogInformation($"RunTutorialStep: User is authenticated: {User.Identity?.IsAuthenticated}");
                if (User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation($"RunTutorialStep: User ID: {User.FindFirst("sub")?.Value}");
                }
                
                // Validate request
                if (string.IsNullOrEmpty(request.TutorialId))
                {
                    return BadRequest(new { message = "Tutorial ID is required." });
                }
                
                if (request.StepIndex < 0)
                {
                    return BadRequest(new { message = "Step index must be a non-negative integer." });
                }
                
                // Load the tutorial
                var tutorial = await _tutorialService.GetTutorialByIdAsync(request.TutorialId);
                if (tutorial == null)
                {
                    return NotFound(new { message = $"Tutorial with ID '{request.TutorialId}' not found." });
                }
                
                // Validate step index
                if (tutorial.Steps == null || tutorial.Steps.Count == 0)
                {
                    return BadRequest(new { message = "This tutorial does not have any interactive steps." });
                }
                
                if (request.StepIndex >= tutorial.Steps.Count)
                {
                    return BadRequest(new { message = $"Step index {request.StepIndex} is out of range. The tutorial has {tutorial.Steps.Count} steps." });
                }
                
                // Get the current step
                var currentStep = tutorial.Steps[request.StepIndex];
                
                // Compare user input with expected command (case-insensitive, trimmed)
                string userInput = request.UserInput.Trim();
                string expectedCommand = currentStep.ExpectedCommand.Trim();
                
                bool isCorrect = false;
                
                // If validation rule exists, use it for validation
                if (currentStep.Validation != null)
                {
                    switch (currentStep.Validation.Type.ToLowerInvariant())
                    {
                        case "contains":
                            isCorrect = userInput.Contains(currentStep.Validation.Value, StringComparison.OrdinalIgnoreCase);
                            break;
                        case "equals":
                            isCorrect = string.Equals(userInput, currentStep.Validation.Value, StringComparison.OrdinalIgnoreCase);
                            break;
                        case "regex":
                            isCorrect = System.Text.RegularExpressions.Regex.IsMatch(userInput, currentStep.Validation.Value);
                            break;
                        default:
                            // Fall back to exact match with ExpectedCommand
                            isCorrect = string.Equals(userInput, expectedCommand, StringComparison.OrdinalIgnoreCase);
                            break;
                    }
                }
                else
                {
                    // No validation rule, use exact match
                    isCorrect = string.Equals(userInput, expectedCommand, StringComparison.OrdinalIgnoreCase);
                }
                
                // Prepare the response
                var response = new RunTutorialStepResponse
                {
                    IsCorrect = isCorrect
                };
                
                if (isCorrect)
                {
                    // Check if this was the last step
                    bool isLastStep = request.StepIndex == tutorial.Steps.Count - 1;
                    
                    response.Message = isLastStep 
                        ? "Congratulations! You've completed the tutorial." 
                        : "Great job! That's the correct command.";
                    
                    response.IsComplete = isLastStep;
                    
                    // Set the next step index if there is one
                    if (!isLastStep)
                    {
                        response.NextStepIndex = request.StepIndex + 1;
                    }
                }
                else
                {
                    response.Message = "That's not quite right. Try again!";
                    response.Hint = currentStep.Hint;
                }
                
                // If a hint from Shello was requested, get it
                if (request.RequestHint)
                {
                    _logger.LogInformation("Requesting hint from Shello for tutorial: {TutorialId}, step: {StepId}", 
                        request.TutorialId, currentStep.Id);
                    
                    try
                    {
                        // Get hint from Shello
                        string hintFromShello = await _shelloService.GetHintFromShelloAsync(
                            request.TutorialId,
                            currentStep.Id,
                            request.UserInput,
                            response.Hint);
                        
                        // Add the hint to the response
                        response.HintFromShello = hintFromShello;
                        
                        _logger.LogInformation("Successfully added Shello hint to response for tutorial: {TutorialId}, step: {StepId}", 
                            request.TutorialId, currentStep.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting hint from Shello for tutorial: {TutorialId}, step: {StepId}", 
                            request.TutorialId, currentStep.Id);
                        
                        // Don't fail the whole request if Shello hint fails
                        response.HintFromShello = "I'm having trouble thinking of a hint right now. Please try again later.";
                    }
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tutorial step: {TutorialId}, Step {StepIndex}", 
                    request.TutorialId, request.StepIndex);
                return StatusCode(500, "An error occurred while processing the tutorial step.");
            }
        }
    }
}