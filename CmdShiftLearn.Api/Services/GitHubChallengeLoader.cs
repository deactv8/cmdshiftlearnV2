using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Helpers;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Net.Http;
using System.Net.Http.Headers;

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
        private readonly string _accessToken;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubChallengeLoader> _logger;
        private readonly string _rawBaseUrl;
        
        private readonly string _apiBaseUrl = "https://api.github.com";
        
        public GitHubChallengeLoader(IConfiguration configuration, ILogger<GitHubChallengeLoader> logger, IHttpClientFactory httpClientFactory)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            
            // Use configuration["GitHub:X"] format with GetValue to support both colon and double underscore formats
            _owner = configuration.GetValue<string>("GitHub:Owner") ?? "deactv8";
            _repo = configuration.GetValue<string>("GitHub:Repo") ?? "content";
            _branch = configuration.GetValue<string>("GitHub:Branch") ?? "master";
            _challengesPath = configuration.GetValue<string>("GitHub:ChallengesPath") ?? "challenges";
            _accessToken = configuration.GetValue<string>("GitHub:AccessToken") ?? "";
            _rawBaseUrl = configuration.GetValue<string>("GitHub:RawBaseUrl") ?? "https://raw.githubusercontent.com";
            
            // Ensure we don't have any cached data by forcing a refresh on every request
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                MustRevalidate = true
            };
            _httpClient = httpClientFactory.CreateClient("GitHub");
            _logger = logger;
            
            _logger.LogInformation("GitHub settings: Owner={Owner}, Repo={Repo}, Branch={Branch}, ChallengesPath={ChallengesPath}, RawBaseUrl={RawBaseUrl}",
                _owner, _repo, _branch, _challengesPath, _rawBaseUrl);
                
            // Configure HttpClient with GitHub API headers
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CmdShiftLearn", "1.0"));
            
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            
            _logger.LogInformation("GitHubChallengeLoader initialized for {Owner}/{Repo}:{Branch}, challenges path: {Path}", 
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
                // Get all files in the challenges directory and its subdirectories
                var files = await GetDirectoryContentsRecursiveAsync(_challengesPath);
                _logger.LogInformation("Found {Count} challenge files in GitHub repository", files.Count);
                
                foreach (var file in files)
                {
                    if (string.IsNullOrEmpty(file)) continue;
                    
                    try
                    {
                        if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || 
                            file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                            file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                        {
                            var challenge = await LoadChallengeFromGitHubAsync(file);
                            if (challenge != null)
                            {
                                _logger.LogInformation("Loaded challenge from GitHub: {Id}, Title: {Title}", 
                                    challenge.Id, challenge.Title);
                                
                                challenges.Add(new ChallengeMetadata
                                {
                                    Id = challenge.Id,
                                    Title = challenge.Title,
                                    Description = challenge.Description ?? "",
                                    Xp = challenge.Xp,
                                    Difficulty = challenge.Difficulty ?? "Beginner",
                                    TutorialId = challenge.TutorialId ?? ""
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading challenge from GitHub: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge files from GitHub repository");
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
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Null or empty challenge ID provided");
                return null;
            }
            
            try
            {
                // Get all challenge files from GitHub
                var files = await GetDirectoryContentsRecursiveAsync(_challengesPath);
                
                // Look for exact matches first (id.json, id.yaml, id.yml)
                var exactMatches = files
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Where(f => 
                        string.Equals(Path.GetFileNameWithoutExtension(f), id, StringComparison.OrdinalIgnoreCase) && 
                        (f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || 
                         f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                         f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                
                if (exactMatches.Any())
                {
                    // Try to load the first exact match
                    return await LoadChallengeFromGitHubAsync(exactMatches[0]);
                }
                
                // If no exact matches, load all challenges and find by ID
                foreach (var file in files)
                {
                    if (string.IsNullOrEmpty(file)) continue;
                    
                    if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || 
                        file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                        file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                    {
                        var challenge = await LoadChallengeFromGitHubAsync(file);
                        if (challenge != null && string.Equals(challenge.Id, id, StringComparison.OrdinalIgnoreCase))
                        {
                            return challenge;
                        }
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
        /// Gets all challenges associated with a specific tutorial from GitHub
        /// </summary>
        /// <param name="tutorialId">The tutorial ID</param>
        /// <returns>A list of challenge metadata for the specified tutorial</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetChallengesByTutorialIdAsync(string tutorialId)
        {
            var challenges = new List<ChallengeMetadata>();
            
            if (string.IsNullOrEmpty(tutorialId))
            {
                _logger.LogWarning("Null or empty tutorial ID provided");
                return challenges;
            }
            
            try
            {
                // Get all challenges
                var allChallenges = await GetAllChallengeMetadataAsync();
                
                // Filter challenges by tutorial ID (case-insensitive)
                challenges = allChallenges
                    .Where(c => !string.IsNullOrEmpty(c.TutorialId) && 
                           string.Equals(c.TutorialId, tutorialId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                _logger.LogInformation("Found {Count} challenges for tutorial ID: {TutorialId}", challenges.Count, tutorialId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenges by tutorial ID: {TutorialId}", tutorialId);
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
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Null or empty file path provided");
                return null;
            }
            
            try
            {
                // Build the URL to the raw content
                var rawUrl = $"{_rawBaseUrl}/{_owner}/{_repo}/{_branch}/{path}";
                
                _logger.LogInformation("Loading challenge from GitHub: {Url}", rawUrl);
                
                // Get the file content
                var response = await _httpClient.GetAsync(rawUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get challenge file from GitHub: {Url}, Status: {Status}", 
                        rawUrl, response.StatusCode);
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty content received from GitHub: {Url}", rawUrl);
                    return null;
                }
                
                var fileExtension = Path.GetExtension(path).ToLowerInvariant();
                
                Challenge? challenge = null;
                
                if (fileExtension == ".json")
                {
                    // Parse JSON file
                    try
                    {
                        challenge = JsonSerializer.Deserialize<Challenge>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        _logger.LogInformation("Loaded challenge from JSON: {Path}", path);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error deserializing JSON challenge: {Path}", path);
                        _logger.LogDebug("JSON content: {Content}", content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                        return null;
                    }
                }
                else if (fileExtension == ".yaml" || fileExtension == ".yml")
                {
                    // Use our helper to parse YAML files
                    challenge = YamlHelpers.DeserializeChallenge(content, _logger);
                    
                    if (challenge == null)
                    {
                        _logger.LogWarning("Failed to deserialize YAML challenge from GitHub: {Path}", path);
                        return null;
                    }
                    
                    _logger.LogInformation("Loaded challenge from YAML: {Path} with {StepCount} steps", 
                        path, challenge.Steps.Count);
                }
                else
                {
                    _logger.LogWarning("Unsupported file extension: {Extension}", fileExtension);
                    return null;
                }
                
                if (challenge == null)
                {
                    _logger.LogWarning("Failed to deserialize challenge from GitHub: {File}", path);
                    return null;
                }
                
                // Initialize required properties with defaults if missing
                challenge.Id ??= Path.GetFileNameWithoutExtension(path);
                challenge.Title ??= challenge.Id;
                challenge.Description ??= "";
                challenge.Difficulty ??= "Beginner";
                challenge.Steps ??= new List<ChallengeStep>();
                challenge.TutorialId ??= "";
                
                return challenge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading challenge from GitHub: {Path}", path);
                return null;
            }
        }
        
        /// <summary>
        /// Gets all files in a directory and its subdirectories from GitHub
        /// </summary>
        /// <param name="path">The directory path</param>
        /// <returns>A list of file paths</returns>
        private async Task<List<string>> GetDirectoryContentsRecursiveAsync(string path)
        {
            var files = new List<string>();
            
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Null or empty directory path provided");
                return files;
            }
            
            try
            {
                // Build the API URL for the contents endpoint
                var apiUrl = $"{_apiBaseUrl}/repos/{_owner}/{_repo}/contents/{path}?ref={_branch}";
                
                _logger.LogInformation("Getting directory contents from GitHub API: {Url}", apiUrl);
                
                // Get the directory contents
                var response = await _httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get directory contents from GitHub: {Url}, Status: {Status}", 
                        apiUrl, response.StatusCode);
                    return files;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty content received from GitHub API: {Url}", apiUrl);
                    return files;
                }
                
                try
                {
                    var items = JsonSerializer.Deserialize<List<GitHubContent>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (items == null || !items.Any())
                    {
                        _logger.LogWarning("No items found in GitHub directory: {Path}", path);
                        return files;
                    }
                    
                    // Process each item in the directory
                    foreach (var item in items)
                    {
                        if (item == null || string.IsNullOrEmpty(item.Type) || string.IsNullOrEmpty(item.Path))
                        {
                            continue;
                        }
                        
                        if (item.Type.Equals("file", StringComparison.OrdinalIgnoreCase))
                        {
                            // Add the file path to the list
                            files.Add(item.Path);
                            _logger.LogDebug("Added file to list: {Path}", item.Path);
                        }
                        else if (item.Type.Equals("dir", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogDebug("Found subdirectory: {Path}", item.Path);
                            // Recursively get the contents of the subdirectory
                            var subdirFiles = await GetDirectoryContentsRecursiveAsync(item.Path);
                            if (subdirFiles != null && subdirFiles.Any())
                            {
                                _logger.LogDebug("Found {Count} files in subdirectory: {Path}", subdirFiles.Count, item.Path);
                                files.AddRange(subdirFiles);
                            }
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Error deserializing GitHub API response: {Path}", path);
                    _logger.LogDebug("Raw content: {Content}", content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting directory contents from GitHub: {Path}", path);
            }
            
            return files;
        }
        
        /// <summary>
        /// GitHub API content item
        /// </summary>
        private class GitHubContent
        {
            public string? Name { get; set; }
            public string? Path { get; set; }
            public string? Type { get; set; }
            public string? Download_url { get; set; }
        }
    }
}