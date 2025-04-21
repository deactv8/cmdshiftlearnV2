using CmdShiftLearn.Api.Models;
using Octokit;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Loads challenges from a GitHub repository
    /// </summary>
    public class GitHubChallengeLoader : IChallengeLoader
    {
        private readonly string _owner;
        private readonly string _repo;
        private readonly string _branch;
        private readonly string _challengesPath;
        private readonly GitHubClient _gitHubClient;
        private readonly ILogger<GitHubChallengeLoader> _logger;
        
        public GitHubChallengeLoader(IConfiguration configuration, ILogger<GitHubChallengeLoader> logger)
        {
            _owner = configuration["GitHub:Owner"] ?? "deactv8";
            _repo = configuration["GitHub:Repo"] ?? "content";
            _branch = configuration["GitHub:Branch"] ?? "main";
            _challengesPath = configuration["GitHub:ChallengesPath"] ?? "challenges";
            _logger = logger;
            
            // Initialize GitHub client
            _gitHubClient = new GitHubClient(new ProductHeaderValue("CmdShiftLearn"));
            
            // Add authentication if token is provided
            var token = configuration["GitHub:AccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                _gitHubClient.Credentials = new Credentials(token);
            }
            
            _logger.LogInformation("Initialized GitHub challenge loader for {Owner}/{Repo}:{Branch}/{Path}", 
                _owner, _repo, _branch, _challengesPath);
        }
        
        /// <summary>
        /// Gets all available challenge metadata from GitHub
        /// </summary>
        /// <returns>A list of challenge metadata</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetAllChallengeMetadataAsync()
        {
            var challenges = new List<ChallengeMetadata>();
            
            try
            {
                // Get all files in the challenges directory
                var contents = await _gitHubClient.Repository.Content.GetAllContents(
                    _owner, _repo, _challengesPath);
                
                foreach (var content in contents)
                {
                    // Skip directories and non-JSON/YAML files
                    if (content.Type == ContentType.Dir || 
                        (!content.Name.EndsWith(".json") && 
                         !content.Name.EndsWith(".yaml") && 
                         !content.Name.EndsWith(".yml")))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var challenge = await LoadChallengeFromGitHubAsync(content.Path);
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
                        _logger.LogError(ex, "Error loading challenge from GitHub: {Path}", content.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge files from GitHub: {Owner}/{Repo}/{Path}", 
                    _owner, _repo, _challengesPath);
            }
            
            return challenges;
        }
        
        /// <summary>
        /// Gets a specific challenge by ID including its script from GitHub
        /// </summary>
        /// <param name="id">The challenge ID</param>
        /// <returns>The challenge with script if found, null otherwise</returns>
        public async Task<Challenge?> GetChallengeByIdAsync(string id)
        {
            try
            {
                // Try to find the challenge file with the given ID
                var jsonPath = $"{_challengesPath}/{id}.json";
                var yamlPath = $"{_challengesPath}/{id}.yaml";
                var ymlPath = $"{_challengesPath}/{id}.yml";
                
                // Try JSON file first
                try
                {
                    var challenge = await LoadChallengeFromGitHubAsync(jsonPath);
                    if (challenge != null)
                    {
                        return challenge;
                    }
                }
                catch (NotFoundException)
                {
                    // File not found, try next format
                }
                
                // Try YAML file
                try
                {
                    var challenge = await LoadChallengeFromGitHubAsync(yamlPath);
                    if (challenge != null)
                    {
                        return challenge;
                    }
                }
                catch (NotFoundException)
                {
                    // File not found, try next format
                }
                
                // Try YML file
                try
                {
                    var challenge = await LoadChallengeFromGitHubAsync(ymlPath);
                    if (challenge != null)
                    {
                        return challenge;
                    }
                }
                catch (NotFoundException)
                {
                    // File not found, try searching all files
                }
                
                // If no direct match, search all files for a challenge with the given ID
                var contents = await _gitHubClient.Repository.Content.GetAllContents(
                    _owner, _repo, _challengesPath);
                
                foreach (var content in contents)
                {
                    // Skip directories and non-JSON/YAML files
                    if (content.Type == ContentType.Dir || 
                        (!content.Name.EndsWith(".json") && 
                         !content.Name.EndsWith(".yaml") && 
                         !content.Name.EndsWith(".yml")))
                    {
                        continue;
                    }
                    
                    var challenge = await LoadChallengeFromGitHubAsync(content.Path);
                    if (challenge != null && challenge.Id == id)
                    {
                        return challenge;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge by ID from GitHub: {Id}", id);
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets all challenges associated with a specific tutorial from GitHub
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
                _logger.LogError(ex, "Error getting challenges by tutorial ID from GitHub: {TutorialId}", tutorialId);
            }
            
            return challenges;
        }
        
        /// <summary>
        /// Loads a challenge from GitHub
        /// </summary>
        /// <param name="path">Path to the challenge file in the repository</param>
        /// <returns>The loaded challenge or null if loading failed</returns>
        private async Task<Challenge?> LoadChallengeFromGitHubAsync(string path)
        {
            try
            {
                // Get the file content from GitHub
                var fileContent = await _gitHubClient.Repository.Content.GetRawContent(
                    _owner, _repo, path);
                
                var fileExtension = Path.GetExtension(path).ToLowerInvariant();
                var content = System.Text.Encoding.UTF8.GetString(fileContent);
                
                Challenge? challenge;
                
                if (fileExtension == ".json")
                {
                    // Parse JSON file
                    challenge = JsonSerializer.Deserialize<Challenge>(content, new JsonSerializerOptions
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
                    
                    challenge = deserializer.Deserialize<Challenge>(content);
                }
                else
                {
                    _logger.LogWarning("Unsupported file extension: {Extension}", fileExtension);
                    return null;
                }
                
                if (challenge == null)
                {
                    _logger.LogWarning("Failed to deserialize challenge from GitHub: {Path}", path);
                    return null;
                }
                
                // If script is a file path, load the content from GitHub
                if (challenge.Script.EndsWith(".ps1") && !challenge.Script.Contains("\n"))
                {
                    var scriptPath = challenge.Script;
                    if (!Path.IsPathRooted(scriptPath))
                    {
                        // Resolve relative path
                        var directory = Path.GetDirectoryName(path) ?? _challengesPath;
                        scriptPath = Path.Combine(directory, scriptPath).Replace("\\", "/");
                    }
                    
                    try
                    {
                        var scriptContent = await _gitHubClient.Repository.Content.GetRawContent(
                            _owner, _repo, scriptPath);
                        
                        challenge.Script = System.Text.Encoding.UTF8.GetString(scriptContent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Challenge script file not found on GitHub: {ScriptPath}", scriptPath);
                    }
                }
                
                return challenge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading challenge from GitHub: {Path}", path);
                return null;
            }
        }
    }
}