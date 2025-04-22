using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Helpers;
using Octokit;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        private readonly GitHubClient _gitHubClient;
        private readonly ILogger<GitHubTutorialLoader> _logger;
        
        public GitHubTutorialLoader(IConfiguration configuration, ILogger<GitHubTutorialLoader> logger)
        {
            _owner = configuration["GitHub:Owner"] ?? "deactv8";
            _repo = configuration["GitHub:Repo"] ?? "content";
            _branch = configuration["GitHub:Branch"] ?? "master"; // Default to master branch
            _tutorialsPath = configuration["GitHub:TutorialsPath"] ?? "tutorials";
            _logger = logger;
            
            // Initialize GitHub client
            _gitHubClient = new GitHubClient(new ProductHeaderValue("CmdShiftLearn"));
            
            // Add authentication if token is provided
            var token = configuration["GitHub:AccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    _gitHubClient.Credentials = new Credentials(token);
                    _logger.LogInformation("GitHub client initialized with authentication token (length: {Length})", token.Length);
                    
                    // Test the token by making a simple API call
                    var user = _gitHubClient.User.Current().GetAwaiter().GetResult();
                    _logger.LogInformation("GitHub token validated successfully. Authenticated as: {User}", user.Login);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating GitHub token. Token may be invalid or have insufficient permissions.");
                    // Continue with unauthenticated access as fallback
                    _gitHubClient.Credentials = null;
                }
            }
            else
            {
                _logger.LogWarning("No GitHub authentication token provided. Using unauthenticated access (rate limits may apply)");
            }
            
            _logger.LogInformation("Initialized GitHub tutorial loader for {Owner}/{Repo}:{Branch}/{Path}", 
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
                _logger.LogInformation("Fetching tutorials from GitHub: {Owner}/{Repo}:{Branch}/{Path}", 
                    _owner, _repo, _branch, _tutorialsPath);
                
                // Get all files in the tutorials directory
                var contents = await _gitHubClient.Repository.Content.GetAllContents(
                    _owner, _repo, _tutorialsPath);
                
                _logger.LogInformation("Found {Count} items in the tutorials directory", contents.Count);
                
                // Enhanced logging: List all files found
                _logger.LogInformation("=== FILES FOUND IN REPOSITORY ===");
                foreach (var item in contents)
                {
                    _logger.LogInformation("- {Type}: {Path} ({Size} bytes)", 
                        item.Type, item.Path, item.Size);
                }
                _logger.LogInformation("=== END OF FILES LIST ===");
                
                int jsonCount = 0;
                int yamlCount = 0;
                int ymlCount = 0;
                int dirCount = 0;
                int otherCount = 0;
                int skippedCount = 0;
                int successCount = 0;
                int failedCount = 0;
                
                foreach (var content in contents)
                {
                    // Enhanced logging for file processing
                    _logger.LogInformation("Processing item: {Path} (Type: {Type})", content.Path, content.Type);
                    
                    // Log what we found
                    if (content.Type == ContentType.Dir)
                    {
                        dirCount++;
                        _logger.LogInformation("Skipping directory: {Name}", content.Name);
                        skippedCount++;
                        continue;
                    }
                    else if (content.Name.EndsWith(".json"))
                    {
                        jsonCount++;
                        _logger.LogInformation("Processing JSON file: {Name}", content.Name);
                    }
                    else if (content.Name.EndsWith(".yaml"))
                    {
                        yamlCount++;
                        _logger.LogInformation("Processing YAML file: {Name}", content.Name);
                    }
                    else if (content.Name.EndsWith(".yml"))
                    {
                        ymlCount++;
                        _logger.LogInformation("Processing YML file: {Name}", content.Name);
                    }
                    else
                    {
                        otherCount++;
                        _logger.LogInformation("Skipping unsupported file: {Name} (extension not recognized)", content.Name);
                        skippedCount++;
                        continue;
                    }
                    
                    try
                    {
                        _logger.LogInformation("Loading tutorial from GitHub: {Path}", content.Path);
                        var tutorial = await LoadTutorialFromGitHubAsync(content.Path);
                        
                        if (tutorial != null)
                        {
                            _logger.LogInformation("Successfully loaded tutorial: {Id} - {Title} - Steps: {StepCount}", 
                                tutorial.Id, tutorial.Title, tutorial.Steps?.Count ?? 0);
                            
                            successCount++;
                            tutorials.Add(new TutorialMetadata
                            {
                                Id = tutorial.Id,
                                Title = tutorial.Title,
                                Description = tutorial.Description,
                                Xp = tutorial.Xp,
                                Difficulty = tutorial.Difficulty
                            });
                        }
                        else
                        {
                            _logger.LogWarning("Failed to load tutorial from file: {Path} (deserialization returned null)", content.Path);
                            failedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading tutorial from GitHub: {Path}", content.Path);
                        failedCount++;
                    }
                }
                
                // Enhanced summary logging
                _logger.LogInformation("=== TUTORIAL LOADING SUMMARY ===");
                _logger.LogInformation("File types found - JSON: {JsonCount}, YAML: {YamlCount}, YML: {YmlCount}, Directories: {DirCount}, Other: {OtherCount}",
                    jsonCount, yamlCount, ymlCount, dirCount, otherCount);
                _logger.LogInformation("Processing results - Success: {SuccessCount}, Failed: {FailedCount}, Skipped: {SkippedCount}",
                    successCount, failedCount, skippedCount);
                _logger.LogInformation("Successfully loaded {Count} tutorials from GitHub", tutorials.Count);
                _logger.LogInformation("=== END OF SUMMARY ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial files from GitHub: {Owner}/{Repo}/{Path}", 
                    _owner, _repo, _tutorialsPath);
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
                _logger.LogInformation("Fetching tutorial with ID: {Id} from GitHub", id);
                
                // Try to find the tutorial file with the given ID
                var jsonPath = $"{_tutorialsPath}/{id}.json";
                var yamlPath = $"{_tutorialsPath}/{id}.yaml";
                var ymlPath = $"{_tutorialsPath}/{id}.yml";
                
                _logger.LogDebug("Checking for tutorial files: {JsonPath}, {YamlPath}, {YmlPath}", 
                    jsonPath, yamlPath, ymlPath);
                
                // Try JSON file first
                try
                {
                    _logger.LogDebug("Attempting to load JSON tutorial: {Path}", jsonPath);
                    var tutorial = await LoadTutorialFromGitHubAsync(jsonPath);
                    if (tutorial != null)
                    {
                        _logger.LogInformation("Successfully loaded tutorial from JSON: {Id}", id);
                        return tutorial;
                    }
                }
                catch (NotFoundException)
                {
                    _logger.LogDebug("JSON file not found: {Path}", jsonPath);
                    // File not found, try next format
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading JSON tutorial: {Path}", jsonPath);
                }
                
                // Try YAML file
                try
                {
                    _logger.LogDebug("Attempting to load YAML tutorial: {Path}", yamlPath);
                    var tutorial = await LoadTutorialFromGitHubAsync(yamlPath);
                    if (tutorial != null)
                    {
                        _logger.LogInformation("Successfully loaded tutorial from YAML: {Id}", id);
                        return tutorial;
                    }
                }
                catch (NotFoundException)
                {
                    _logger.LogDebug("YAML file not found: {Path}", yamlPath);
                    // File not found, try next format
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading YAML tutorial: {Path}", yamlPath);
                }
                
                // Try YML file
                try
                {
                    _logger.LogDebug("Attempting to load YML tutorial: {Path}", ymlPath);
                    var tutorial = await LoadTutorialFromGitHubAsync(ymlPath);
                    if (tutorial != null)
                    {
                        _logger.LogInformation("Successfully loaded tutorial from YML: {Id}", id);
                        return tutorial;
                    }
                }
                catch (NotFoundException)
                {
                    _logger.LogDebug("YML file not found: {Path}", ymlPath);
                    // File not found, try searching all files
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading YML tutorial: {Path}", ymlPath);
                }
                
                _logger.LogInformation("No direct file match found for ID: {Id}, searching all files", id);
                
                try {
                    // If no direct match, search all files for a tutorial with the given ID
                    var contents = await _gitHubClient.Repository.Content.GetAllContents(
                        _owner, _repo, _tutorialsPath);
                    
                    _logger.LogDebug("Found {Count} files in tutorials directory", contents.Count);
                    
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
                        
                        _logger.LogDebug("Checking file for matching ID: {Path}", content.Path);
                        var tutorial = await LoadTutorialFromGitHubAsync(content.Path);
                        if (tutorial != null && tutorial.Id == id)
                        {
                            _logger.LogInformation("Found matching tutorial in file: {Path}", content.Path);
                            return tutorial;
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error searching all files in GitHub repository: {Owner}/{Repo}/{Path}", 
                        _owner, _repo, _tutorialsPath);
                }
                
                _logger.LogWarning("No tutorial found with ID: {Id} in GitHub repository", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial by ID from GitHub: {Id}", id);
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
                _logger.LogInformation("Fetching content from GitHub: {Path}", path);
                
                // Get the file content from GitHub
                byte[] fileContent;
                try
                {
                    fileContent = await _gitHubClient.Repository.Content.GetRawContent(
                        _owner, _repo, path);
                    
                    _logger.LogInformation("Successfully fetched {Length} bytes from {Path}", fileContent.Length, path);
                }
                catch (NotFoundException ex)
                {
                    _logger.LogWarning("File not found on GitHub: {Path}. Error: {Message}", path, ex.Message);
                    return null;
                }
                catch (RateLimitExceededException ex)
                {
                    _logger.LogError("GitHub API rate limit exceeded. Reset at: {ResetTime}. Error: {Message}", 
                        ex.Reset, ex.Message);
                    return null;
                }
                catch (AuthorizationException ex)
                {
                    _logger.LogError("GitHub authorization error: {Message}. Check your access token.", ex.Message);
                    return null;
                }
                catch (ApiException ex)
                {
                    _logger.LogError("GitHub API error: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error fetching content from GitHub: {Path}", path);
                    return null;
                }
                
                var fileExtension = Path.GetExtension(path).ToLowerInvariant();
                var content = System.Text.Encoding.UTF8.GetString(fileContent);
                
                // Temporarily log the raw content (first 500 chars max)
                var contentPreview = content.Length <= 500 ? content : content.Substring(0, 500) + "...";
                _logger.LogInformation("=== RAW CONTENT PREVIEW FOR {Path} ===\n{Content}\n=== END OF CONTENT PREVIEW ===", 
                    path, contentPreview);
                
                Tutorial? tutorial = null;
                
                if (fileExtension == ".json")
                {
                    _logger.LogInformation("Deserializing JSON tutorial from {Path}", path);
                    try
                    {
                        // Parse JSON file
                        tutorial = JsonSerializer.Deserialize<Tutorial>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        _logger.LogInformation("JSON deserialization result: {Success}", tutorial != null ? "Success" : "Failed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "JSON deserialization failed for {Path}: {ErrorMessage}", path, ex.Message);
                        return null;
                    }
                }
                else if (fileExtension == ".yaml" || fileExtension == ".yml")
                {
                    _logger.LogInformation("Deserializing YAML tutorial from {Path}", path);
                    try
                    {
                        // Parse YAML file using our helper method
                        tutorial = YamlHelpers.DeserializeTutorial(content, _logger);
                        
                        if (tutorial == null)
                        {
                            _logger.LogWarning("Failed to deserialize YAML tutorial from GitHub: {Path} (YamlHelpers returned null)", path);
                        }
                        else
                        {
                            _logger.LogInformation("YAML deserialization successful for {Path}", path);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "YAML deserialization failed for {Path}: {ErrorMessage}", path, ex.Message);
                        return null;
                    }
                }
                else
                {
                    _logger.LogWarning("Unsupported file extension: {Extension} for file {Path}", fileExtension, path);
                    return null;
                }
                
                if (tutorial == null)
                {
                    _logger.LogWarning("Failed to deserialize tutorial from GitHub: {Path} (result was null)", path);
                    return null;
                }
                
                // Ensure required fields are set
                if (string.IsNullOrEmpty(tutorial.Id))
                {
                    _logger.LogWarning("Tutorial ID is missing in {Path}", path);
                    tutorial.Id = Path.GetFileNameWithoutExtension(path);
                }
                
                if (string.IsNullOrEmpty(tutorial.Title))
                {
                    _logger.LogWarning("Tutorial Title is missing in {Path}", path);
                    tutorial.Title = tutorial.Id;
                }
                
                if (tutorial.Steps == null)
                {
                    _logger.LogWarning("Tutorial Steps is null in {Path}", path);
                    tutorial.Steps = new List<TutorialStep>();
                }
                
                // Log successful deserialization with detailed information
                _logger.LogInformation("Successfully deserialized tutorial: {Id}, Title: {Title}, Steps: {StepCount}", 
                    tutorial.Id, tutorial.Title, tutorial.Steps.Count);
                
                // Log details about steps if present
                if (tutorial.Steps.Count > 0)
                {
                    _logger.LogInformation("Tutorial {Id} has {Count} steps:", tutorial.Id, tutorial.Steps.Count);
                    for (int i = 0; i < tutorial.Steps.Count; i++)
                    {
                        var step = tutorial.Steps[i];
                        _logger.LogInformation("  Step {Index}: Instructions length: {InstructionsLength}, ExpectedCommand: {Command}",
                            i + 1, step.Instructions?.Length ?? 0, step.ExpectedCommand);
                    }
                }
                else
                {
                    _logger.LogInformation("Tutorial {Id} has no steps (Steps array is empty)", tutorial.Id);
                }
                
                // If content is a file path, load the content from GitHub
                if (!string.IsNullOrEmpty(tutorial.Content) && 
                    tutorial.Content.EndsWith(".ps1") && 
                    !tutorial.Content.Contains("\n"))
                {
                    var contentPath = tutorial.Content;
                    if (!Path.IsPathRooted(contentPath))
                    {
                        // Resolve relative path
                        var directory = Path.GetDirectoryName(path) ?? _tutorialsPath;
                        contentPath = Path.Combine(directory, contentPath).Replace("\\", "/");
                    }
                    
                    _logger.LogDebug("Tutorial content references external file: {ContentPath}", contentPath);
                    
                    try
                    {
                        var scriptContent = await _gitHubClient.Repository.Content.GetRawContent(
                            _owner, _repo, contentPath);
                        
                        tutorial.Content = System.Text.Encoding.UTF8.GetString(scriptContent);
                        _logger.LogDebug("Successfully loaded external content file: {ContentPath}", contentPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Tutorial content file not found on GitHub: {ContentPath}", contentPath);
                        // Don't fail if content file is missing, just leave the content as is
                    }
                }
                
                return tutorial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading tutorial from GitHub: {Path}", path);
                return null;
            }
        }
    }
}