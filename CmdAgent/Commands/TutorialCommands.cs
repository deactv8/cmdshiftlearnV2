using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CmdAgent.Commands
{
    /// <summary>
    /// Commands for interacting with tutorials
    /// </summary>
    public class TutorialCommands
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TutorialCommands> _logger;
        private readonly string _apiBaseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public TutorialCommands(HttpClient httpClient, ILogger<TutorialCommands> logger, string apiBaseUrl)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiBaseUrl = apiBaseUrl;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Creates the command for starting a tutorial
        /// </summary>
        public Command CreateStartTutorialCommand()
        {
            var command = new Command("start-tutorial", "Start an interactive tutorial")
            {
                new Argument<string>("tutorialId", "The ID of the tutorial to start"),
                new Option<bool>("--hint", "Automatically request Shello hints for each step")
            };

            command.Handler = CommandHandler.Create<string, bool>(StartTutorialAsync);
            return command;
        }

        /// <summary>
        /// Starts an interactive tutorial
        /// </summary>
        private async Task StartTutorialAsync(string tutorialId, bool hint)
        {
            try
            {
                // 1. Fetch tutorial data
                var tutorial = await FetchTutorialAsync(tutorialId);
                if (tutorial == null)
                {
                    Console.WriteLine($"Tutorial with ID '{tutorialId}' not found.");
                    return;
                }

                // Display tutorial information
                Console.WriteLine();
                Console.WriteLine($"=== {tutorial.Title} ===");
                Console.WriteLine($"Difficulty: {tutorial.Difficulty}");
                Console.WriteLine($"XP: {tutorial.Xp}");
                Console.WriteLine();
                Console.WriteLine(tutorial.Description);
                Console.WriteLine();

                if (tutorial.Steps == null || tutorial.Steps.Count == 0)
                {
                    Console.WriteLine("This tutorial doesn't have any interactive steps.");
                    return;
                }

                // 2. Begin step-by-step loop
                int currentStepIndex = 0;
                bool isComplete = false;

                while (!isComplete)
                {
                    var currentStep = tutorial.Steps[currentStepIndex];
                    
                    // Display step information with progress
                    Console.WriteLine();
                    Console.WriteLine($"Step {currentStepIndex + 1} of {tutorial.Steps.Count}: {currentStep.Title}");
                    Console.WriteLine(new string('-', 50));
                    Console.WriteLine(currentStep.Instructions);
                    Console.WriteLine();

                    // 3. Wait for user input
                    bool isCorrect = false;
                    while (!isCorrect)
                    {
                        Console.Write("> ");
                        string userInput = Console.ReadLine() ?? string.Empty;

                        // 4. Submit input to API
                        var response = await SubmitStepAsync(tutorialId, currentStepIndex, userInput, hint);
                        
                        // 5. Display result
                        isCorrect = response.IsCorrect;
                        
                        Console.WriteLine();
                        Console.WriteLine(response.Message);
                        
                        if (!isCorrect)
                        {
                            // Display hint if available
                            if (!string.IsNullOrEmpty(response.Hint))
                            {
                                Console.WriteLine();
                                Console.WriteLine($"Hint: {response.Hint}");
                            }
                            
                            // Display Shello hint if available
                            if (!string.IsNullOrEmpty(response.HintFromShello))
                            {
                                Console.WriteLine();
                                Console.WriteLine($"🐚 Shello says: {response.HintFromShello}");
                            }
                            
                            // If hint wasn't requested but is available, offer to request one
                            if (!hint && string.IsNullOrEmpty(response.HintFromShello))
                            {
                                Console.WriteLine();
                                Console.Write("Would you like a hint from Shello? (y/n): ");
                                string answer = Console.ReadLine() ?? string.Empty;
                                
                                if (answer.Trim().ToLower() == "y")
                                {
                                    // Re-submit with hint request
                                    response = await SubmitStepAsync(tutorialId, currentStepIndex, userInput, true);
                                    
                                    if (!string.IsNullOrEmpty(response.HintFromShello))
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine($"🐚 Shello says: {response.HintFromShello}");
                                    }
                                }
                            }
                            
                            Console.WriteLine();
                            Console.WriteLine("Try again...");
                        }
                        else
                        {
                            // Move to next step or complete
                            isComplete = response.IsComplete;
                            if (!isComplete && response.NextStepIndex.HasValue)
                            {
                                currentStepIndex = response.NextStepIndex.Value;
                            }
                        }
                    }
                }

                // 6. Tutorial completed
                Console.WriteLine();
                Console.WriteLine("🎉 Congratulations! You've completed the tutorial! 🎉");
                Console.WriteLine($"You earned {tutorial.Xp} XP!");
                Console.WriteLine();
                
                // Ask if user wants to mark the tutorial as complete
                Console.Write("Would you like to mark this tutorial as complete in your profile? (y/n): ");
                string markComplete = Console.ReadLine() ?? string.Empty;
                
                if (markComplete.Trim().ToLower() == "y")
                {
                    await MarkTutorialCompleteAsync(tutorialId);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while running tutorial");
                Console.WriteLine($"Error: {ex.Message}");
                
                if (ex.StatusCode.HasValue)
                {
                    if (ex.StatusCode.Value == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("You need to be authenticated to run tutorials. Please log in first.");
                    }
                    else
                    {
                        Console.WriteLine($"HTTP Status: {(int)ex.StatusCode.Value} {ex.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running tutorial");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetches a tutorial by ID
        /// </summary>
        private async Task<Tutorial?> FetchTutorialAsync(string tutorialId)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/tutorials/{tutorialId}";
                _logger.LogInformation("Fetching tutorial: {Url}", url);
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var tutorial = await response.Content.ReadFromJsonAsync<Tutorial>(_jsonOptions);
                return tutorial;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching tutorial {TutorialId}", tutorialId);
                
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"Tutorial with ID '{tutorialId}' not found.");
                    return null;
                }
                
                throw;
            }
        }

        /// <summary>
        /// Submits a step to the API
        /// </summary>
        private async Task<RunTutorialStepResponse> SubmitStepAsync(string tutorialId, int stepIndex, string userInput, bool requestHint)
        {
            string url = $"{_apiBaseUrl}/api/tutorials/run-step";
            _logger.LogInformation("Submitting step: {Url}, TutorialId: {TutorialId}, StepIndex: {StepIndex}", 
                url, tutorialId, stepIndex);
            
            var request = new RunTutorialStepRequest
            {
                TutorialId = tutorialId,
                StepIndex = stepIndex,
                UserInput = userInput,
                RequestHint = requestHint
            };
            
            var content = JsonContent.Create(request);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<RunTutorialStepResponse>(_jsonOptions);
            return result ?? new RunTutorialStepResponse
            {
                IsCorrect = false,
                Message = "Error: Could not parse response from server."
            };
        }

        /// <summary>
        /// Marks a tutorial as complete
        /// </summary>
        private async Task MarkTutorialCompleteAsync(string tutorialId)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/UserProfile/complete-tutorial";
                _logger.LogInformation("Marking tutorial as complete: {Url}, TutorialId: {TutorialId}", url, tutorialId);
                
                var request = new { tutorialId };
                var content = JsonContent.Create(request);
                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Tutorial marked as complete in your profile!");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    Console.WriteLine("This tutorial was already marked as complete in your profile.");
                }
                else
                {
                    response.EnsureSuccessStatusCode(); // Will throw for other status codes
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while marking tutorial as complete {TutorialId}", tutorialId);
                
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("You need to be authenticated to mark tutorials as complete. Please log in first.");
                }
                else
                {
                    Console.WriteLine($"Error marking tutorial as complete: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Models for tutorial API
    /// </summary>
    #region Models
    
    public class Tutorial
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Xp { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<TutorialStep> Steps { get; set; } = new List<TutorialStep>();
    }
    
    public class TutorialStep
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string ExpectedCommand { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public ValidationRule? Validation { get; set; }
    }
    
    public class ValidationRule
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
    
    public class RunTutorialStepRequest
    {
        public string TutorialId { get; set; } = string.Empty;
        public int StepIndex { get; set; }
        public string UserInput { get; set; } = string.Empty;
        public bool RequestHint { get; set; } = false;
    }
    
    public class RunTutorialStepResponse
    {
        public bool IsCorrect { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string? HintFromShello { get; set; }
        public int? NextStepIndex { get; set; }
        public bool IsComplete { get; set; }
    }
    
    #endregion
}