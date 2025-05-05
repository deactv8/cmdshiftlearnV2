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
    /// Loads tutorials from a GitHub repository
    /// </summary>
    public class GitHubTutorialLoader : ITutorialLoader
    {
        private readonly string _owner;
        private readonly string _repo;
        private readonly string _branch;
        private readonly string _tutorialsPath;
        private readonly string _accessToken;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubTutorialLoader> _logger;
        private readonly string _rawBaseUrl;
        
        private readonly string _apiBaseUrl = "https://api.github.com";
        
        public GitHubTutorialLoader(IConfiguration configuration, ILogger<GitHubTutorialLoader> logger, IHttpClientFactory httpClientFactory)
        {
            _owner = configuration["GitHub:Owner"] ?? "deactv8";
            _repo = configuration["GitHub:Repo"] ?? "content";
            _branch = configuration["GitHub:Branch"] ?? "master";
            _tutorialsPath = configuration["GitHub:TutorialsPath"] ?? "tutorials";
            _accessToken = configuration["GitHub:AccessToken"] ?? "";
            _rawBaseUrl = configuration["GitHub:RawBaseUrl"] ?? "https://raw.githubusercontent.com";
            _httpClient = httpClientFactory.CreateClient("GitHub");
            _logger = logger;
            
            _logger.LogInformation("GitHub settings: Owner={Owner}, Repo={Repo}, Branch={Branch}, TutorialsPath={TutorialsPath}, RawBaseUrl={RawBaseUrl}",
                _owner, _repo, _branch, _tutorialsPath, _rawBaseUrl);
                
            // Configure HttpClient with GitHub API headers
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CmdShiftLearn", "1.0"));
            
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            
            _logger.LogInformation("GitHubTutorialLoader initialized for {Owner}/{Repo}:{Branch}, tutorials path: {Path}", 
                _owner, _repo, _branch, _tutorialsPath);
        }
        
        /// <summary>
        /// Gets all available tutorial metadata from GitHub
        /// </summary>
        /// <returns>A list of tutorial metadata</returns>
        public async Task<IEnumerable<TutorialMetadata>> GetAllTutorialMetadataAsync()
        {
            var tutorials = new List<TutorialMetadata>();
            
            try
            {
                // Get all files in the tutorials directory and its subdirectories
                var files = await GetDirectoryContentsRecursiveAsync(_tutorialsPath);
                _logger.LogInformation("Found {Count} tutorial files in GitHub repository", files.Count);
                
                foreach (var file in files)
                {
                    try
                    {
                        if (file.EndsWith(".json") || file.EndsWith(".yaml") || file.EndsWith(".yml"))
                        {
                            var tutorial = await LoadTutorialFromGitHubAsync(file);
                            if (tutorial != null)
                            {
                                _logger.LogInformation("Loaded tutorial from GitHub: {Id}, Title: {Title}", 
                                    tutorial.Id, tutorial.Title);
                                
                                tutorials.Add(new TutorialMetadata
                                {
                                    Id = tutorial.Id,
                                    Title = tutorial.Title,
                                    Description = tutorial.Description,
                                    Xp = tutorial.Xp,
                                    Difficulty = tutorial.Difficulty
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading tutorial from GitHub: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial files from GitHub repository");
            }
            
            return tutorials;
        }
        
        /// <summary>
        /// Gets a specific tutorial by ID including its content from GitHub
        /// </summary>
        /// <param name="id">The tutorial ID</param>
        /// <returns>The tutorial with content if found, null otherwise</returns>
        public async Task<Tutorial?> GetTutorialByIdAsync(string id)
        {
            try
            {
                // Get all tutorial files from GitHub
                var files = await GetDirectoryContentsRecursiveAsync(_tutorialsPath);
                
                // Look for exact matches first (id.json, id.yaml, id.yml)
                var exactMatches = files.Where(f => 
                    Path.GetFileNameWithoutExtension(f) == id && 
                    (f.EndsWith(".json") || f.EndsWith(".yaml") || f.EndsWith(".yml"))).ToList();
                
                if (exactMatches.Any())
                {
                    // Try to load the first exact match
                    return await LoadTutorialFromGitHubAsync(exactMatches[0]);
                }
                
                // If no exact matches, load all tutorials and find by ID
                foreach (var file in files)
                {
                    if (file.EndsWith(".json") || file.EndsWith(".yaml") || file.EndsWith(".yml"))
                    {
                        var tutorial = await LoadTutorialFromGitHubAsync(file);
                        if (tutorial != null && tutorial.Id == id)
                        {
                            return tutorial;
                        }
                    }
                }
                
                _logger.LogWarning("Tutorial not found with ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial by ID: {Id}", id);
            }
            
            return null;
        }
        
        /// <summary>
        /// Loads a tutorial from GitHub
        /// </summary>
        /// <param name="path">Path to the tutorial file in the repository</param>
        /// <returns>The loaded tutorial or null if loading failed</returns>
        private async Task<Tutorial?> LoadTutorialFromGitHubAsync(string path)
        {
            try
            {
                // Build the URL to the raw content
                var rawUrl = $"{_rawBaseUrl}/{_owner}/{_repo}/{_branch}/{path}";
                
                _logger.LogInformation("Loading tutorial from GitHub: {Url}", rawUrl);
                
                // Get the file content
                var response = await _httpClient.GetAsync(rawUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get tutorial file from GitHub: {Url}, Status: {Status}", 
                        rawUrl, response.StatusCode);
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var fileExtension = Path.GetExtension(path).ToLowerInvariant();
                
                Tutorial? tutorial;
                
                if (fileExtension == ".json")
                {
                    // Parse JSON file
                    tutorial = JsonSerializer.Deserialize<Tutorial>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else if (fileExtension == ".yaml" || fileExtension == ".yml")
                {
                    // Parse YAML file using our helper method
                    tutorial = YamlHelpers.DeserializeTutorial(content, _logger);
                    
                    if (tutorial == null)
                    {
                        _logger.LogWarning("Failed to deserialize YAML tutorial from GitHub: {Path}", path);
                    }
                }
                else
                {
                    _logger.LogWarning("Unsupported file extension: {Extension}", fileExtension);
                    return null;
                }
                
                if (tutorial == null)
                {
                    _logger.LogWarning("Failed to deserialize tutorial from GitHub: {Path}", path);
                    return null;
                }
                
                // If content is a file path, load the content from the file
                if (tutorial.Content.EndsWith(".ps1") && !tutorial.Content.Contains("\n"))
                {
                    var contentPath = Path.Combine(Path.GetDirectoryName(path) ?? "", tutorial.Content);
                    var contentRawUrl = $"{_rawBaseUrl}/{_owner}/{_repo}/{_branch}/{contentPath}";
                    
                    var contentResponse = await _httpClient.GetAsync(contentRawUrl);
                    if (contentResponse.IsSuccessStatusCode)
                    {
                        tutorial.Content = await contentResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Tutorial content file not found on GitHub: {ContentPath}, Status: {Status}", 
                            contentPath, contentResponse.StatusCode);
                    }
                }
                
                return tutorial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tutorial from GitHub: {Path}", path);
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
                
                try
                {
                    var items = JsonSerializer.Deserialize<List<GitHubContent>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (items == null)
                    {
                        _logger.LogWarning("Failed to deserialize GitHub directory contents: {Path}", path);
                        return files;
                    }
                    
                    // Process each item in the directory
                    foreach (var item in items)
                    {
                        if (item.Type == "file")
                        {
                            // Add the file path to the list
                            files.Add(item.Path);
                            _logger.LogDebug("Added file to list: {Path}", item.Path);
                        }
                        else if (item.Type == "dir")
                        {
                            _logger.LogDebug("Found subdirectory: {Path}", item.Path);
                            // Recursively get the contents of the subdirectory
                            var subdirFiles = await GetDirectoryContentsRecursiveAsync(item.Path);
                            _logger.LogDebug("Found {Count} files in subdirectory: {Path}", subdirFiles.Count, item.Path);
                            files.AddRange(subdirFiles);
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Error deserializing GitHub API response: {Path}", path);
                    _logger.LogDebug("Raw content: {Content}", content);
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