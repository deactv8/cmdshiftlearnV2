using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;

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

        /// <summary>
        /// Gets the user's progress including level, XP, completed tutorials, and recent XP history
        /// </summary>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/UserProfile/progress
        ///     {
        ///        "level": 2,
        ///        "xp": 150,
        ///        "completedTutorials": ["powershell-basics-1", "powershell-basics-2"],
        ///        "xpLog": [
        ///           {
        ///              "amount": 100,
        ///              "reason": "Completed powershell-basics-1",
        ///              "date": "2025-04-16T21:28:42.920Z"
        ///           },
        ///           {
        ///              "amount": 50,
        ///              "reason": "Completed powershell-basics-2",
        ///              "date": "2025-04-16T21:33:22.540Z"
        ///           }
        ///        ]
        ///     }
        ///
        /// </remarks>
        /// <returns>The user's progress information</returns>
        /// <response code="200">Returns the user's progress</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user profile is not found</response>
        [HttpGet("progress")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProgressResponse>> GetUserProgress()
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

            // Create the progress response
            var progressResponse = new UserProgressResponse
            {
                Level = userProfile.Level,
                XP = userProfile.XP,
                // Convert dictionary to array of completed tutorial IDs (where value is true)
                CompletedTutorials = userProfile.CompletedTutorials
                    .Where(t => t.Value)
                    .Select(t => t.Key)
                    .ToList(),
                // Get the last 5 XP log entries sorted by newest first
                XpLog = userProfile.XpLog
                    .OrderByDescending(x => x.Date)
                    .Take(5)
                    .ToList()
            };

            return Ok(progressResponse);
        }

        /// <summary>
        /// Claims the daily login bonus of 25 XP if not already claimed today
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/UserProfile/daily-login
        ///     
        /// Sample response:
        ///     {
        ///         "id": "user123",
        ///         "supabaseUid": "auth0|123456789",
        ///         "email": "user@example.com",
        ///         "xp": 175,
        ///         "level": 2,
        ///         "completedTutorials": {
        ///             "powershell-basics-1": true,
        ///             "powershell-basics-2": true
        ///         },
        ///         "xpLog": [
        ///             {
        ///                 "amount": 25,
        ///                 "reason": "Daily login bonus",
        ///                 "date": "2025-04-17T08:15:30.123Z"
        ///             },
        ///             ...
        ///         ],
        ///         "createdAt": "2025-04-10T12:00:00.000Z",
        ///         "updatedAt": "2025-04-17T08:15:30.123Z",
        ///         "lastLoginAt": "2025-04-17T08:15:30.123Z"
        ///     }
        ///
        /// </remarks>
        /// <returns>The updated user profile with the daily login bonus XP</returns>
        /// <response code="200">Returns the updated user profile</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user profile is not found</response>
        /// <response code="409">If the daily login bonus was already claimed today</response>
        [HttpPost("daily-login")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserProfile>> ClaimDailyLoginBonus()
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

            // Get current UTC date (without time)
            var today = DateTime.UtcNow.Date;

            // Check if the user has already claimed the daily login bonus today
            if (userProfile.LastLoginAt.HasValue && userProfile.LastLoginAt.Value.Date == today)
            {
                return Conflict(new { message = "Daily login bonus already claimed today." });
            }

            // Award 25 XP for daily login
            userProfile.XP += 25;
            
            // Calculate level using the helper method
            userProfile.Level = _userProfileService.CalculateLevel(userProfile.XP);
            
            // Update the timestamps
            var currentUtc = DateTime.UtcNow;
            userProfile.UpdatedAt = currentUtc;
            userProfile.LastLoginAt = currentUtc;
            
            // Add to XP history log
            userProfile.XpLog.Add(new Models.XpEntry
            {
                Amount = 25,
                Reason = "Daily login bonus",
                Date = currentUtc
            });

            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            return Ok(updatedProfile);
        }

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

    public class UserProgressResponse
    {
        /// <summary>
        /// The user's current level
        /// </summary>
        public int Level { get; set; }
        
        /// <summary>
        /// The user's total XP
        /// </summary>
        public int XP { get; set; }
        
        /// <summary>
        /// List of completed tutorial IDs
        /// </summary>
        public List<string> CompletedTutorials { get; set; } = new List<string>();
        
        /// <summary>
        /// The last 5 XP log entries, sorted by newest first
        /// </summary>
        public List<XpEntry> XpLog { get; set; } = new List<XpEntry>();
    }
}