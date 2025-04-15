namespace CmdShiftLearn.Api.Models
{
    public class SupabaseSettings
    {
        public string Url { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string JwtSecret { get; set; } = string.Empty;
    }
}