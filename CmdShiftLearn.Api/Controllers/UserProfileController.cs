using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            
            // Simple level calculation: level = 1 + XP / 100
            userProfile.Level = 1 + (userProfile.XP / 100);

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
    }
}