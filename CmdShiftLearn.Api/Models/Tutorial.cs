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
}