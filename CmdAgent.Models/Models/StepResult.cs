namespace CmdAgent.Models
{
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