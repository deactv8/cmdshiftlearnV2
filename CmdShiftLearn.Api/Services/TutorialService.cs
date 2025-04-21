using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Service for managing tutorials
    /// </summary>
    public class TutorialService : ITutorialService
    {
        private readonly ITutorialLoader _tutorialLoader;
        private readonly ILogger<TutorialService> _logger;
        
        public TutorialService(ITutorialLoader tutorialLoader, ILogger<TutorialService> logger)
        {
            _tutorialLoader = tutorialLoader;
            _logger = logger;
            
            _logger.LogInformation("TutorialService initialized with loader: {LoaderType}", 
                tutorialLoader.GetType().Name);
        }
        
        /// <summary>
        /// Gets all available tutorial metadata
        /// </summary>
        /// <returns>A list of tutorial metadata</returns>
        public async Task<IEnumerable<TutorialMetadata>> GetAllTutorialMetadataAsync()
        {
            try
            {
                _logger.LogInformation("Getting all tutorial metadata using {LoaderType}", 
                    _tutorialLoader.GetType().Name);
                
                var tutorials = await _tutorialLoader.GetAllTutorialMetadataAsync();
                
                _logger.LogInformation("Retrieved {Count} tutorials", tutorials?.Count() ?? 0);
                
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
                _logger.LogInformation("Getting tutorial by ID: {Id} using {LoaderType}", 
                    id, _tutorialLoader.GetType().Name);
                
                var tutorial = await _tutorialLoader.GetTutorialByIdAsync(id);
                
                if (tutorial != null)
                {
                    _logger.LogInformation("Retrieved tutorial: {Id}, Title: {Title}, Steps: {StepCount}", 
                        tutorial.Id, tutorial.Title, tutorial.Steps?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("Tutorial not found with ID: {Id}", id);
                }
                
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