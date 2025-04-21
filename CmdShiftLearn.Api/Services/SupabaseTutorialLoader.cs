using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Helpers;
using Supabase;
using Postgrest.Models;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Loads tutorials from Supabase
    /// </summary>
    public class SupabaseTutorialLoader : ITutorialLoader
    {
        private readonly Client _supabaseClient;
        private readonly ILogger<SupabaseTutorialLoader> _logger;
        private readonly string _tutorialsTable;
        
        public SupabaseTutorialLoader(IConfiguration configuration, ILogger<SupabaseTutorialLoader> logger)
        {
            var url = configuration["Supabase:Url"] ?? throw new ArgumentNullException("Supabase:Url");
            var key = configuration["Supabase:ApiKey"] ?? throw new ArgumentNullException("Supabase:ApiKey");
            _tutorialsTable = configuration["ContentSources:Tutorials:TableName"] ?? "tutorials";
            _logger = logger;
            
            // Initialize Supabase client
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            
            _supabaseClient = new Client(url, key, options);
            
            _logger.LogInformation("Initialized Supabase tutorial loader for table: {Table}", _tutorialsTable);
        }
        
        /// <summary>
        /// Gets all available tutorial metadata from Supabase
        /// </summary>
        /// <returns>A list of tutorial metadata</returns>
        public async Task<IEnumerable<TutorialMetadata>> GetAllTutorialMetadataAsync()
        {
            var tutorials = new List<TutorialMetadata>();
            
            try
            {
                // Query the tutorials table
                var response = await _supabaseClient
                    .From<SupabaseTutorial>()
                    .Select("id, title, description, xp, difficulty")
                    .Get();
                
                var tutorialRecords = response.Models;
                
                foreach (var record in tutorialRecords)
                {
                    tutorials.Add(new TutorialMetadata
                    {
                        Id = record.Id,
                        Title = record.Title,
                        Description = record.Description,
                        Xp = record.Xp,
                        Difficulty = record.Difficulty
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorials from Supabase");
            }
            
            return tutorials;
        }
        
        /// <summary>
        /// Gets a specific tutorial by ID including its content from Supabase
        /// </summary>
        /// <param name="id">The tutorial ID</param>
        /// <returns>The tutorial with content if found, null otherwise</returns>
        public async Task<Tutorial?> GetTutorialByIdAsync(string id)
        {
            try
            {
                // Query the tutorials table for the specific ID
                var response = await _supabaseClient
                    .From<SupabaseTutorial>()
                    .Select("*")
                    .Filter("id", Postgrest.Constants.Operator.Equals, id)
                    .Single();
                
                if (response == null)
                {
                    return null;
                }
                
                // Convert to Tutorial model
                var tutorial = new Tutorial
                {
                    Id = response.Id,
                    Title = response.Title,
                    Description = response.Description,
                    Content = response.Content,
                    Xp = response.Xp,
                    Difficulty = response.Difficulty,
                    Steps = new List<TutorialStep>()
                };
                
                // Parse steps if available
                if (!string.IsNullOrEmpty(response.Steps))
                {
                    try
                    {
                        var steps = JsonSerializer.Deserialize<List<TutorialStep>>(response.Steps, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (steps != null)
                        {
                            tutorial.Steps = steps;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing steps for tutorial: {Id}", id);
                    }
                }
                
                // If content is stored as a JSON or YAML string, parse it
                if (response.ContentFormat == "json")
                {
                    try
                    {
                        var contentObj = JsonSerializer.Deserialize<dynamic>(response.Content);
                        // Handle JSON content if needed
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing JSON content for tutorial: {Id}", id);
                    }
                }
                else if (response.ContentFormat == "yaml" || response.ContentFormat == "yml")
                {
                    try
                    {
                        // If the content is in YAML format, try to parse it as a Tutorial
                        var yamlTutorial = YamlHelpers.DeserializeTutorial(response.Content, _logger);
                        if (yamlTutorial != null)
                        {
                            // Copy any additional properties from the YAML content
                            if (!string.IsNullOrEmpty(yamlTutorial.Title))
                                tutorial.Title = yamlTutorial.Title;
                            
                            if (!string.IsNullOrEmpty(yamlTutorial.Description))
                                tutorial.Description = yamlTutorial.Description;
                            
                            if (yamlTutorial.Xp > 0)
                                tutorial.Xp = yamlTutorial.Xp;
                            
                            if (!string.IsNullOrEmpty(yamlTutorial.Difficulty))
                                tutorial.Difficulty = yamlTutorial.Difficulty;
                            
                            if (!string.IsNullOrEmpty(yamlTutorial.Content))
                                tutorial.Content = yamlTutorial.Content;
                            
                            // Copy steps if available
                            if (yamlTutorial.Steps != null && yamlTutorial.Steps.Count > 0)
                            {
                                tutorial.Steps = yamlTutorial.Steps;
                                _logger.LogDebug("Loaded {Count} steps from YAML content for tutorial: {Id}", 
                                    yamlTutorial.Steps.Count, id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing YAML content for tutorial: {Id}", id);
                    }
                }
                
                return tutorial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutorial by ID from Supabase: {Id}", id);
                return null;
            }
        }
        
        /// <summary>
        /// Supabase tutorial model for database mapping
        /// </summary>
        [Postgrest.Attributes.Table("tutorials")]
        private class SupabaseTutorial : BaseModel
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string ContentFormat { get; set; } = "text"; // text, json, yaml, yml
            public int Xp { get; set; } = 0;
            public string Difficulty { get; set; } = "beginner";
            public string Steps { get; set; } = string.Empty; // JSON string of steps
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}