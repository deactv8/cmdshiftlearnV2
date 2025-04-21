namespace CmdShiftLearn.Api.Models
{
    /// <summary>
    /// Represents a tutorial with metadata and content
    /// </summary>
    public class Tutorial
    {
        /// <summary>
        /// Unique identifier for the tutorial
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Title of the tutorial
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the tutorial
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// XP awarded for completing the tutorial
        /// </summary>
        public int Xp { get; set; }
        
        /// <summary>
        /// Difficulty level of the tutorial (e.g., "Beginner", "Intermediate", "Advanced")
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;
        
        /// <summary>
        /// Content of the tutorial (PowerShell script or instructions)
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Interactive steps for the tutorial
        /// </summary>
        public List<TutorialStep> Steps { get; set; } = new List<TutorialStep>();
    }
    
    /// <summary>
    /// Represents a step in an interactive tutorial
    /// </summary>
    public class TutorialStep
    {
        /// <summary>
        /// Unique identifier for the step
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Title of the step
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Instructions for the step (can be mapped from 'instruction' in YAML)
        /// </summary>
        public string Instructions { get; set; } = string.Empty;
        
        /// <summary>
        /// The expected command or code to complete this step (can be mapped from 'command' in YAML)
        /// </summary>
        public string ExpectedCommand { get; set; } = string.Empty;
        
        /// <summary>
        /// A hint to show if the user gets stuck
        /// </summary>
        public string Hint { get; set; } = string.Empty;
        
        /// <summary>
        /// Validation rules for the step
        /// </summary>
        public ValidationRule? Validation { get; set; }
    }
    
    /// <summary>
    /// Represents validation rules for a tutorial step
    /// </summary>
    public class ValidationRule
    {
        /// <summary>
        /// Type of validation (e.g., 'contains', 'equals', 'regex')
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Value to validate against
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents tutorial metadata without the full content
    /// </summary>
    public class TutorialMetadata
    {
        /// <summary>
        /// Unique identifier for the tutorial
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Title of the tutorial
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the tutorial
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// XP awarded for completing the tutorial
        /// </summary>
        public int Xp { get; set; }
        
        /// <summary>
        /// Difficulty level of the tutorial (e.g., "Beginner", "Intermediate", "Advanced")
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Request model for running a tutorial step
    /// </summary>
    public class RunTutorialStepRequest
    {
        /// <summary>
        /// The ID of the tutorial
        /// </summary>
        public string TutorialId { get; set; } = string.Empty;
        
        /// <summary>
        /// The index of the step to run (0-based)
        /// </summary>
        public int StepIndex { get; set; }
        
        /// <summary>
        /// The user's input command or code
        /// </summary>
        public string UserInput { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether to request a hint from Shello
        /// </summary>
        public bool RequestHint { get; set; } = false;
    }
    
    /// <summary>
    /// Response model for the result of running a tutorial step
    /// </summary>
    public class RunTutorialStepResponse
    {
        /// <summary>
        /// Whether the user's input was correct
        /// </summary>
        public bool IsCorrect { get; set; }
        
        /// <summary>
        /// Feedback message for the user
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Hint to help the user if they got it wrong
        /// </summary>
        public string? Hint { get; set; }
        
        /// <summary>
        /// AI-generated hint from Shello if requested
        /// </summary>
        public string? HintFromShello { get; set; }
        
        /// <summary>
        /// The index of the next step (null if this was the last step)
        /// </summary>
        public int? NextStepIndex { get; set; }
        
        /// <summary>
        /// Whether the tutorial is now complete
        /// </summary>
        public bool IsComplete { get; set; }
    }
}