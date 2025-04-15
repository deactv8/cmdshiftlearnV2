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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}