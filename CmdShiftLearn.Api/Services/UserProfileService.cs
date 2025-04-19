using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile?> GetUserProfileAsync(string supabaseUid);
        Task<UserProfile> CreateUserProfileAsync(string supabaseUid, string email);
        Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile);
        int CalculateLevel(int xp);
        Task<UserProfile> AwardAchievementAsync(UserProfile profile, string id, string title, string description);
        Task<UserProfile> AwardMilestoneAsync(UserProfile profile, string milestoneId);
    }

    public class UserProfileService : IUserProfileService
    {
        private readonly IEventLogger _eventLogger;
        
        // In a real application, this would be stored in a database
        private static readonly Dictionary<string, UserProfile> _userProfiles = new();
        
        public UserProfileService(IEventLogger eventLogger)
        {
            _eventLogger = eventLogger;
        }

        public Task<UserProfile?> GetUserProfileAsync(string supabaseUid)
        {
            _userProfiles.TryGetValue(supabaseUid, out var userProfile);
            return Task.FromResult(userProfile);
        }

        public Task<UserProfile> CreateUserProfileAsync(string supabaseUid, string email)
        {
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                SupabaseUid = supabaseUid,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _userProfiles[supabaseUid] = userProfile;
            return Task.FromResult(userProfile);
        }

        public Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile)
        {
            userProfile.UpdatedAt = DateTime.UtcNow;
            _userProfiles[userProfile.SupabaseUid] = userProfile;
            return Task.FromResult(userProfile);
        }
        
        // Helper method to log XP added events
        public async Task LogXpAddedAsync(UserProfile profile, int amount, string reason)
        {
            await _eventLogger.LogAsync(new PlatformEvent
            {
                EventType = "xp.added",
                UserId = profile.SupabaseUid,
                Description = $"Added {amount} XP: {reason}"
            });
        }
        
        public int CalculateLevel(int xp)
        {
            // Simple level calculation: level = Math.Floor(xp / 100) + 1
            return (int)Math.Floor(xp / 100.0) + 1;
        }
        
        /// <summary>
        /// Awards an achievement to a user if they don't already have it
        /// </summary>
        /// <param name="profile">The user profile</param>
        /// <param name="id">Unique identifier for the achievement</param>
        /// <param name="title">Display title of the achievement</param>
        /// <param name="description">Description of how the achievement was earned</param>
        /// <returns>The updated user profile</returns>
        public async Task<UserProfile> AwardAchievementAsync(UserProfile profile, string id, string title, string description)
        {
            // Initialize the Achievements collection if it's null
            if (profile.Achievements == null)
            {
                profile.Achievements = new List<Achievement>();
            }
            
            // Check if the user already has this achievement
            if (!profile.Achievements.Any(a => a.Id == id))
            {
                // Create and add the new achievement
                var achievement = new Achievement
                {
                    Id = id,
                    Title = title,
                    Description = description,
                    UnlockedAt = DateTime.UtcNow
                };
                
                profile.Achievements.Add(achievement);
                
                // Update the user profile
                await UpdateUserProfileAsync(profile);
                
                // Log the achievement (for now, we'll add notifications later)
                Console.WriteLine($"Achievement unlocked for {profile.Email}: {title} - {description}");
                
                // Log the achievement event
                await _eventLogger.LogAsync(new PlatformEvent
                {
                    EventType = "achievement.unlocked",
                    UserId = profile.SupabaseUid,
                    Description = $"Unlocked achievement: {title} - {description}"
                });
            }
            
            return profile;
        }
        
        /// <summary>
        /// Awards a milestone to a user if they don't already have it
        /// </summary>
        /// <param name="profile">The user profile</param>
        /// <param name="milestoneId">Unique identifier for the milestone</param>
        /// <returns>The updated user profile</returns>
        public async Task<UserProfile> AwardMilestoneAsync(UserProfile profile, string milestoneId)
        {
            // Initialize the Milestones collection if it's null
            if (profile.Milestones == null)
            {
                profile.Milestones = new Dictionary<string, bool>();
            }
            
            // Check if the user already has this milestone
            if (!profile.Milestones.ContainsKey(milestoneId) || !profile.Milestones[milestoneId])
            {
                // Mark the milestone as completed
                profile.Milestones[milestoneId] = true;
                
                // Update the user profile
                await UpdateUserProfileAsync(profile);
                
                // Log the milestone (for now, we'll add more functionality later)
                Console.WriteLine($"Milestone unlocked for {profile.Email}: {milestoneId}");
                
                // Log the milestone event
                await _eventLogger.LogAsync(new PlatformEvent
                {
                    EventType = "milestone.unlocked",
                    UserId = profile.SupabaseUid,
                    Description = $"Unlocked milestone: {milestoneId}"
                });
            }
            
            return profile;
        }
    }
}