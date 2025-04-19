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
        private readonly IEventLogger _eventLogger;

        public UserProfileController(IUserProfileService userProfileService, IEventLogger eventLogger)
        {
            _userProfileService = userProfileService;
            _eventLogger = eventLogger;
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

            // Check if this is the first time the user is getting XP
            bool isFirstXp = userProfile.XpLog.Count == 0;
            
            // Add XP and calculate new level
            int oldLevel = userProfile.Level;
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

            // Update the user profile
            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            
            // Log XP added event
            await _userProfileService.LogXpAddedAsync(updatedProfile, request.Amount, request.Reason);
            
            // Award achievements and milestones (non-blocking)
            if (isFirstXp)
            {
                // First Blood achievement for gaining XP for the first time
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "first-blood", 
                    "First Blood", 
                    "Awarded for gaining XP for the first time"
                );
            }
            
            // Level Up achievement if the user reached level 2 or higher
            if (userProfile.Level >= 2 && oldLevel < userProfile.Level)
            {
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "level-up", 
                    "Level Up!", 
                    "Reached Level 2 or higher"
                );
            }
            
            // Award milestone when user reaches 500 XP
            if (userProfile.XP >= 500 && (userProfile.XP - request.Amount) < 500)
            {
                _ = _userProfileService.AwardMilestoneAsync(
                    updatedProfile,
                    "reached-500-xp"
                );
            }
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
        /// Gets the user's progress including level, XP, XP leveling details, completed tutorials, completed challenges, and recent XP history
        /// </summary>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/UserProfile/progress
        ///     {
        ///        "level": 2,
        ///        "xp": 150,
        ///        "xpToNextLevel": 50,
        ///        "nextLevelXp": 200,
        ///        "completedTutorials": ["powershell-basics-1", "powershell-basics-2"],
        ///        "completedChallenges": ["powershell-challenge-1"],
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

            // Calculate next level XP threshold using the formula: nextLevelXp = currentLevel * 100
            int nextLevelXp = userProfile.Level * 100;
            
            // Calculate XP needed to reach the next level (minimum of 0)
            int xpToNextLevel = Math.Max(0, nextLevelXp - userProfile.XP);
            
            // Create the progress response
            var progressResponse = new UserProgressResponse
            {
                Level = userProfile.Level,
                XP = userProfile.XP,
                XpToNextLevel = xpToNextLevel,
                NextLevelXp = nextLevelXp,
                // Convert dictionary to array of completed tutorial IDs (where value is true)
                CompletedTutorials = userProfile.CompletedTutorials
                    .Where(t => t.Value)
                    .Select(t => t.Key)
                    .ToList(),
                // Convert dictionary to array of completed challenge IDs (where value is true)
                CompletedChallenges = userProfile.CompletedChallenges
                    .Where(c => c.Value)
                    .Select(c => c.Key)
                    .ToList(),
                // Convert dictionary to array of unlocked milestone IDs (where value is true)
                Milestones = userProfile.Milestones
                    .Where(m => m.Value)
                    .Select(m => m.Key)
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

            // Check if this is the first daily login claim
            bool isFirstDailyLogin = !userProfile.LastLoginAt.HasValue;
            
            // Award 25 XP for daily login
            int oldLevel = userProfile.Level;
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

            // Update the user profile
            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            
            // Award achievements (non-blocking)
            if (isFirstDailyLogin)
            {
                // Showed Up achievement for claiming a daily login bonus for the first time
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "showed-up", 
                    "Showed Up", 
                    "Claimed a daily login bonus for the first time"
                );
            }
            
            // Level Up achievement if the user reached level 2 or higher
            if (userProfile.Level >= 2 && oldLevel < userProfile.Level)
            {
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "level-up", 
                    "Level Up!", 
                    "Reached Level 2 or higher"
                );
            }
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

            // Check if this is the first tutorial completion
            bool isFirstTutorial = !userProfile.CompletedTutorials.Any(t => t.Value);
            
            // Mark the tutorial as completed
            userProfile.CompletedTutorials[request.TutorialId] = true;

            // Add XP and calculate new level
            int oldLevel = userProfile.Level;
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

            // Update the user profile
            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            
            // Log XP added event
            await _userProfileService.LogXpAddedAsync(updatedProfile, request.XP, $"Completed {request.TutorialId}");
            
            // Log tutorial completion event
            await _eventLogger.LogAsync(new PlatformEvent
            {
                EventType = "tutorial.completed",
                UserId = updatedProfile.SupabaseUid,
                Description = $"Completed tutorial: {request.TutorialId}"
            });
            
            // Award achievements and milestones (non-blocking)
            if (isFirstTutorial)
            {
                // Terminal Initiate achievement for completing the first tutorial
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "terminal-initiate", 
                    "Terminal Initiate", 
                    "Completed your first tutorial"
                );
                
                // Award milestone for completing first tutorial
                _ = _userProfileService.AwardMilestoneAsync(
                    updatedProfile,
                    "completed-first-tutorial"
                );
            }
            
            // Level Up achievement if the user reached level 2 or higher
            if (userProfile.Level >= 2 && oldLevel < userProfile.Level)
            {
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "level-up", 
                    "Level Up!", 
                    "Reached Level 2 or higher"
                );
            }
            return Ok(updatedProfile);
        }
        
        /// <summary>
        /// Marks a challenge as completed and awards XP to the user
        /// </summary>
        /// <param name="request">The challenge completion request containing challengeId and XP amount</param>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/UserProfile/complete-challenge
        ///     {
        ///        "challengeId": "powershell-challenge-1",
        ///        "xp": 100
        ///     }
        ///
        /// </remarks>
        /// <returns>The updated user profile with the completed challenge and new XP</returns>
        /// <response code="200">Returns the updated user profile</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user profile is not found</response>
        /// <response code="409">If the challenge is already marked as completed</response>
        [HttpPost("complete-challenge")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserProfile>> CompleteChallenge([FromBody] CompleteChallengeRequest request)
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

            // Check if the challenge is already completed
            if (userProfile.CompletedChallenges.ContainsKey(request.ChallengeId) && 
                userProfile.CompletedChallenges[request.ChallengeId])
            {
                return Conflict(new { message = "Challenge already completed." });
            }

            // Check if this is the first challenge completion
            bool isFirstChallenge = !userProfile.CompletedChallenges.Any(c => c.Value);
            
            // Mark the challenge as completed
            userProfile.CompletedChallenges[request.ChallengeId] = true;

            // Add XP and calculate new level
            int oldLevel = userProfile.Level;
            userProfile.XP += request.Xp;
            
            // Calculate level using the helper method
            userProfile.Level = _userProfileService.CalculateLevel(userProfile.XP);
            
            // Update the timestamp
            userProfile.UpdatedAt = DateTime.UtcNow;
            
            // Add to XP history log with the challenge completion reason
            userProfile.XpLog.Add(new Models.XpEntry
            {
                Amount = request.Xp,
                Reason = $"Completed challenge {request.ChallengeId}",
                Date = DateTime.UtcNow
            });

            // Update the user profile
            var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);
            
            // Log XP added event
            await _userProfileService.LogXpAddedAsync(updatedProfile, request.Xp, $"Completed challenge {request.ChallengeId}");
            
            // Log challenge completion event
            await _eventLogger.LogAsync(new PlatformEvent
            {
                EventType = "challenge.completed",
                UserId = updatedProfile.SupabaseUid,
                Description = $"Completed challenge: {request.ChallengeId}"
            });
            
            // Award achievements (non-blocking)
            if (isFirstChallenge)
            {
                // Challenge Accepted achievement for completing the first challenge
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "challenge-accepted", 
                    "Challenge Accepted", 
                    "Completed your first challenge"
                );
            }
            
            // Level Up achievement if the user reached level 2 or higher
            if (userProfile.Level >= 2 && oldLevel < userProfile.Level)
            {
                _ = _userProfileService.AwardAchievementAsync(
                    updatedProfile, 
                    "level-up", 
                    "Level Up!", 
                    "Reached Level 2 or higher"
                );
            }
            return Ok(updatedProfile);
        }
        
        /// <summary>
        /// Gets the achievements unlocked by the currently logged-in user
        /// </summary>
        /// <remarks>
        /// Sample response:
        ///
        ///     GET /api/UserProfile/achievements
        ///     {
        ///        "achievements": [
        ///           {
        ///              "id": "first-login",
        ///              "title": "First Steps",
        ///              "description": "Logged in for the first time",
        ///              "unlockedAt": "2025-04-15T18:30:25.123Z"
        ///           },
        ///           {
        ///              "id": "complete-tutorial",
        ///              "title": "Quick Learner",
        ///              "description": "Completed your first tutorial",
        ///              "unlockedAt": "2025-04-16T14:22:18.456Z"
        ///           }
        ///        ]
        ///     }
        ///
        /// </remarks>
        /// <returns>The user's unlocked achievements</returns>
        /// <response code="200">Returns the user's achievements</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user profile is not found</response>
        [HttpGet("achievements")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserAchievementsResponse>> GetUserAchievements()
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

            // Create the achievements response
            var achievementsResponse = new UserAchievementsResponse
            {
                Achievements = userProfile.Achievements
            };

            return Ok(achievementsResponse);
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
    
    public class CompleteChallengeRequest
    {
        public string ChallengeId { get; set; } = string.Empty;
        public int Xp { get; set; }
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
        /// The amount of XP needed to reach the next level
        /// </summary>
        public int XpToNextLevel { get; set; }
        
        /// <summary>
        /// The total XP threshold for the next level
        /// </summary>
        public int NextLevelXp { get; set; }
        
        /// <summary>
        /// List of completed tutorial IDs
        /// </summary>
        public List<string> CompletedTutorials { get; set; } = new List<string>();
        
        /// <summary>
        /// List of completed challenge IDs
        /// </summary>
        public List<string> CompletedChallenges { get; set; } = new List<string>();
        
        /// <summary>
        /// List of unlocked milestone IDs
        /// </summary>
        public List<string> Milestones { get; set; } = new List<string>();
        
        /// <summary>
        /// The last 5 XP log entries, sorted by newest first
        /// </summary>
        public List<XpEntry> XpLog { get; set; } = new List<XpEntry>();
    }

    public class UserAchievementsResponse
    {
        /// <summary>
        /// List of achievements unlocked by the user
        /// </summary>
        public List<Achievement> Achievements { get; set; } = new List<Achievement>();
    }
}