namespace CmdShiftLearn.Api.Models
{
    public class PlatformEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}