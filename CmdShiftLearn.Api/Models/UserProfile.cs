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
        public List<XpEntry> XpLog { get; set; } = new List<XpEntry>();
        public List<Achievement> Achievements { get; set; } = new();
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
}