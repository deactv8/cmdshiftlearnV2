using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Service for managing challenges
    /// </summary>
    public class ChallengeService
    {
        private readonly IChallengeLoader _challengeLoader;
        private readonly ILogger<ChallengeService> _logger;
        
        public ChallengeService(IChallengeLoader challengeLoader, ILogger<ChallengeService> logger)
        {
            _challengeLoader = challengeLoader;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets all available challenge metadata
        /// </summary>
        /// <returns>A list of challenge metadata</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetAllChallengeMetadataAsync()
        {
            try
            {
                var challenges = await _challengeLoader.GetAllChallengeMetadataAsync();
                return challenges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all challenge metadata");
                throw;
            }
        }
        
        /// <summary>
        /// Gets a specific challenge by ID including its script
        /// </summary>
        /// <param name="id">The challenge ID</param>
        /// <returns>The challenge with script if found, null otherwise</returns>
        public async Task<Challenge?> GetChallengeByIdAsync(string id)
        {
            try
            {
                var challenge = await _challengeLoader.GetChallengeByIdAsync(id);
                return challenge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge by ID: {Id}", id);
                throw;
            }
        }
        
        /// <summary>
        /// Gets all challenges associated with a specific tutorial
        /// </summary>
        /// <param name="tutorialId">The tutorial ID</param>
        /// <returns>A list of challenge metadata for the specified tutorial</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetChallengesByTutorialIdAsync(string tutorialId)
        {
            try
            {
                var challenges = await _challengeLoader.GetChallengesByTutorialIdAsync(tutorialId);
                return challenges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenges by tutorial ID: {TutorialId}", tutorialId);
                throw;
            }
        }
    }
}