using System.Collections.Generic;

namespace CmdAgent.Models
{
    public class Tutorial
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<TutorialStep> Steps { get; set; } = new List<TutorialStep>();
    }
}