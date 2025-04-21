using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    /// <summary>
    /// Interface for Shello service that provides AI-powered responses
    /// </summary>
    public interface IShelloService
    {
        /// <summary>
        /// Gets a hint from Shello based on the tutorial, step, and user input
        /// </summary>
        /// <param name="tutorialId">The ID of the tutorial</param>
        /// <param name="stepId">The ID of the step</param>
        /// <param name="userInput">The user's input</param>
        /// <param name="previousHint">Optional previous hint that was given</param>
        /// <returns>A hint from Shello</returns>
        Task<string> GetHintFromShelloAsync(string tutorialId, string stepId, string userInput, string? previousHint = null);
    }

    /// <summary>
    /// Service for interacting with OpenAI to generate AI-powered responses from Shello
    /// </summary>
    public class ShelloService : IShelloService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ShelloService> _logger;
        private readonly ITutorialService _tutorialService;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly float _temperature;
        private readonly int _maxRetries;
        private readonly int _retryDelayMs;

        /// <summary>
        /// Initializes a new instance of the ShelloService
        /// </summary>
        public ShelloService(
            HttpClient httpClient,
            IOptions<OpenAISettings> options,
            ILogger<ShelloService> logger,
            ITutorialService tutorialService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tutorialService = tutorialService;
            
            // Load configuration from options
            var settings = options.Value;
            _apiKey = settings.ApiKey;
            _model = settings.Model;
            _temperature = settings.Temperature;
            _maxRetries = settings.MaxRetries;
            _retryDelayMs = settings.RetryDelayMs;
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("OpenAI API key not configured. Shello service will not function correctly.");
                throw new InvalidOperationException("OpenAI API key not configured");
            }
            
            // Configure HttpClient
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            
            _logger.LogInformation("ShelloService initialized with model: {Model}, temperature: {Temperature}", 
                _model, _temperature);
        }

        /// <summary>
        /// Gets a hint from Shello based on the tutorial, step, and user input
        /// </summary>
        /// <param name="tutorialId">The ID of the tutorial</param>
        /// <param name="stepId">The ID of the step</param>
        /// <param name="userInput">The user's input</param>
        /// <param name="previousHint">Optional previous hint that was given</param>
        /// <returns>A hint from Shello</returns>
        public async Task<string> GetHintFromShelloAsync(string tutorialId, string stepId, string userInput, string? previousHint = null)
        {
            try
            {
                _logger.LogInformation("Generating hint for tutorial: {TutorialId}, step: {StepId}", 
                    tutorialId, stepId);
                
                // Get tutorial context
                var tutorial = await _tutorialService.GetTutorialByIdAsync(tutorialId);
                if (tutorial == null)
                {
                    _logger.LogWarning("Tutorial not found: {TutorialId}", tutorialId);
                    return "I'm sorry, but I couldn't find that tutorial. Let's try a different approach!";
                }
                
                // Find the specific step
                var step = tutorial.Steps.FirstOrDefault(s => s.Id == stepId);
                if (step == null)
                {
                    _logger.LogWarning("Step not found: {StepId} in tutorial: {TutorialId}", stepId, tutorialId);
                    return "I'm sorry, but I couldn't find that specific step. Let's focus on what you're trying to do!";
                }
                
                // Create the messages for the OpenAI API
                var messages = new List<object>
                {
                    new
                    {
                        role = "system",
                        content = "You are Shello, an upbeat PowerShell teaching assistant. Respond with clear, kind, and supportive hints or guidance to help the user understand their mistake and try again."
                    },
                    new
                    {
                        role = "user",
                        content = $"I'm working on a PowerShell tutorial titled '{tutorial.Title}'. The current step is: '{step.Title}' with instructions: '{step.Instructions}'. The expected command is: '{step.ExpectedCommand}'. I entered: '{userInput}'. {(previousHint != null ? $"You previously gave me this hint: '{previousHint}'" : "")} Please give me a helpful hint without directly giving me the answer."
                    }
                };
                
                // Create the request payload
                var requestPayload = new
                {
                    model = _model,
                    messages,
                    temperature = _temperature,
                    max_tokens = 300,
                    top_p = 1,
                    frequency_penalty = 0,
                    presence_penalty = 0
                };
                
                // Log the request for debugging
                var requestJson = JsonSerializer.Serialize(requestPayload);
                _logger.LogDebug("OpenAI API Request: {Request}", requestJson);
                
                // Send the request to OpenAI with retry logic
                string responseContent = await SendWithRetryAsync(requestPayload);
                
                // Parse the response
                using var responseDoc = JsonDocument.Parse(responseContent);
                var choices = responseDoc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    var content = message.GetProperty("content").GetString();
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        _logger.LogInformation("Successfully generated hint for tutorial: {TutorialId}, step: {StepId}", 
                            tutorialId, stepId);
                        return content;
                    }
                }
                
                _logger.LogWarning("Failed to extract hint from OpenAI response: {Response}", responseContent);
                return "I'm thinking about how to help you with this. Let's look at the instructions again and try a different approach!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hint for tutorial: {TutorialId}, step: {StepId}", 
                    tutorialId, stepId);
                return "I'm having trouble thinking right now. Let's try a simpler approach: check your syntax and make sure you're using the right PowerShell commands!";
            }
        }
        
        /// <summary>
        /// Sends a request to the OpenAI API with retry logic
        /// </summary>
        /// <param name="payload">The request payload</param>
        /// <returns>The response content</returns>
        private async Task<string> SendWithRetryAsync(object payload)
        {
            int attempts = 0;
            Exception? lastException = null;
            
            while (attempts < _maxRetries)
            {
                try
                {
                    attempts++;
                    
                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");
                    
                    var response = await _httpClient.PostAsync("chat/completions", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("OpenAI API returned non-success status code: {StatusCode}, Content: {Content}, Attempt: {Attempt}/{MaxRetries}",
                        response.StatusCode, errorContent, attempts, _maxRetries);
                    
                    // If we get a rate limit error, wait longer
                    if ((int)response.StatusCode == 429)
                    {
                        await Task.Delay(_retryDelayMs * 2);
                    }
                    else
                    {
                        await Task.Delay(_retryDelayMs);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Error calling OpenAI API, Attempt: {Attempt}/{MaxRetries}", 
                        attempts, _maxRetries);
                    await Task.Delay(_retryDelayMs);
                }
            }
            
            if (lastException != null)
            {
                throw new Exception($"Failed to call OpenAI API after {_maxRetries} attempts", lastException);
            }
            
            throw new Exception($"Failed to call OpenAI API after {_maxRetries} attempts");
        }
    }
}