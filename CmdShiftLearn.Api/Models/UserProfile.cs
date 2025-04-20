namespace CmdShiftLearn.Api.Models
{
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string SupabaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int XP { get; set; } = 0;
        public int Level { get; set; } = 1;
        public Dictionary<string, bool> CompletedTutorials { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> CompletedChallenges { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> Milestones { get; set; } = new Dictionary<string, bool>();
        public List<XpEntry> XpLog { get; set; } = new List<XpEntry>();
        public List<Achievement> Achievements { get; set; } = new();
        public List<Reward> Rewards { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }

    public class XpEntry
    {
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class Achievement
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class Reward
    {
        /// <summary>
        /// Unique identifier for the reward
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name of the reward
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the reward
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of reward (e.g., "theme", "badge", "content", "feature")
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional data specific to the reward type (JSON serialized)
        /// </summary>
        public string Data { get; set; } = string.Empty;
        
        /// <summary>
        /// When the reward was unlocked
        /// </summary>
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }
}