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
                // Get all JSON and YAML files in the challenges directory
                var challengeFiles = Directory.GetFiles(_challengesDirectory, "*.json")
                    .Concat(Directory.GetFiles(_challengesDirectory, "*.yaml"))
                    .Concat(Directory.GetFiles(_challengesDirectory, "*.yml"));
                
                foreach (var file in challengeFiles)
                {
                    try
                    {
                        var challenge = await LoadChallengeFromFileAsync(file);
                        if (challenge != null)
                        {
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
        /// Gets a specific challenge by ID including its script
        /// </summary>
        /// <param name="id">The challenge ID</param>
        /// <returns>The challenge with script if found, null otherwise</returns>
        public async Task<Challenge?> GetChallengeByIdAsync(string id)
        {
            try
            {
                // Look for a challenge file with the given ID
                var jsonFile = Path.Combine(_challengesDirectory, $"{id}.json");
                var yamlFile = Path.Combine(_challengesDirectory, $"{id}.yaml");
                var ymlFile = Path.Combine(_challengesDirectory, $"{id}.yml");
                
                if (File.Exists(jsonFile))
                {
                    return await LoadChallengeFromFileAsync(jsonFile);
                }
                else if (File.Exists(yamlFile))
                {
                    return await LoadChallengeFromFileAsync(yamlFile);
                }
                else if (File.Exists(ymlFile))
                {
                    return await LoadChallengeFromFileAsync(ymlFile);
                }
                
                // If no direct match, search all files for a challenge with the given ID
                var challengeFiles = Directory.GetFiles(_challengesDirectory, "*.json")
                    .Concat(Directory.GetFiles(_challengesDirectory, "*.yaml"))
                    .Concat(Directory.GetFiles(_challengesDirectory, "*.yml"));
                
                foreach (var file in challengeFiles)
                {
                    var challenge = await LoadChallengeFromFileAsync(file);
                    if (challenge != null && challenge.Id == id)
                    {
                        return challenge;
                    }
                }
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
                }
                else if (fileExtension == ".yaml" || fileExtension == ".yml")
                {
                    // Parse YAML file
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();
                    
                    challenge = deserializer.Deserialize<Challenge>(fileContent);
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
                
                // If script is a file path, load the content from the file
                if (challenge.Script.EndsWith(".ps1") && !challenge.Script.Contains("\n"))
                {
                    var scriptPath = Path.IsPathRooted(challenge.Script)
                        ? challenge.Script
                        : Path.Combine(Path.GetDirectoryName(filePath) ?? _challengesDirectory, challenge.Script);
                    
                    if (File.Exists(scriptPath))
                    {
                        challenge.Script = await File.ReadAllTextAsync(scriptPath);
                    }
                    else
                    {
                        _logger.LogWarning("Challenge script file not found: {ScriptPath}", scriptPath);
                    }
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