using CmdShiftLearn.Api.Models;
using Supabase;
using Postgrest.Models;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Loads challenges from Supabase
    /// </summary>
    public class SupabaseChallengeLoader : IChallengeLoader
    {
        private readonly Client _supabaseClient;
        private readonly ILogger<SupabaseChallengeLoader> _logger;
        private readonly string _challengesTable;
        
        public SupabaseChallengeLoader(IConfiguration configuration, ILogger<SupabaseChallengeLoader> logger)
        {
            var url = configuration["Supabase:Url"] ?? throw new ArgumentNullException("Supabase:Url");
            var key = configuration["Supabase:ApiKey"] ?? throw new ArgumentNullException("Supabase:ApiKey");
            _challengesTable = configuration["ContentSources:Challenges:TableName"] ?? "challenges";
            _logger = logger;
            
            // Initialize Supabase client
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            
            _supabaseClient = new Client(url, key, options);
            
            _logger.LogInformation("Initialized Supabase challenge loader for table: {Table}", _challengesTable);
        }
        
        /// <summary>
        /// Gets all available challenge metadata from Supabase
        /// </summary>
        /// <returns>A list of challenge metadata</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetAllChallengeMetadataAsync()
        {
            var challenges = new List<ChallengeMetadata>();
            
            try
            {
                // Query the challenges table
                var response = await _supabaseClient
                    .From<SupabaseChallenge>()
                    .Select("id, title, description, xp, difficulty, tutorial_id")
                    .Get();
                
                var challengeRecords = response.Models;
                
                foreach (var record in challengeRecords)
                {
                    challenges.Add(new ChallengeMetadata
                    {
                        Id = record.Id,
                        Title = record.Title,
                        Description = record.Description,
                        Xp = record.Xp,
                        Difficulty = record.Difficulty,
                        TutorialId = record.TutorialId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenges from Supabase");
            }
            
            return challenges;
        }
        
        /// <summary>
        /// Gets a specific challenge by ID including its script from Supabase
        /// </summary>
        /// <param name="id">The challenge ID</param>
        /// <returns>The challenge with script if found, null otherwise</returns>
        public async Task<Challenge?> GetChallengeByIdAsync(string id)
        {
            try
            {
                // Query the challenges table for the specific ID
                var response = await _supabaseClient
                    .From<SupabaseChallenge>()
                    .Select("*")
                    .Filter("id", Postgrest.Constants.Operator.Equals, id)
                    .Single();
                
                if (response == null)
                {
                    return null;
                }
                
                // Convert to Challenge model
                var challenge = new Challenge
                {
                    Id = response.Id,
                    Title = response.Title,
                    Description = response.Description,
                    Script = response.Script,
                    Xp = response.Xp,
                    Difficulty = response.Difficulty,
                    TutorialId = response.TutorialId
                };
                
                // If script is stored as a JSON or YAML string, parse it
                if (response.ScriptFormat == "json")
                {
                    try
                    {
                        var scriptObj = JsonSerializer.Deserialize<dynamic>(response.Script);
                        // Handle JSON script if needed
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing JSON script for challenge: {Id}", id);
                    }
                }
                else if (response.ScriptFormat == "yaml" || response.ScriptFormat == "yml")
                {
                    try
                    {
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();
                        
                        var scriptObj = deserializer.Deserialize<dynamic>(response.Script);
                        // Handle YAML script if needed
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing YAML script for challenge: {Id}", id);
                    }
                }
                
                return challenge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenge by ID from Supabase: {Id}", id);
                return null;
            }
        }
        
        /// <summary>
        /// Gets all challenges associated with a specific tutorial from Supabase
        /// </summary>
        /// <param name="tutorialId">The tutorial ID</param>
        /// <returns>A list of challenge metadata for the specified tutorial</returns>
        public async Task<IEnumerable<ChallengeMetadata>> GetChallengesByTutorialIdAsync(string tutorialId)
        {
            var challenges = new List<ChallengeMetadata>();
            
            try
            {
                // Query the challenges table for the specific tutorial ID
                var response = await _supabaseClient
                    .From<SupabaseChallenge>()
                    .Select("id, title, description, xp, difficulty, tutorial_id")
                    .Filter("tutorial_id", Postgrest.Constants.Operator.Equals, tutorialId)
                    .Get();
                
                var challengeRecords = response.Models;
                
                foreach (var record in challengeRecords)
                {
                    challenges.Add(new ChallengeMetadata
                    {
                        Id = record.Id,
                        Title = record.Title,
                        Description = record.Description,
                        Xp = record.Xp,
                        Difficulty = record.Difficulty,
                        TutorialId = record.TutorialId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challenges by tutorial ID from Supabase: {TutorialId}", tutorialId);
            }
            
            return challenges;
        }
        
        /// <summary>
        /// Supabase challenge model for database mapping
        /// </summary>
        [Postgrest.Attributes.Table("challenges")]
        private class SupabaseChallenge : BaseModel
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Script { get; set; } = string.Empty;
            public string ScriptFormat { get; set; } = "text"; // text, json, yaml, yml
            public int Xp { get; set; } = 0;
            public string Difficulty { get; set; } = "beginner";
            public string TutorialId { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}