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
        /// Safely deserializes a YAML string to a Challenge object, ensuring steps are properly loaded
        /// </summary>
        /// <param name="yamlContent">The YAML content to deserialize</param>
        /// <param name="logger">Optional logger for debugging</param>
        /// <returns>The deserialized Challenge object</returns>
        public static Challenge? DeserializeChallenge(string yamlContent, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                logger?.LogWarning("YAML content is null or empty");
                return null;
            }
            
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
                
                Dictionary<string, object>? yamlObject;
                
                try
                {
                    // Parse the YAML as a dictionary first for more control
                    yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to deserialize YAML to dictionary. YAML content:\n{Content}", contentPreview);
                    return null;
                }
                
                if (yamlObject == null)
                {
                    logger?.LogWarning("Failed to deserialize YAML to dictionary (result was null)");
                    return null;
                }
                
                // Create a new Challenge object
                var challenge = new Challenge();
                
                // Map basic properties
                if (yamlObject.TryGetValue("id", out var id) && id != null)
                    challenge.Id = id.ToString() ?? string.Empty;
                
                if (yamlObject.TryGetValue("title", out var title) && title != null)
                    challenge.Title = title.ToString() ?? string.Empty;
                
                if (yamlObject.TryGetValue("description", out var description) && description != null)
                    challenge.Description = description.ToString() ?? string.Empty;
                
                // Handle xpReward -> Xp mapping
                if (yamlObject.TryGetValue("xpReward", out var xpReward) && xpReward != null)
                {
                    if (int.TryParse(xpReward.ToString(), out var xpValue))
                        challenge.Xp = xpValue;
                    else
                        logger?.LogWarning("Failed to parse XP value: {Value}", xpReward);
                }
                else if (yamlObject.TryGetValue("xp", out var xp) && xp != null)
                {
                    if (int.TryParse(xp.ToString(), out var xpValue))
                        challenge.Xp = xpValue;
                    else
                        logger?.LogWarning("Failed to parse XP value: {Value}", xp);
                }
                
                if (yamlObject.TryGetValue("difficulty", out var difficulty) && difficulty != null)
                    challenge.Difficulty = difficulty.ToString() ?? string.Empty;
                
                // Handle prerequisiteTutorial -> TutorialId mapping
                if (yamlObject.TryGetValue("prerequisiteTutorial", out var prerequisiteTutorial) && prerequisiteTutorial != null)
                    challenge.TutorialId = prerequisiteTutorial.ToString() ?? string.Empty;
                else if (yamlObject.TryGetValue("tutorialId", out var tutorialId) && tutorialId != null)
                    challenge.TutorialId = tutorialId.ToString() ?? string.Empty;
                
                // Initialize steps list
                challenge.Steps = new List<ChallengeStep>();
                
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
                            foreach (var stepDict in stepDicts)
                            {
                                var step = new ChallengeStep();
                                
                                // Map step properties with proper name conversion
                                if (stepDict.TryGetValue("id", out var stepId) && stepId != null)
                                    step.Id = stepId.ToString() ?? string.Empty;
                                
                                if (stepDict.TryGetValue("title", out var stepTitle) && stepTitle != null)
                                    step.Title = stepTitle.ToString() ?? string.Empty;
                                
                                // Map 'instructions' or 'instruction' to 'Instructions'
                                if (stepDict.TryGetValue("instructions", out var instructions) && instructions != null)
                                    step.Instructions = instructions.ToString() ?? string.Empty;
                                else if (stepDict.TryGetValue("instruction", out var instruction) && instruction != null)
                                    step.Instructions = instruction.ToString() ?? string.Empty;
                                
                                // Map 'expectedCommand' or 'command' to 'ExpectedCommand'
                                if (stepDict.TryGetValue("expectedCommand", out var expectedCommand) && expectedCommand != null)
                                    step.ExpectedCommand = expectedCommand.ToString() ?? string.Empty;
                                else if (stepDict.TryGetValue("command", out var command) && command != null)
                                    step.ExpectedCommand = command.ToString() ?? string.Empty;
                                
                                if (stepDict.TryGetValue("hint", out var hint) && hint != null)
                                    step.Hint = hint.ToString() ?? string.Empty;
                                
                                // Process XP value - handle both xp and xpReward
                                if (stepDict.TryGetValue("xpReward", out var stepXpReward) && stepXpReward != null)
                                {
                                    if (int.TryParse(stepXpReward.ToString(), out var stepXpValue))
                                        step.Xp = stepXpValue;
                                    else
                                        logger?.LogWarning("Failed to parse step XP value: {Value}", stepXpReward);
                                }
                                else if (stepDict.TryGetValue("xp", out var stepXp) && stepXp != null)
                                {
                                    if (int.TryParse(stepXp.ToString(), out var stepXpValue))
                                        step.Xp = stepXpValue;
                                    else
                                        logger?.LogWarning("Failed to parse step XP value: {Value}", stepXp);
                                }
                                
                                // Ensure required fields have at least empty values
                                step.Id ??= string.Empty;
                                step.Title ??= string.Empty;
                                step.Instructions ??= string.Empty;
                                step.ExpectedCommand ??= string.Empty;
                                step.Hint ??= string.Empty;
                                
                                // Log step details for debugging
                                logger?.LogDebug("Mapped challenge step: Id={Id}, Title={Title}, Instructions={InstructionsLength}, ExpectedCommand={Command}",
                                    step.Id, step.Title, step.Instructions.Length, step.ExpectedCommand);
                                
                                challenge.Steps.Add(step);
                            }
                            
                            logger?.LogDebug("Successfully mapped {Count} challenge steps", challenge.Steps.Count);
                        }
                        else
                        {
                            logger?.LogWarning("Challenge steps array was empty or null after deserialization");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Error processing challenge steps from YAML");
                    }
                }
                else
                {
                    logger?.LogWarning("No 'steps' key found in challenge YAML dictionary");
                }
                
                logger?.LogDebug("Successfully deserialized challenge: {Id}, Title: {Title}, Steps: {StepCount}", 
                    challenge.Id, challenge.Title, challenge.Steps.Count);
                
                return challenge;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error deserializing YAML challenge");
                return null;
            }
        }
        
        /// <summary>
        /// Safely deserializes a YAML string to a Tutorial object, ensuring steps are properly loaded
        /// </summary>
        /// <param name="yamlContent">The YAML content to deserialize</param>
        /// <param name="logger">Optional logger for debugging</param>
        /// <returns>The deserialized Tutorial object</returns>
        public static Tutorial? DeserializeTutorial(string yamlContent, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                logger?.LogWarning("YAML content is null or empty");
                return null;
            }
            
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
                
                Dictionary<string, object>? yamlObject;
                
                try
                {
                    // Parse the YAML as a dictionary first for more control
                    yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to deserialize YAML to dictionary. YAML content:\n{Content}", contentPreview);
                    return null;
                }
                
                if (yamlObject == null)
                {
                    logger?.LogWarning("Failed to deserialize YAML to dictionary (result was null)");
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
                
                if (yamlObject.TryGetValue("xp", out var xp) && xp != null)
                {
                    if (int.TryParse(xp.ToString(), out var xpValue))
                        tutorial.Xp = xpValue;
                    else
                        logger?.LogWarning("Failed to parse XP value: {Value}", xp);
                }
                
                if (yamlObject.TryGetValue("difficulty", out var difficulty) && difficulty != null)
                    tutorial.Difficulty = difficulty.ToString() ?? string.Empty;
                
                if (yamlObject.TryGetValue("content", out var content) && content != null)
                    tutorial.Content = content.ToString() ?? string.Empty;
                
                // Initialize steps list
                tutorial.Steps = new List<TutorialStep>();
                
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
                            foreach (var stepDict in stepDicts)
                            {
                                var step = new TutorialStep();
                                
                                // Map step properties with proper name conversion
                                if (stepDict.TryGetValue("id", out var stepId) && stepId != null)
                                    step.Id = stepId.ToString() ?? string.Empty;
                                
                                if (stepDict.TryGetValue("title", out var stepTitle) && stepTitle != null)
                                    step.Title = stepTitle.ToString() ?? string.Empty;
                                
                                // Map 'instructions' or 'instruction' to 'Instructions'
                                if (stepDict.TryGetValue("instructions", out var instructions) && instructions != null)
                                    step.Instructions = instructions.ToString() ?? string.Empty;
                                else if (stepDict.TryGetValue("instruction", out var instruction) && instruction != null)
                                    step.Instructions = instruction.ToString() ?? string.Empty;
                                
                                // Map 'expectedCommand' or 'command' to 'ExpectedCommand'
                                if (stepDict.TryGetValue("expectedCommand", out var expectedCommand) && expectedCommand != null)
                                    step.ExpectedCommand = expectedCommand.ToString() ?? string.Empty;
                                else if (stepDict.TryGetValue("command", out var command) && command != null)
                                    step.ExpectedCommand = command.ToString() ?? string.Empty;
                                
                                if (stepDict.TryGetValue("hint", out var hint) && hint != null)
                                    step.Hint = hint.ToString() ?? string.Empty;
                                
                                // Process XP value if it exists
                                if (stepDict.TryGetValue("xp", out var stepXp) && stepXp != null)
                                {
                                    if (int.TryParse(stepXp.ToString(), out var stepXpValue))
                                        step.Xp = stepXpValue;
                                    else
                                        logger?.LogWarning("Failed to parse step XP value: {Value}", stepXp);
                                }
                                
                                // Process validation if it exists
                                if (stepDict.TryGetValue("validation", out var validationObj) && validationObj != null)
                                {
                                    try
                                    {
                                        var validationJson = JsonSerializer.Serialize(validationObj);
                                        logger?.LogDebug("Validation JSON: {Json}", validationJson);
                                        
                                        var validation = JsonSerializer.Deserialize<ValidationRule>(validationJson);
                                        
                                        if (validation != null)
                                        {
                                            step.Validation = validation;
                                            logger?.LogDebug("Successfully mapped validation rule: Type={Type}, Value={Value}", 
                                                validation.Type, validation.Value);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger?.LogWarning(ex, "Error deserializing validation rule");
                                    }
                                }
                                
                                // Ensure required fields have at least empty values
                                step.Id ??= string.Empty;
                                step.Title ??= string.Empty;
                                step.Instructions ??= string.Empty;
                                step.ExpectedCommand ??= string.Empty;
                                
                                // Log step details for debugging
                                logger?.LogDebug("Mapped step: Id={Id}, Title={Title}, Instructions={InstructionsLength}, ExpectedCommand={Command}",
                                    step.Id, step.Title, step.Instructions.Length, step.ExpectedCommand);
                                
                                tutorial.Steps.Add(step);
                            }
                            
                            logger?.LogDebug("Successfully mapped {Count} steps", tutorial.Steps.Count);
                        }
                        else
                        {
                            logger?.LogWarning("Steps array was empty or null after deserialization");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Error processing steps from YAML");
                    }
                }
                else
                {
                    logger?.LogWarning("No 'steps' key found in YAML dictionary");
                }
                
                logger?.LogDebug("Successfully deserialized tutorial: {Id}, Title: {Title}, Steps: {StepCount}", 
                    tutorial.Id, tutorial.Title, tutorial.Steps.Count);
                
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