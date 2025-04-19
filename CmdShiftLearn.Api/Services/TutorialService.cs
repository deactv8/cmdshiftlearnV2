using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Service for managing tutorials
    /// </summary>
    public class TutorialService
    {
        private readonly ITutorialLoader _tutorialLoader;
        private readonly ILogger<TutorialService> _logger;
        
        public TutorialService(ITutorialLoader tutorialLoader, ILogger<TutorialService> logger)
        {
            _tutorialLoader = tutorialLoader;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets all available tutorial metadata
        /// </summary>
        /// <returns>A list of tutorial metadata</returns>
        public async Task<IEnumerable<TutorialMetadata>> GetAllTutorialMetadataAsync()
        {
            try
            {
                var tutorials = await _tutorialLoader.GetAllTutorialMetadataAsync();
                return tutorials;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tutorial metadata");
                throw;
            }
        }
        
        /// <summary>
        /// Gets a specific tutorial by ID including its content
        /// </summary>
        /// <param name="id">The tutorial ID</param>
        /// <returns>The tutorial with content if found, null otherwise</returns>
        public async Task<Tutorial?> GetTutorialByIdAsync(string id)
        {
            try
            {
                var tutorial = await _tutorialLoader.GetTutorialByIdAsync(id);
                return tutorial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial by ID: {Id}", id);
                throw;
            }
        }
    }
}