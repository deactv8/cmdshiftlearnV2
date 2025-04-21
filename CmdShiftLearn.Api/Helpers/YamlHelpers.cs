using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Helpers
{
    /// <summary>
    /// Helper methods for YAML processing
    /// </summary>
    public static class YamlHelpers
    {
        /// <summary>
        /// Safely deserializes a YAML string to a Tutorial object, ensuring steps are properly loaded
        /// </summary>
        /// <param name="yamlContent">The YAML content to deserialize</param>
        /// <param name="logger">Optional logger for debugging</param>
        /// <returns>The deserialized Tutorial object</returns>
        public static Tutorial? DeserializeTutorial(string yamlContent, ILogger? logger = null)
        {
            try
            {
                // Log the first 500 characters of the YAML content for debugging
                var contentPreview = yamlContent.Length <= 500 ? yamlContent : yamlContent.Substring(0, 500) + "...";
                logger?.LogDebug("YAML content preview:\n{Content}", contentPreview);
                
                // Create YAML deserializer with camel case naming convention
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                
                // Parse the YAML as a dictionary first for more control
                var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                
                if (yamlObject == null)
                {
                    logger?.LogWarning("Failed to deserialize YAML to dictionary");
                    return null;
                }
                
                // Create a new Tutorial object
                var tutorial = new Tutorial();
                
                // Map basic properties
                if (yamlObject.TryGetValue("id", out var id) && id != null)
                    tutorial.Id = id.ToString() ?? string.Empty;
                
                if (yamlObject.TryGetValue("title", out var title) && title != null)
                    tutorial.Title = title.ToString() ?? string.Empty;
                
                if (yamlObject.TryGetValue("description", out var description) && description != null)
                    tutorial.Description = description.ToString() ?? string.Empty;
                
                if (yamlObject.TryGetValue("xp", out var xp) && xp != null && int.TryParse(xp.ToString(), out var xpValue))
                    tutorial.Xp = xpValue;
                
                if (yamlObject.TryGetValue("difficulty", out var difficulty) && difficulty != null)
                    tutorial.Difficulty = difficulty.ToString() ?? string.Empty;
                
                if (yamlObject.TryGetValue("content", out var content) && content != null)
                    tutorial.Content = content.ToString() ?? string.Empty;
                
                // Process steps if they exist
                if (yamlObject.TryGetValue("steps", out var stepsObj) && stepsObj != null)
                {
                    logger?.LogDebug("Found steps in YAML dictionary");
                    
                    try
                    {
                        // Convert the steps object to JSON for intermediate processing
                        var stepsJson = JsonSerializer.Serialize(stepsObj);
                        logger?.LogDebug("Steps JSON: {Json}", stepsJson);
                        
                        // Deserialize to a list of dictionaries for manual mapping
                        var stepDicts = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(stepsJson);
                        
                        if (stepDicts != null && stepDicts.Count > 0)
                        {
                            var steps = new List<TutorialStep>();
                            
                            foreach (var stepDict in stepDicts)
                            {
                                var step = new TutorialStep();
                                
                                // Map step properties with proper name conversion
                                if (stepDict.TryGetValue("id", out var stepId) && stepId != null)
                                    step.Id = stepId.ToString() ?? string.Empty;
                                
                                if (stepDict.TryGetValue("title", out var stepTitle) && stepTitle != null)
                                    step.Title = stepTitle.ToString() ?? string.Empty;
                                
                                // Map 'instruction' to 'Instructions'
                                if (stepDict.TryGetValue("instruction", out var instruction) && instruction != null)
                                    step.Instructions = instruction.ToString() ?? string.Empty;
                                
                                // Map 'command' to 'ExpectedCommand'
                                if (stepDict.TryGetValue("command", out var command) && command != null)
                                    step.ExpectedCommand = command.ToString() ?? string.Empty;
                                
                                if (stepDict.TryGetValue("hint", out var hint) && hint != null)
                                    step.Hint = hint.ToString() ?? string.Empty;
                                
                                // Process validation if it exists
                                if (stepDict.TryGetValue("validation", out var validationObj) && validationObj != null)
                                {
                                    var validationJson = JsonSerializer.Serialize(validationObj);
                                    var validation = JsonSerializer.Deserialize<ValidationRule>(validationJson);
                                    
                                    if (validation != null)
                                    {
                                        step.Validation = validation;
                                    }
                                }
                                
                                steps.Add(step);
                            }
                            
                            logger?.LogDebug("Successfully mapped {Count} steps", steps.Count);
                            tutorial.Steps = steps;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Error processing steps from YAML");
                    }
                }
                
                logger?.LogDebug("Successfully deserialized tutorial: {Id}, Title: {Title}, Steps: {StepCount}", 
                    tutorial.Id, tutorial.Title, tutorial.Steps?.Count ?? 0);
                
                return tutorial;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error deserializing YAML tutorial");
                return null;
            }
        }
    }
}