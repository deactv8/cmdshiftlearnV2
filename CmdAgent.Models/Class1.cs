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

    public class TutorialStep
    {
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string ExpectedCommand { get; set; } = string.Empty;
    }

    public class StepResult
    {
        public bool IsCorrect { get; set; }
        public bool IsComplete { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string? HintFromShello { get; set; }
        public int? NextStepIndex { get; set; }
        public int XpEarned { get; set; }
    }
}