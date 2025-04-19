using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Interface for loading tutorials from various sources
    /// </summary>
    public interface ITutorialLoader
    {
        /// <summary>
        /// Gets all available tutorial metadata
        /// </summary>
        /// <returns>A list of tutorial metadata</returns>
        Task<IEnumerable<TutorialMetadata>> GetAllTutorialMetadataAsync();
        
        /// <summary>
        /// Gets a specific tutorial by ID including its content
        /// </summary>
        /// <param name="id">The tutorial ID</param>
        /// <returns>The tutorial with content if found, null otherwise</returns>
        Task<Tutorial?> GetTutorialByIdAsync(string id);
    }
}