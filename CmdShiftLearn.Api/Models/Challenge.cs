using System.Text.Json.Serialization;

namespace CmdShiftLearn.Api.Models
{
    /// <summary>
    /// Represents a challenge with metadata and steps
    /// </summary>
    public class Challenge
    {
        /// <summary>
        /// Unique identifier for the challenge
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Title of the challenge
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the challenge
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// XP awarded for completing the challenge
        /// </summary>
        [JsonPropertyName("xpReward")]
        public int Xp { get; set; }
        
        /// <summary>
        /// Difficulty level of the challenge (e.g., "Beginner", "Intermediate", "Advanced")
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional link to a related tutorial
        /// </summary>
        [JsonPropertyName("prerequisiteTutorial")]
        public string TutorialId { get; set; } = string.Empty;
        
        /// <summary>
        /// Steps for the challenge
        /// </summary>
        public List<ChallengeStep> Steps { get; set; } = new List<ChallengeStep>();
    }
    
    /// <summary>
    /// Represents a single step in a challenge
    /// </summary>
    public class ChallengeStep
    {
        /// <summary>
        /// Identifier for the step
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Title of the step
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Instructions for the step
        /// </summary>
        public string Instructions { get; set; } = string.Empty;
        
        /// <summary>
        /// Expected command to complete the step
        /// </summary>
        public string ExpectedCommand { get; set; } = string.Empty;
        
        /// <summary>
        /// Hint for the step
        /// </summary>
        public string Hint { get; set; } = string.Empty;
        
        /// <summary>
        /// XP awarded for completing the step
        /// </summary>
        [JsonPropertyName("xpReward")]
        public int Xp { get; set; }
    }
    
    /// <summary>
    /// Represents challenge metadata without the full content
    /// </summary>
    public class ChallengeMetadata
    {
        /// <summary>
        /// Unique identifier for the challenge
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Title of the challenge
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the challenge
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// XP awarded for completing the challenge
        /// </summary>
        public int Xp { get; set; }
        
        /// <summary>
        /// Difficulty level of the challenge (e.g., "Beginner", "Intermediate", "Advanced")
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional link to a related tutorial
        /// </summary>
        public string TutorialId { get; set; } = string.Empty;
    }
}