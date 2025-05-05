using CmdShiftLearn.Api.Models;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Loads challenges from the file system
    /// </summary>
    public class FileChallengeLoader : IChallengeLoader
    {
        private readonly string _challengesDirectory;
        private readonly ILogger<FileChallengeLoader> _logger;
        
        public FileChallengeLoader(IConfiguration configuration, ILogger<FileChallengeLoader> logger)
        {
            // Get the challenges directory from configuration or use a default path
            _challengesDirectory = configuration["ChallengesDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "scripts", "challenges");
            _logger = logger;
            
            _logger.LogInformation("Using challenges directory: {Directory}", _challengesDirectory);
            
            // Ensure the challenges directory exists
            if (!Directory.Exists(_challengesDirectory))
            {
                Directory.CreateDirectory(_challengesDirectory);
                _logger.LogInformation("Created challenges directory: {Directory}", _challengesDirectory);
            }
        }
        
        /// <summary>
        /// Gets all available challenge metadata
        /// </summary>
        /// <returns>A list of challenge metadata</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetAllChallengeMetadataAsync()
        {
            var challenges = new List<ChallengeMetadata>();
            
            try
            {
                // Get all JSON and YAML files in the challenges directory AND subdirectories
                var challengeFiles = new List<string>();
                
                // Get files directly in the challenges directory
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.json"));
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.yaml"));
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.yml"));
                
                // Also search in subdirectories
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.json", SearchOption.AllDirectories));
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.yaml", SearchOption.AllDirectories));
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.yml", SearchOption.AllDirectories));
                
                // Remove duplicates (files that were found in both the first and recursive searches)
                challengeFiles = challengeFiles.Distinct().ToList();
                
                _logger.LogInformation("Found {Count} challenge files", challengeFiles.Count);
                
                foreach (var file in challengeFiles)
                {
                    try
                    {
                        var challenge = await LoadChallengeFromFileAsync(file);
                        if (challenge != null)
                        {
                            _logger.LogInformation("Loaded challenge: {Title} ({Id})", challenge.Title, challenge.Id);
                            challenges.Add(new ChallengeMetadata
                            {
                                Id = challenge.Id,
                                Title = challenge.Title,
                                Description = challenge.Description,
                                Xp = challenge.Xp,
                                Difficulty = challenge.Difficulty,
                                TutorialId = challenge.TutorialId
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading challenge from file: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge files from directory: {Directory}", _challengesDirectory);
            }
            
            return challenges;
        }
        
        /// <summary>
        /// Gets a specific challenge by ID including its steps
        /// </summary>
        /// <param name="id">The challenge ID</param>
        /// <returns>The challenge with steps if found, null otherwise</returns>
        public async Task<Challenge?> GetChallengeByIdAsync(string id)
        {
            try
            {
                // Look for a challenge file with the given ID in both the main directory and subdirectories
                var jsonFiles = Directory.GetFiles(_challengesDirectory, $"{id}.json", SearchOption.AllDirectories);
                var yamlFiles = Directory.GetFiles(_challengesDirectory, $"{id}.yaml", SearchOption.AllDirectories);
                var ymlFiles = Directory.GetFiles(_challengesDirectory, $"{id}.yml", SearchOption.AllDirectories);
                
                if (jsonFiles.Length > 0)
                {
                    return await LoadChallengeFromFileAsync(jsonFiles[0]);
                }
                else if (yamlFiles.Length > 0)
                {
                    return await LoadChallengeFromFileAsync(yamlFiles[0]);
                }
                else if (ymlFiles.Length > 0)
                {
                    return await LoadChallengeFromFileAsync(ymlFiles[0]);
                }
                
                // If no direct match, search all files for a challenge with the given ID
                var challengeFiles = new List<string>();
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.json", SearchOption.AllDirectories));
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.yaml", SearchOption.AllDirectories));
                challengeFiles.AddRange(Directory.GetFiles(_challengesDirectory, "*.yml", SearchOption.AllDirectories));
                
                foreach (var file in challengeFiles)
                {
                    var challenge = await LoadChallengeFromFileAsync(file);
                    if (challenge != null && challenge.Id == id)
                    {
                        return challenge;
                    }
                }
                
                _logger.LogWarning("Challenge not found with ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge by ID: {Id}", id);
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets all challenges associated with a specific tutorial
        /// </summary>
        /// <param name="tutorialId">The tutorial ID</param>
        /// <returns>A list of challenge metadata for the specified tutorial</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetChallengesByTutorialIdAsync(string tutorialId)
        {
            var challenges = new List<ChallengeMetadata>();
            
            try
            {
                // Get all challenges
                var allChallenges = await GetAllChallengeMetadataAsync();
                
                // Filter challenges by tutorial ID
                challenges = allChallenges.Where(c => c.TutorialId == tutorialId).ToList();
                
                _logger.LogInformation("Found {Count} challenges for tutorial ID: {TutorialId}", challenges.Count, tutorialId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenges by tutorial ID: {TutorialId}", tutorialId);
            }
            
            return challenges;
        }
        
        /// <summary>
        /// Loads a challenge from a file
        /// </summary>
        /// <param name="filePath">Path to the challenge file</param>
        /// <returns>The loaded challenge or null if loading failed</returns>
        private async Task<Challenge?> LoadChallengeFromFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Loading challenge from file: {FilePath}", filePath);
                var fileContent = await File.ReadAllTextAsync(filePath);
                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                
                Challenge? challenge;
                
                if (fileExtension == ".json")
                {
                    // Parse JSON file
                    challenge = JsonSerializer.Deserialize<Challenge>(fileContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    _logger.LogInformation("Loaded challenge from JSON: {FilePath}", filePath);
                }
                else if (fileExtension == ".yaml" || fileExtension == ".yml")
                {
                    // Parse YAML file
                    try {
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .Build();
                        
                        challenge = deserializer.Deserialize<Challenge>(fileContent);
                        
                        // Validate required properties
                        if (string.IsNullOrEmpty(challenge?.Id))
                        {
                            _logger.LogError("Challenge is missing required 'id' property: {FilePath}", filePath);
                            return null;
                        }
                        
                        if (string.IsNullOrEmpty(challenge.Title))
                        {
                            _logger.LogError("Challenge is missing required 'title' property: {FilePath}", filePath);
                            return null;
                        }
                        
                        // Ensure Steps collection is initialized
                        challenge.Steps ??= new List<ChallengeStep>();
                        
                        _logger.LogInformation("Loaded challenge from YAML: {FilePath} with {StepCount} steps", 
                            filePath, challenge.Steps.Count);
                    }
                    catch (Exception yamlEx) {
                        _logger.LogError(yamlEx, "Error deserializing YAML challenge: {FilePath}", filePath);
                        _logger.LogInformation("YAML content: {Content}", fileContent);
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning("Unsupported file extension: {Extension}", fileExtension);
                    return null;
                }
                
                if (challenge == null)
                {
                    _logger.LogWarning("Failed to deserialize challenge from file: {File}", filePath);
                    return null;
                }
                
                // Validate steps
                if (challenge.Steps != null && challenge.Steps.Any())
                {
                    foreach (var step in challenge.Steps)
                    {
                        if (string.IsNullOrEmpty(step.Id))
                        {
                            _logger.LogWarning("Challenge step is missing required 'id' property in challenge: {ChallengeId}", challenge.Id);
                        }
                        
                        if (string.IsNullOrEmpty(step.Title))
                        {
                            _logger.LogWarning("Challenge step is missing required 'title' property in challenge: {ChallengeId}", challenge.Id);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Challenge has no steps: {ChallengeId}", challenge.Id);
                }
                
                return challenge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading challenge from file: {File}", filePath);
                return null;
            }
        }
    }
}