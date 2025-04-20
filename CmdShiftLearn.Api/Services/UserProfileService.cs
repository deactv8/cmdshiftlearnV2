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
        Task<UserProfile> CheckAndAwardXpRewardsAsync(UserProfile profile, int previousXp);
        Task LogXpAddedAsync(UserProfile profile, int amount, string reason);
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
        
        /// <summary>
        /// Checks if the user has reached any XP thresholds and awards corresponding rewards
        /// </summary>
        /// <param name="profile">The user profile</param>
        /// <param name="previousXp">The user's XP before the latest addition</param>
        /// <returns>The updated user profile</returns>
        public async Task<UserProfile> CheckAndAwardXpRewardsAsync(UserProfile profile, int previousXp)
        {
            // Define XP thresholds and their corresponding rewards
            var xpRewards = new Dictionary<int, (string id, string name, string description, string type, string data)>
            {
                { 100, ("dark-theme", "Dark Theme", "Unlock the dark theme for the terminal", "theme", "{\"theme\":\"dark\"}") },
                { 250, ("syntax-highlighting", "Syntax Highlighting", "Unlock syntax highlighting for code blocks", "feature", "{\"feature\":\"syntax-highlighting\"}") },
                { 500, ("advanced-commands", "Advanced Commands", "Unlock access to advanced PowerShell commands", "content", "{\"contentType\":\"commands\",\"level\":\"advanced\"}") },
                { 1000, ("custom-prompt", "Custom Prompt", "Customize your terminal prompt", "feature", "{\"feature\":\"custom-prompt\"}") },
                { 2000, ("expert-badge", "PowerShell Expert", "You've earned the PowerShell Expert badge", "badge", "{\"badge\":\"powershell-expert\",\"color\":\"gold\"}") }
            };
            
            // Check each threshold to see if the user has crossed it with this XP update
            foreach (var threshold in xpRewards.Keys.OrderBy(k => k))
            {
                // If the user's previous XP was below the threshold but current XP is at or above it
                if (previousXp < threshold && profile.XP >= threshold)
                {
                    var (id, name, description, type, data) = xpRewards[threshold];
                    
                    // Check if the user already has this reward
                    if (!profile.Rewards.Any(r => r.Id == id))
                    {
                        // Create the new reward
                        var reward = new Reward
                        {
                            Id = id,
                            Name = name,
                            Description = description,
                            Type = type,
                            Data = data,
                            UnlockedAt = DateTime.UtcNow
                        };
                        
                        // Add the reward to the user's profile
                        profile.Rewards.Add(reward);
                        
                        // Update the user profile
                        await UpdateUserProfileAsync(profile);
                        
                        // Log the reward unlock
                        Console.WriteLine($"XP Reward unlocked for {profile.Email}: {name} - {description}");
                        
                        // Log the reward event
                        await _eventLogger.LogAsync(new PlatformEvent
                        {
                            EventType = "reward.unlocked",
                            UserId = profile.SupabaseUid,
                            Description = $"Unlocked reward: {name} - {description}"
                        });
                    }
                }
            }
            
            return profile;
        }
    }
}