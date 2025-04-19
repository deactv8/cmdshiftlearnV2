using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Interface for loading challenges from various sources
    /// </summary>
    public interface IChallengeLoader
    {
        /// <summary>
        /// Gets all available challenge metadata
        /// </summary>
        /// <returns>A list of challenge metadata</returns>
        Task<IEnumerable<ChallengeMetadata>> GetAllChallengeMetadataAsync();
        
        /// <summary>
        /// Gets a specific challenge by ID including its script
        /// </summary>
        /// <param name="id">The challenge ID</param>
        /// <returns>The challenge with script if found, null otherwise</returns>
        Task<Challenge?> GetChallengeByIdAsync(string id);
        
        /// <summary>
        /// Gets all challenges associated with a specific tutorial
        /// </summary>
        /// <param name="tutorialId">The tutorial ID</param>
        /// <returns>A list of challenge metadata for the specified tutorial</returns>
        Task<IEnumerable<ChallengeMetadata>> GetChallengesByTutorialIdAsync(string tutorialId);
    }
}