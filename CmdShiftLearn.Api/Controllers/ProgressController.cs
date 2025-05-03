using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProgressController : ControllerBase
    {
        private readonly ILogger<ProgressController> _logger;

        public ProgressController(ILogger<ProgressController> logger)
        {
            _logger = logger;
        }

        [HttpPost("tutorial-complete")]
        public IActionResult TutorialComplete([FromBody] TutorialCompleteRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (request == null || string.IsNullOrEmpty(request.TutorialId))
            {
                return BadRequest(new { Message = "TutorialId is required" });
            }

            _logger.LogInformation(
                "User {UserId} completed tutorial {TutorialId} and earned {XP} XP", 
                userId, 
                request.TutorialId,
                request.XpEarned);

            // In a real implementation, you'd store this in a database
            // For now, we'll just return success
            
            return Ok(new { Success = true });
        }
    }

    public class TutorialCompleteRequest
    {
        public string TutorialId { get; set; }
        public int XpEarned { get; set; }
    }
}