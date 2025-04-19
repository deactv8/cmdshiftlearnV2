namespace CmdShiftLearn.Api.Models
{
    /// <summary>
    /// Represents a challenge with metadata and script content
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
        public int Xp { get; set; }
        
        /// <summary>
        /// Difficulty level of the challenge (e.g., "Beginner", "Intermediate", "Advanced")
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional link to a related tutorial
        /// </summary>
        public string TutorialId { get; set; } = string.Empty;
        
        /// <summary>
        /// Content of the challenge (PowerShell script)
        /// </summary>
        public string Script { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents challenge metadata without the full script content
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