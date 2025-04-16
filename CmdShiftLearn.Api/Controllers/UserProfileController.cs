using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> GetMyProfile()
        {
            var supabaseUid = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(supabaseUid))
            {
                return Unauthorized();
            }

            var userProfile = await _userProfileService.GetUserProfileAsync(supabaseUid);
            if (userProfile == null)
            {
                return NotFound();
            }

            return Ok(userProfile);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> UpdateMyProfile([FromBody] UserProfileUpdateRequest request)
        {
            var supabaseUid = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(supabaseUid))
            {
                return Unauthorized();
            }

            var userProfile = await _userProfileService.GetUserProfileAsync(supabaseUid);
            if (userProfile == null)
            {
                return NotFound();
            }

            // Update only allowed fields
            if (request.CompletedTutorials != null)
            {
                userProfile.CompletedTutorials = request.CompletedTutorials;
            }

            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            return Ok(updatedProfile);
        }

        /// <summary>
        /// Awards XP to the currently logged-in user
        /// </summary>
        /// <param name="request">The XP request containing amount and reason</param>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/UserProfile/xp
        ///     {
        ///        "amount": 50,
        ///        "reason": "Completed first tutorial"
        ///     }
        ///
        /// </remarks>
        /// <returns>The updated user profile with new XP and level</returns>
        [HttpPost("xp")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> AddXp([FromBody] AddXpRequest request)
        {
            var supabaseUid = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(supabaseUid))
            {
                return Unauthorized();
            }

            var userProfile = await _userProfileService.GetUserProfileAsync(supabaseUid);
            if (userProfile == null)
            {
                return NotFound();
            }

            // Add XP and calculate new level
            userProfile.XP += request.Amount;
            
            // Calculate level using the helper method
            userProfile.Level = _userProfileService.CalculateLevel(userProfile.XP);
            
            // Update the timestamp
            userProfile.UpdatedAt = DateTime.UtcNow;
            
            // Add to XP history log
            userProfile.XpLog.Add(new Models.XpEntry
            {
                Amount = request.Amount,
                Reason = request.Reason,
                Date = DateTime.UtcNow
            });

            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            return Ok(updatedProfile);
        }
        
        /// <summary>
        /// Marks a tutorial as completed and awards XP to the user
        /// </summary>
        /// <param name="request">The tutorial completion request containing tutorialId and XP amount</param>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/UserProfile/complete-tutorial
        ///     {
        ///        "tutorialId": "powershell-basics.lesson-1",
        ///        "xp": 50
        ///     }
        ///
        /// </remarks>
        /// <returns>The updated user profile with the completed tutorial and new XP</returns>
        /// <response code="200">Returns the updated user profile</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user profile is not found</response>
        /// <response code="409">If the tutorial is already marked as completed</response>
        [HttpPost("complete-tutorial")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserProfile>> CompleteTutorial([FromBody] CompleteTutorialRequest request)
        {
            var supabaseUid = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(supabaseUid))
            {
                return Unauthorized();
            }

            var userProfile = await _userProfileService.GetUserProfileAsync(supabaseUid);
            if (userProfile == null)
            {
                return NotFound();
            }

            // Check if the tutorial is already completed
            if (userProfile.CompletedTutorials.ContainsKey(request.TutorialId) && 
                userProfile.CompletedTutorials[request.TutorialId])
            {
                return Conflict(new { message = "Tutorial already completed." });
            }

            // Mark the tutorial as completed
            userProfile.CompletedTutorials[request.TutorialId] = true;

            // Add XP and calculate new level
            userProfile.XP += request.XP;
            
            // Calculate level using the helper method
            userProfile.Level = _userProfileService.CalculateLevel(userProfile.XP);
            
            // Update the timestamp
            userProfile.UpdatedAt = DateTime.UtcNow;
            
            // Add to XP history log with the tutorial completion reason
            userProfile.XpLog.Add(new Models.XpEntry
            {
                Amount = request.XP,
                Reason = $"Completed {request.TutorialId}",
                Date = DateTime.UtcNow
            });

            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            return Ok(updatedProfile);
        }
    }

    public class UserProfileUpdateRequest
    {
        public Dictionary<string, bool>? CompletedTutorials { get; set; }
    }

    public class AddXpRequest
    {
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class CompleteTutorialRequest
    {
        public string TutorialId { get; set; } = string.Empty;
        public int XP { get; set; }
    }
}