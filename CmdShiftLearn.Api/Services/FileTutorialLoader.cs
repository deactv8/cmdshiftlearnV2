using CmdShiftLearn.Api.Models;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Loads tutorials from the file system
    /// </summary>
    public class FileTutorialLoader : ITutorialLoader
    {
        private readonly string _tutorialsDirectory;
        private readonly ILogger<FileTutorialLoader> _logger;
        
        public FileTutorialLoader(IConfiguration configuration, ILogger<FileTutorialLoader> logger)
        {
            // Get the tutorials directory from configuration or use a default path
            _tutorialsDirectory = configuration["TutorialsDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "scripts", "tutorials");
            _logger = logger;
            
            // Ensure the tutorials directory exists
            if (!Directory.Exists(_tutorialsDirectory))
            {
                Directory.CreateDirectory(_tutorialsDirectory);
                _logger.LogInformation("Created tutorials directory: {Directory}", _tutorialsDirectory);
            }
        }
        
        /// <summary>
        /// Gets all available tutorial metadata
        /// </summary>
        /// <returns>A list of tutorial metadata</returns>
        public async Task<IEnumerable<TutorialMetadata>> GetAllTutorialMetadataAsync()
        {
            var tutorials = new List<TutorialMetadata>();
            
            try
            {
                // Get all JSON and YAML files in the tutorials directory
                var tutorialFiles = Directory.GetFiles(_tutorialsDirectory, "*.json")
                    .Concat(Directory.GetFiles(_tutorialsDirectory, "*.yaml"))
                    .Concat(Directory.GetFiles(_tutorialsDirectory, "*.yml"));
                
                foreach (var file in tutorialFiles)
                {
                    try
                    {
                        var tutorial = await LoadTutorialFromFileAsync(file);
                        if (tutorial != null)
                        {
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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading tutorial from file: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial files from directory: {Directory}", _tutorialsDirectory);
            }
            
            return tutorials;
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
                // Look for a tutorial file with the given ID
                var jsonFile = Path.Combine(_tutorialsDirectory, $"{id}.json");
                var yamlFile = Path.Combine(_tutorialsDirectory, $"{id}.yaml");
                var ymlFile = Path.Combine(_tutorialsDirectory, $"{id}.yml");
                
                if (File.Exists(jsonFile))
                {
                    return await LoadTutorialFromFileAsync(jsonFile);
                }
                else if (File.Exists(yamlFile))
                {
                    return await LoadTutorialFromFileAsync(yamlFile);
                }
                else if (File.Exists(ymlFile))
                {
                    return await LoadTutorialFromFileAsync(ymlFile);
                }
                
                // If no direct match, search all files for a tutorial with the given ID
                var tutorialFiles = Directory.GetFiles(_tutorialsDirectory, "*.json")
                    .Concat(Directory.GetFiles(_tutorialsDirectory, "*.yaml"))
                    .Concat(Directory.GetFiles(_tutorialsDirectory, "*.yml"));
                
                foreach (var file in tutorialFiles)
                {
                    var tutorial = await LoadTutorialFromFileAsync(file);
                    if (tutorial != null && tutorial.Id == id)
                    {
                        return tutorial;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial by ID: {Id}", id);
            }
            
            return null;
        }
        
        /// <summary>
        /// Loads a tutorial from a file
        /// </summary>
        /// <param name="filePath">Path to the tutorial file</param>
        /// <returns>The loaded tutorial or null if loading failed</returns>
        private async Task<Tutorial?> LoadTutorialFromFileAsync(string filePath)
        {
            try
            {
                var fileContent = await File.ReadAllTextAsync(filePath);
                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                
                Tutorial? tutorial;
                
                if (fileExtension == ".json")
                {
                    // Parse JSON file
                    tutorial = JsonSerializer.Deserialize<Tutorial>(fileContent, new JsonSerializerOptions
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
                    
                    tutorial = deserializer.Deserialize<Tutorial>(fileContent);
                }
                else
                {
                    _logger.LogWarning("Unsupported file extension: {Extension}", fileExtension);
                    return null;
                }
                
                if (tutorial == null)
                {
                    _logger.LogWarning("Failed to deserialize tutorial from file: {File}", filePath);
                    return null;
                }
                
                // If content is a file path, load the content from the file
                if (tutorial.Content.EndsWith(".ps1") && !tutorial.Content.Contains("\n"))
                {
                    var contentPath = Path.IsPathRooted(tutorial.Content)
                        ? tutorial.Content
                        : Path.Combine(Path.GetDirectoryName(filePath) ?? _tutorialsDirectory, tutorial.Content);
                    
                    if (File.Exists(contentPath))
                    {
                        tutorial.Content = await File.ReadAllTextAsync(contentPath);
                    }
                    else
                    {
                        _logger.LogWarning("Tutorial content file not found: {ContentPath}", contentPath);
                    }
                }
                
                return tutorial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tutorial from file: {File}", filePath);
                return null;
            }
        }
    }
}