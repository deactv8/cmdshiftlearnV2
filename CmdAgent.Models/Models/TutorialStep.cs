namespace CmdAgent.Models
{
    public class TutorialStep
    {
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string ExpectedCommand { get; set; } = string.Empty;
    }
}