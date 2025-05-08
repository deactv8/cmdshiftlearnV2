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
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            
            // Assign logger first so we can use it safely
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("GitHub");
            
            // Now safe to use logger
            try
            {
                // Debug logging to check environment variable resolution
                logger.LogInformation("GitHub__Repo direct: {0}", configuration.GetValue<string>("GitHub__Repo"));
                logger.LogInformation("GitHub:Repo direct: {0}", configuration.GetValue<string>("GitHub:Repo"));
                logger.LogInformation("GitHub__Repo from indexer: {0}", configuration["GitHub__Repo"]);
                logger.LogInformation("GitHub:Repo from indexer: {0}", configuration["GitHub:Repo"]);
                
                // Check if GitHub token exists
                var githubToken = configuration.GetValue<string>("GitHub__Token");
                if (!string.IsNullOrEmpty(githubToken))
                {
                    logger.LogInformation("GitHub__Token found - will use for authentication");
                }
                else
                {
                    logger.LogWarning("GitHub__Token not found - API requests may be rate limited or fail with 401");
                }
            }
            catch (Exception ex)
            {
                // Catch any logging errors but don't fail initialization
                Console.WriteLine($"Warning: Error during config logging: {ex.Message}");
            }
            
            // Use configuration.GetValue to support both colon and double underscore formats in environment variables
            _owner = configuration.GetValue<string>("GitHub:Owner") ?? "deactv8";
            _repo = configuration.GetValue<string>("GitHub:Repo") ?? "content";
            _branch = configuration.GetValue<string>("GitHub:Branch") ?? "master";
            _tutorialsPath = configuration.GetValue<string>("GitHub:TutorialsPath") ?? "tutorials";
            _accessToken = configuration.GetValue<string>("GitHub:AccessToken") ?? "";
            _rawBaseUrl = configuration.GetValue<string>("GitHub:RawBaseUrl") ?? "https://raw.githubusercontent.com";
            
            // Ensure we don't have any cached data by forcing a refresh on every request
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                MustRevalidate = true
            };
            
            _logger?.LogInformation("GitHub settings: Owner={Owner}, Repo={Repo}, Branch={Branch}, TutorialsPath={TutorialsPath}, RawBaseUrl={RawBaseUrl}",
                _owner, _repo, _branch, _tutorialsPath, _rawBaseUrl);
                
            // Configure HttpClient with GitHub API headers
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CmdShiftLearn", "1.0"));
            
            // Try to get token from environment variables first (GitHub__Token)
            githubToken = configuration.GetValue<string>("GitHub__Token");
            if (!string.IsNullOrEmpty(githubToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
                _logger?.LogInformation("Using GitHub__Token for API authentication");
            }
            // Fall back to configured AccessToken if GitHub__Token is not available
            else if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                _logger?.LogInformation("Using configured AccessToken for GitHub API authentication");
            }
            else
            {
                _logger?.LogWarning("No GitHub authentication token available - API requests may fail");
            }
            
            _logger?.LogInformation("GitHubTutorialLoader initialized for {Owner}/{Repo}:{Branch}, tutorials path: {Path}", 
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
                _logger?.LogInformation("Found {Count} tutorial files in GitHub repository", files.Count);
                
                foreach (var file in files)
                {
                    if (string.IsNullOrEmpty(file)) continue;
                    
                    try
                    {
                        if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || 
                            file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                            file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                        {
                            var tutorial = await LoadTutorialFromGitHubAsync(file);
                            if (tutorial != null)
                            {
                                _logger?.LogInformation("Loaded tutorial from GitHub: {Id}, Title: {Title}", 
                                    tutorial.Id, tutorial.Title);
                                
                                tutorials.Add(new TutorialMetadata
                                {
                                    Id = tutorial.Id,
                                    Title = tutorial.Title,
                                    Description = tutorial.Description ?? "",
                                    Xp = tutorial.Xp,
                                    Difficulty = tutorial.Difficulty ?? "Beginner"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error loading tutorial from GitHub: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting tutorial files from GitHub repository");
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
            if (string.IsNullOrEmpty(id))
            {
                _logger?.LogWarning("Null or empty tutorial ID provided");
                return null;
            }
            
            try
            {
                // Get all tutorial files from GitHub
                var files = await GetDirectoryContentsRecursiveAsync(_tutorialsPath);
                
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
                    return await LoadTutorialFromGitHubAsync(exactMatches[0]);
                }
                
                // If no exact matches, load all tutorials and find by ID
                foreach (var file in files)
                {
                    if (string.IsNullOrEmpty(file)) continue;
                    
                    if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || 
                        file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                        file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                    {
                        var tutorial = await LoadTutorialFromGitHubAsync(file);
                        if (tutorial != null && string.Equals(tutorial.Id, id, StringComparison.OrdinalIgnoreCase))
                        {
                            return tutorial;
                        }
                    }
                }
                
                _logger?.LogWarning("Tutorial not found with ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting tutorial by ID: {Id}", id);
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
            if (string.IsNullOrEmpty(path))
            {
                _logger?.LogWarning("Null or empty file path provided");
                return null;
            }
            
            try
            {
                // Build the URL to the raw content
                var rawUrl = $"{_rawBaseUrl}/{_owner}/{_repo}/{_branch}/{path}";
                
                _logger?.LogInformation("Loading tutorial from GitHub: {Url}", rawUrl);
                
                // Get the file content
                var response = await _httpClient.GetAsync(rawUrl);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger?.LogError("GitHub API authentication failed with 401 Unauthorized when loading tutorial. Please check your GitHub__Token environment variable.");
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to get tutorial file from GitHub: {Url}, Status: {Status}", 
                            rawUrl, response.StatusCode);
                    }
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    _logger?.LogWarning("Empty content received from GitHub: {Url}", rawUrl);
                    return null;
                }
                
                var fileExtension = Path.GetExtension(path).ToLowerInvariant();
                
                Tutorial? tutorial = null;
                
                if (fileExtension == ".json")
                {
                    // Parse JSON file
                    try
                    {
                        tutorial = JsonSerializer.Deserialize<Tutorial>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        _logger?.LogInformation("Loaded tutorial from JSON: {Path}", path);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger?.LogError(jsonEx, "Error deserializing JSON tutorial: {Path}", path);
                        _logger?.LogDebug("JSON content: {Content}", content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                        return null;
                    }
                }
                else if (fileExtension == ".yaml" || fileExtension == ".yml")
                {
                    // Parse YAML file using our helper method
                    tutorial = YamlHelpers.DeserializeTutorial(content, _logger);
                    
                    if (tutorial == null)
                    {
                        _logger?.LogWarning("Failed to deserialize YAML tutorial from GitHub: {Path}", path);
                    }
                }
                else
                {
                    _logger?.LogWarning("Unsupported file extension: {Extension}", fileExtension);
                    return null;
                }
                
                if (tutorial == null)
                {
                    _logger?.LogWarning("Failed to deserialize tutorial from GitHub: {Path}", path);
                    return null;
                }
                
                // Initialize required properties with defaults if missing
                tutorial.Id ??= Path.GetFileNameWithoutExtension(path);
                tutorial.Title ??= tutorial.Id;
                tutorial.Description ??= "";
                tutorial.Content ??= "";
                tutorial.Difficulty ??= "Beginner";
                tutorial.Steps ??= new List<TutorialStep>();
                
                // If content is a file path, load the content from the file
                if (!string.IsNullOrEmpty(tutorial.Content) &&
                    tutorial.Content.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) && 
                    !tutorial.Content.Contains("\n"))
                {
                    var directoryName = Path.GetDirectoryName(path) ?? "";
                    var contentPath = Path.Combine(directoryName, tutorial.Content);
                    var contentRawUrl = $"{_rawBaseUrl}/{_owner}/{_repo}/{_branch}/{contentPath}";
                    
                    var contentResponse = await _httpClient.GetAsync(contentRawUrl);
                    if (contentResponse.IsSuccessStatusCode)
                    {
                        var contentText = await contentResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(contentText))
                        {
                            tutorial.Content = contentText;
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("Tutorial content file not found on GitHub: {ContentPath}, Status: {Status}", 
                            contentPath, contentResponse.StatusCode);
                    }
                }
                
                return tutorial;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading tutorial from GitHub: {Path}", path);
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
                _logger?.LogWarning("Null or empty directory path provided");
                return files;
            }
            
            try
            {
                // Build the API URL for the contents endpoint
                var apiUrl = $"{_apiBaseUrl}/repos/{_owner}/{_repo}/contents/{path}?ref={_branch}";
                
                _logger?.LogInformation("Getting directory contents from GitHub API: {Url}", apiUrl);
                
                // Get the directory contents
                var response = await _httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger?.LogError("GitHub API authentication failed with 401 Unauthorized. Please check your GitHub__Token environment variable.");
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to get directory contents from GitHub: {Url}, Status: {Status}", 
                            apiUrl, response.StatusCode);
                    }
                    return files;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    _logger?.LogWarning("Empty content received from GitHub API: {Url}", apiUrl);
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
                        _logger?.LogWarning("No items found in GitHub directory: {Path}", path);
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
                            _logger?.LogDebug("Added file to list: {Path}", item.Path);
                        }
                        else if (item.Type.Equals("dir", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger?.LogDebug("Found subdirectory: {Path}", item.Path);
                            // Recursively get the contents of the subdirectory
                            var subdirFiles = await GetDirectoryContentsRecursiveAsync(item.Path);
                            if (subdirFiles != null && subdirFiles.Any())
                            {
                                _logger?.LogDebug("Found {Count} files in subdirectory: {Path}", subdirFiles.Count, item.Path);
                                files.AddRange(subdirFiles);
                            }
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger?.LogError(jsonEx, "Error deserializing GitHub API response: {Path}", path);
                    _logger?.LogDebug("Raw content: {Content}", content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting directory contents from GitHub: {Path}", path);
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
